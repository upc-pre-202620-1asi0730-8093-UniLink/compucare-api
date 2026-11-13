using CompuCare.Application.Common;
using CompuCare.Domain.CompaniesEquipment;
using CompuCare.Domain.IdentityAccess;
using CompuCare.Domain.ServiceRequests;
using CompuCare.Domain.QuotationsPayments;
using CompuCare.Domain.SubscriptionsCoverage;

namespace CompuCare.Application.CompaniesEquipment;

public sealed record LocationResource(Guid Id, Guid CompanyId, string Name, string Desk, bool Active);
public sealed record CreateLocationResource(string Name, string Desk);
public sealed record PatchLocationResource(bool? Active, string? Name, string? Desk);
public sealed record EquipmentResource(Guid Id, Guid CompanyId, string Code, string Name,
    string SerialNumber, string Type, Guid? LocationId, Guid? AssignedTo, string Status);
public sealed record CreateEquipmentResource(string Name, string SerialNumber, string Type, Guid? LocationId);
public sealed record AssignEquipmentResource(Guid? EmployeeId);
public sealed record AssignmentResource(Guid Id, Guid EquipmentId, Guid? EmployeeId, DateTime At);
public sealed record EquipmentHistoryResource(
    EquipmentResource Equipment, List<AssignmentResource> Assignments,
    List<object> Tickets, List<object> Maintenances);

