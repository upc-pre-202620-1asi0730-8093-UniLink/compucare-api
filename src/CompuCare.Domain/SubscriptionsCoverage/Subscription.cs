using CompuCare.Domain.Common;

namespace CompuCare.Domain.SubscriptionsCoverage;

/// <summary>BC03 — Core. Initial aggregate sketch; period idempotency is not implemented yet.</summary>
public sealed class Subscription
{
    private Subscription() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid PlanId { get; private set; }
    public decimal MonthlyHours { get; private set; }
    public decimal UsedHours { get; private set; }
    public DateTime StartedAtUtc { get; private set; }

    public static Subscription Activate(Guid companyId, Guid planId, decimal includedHours)
    {
        if (companyId == Guid.Empty || planId == Guid.Empty || includedHours <= 0)
            throw new DomainRuleException("A company, plan and positive monthly coverage are required.");
        return new Subscription { Id = Guid.NewGuid(), CompanyId = companyId, PlanId = planId,
            MonthlyHours = includedHours, StartedAtUtc = DateTime.UtcNow };
    }
    public decimal AvailableHours => Math.Max(0, MonthlyHours - UsedHours);
    // Consumption is intentionally NOT exposed until an idempotent operation journal is implemented.
}
