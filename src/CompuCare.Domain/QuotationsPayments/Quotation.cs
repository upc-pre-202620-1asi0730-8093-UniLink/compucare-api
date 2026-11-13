using CompuCare.Domain.Common;
namespace CompuCare.Domain.QuotationsPayments;
public enum QuotationStatus { Pending = 0, Approved = 1, Rejected = 2, PaidSimulated = 3 }

/// <summary>BC05 Supporting aggregate; line-item prices belong to this aggregate.</summary>
public sealed class Quotation
{
    private Quotation() { }
    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid TechnicianId { get; private set; }
    public decimal Total { get; private set; }
    public QuotationStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public string? Receipt { get; private set; }
    private readonly List<QuotationLine> _items = [];
    public IReadOnlyCollection<QuotationLine> Items => _items.AsReadOnly();

    public static Quotation Issue(Guid requestId, Guid companyId, decimal total)
    {
        if (requestId == Guid.Empty || companyId == Guid.Empty || total <= 0)
            throw new DomainRuleException("Request, company and positive total are required.");
        var q = new Quotation { Id = Guid.NewGuid(), RequestId = requestId,
            CompanyId = companyId, Total = total, Status = QuotationStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow };
        q._items.Add(QuotationLine.Create("Service additional charge", total));
        return q;
    }

    public static Quotation IssueWithItems(Guid requestId, Guid companyId, Guid technicianId,
        IReadOnlyList<(string Description, decimal Cost)> items)
    {
        if (requestId == Guid.Empty || companyId == Guid.Empty || technicianId == Guid.Empty ||
            items is null || items.Count == 0 || items.Count > 50)
            throw new DomainRuleException("Quotation must have a request, company, technician and 1-50 items.");
        var value = new Quotation { Id = Guid.NewGuid(), RequestId = requestId,
            CompanyId = companyId, TechnicianId = technicianId, Status = QuotationStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow };
        foreach (var item in items) value._items.Add(QuotationLine.Create(item.Description, item.Cost));
        value.Total = value._items.Sum(x => x.Cost);
        return value;
    }
    public void Approve(bool isAuthorizedCompanyManager)
    {
        if (Status != QuotationStatus.Pending || !isAuthorizedCompanyManager)
            throw new DomainRuleException("Only the authorized company manager may approve a pending quote.");
        Status = QuotationStatus.Approved;
    }
    public void Reject(bool isAuthorizedCompanyManager)
    {
        if (Status != QuotationStatus.Pending || !isAuthorizedCompanyManager)
            throw new DomainRuleException("Only the authorized company manager may reject a pending quote.");
        Status = QuotationStatus.Rejected;
    }
    public void RecordSimulatedPayment()
    {
        if (Status != QuotationStatus.Approved)
            throw new DomainRuleException("Only approved quotations accept simulated payment.");
        PaidAtUtc = DateTime.UtcNow;
        Receipt = $"REC-{Id.ToString("N")[..10].ToUpperInvariant()}";
        Status = QuotationStatus.PaidSimulated;
    }
}
public sealed class QuotationLine
{
    private QuotationLine() { }
    public Guid Id { get; private set; }
    public string Description { get; private set; } = "";
    public decimal Cost { get; private set; }
    public static QuotationLine Create(string description, decimal cost)
    {
        if (string.IsNullOrWhiteSpace(description) || description.Length > 500 ||
            cost <= 0 || decimal.Round(cost, 2) != cost)
            throw new DomainRuleException("Line items require description and positive costs with up to two decimals.");
        return new QuotationLine { Id = Guid.NewGuid(), Description = description.Trim(), Cost = cost };
    }
}
