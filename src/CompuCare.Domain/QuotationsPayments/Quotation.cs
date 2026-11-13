using CompuCare.Domain.Common;

namespace CompuCare.Domain.QuotationsPayments;

public enum QuotationStatus { Pending = 0, Approved = 1, Rejected = 2, PaidSimulated = 3 }

/// <summary>BC05 — Supporting. Simplified state model; line-item pricing is pending.</summary>
public sealed class Quotation
{
    private Quotation() { }
    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid CompanyId { get; private set; }
    public decimal Total { get; private set; }
    public QuotationStatus Status { get; private set; }

    public static Quotation Issue(Guid requestId, Guid companyId, decimal total)
    {
        if (requestId == Guid.Empty || companyId == Guid.Empty || total <= 0)
            throw new DomainRuleException("Request, company and positive total are required.");
        return new Quotation { Id = Guid.NewGuid(), RequestId = requestId, CompanyId = companyId,
            Total = total, Status = QuotationStatus.Pending };
    }
    public void Approve(bool isAuthorizedCompanyManager)
    {
        if (Status != QuotationStatus.Pending || !isAuthorizedCompanyManager)
            throw new DomainRuleException("Quotation approval requires a pending quote and authorized manager.");
        Status = QuotationStatus.Approved;
    }
    public void Reject(bool isAuthorizedCompanyManager)
    {
        if (Status != QuotationStatus.Pending || !isAuthorizedCompanyManager)
            throw new DomainRuleException("Quotation rejection requires a pending quote and authorized manager.");
        Status = QuotationStatus.Rejected;
    }
    public void RecordSimulatedPayment()
    {
        if (Status != QuotationStatus.Approved)
            throw new DomainRuleException("Only approved quotations accept simulated payment.");
        Status = QuotationStatus.PaidSimulated;
    }
}
