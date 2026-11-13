using CompuCare.Application.Common;
using CompuCare.Domain.CompaniesEquipment;
using CompuCare.Domain.IdentityAccess;
using CompuCare.Domain.QuotationsPayments;
using CompuCare.Domain.ServiceRequests;
using CompuCare.Domain.SubscriptionsCoverage;

namespace CompuCare.Application.ServiceRequests;

public sealed record ReportIssueResource(Guid EquipmentId, string Category, string Description);
public sealed record AssignTechnicianResource(Guid TechnicianId);
public sealed record DiagnoseResource(string Diagnosis, string WorkDone, decimal Hours);
public sealed record TimelineResource(string Status, DateTime At);
public sealed record ServiceRequestResource(
    Guid Id, Guid CompanyId, Guid EmployeeId, Guid EquipmentId, Guid? TechnicianId,
    string Category, string Description, string Status, bool Paused, DateTime CreatedAt,
    string? EquipmentName, string? EmployeeName, string? TechnicianName,
    string? Diagnosis, string? WorkDone, decimal Hours, List<TimelineResource> Timeline);

public sealed class ServiceRequestService(
    IEntityStore<ServiceRequest> tickets,
    IEntityStore<Equipment> equipments,
    IEntityStore<UserAccount> users,
    IEntityStore<Quotation> quotations,
    IEntityStore<Subscription> subscriptions,
    INotificationWriter notify,
    ICurrentUser current)
{
    private static string Label(ServiceRequestStatus status) => status switch
    {
        ServiceRequestStatus.PendingAssignment => "Pendiente de Asignación",
        ServiceRequestStatus.Assigned => "Asignado",
        ServiceRequestStatus.InDiagnosis => "En Diagnóstico",
        ServiceRequestStatus.Resolved => "Atendido",
        ServiceRequestStatus.Cancelled => "Cancelado",
        _ => "Sin estado"
    };
    private async Task<ServiceRequestResource> MapAsync(ServiceRequest item, CancellationToken ct)
    {
        var eq = await equipments.FirstAsync(x => x.Id == item.EquipmentId, ct);
        var employee = await users.FirstAsync(x => x.Id == item.RequesterId, ct);
        var tech = item.TechnicianId.HasValue
            ? await users.FirstAsync(x => x.Id == item.TechnicianId, ct)
            : null;
        return new ServiceRequestResource(item.Id, item.CompanyId, item.RequesterId, item.EquipmentId,
            item.TechnicianId, item.Category, item.Description, Label(item.Status), item.Paused,
            item.CreatedAtUtc, eq?.Name, employee?.Name, tech?.Name,
            item.Diagnosis, item.WorkDone, item.WorkHours,
            item.Timeline.OrderBy(x => x.At).Select(x => new TimelineResource(x.Status, x.At)).ToList());
    }

    private async Task<ServiceRequest> AuthorizedTicketAsync(Guid id, CancellationToken ct)
    {
        var ticket = await tickets.FirstAsync(x => x.Id == id, ct)
            ?? throw new AppException(404, "Request not found.");
        bool allowed = current.Role switch
        {
            "sysadmin" => true,
            "admin" or "company_manager" => ticket.CompanyId == current.CompanyId,
            "employee" => ticket.RequesterId == current.UserId,
            "technician" => ticket.TechnicianId == current.UserId,
            _ => false
        };
        if (!allowed) throw new AppException(404, "Request not found.");
        return ticket;
    }

    public async Task<List<ServiceRequestResource>> TicketsAsync(CancellationToken ct)
    {
        Access.Require(current, "employee", "admin", "company_manager", "sysadmin", "technician");
        var items = await tickets.ListAsync(x => true, ct);
        items = current.Role switch
        {
            "sysadmin" => items,
            "admin" or "company_manager" => items.Where(x => x.CompanyId == current.CompanyId).ToList(),
            "employee" => items.Where(x => x.RequesterId == current.UserId).ToList(),
            "technician" => items.Where(x => x.TechnicianId == current.UserId).ToList(),
            _ => []
        };
        var result = new List<ServiceRequestResource>();
        foreach (var item in items.OrderByDescending(x => x.CreatedAtUtc))
            result.Add(await MapAsync(item, ct));
        return result;
    }

    public async Task<ServiceRequestResource> ReportAsync(ReportIssueResource request, CancellationToken ct)
    {
        Access.Require(current, "employee");
        var companyId = Access.Company(current);
        var equipment = await equipments.FirstAsync(x => x.Id == request.EquipmentId && x.CompanyId == companyId &&
            x.AssignedEmployeeId == current.UserId, ct)
            ?? throw new AppException(403, "This device is not assigned to you.");
        var ticket = ServiceRequest.Report(companyId, equipment.Id, current.UserId, request.Description, request.Category);
        await tickets.AddAsync(ticket, ct);
        await tickets.SaveAsync(ct);
        return await MapAsync(ticket, ct);
    }

    public async Task<ServiceRequestResource> CancelAsync(Guid id, CancellationToken ct)
    {
        Access.Require(current, "employee");
        var ticket = await AuthorizedTicketAsync(id, ct);
        ticket.Cancel(current.UserId);
        await tickets.SaveAsync(ct);
        return await MapAsync(ticket, ct);
    }

    public async Task<ServiceRequestResource> AssignAsync(Guid id, AssignTechnicianResource request, CancellationToken ct)
    {
        Access.Require(current, "sysadmin");
        var ticket = await AuthorizedTicketAsync(id, ct);
        var technician = await users.FirstAsync(x => x.Id == request.TechnicianId && x.Active && x.Role == "technician", ct)
            ?? throw new AppException(400, "Technician not found.");
        ticket.AssignTo(technician.Id);
        await notify.NotifyAsync(technician.Id, $"Ticket #{ticket.Id} has been assigned to your account.", ct);
        await tickets.SaveAsync(ct);
        return await MapAsync(ticket, ct);
    }

    public async Task<ServiceRequestResource> StartAsync(Guid id, CancellationToken ct)
    {
        Access.Require(current, "technician");
        var ticket = await AuthorizedTicketAsync(id, ct);
        ticket.StartDiagnosis(current.UserId);
        await tickets.SaveAsync(ct);
        return await MapAsync(ticket, ct);
    }

    public async Task<ServiceRequestResource> DiagnoseAsync(Guid id, DiagnoseResource request, CancellationToken ct)
    {
        Access.Require(current, "technician");
        var ticket = await AuthorizedTicketAsync(id, ct);
        var quotes = await quotations.ListAsync(x => x.RequestId == ticket.Id, ct);
        var unresolved = quotes.Any(q => q.Status is QuotationStatus.Pending or QuotationStatus.Rejected);
        var subscription = await subscriptions.FirstAsync(x => x.CompanyId == ticket.CompanyId, ct);
        var percentBefore = subscription is null || subscription.MonthlyHours <= 0m ? 0m :
            100m * subscription.UsedHours / subscription.MonthlyHours;
        ticket.Resolve(current.UserId, request.Hours, unresolved, request.Diagnosis, request.WorkDone);
        if (subscription is not null)
        {
            subscription.Consume(ticket.Id, request.Hours);
            var percentAfter = 100m * subscription.UsedHours / subscription.MonthlyHours;
            if (percentBefore < 80m && percentAfter >= 80m)
            {
                var recipients = await users.ListAsync(x => x.CompanyId == ticket.CompanyId &&
                    (x.Role == "admin" || x.Role == "company_manager"), ct);
                foreach (var recipient in recipients)
                    await notify.NotifyAsync(recipient.Id, "You have used 80% of your monthly support hours.", ct);
            }
        }
        // One EF Core DbContext tracks request, subscription, coverage journal, and notifications.
        await tickets.SaveAsync(ct);
        return await MapAsync(ticket, ct);
    }
}