public sealed class CompanyEquipmentService(
    IEntityStore<Location> locations,
    IEntityStore<Equipment> equipment,
    IEntityStore<EquipmentAssignment> assignments,
    IEntityStore<UserAccount> users,
    IEntityStore<ServiceRequest> tickets,
    IEntityStore<Quotation> quotations,
    IEntityStore<MaintenanceVisit> visits,
    ICurrentUser current)
{
    private Guid Company => Access.Company(current);
    private void RequireCompany() => Access.Require(current, "admin", "company_manager", "employee");
    private void RequireManager() => Access.Require(current, "admin", "company_manager");
    private static LocationResource Map(Location x) =>
        new(x.Id, x.CompanyId, x.Name, x.Desk, x.Active);

    private async Task<EquipmentResource> MapAsync(Equipment x, CancellationToken ct)
    {
        var active = await tickets.FirstAsync(t => t.EquipmentId == x.Id &&
            (t.Status == ServiceRequestStatus.PendingAssignment ||
             t.Status == ServiceRequestStatus.Assigned ||
             t.Status == ServiceRequestStatus.InDiagnosis), ct);
        return new(x.Id, x.CompanyId, x.Code, x.Name, x.SerialNumber, x.Type, x.LocationId,
            x.AssignedEmployeeId, active is null ? "Operativo" : "En Reparación");
    }

    public async Task<List<LocationResource>> LocationsAsync(CancellationToken ct)
    {
        RequireCompany();
        var data = await locations.ListAsync(x => x.CompanyId == Company, ct);
        return data.Select(Map).ToList();
    }
    public async Task<LocationResource> CreateLocationAsync(CreateLocationResource resource, CancellationToken ct)
    {
        RequireManager();
        var location = Location.Register(Company, resource.Name, resource.Desk);
        await locations.AddAsync(location, ct);
        await locations.SaveAsync(ct);
        return Map(location);
    }
    public async Task<LocationResource> PatchLocationAsync(Guid id, PatchLocationResource resource, CancellationToken ct)
    {
        RequireManager();
        var value = await locations.FirstAsync(x => x.Id == id && x.CompanyId == Company, ct)
            ?? throw new AppException(404, "Location not found.");
        if (resource.Name is not null || resource.Desk is not null)
            value.Rename(resource.Name ?? value.Name, resource.Desk ?? value.Desk);
        if (resource.Active.HasValue) value.SetActive(resource.Active.Value);
        await locations.SaveAsync(ct);
        return Map(value);
    }
    public async Task<List<EquipmentResource>> EquipmentsAsync(
        string? status, string? type, Guid? locationId, string? q, CancellationToken ct)
    {
        RequireCompany();
        var data = await equipment.ListAsync(x => x.CompanyId == Company, ct);
        if (current.Role == "employee")
            data = data.Where(x => x.AssignedEmployeeId == current.UserId).ToList();
        if (locationId.HasValue) data = data.Where(x => x.LocationId == locationId).ToList();
        if (!string.IsNullOrWhiteSpace(type)) data = data.Where(x => x.Type == type).ToList();
        if (!string.IsNullOrWhiteSpace(q)) data = data.Where(x =>
            $"{x.Code} {x.Name} {x.SerialNumber}".Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        var mapped = new List<EquipmentResource>();
        foreach (var item in data) mapped.Add(await MapAsync(item, ct));
        if (!string.IsNullOrWhiteSpace(status)) mapped = mapped.Where(x => x.Status == status).ToList();
        return mapped;
    }
    public async Task<EquipmentResource> CreateEquipmentAsync(CreateEquipmentResource resource, CancellationToken ct)
    {
        RequireManager();
        var companyId = Company;
        var serial = resource.SerialNumber?.Trim() ?? "";
        if (await equipment.FirstAsync(x => x.CompanyId == companyId && x.SerialNumber == serial, ct) is not null)
            throw new AppException(409, "A device with this serial number already exists.");
        if (resource.LocationId.HasValue)
        {
            var site = await locations.FirstAsync(x => x.Id == resource.LocationId && x.CompanyId == companyId && x.Active, ct);
            if (site is null) throw new AppException(400, "Selected location does not belong to the company or is inactive.");
        }
        var value = Equipment.Register(companyId, serial, resource.Name, resource.Type, resource.LocationId);
        await equipment.AddAsync(value, ct);
        await equipment.SaveAsync(ct);
        return await MapAsync(value, ct);
    }
    public async Task<EquipmentResource> AssignAsync(Guid id, AssignEquipmentResource resource, CancellationToken ct)
    {
        RequireManager();
        var item = await equipment.FirstAsync(x => x.Id == id && x.CompanyId == Company, ct)
            ?? throw new AppException(404, "Equipment not found.");
        if (resource.EmployeeId.HasValue)
        {
            var person = await users.FirstAsync(x => x.Id == resource.EmployeeId && x.Role == "employee" &&
                x.CompanyId == Company && x.Active, ct);
            item.AssignTo(resource.EmployeeId.Value, person is not null);
        }
        else item.Unassign();
        await assignments.AddAsync(EquipmentAssignment.Record(id, resource.EmployeeId), ct);
        await equipment.SaveAsync(ct);
        return await MapAsync(item, ct);
    }

    public async Task<EquipmentHistoryResource> HistoryAsync(Guid id, CancellationToken ct)
    {
        RequireManager();
        var item = await equipment.FirstAsync(x => x.Id == id && x.CompanyId == Company, ct)
            ?? throw new AppException(404, "Equipment not found.");
        var events = await assignments.ListAsync(x => x.EquipmentId == id, ct);
        var linkedTickets = await tickets.ListAsync(x => x.EquipmentId == id && x.CompanyId == Company, ct);
        var related = new List<object>();
        foreach (var ticket in linkedTickets.OrderByDescending(x => x.CreatedAtUtc))
        {
            var ticketQuotes = await quotations.ListAsync(q => q.RequestId == ticket.Id, ct);
            related.Add(new
            {
                ticket.Id, ticket.Category, ticket.Description,
                Status = ticket.Status switch {
                    ServiceRequestStatus.PendingAssignment => "Pendiente de Asignación",
                    ServiceRequestStatus.Assigned => "Asignado",
                    ServiceRequestStatus.InDiagnosis => "En Diagnóstico",
                    ServiceRequestStatus.Resolved => "Atendido",
                    ServiceRequestStatus.Cancelled => "Cancelado",
                    _ => "Sin estado"
                },
                Quotes = ticketQuotes.Select(q => new {
                    q.Id, TicketId = q.RequestId, q.Total, Status = q.Status.ToString()
                }).ToList()
            });
        }
        var maintenances = await visits.ListAsync(x => x.CompanyId == Company, ct);
        var linkedVisits = maintenances.Where(x => x.EquipmentIds.Contains(id))
            .OrderByDescending(x => x.Date)
            .Select(x => (object)new { x.Id, x.Date, x.Time, x.Status }).ToList();
        return new(await MapAsync(item, ct), events.OrderByDescending(x => x.At)
            .Select(x => new AssignmentResource(x.Id, x.EquipmentId, x.EmployeeId, x.At)).ToList(),
            related, linkedVisits);
    }
}
