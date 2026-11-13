using CompuCare.Application.Common;
using CompuCare.Domain.IdentityAccess;
using CompuCare.Domain.QuotationsPayments;
using CompuCare.Domain.ServiceRequests;

namespace CompuCare.Application.QuotationsPayments;

public sealed record QuotationLineResource(string Description, decimal Cost);
public sealed record IssueQuotationResource(Guid TicketId, List<QuotationLineResource> Items);
public sealed record QuotationStatusResource(string Status);
public sealed record TestCardResource(string Holder, string Last4);
public sealed record QuotationResource(Guid Id, Guid TicketId, Guid CompanyId, Guid TechnicianId,
    List<QuotationLineResource> Items, decimal Total, string Status, DateTime CreatedAt,
    DateTime? PaidAt, string? Receipt);

public sealed class QuotationService(
    IEntityStore<Quotation> quotations,
    IEntityStore<ServiceRequest> tickets,
    IEntityStore<UserAccount> users,
    INotificationWriter notify,
    ICurrentUser current)
{
    private static string Label(QuotationStatus status) => status switch {
        QuotationStatus.Pending => "PENDING",
        QuotationStatus.Approved => "APPROVED",
        QuotationStatus.Rejected => "REJECTED",
        QuotationStatus.PaidSimulated => "PAID",
        _ => "UNKNOWN"
    };
    private static QuotationResource Map(Quotation q) => new(q.Id, q.RequestId, q.CompanyId, q.TechnicianId,
        q.Items.Select(x => new QuotationLineResource(x.Description, x.Cost)).ToList(),
        q.Total, Label(q.Status), q.CreatedAtUtc, q.PaidAtUtc, q.Receipt);

    public async Task<List<QuotationResource>> QuotesAsync(CancellationToken ct)
    {
        Access.Require(current, "admin", "company_manager", "technician", "sysadmin");
        var items = await quotations.ListAsync(x => true, ct);
        items = current.Role switch {
            "admin" or "company_manager" => items.Where(x => x.CompanyId == current.CompanyId).ToList(),
            "technician" => items.Where(x => x.TechnicianId == current.UserId).ToList(),
            "sysadmin" => items,
            _ => []
        };
        return items.OrderByDescending(x => x.CreatedAtUtc).Select(Map).ToList();
    }
    public async Task<QuotationResource> CreateAsync(IssueQuotationResource resource, CancellationToken ct)
    {
        Access.Require(current, "technician");
        var ticket = await tickets.FirstAsync(x => x.Id == resource.TicketId && x.TechnicianId == current.UserId, ct)
            ?? throw new AppException(404, "Assigned ticket not found.");
        if (ticket.Status is not (ServiceRequestStatus.Assigned or ServiceRequestStatus.InDiagnosis) || ticket.Paused)
            throw new AppException(409, "Quotation not permitted for this ticket state.");
        if (resource.Items is null || resource.Items.Count == 0)
            throw new AppException(400, "At least one quoted item is required.");
        var value = Quotation.IssueWithItems(ticket.Id, ticket.CompanyId, current.UserId,
            resource.Items.Select(x => (x.Description, x.Cost)).ToList());
        await quotations.AddAsync(value, ct);
        var recipients = await users.ListAsync(x => x.CompanyId == ticket.CompanyId &&
            (x.Role == "admin" || x.Role == "company_manager") && x.Active, ct);
        foreach (var person in recipients)
            await notify.NotifyAsync(person.Id, $"A new quotation #{value.Id} requires your approval.", ct);
        await quotations.SaveAsync(ct);
        return Map(value);
    }
    public async Task<QuotationResource> UpdateStatusAsync(Guid id, QuotationStatusResource resource, CancellationToken ct)
    {
        Access.Require(current, "admin", "company_manager");
        var company = Access.Company(current);
        var value = await quotations.FirstAsync(x => x.Id == id && x.CompanyId == company, ct)
            ?? throw new AppException(404, "Quotation not found.");
        var ticket = await tickets.FirstAsync(x => x.Id == value.RequestId && x.CompanyId == company, ct)
            ?? throw new AppException(404, "Service request not found.");
        if (resource.Status == "APPROVED")
        {
            value.Approve(true);
            var rejected = await quotations.ListAsync(x => x.RequestId == value.RequestId &&
                x.Status == QuotationStatus.Rejected, ct);
            if (rejected.Count == 0) ticket.SetPaused(false);
            await notify.NotifyAsync(value.TechnicianId, $"Quotation #{value.Id} approved.", ct);
        }
        else if (resource.Status == "REJECTED") { value.Reject(true); ticket.SetPaused(true); }
        else throw new AppException(400, "Only APPROVED or REJECTED are allowed.");
        await quotations.SaveAsync(ct);
        return Map(value);
    }
    public async Task<QuotationResource> PayAsync(Guid id, TestCardResource resource, CancellationToken ct)
    {
        Access.Require(current, "admin", "company_manager");
        var value = await quotations.FirstAsync(x => x.Id == id && x.CompanyId == Access.Company(current), ct)
            ?? throw new AppException(404, "Quotation not found.");
        if (string.IsNullOrWhiteSpace(resource.Holder) || resource.Last4?.Length != 4 ||
            !resource.Last4.All(char.IsDigit))
            throw new AppException(400, "Only a fictitious cardholder and four test digits are allowed.");
        // Do not persist card digits, CVV or expiry. No payment gateway is called.
        value.RecordSimulatedPayment();
        await quotations.SaveAsync(ct);
        return Map(value);
    }
}
