using CompuCare.Domain.Common;

namespace CompuCare.Domain.SubscriptionsCoverage;

/// <summary>BC03 Core aggregate. Preventive slots and support hour consumption belong here.</summary>
public sealed class Subscription
{
    private Subscription() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid PlanId { get; private set; }
    public string PlanName { get; private set; } = "";
    public decimal MonthlyHours { get; private set; }
    public decimal UsedHours { get; private set; }
    public int PreventiveTotal { get; private set; }
    public int PreventiveUsed { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime RenewsAtUtc { get; private set; }
    public string Status { get; private set; } = "active";
    public Guid Version { get; private set; } = Guid.NewGuid();
    private readonly List<CoverageConsumption> _consumptions = [];
    public IReadOnlyCollection<CoverageConsumption> Consumptions => _consumptions.AsReadOnly();

    public static Subscription Activate(Guid companyId, Guid planId, decimal includedHours)
    {
        if (companyId == Guid.Empty || planId == Guid.Empty || includedHours <= 0)
            throw new DomainRuleException("A company, plan and positive monthly coverage are required.");
        var now = DateTime.UtcNow;
        return new Subscription { Id = Guid.NewGuid(), CompanyId = companyId, PlanId = planId,
            MonthlyHours = includedHours, StartedAtUtc = now, RenewsAtUtc = now.AddMonths(1) };
    }
    public static Subscription Activate(Guid companyId, Plan plan)
    {
        var sub = Activate(companyId, plan.Id, plan.Hours);
        sub.PlanName = plan.Name; sub.PreventiveTotal = plan.PreventiveVisits;
        return sub;
    }
    public decimal AvailableHours => Math.Max(0, MonthlyHours - UsedHours);
    public int PreventiveRemaining => Math.Max(0, PreventiveTotal - PreventiveUsed);

    public void Renew(Plan plan)
    {
        if (plan.Id == Guid.Empty) throw new DomainRuleException("Plan is required.");
        PlanId = plan.Id; PlanName = plan.Name; MonthlyHours = plan.Hours;
        PreventiveTotal = plan.PreventiveVisits; PreventiveUsed = 0; UsedHours = 0;
        StartedAtUtc = DateTime.UtcNow; RenewsAtUtc = StartedAtUtc.AddMonths(1);
        Version = Guid.NewGuid();
    }
    public bool Consume(Guid ticketId, decimal hours)
    {
        if (ticketId == Guid.Empty || hours <= 0)
            throw new DomainRuleException("A ticket and positive support hours are required.");
        if (_consumptions.Any(x => x.TicketId == ticketId && x.PeriodStartedAtUtc == StartedAtUtc))
            return false; // idempotent request retry for current billing period
        _consumptions.Add(CoverageConsumption.Record(ticketId, StartedAtUtc, hours));
        UsedHours += hours;
        Version = Guid.NewGuid();
        return true;
    }
    public void BookPreventive()
    {
        if (PreventiveRemaining < 1) throw new DomainRuleException("No preventive visits left in this period.");
        PreventiveUsed++;
        Version = Guid.NewGuid();
    }
}
public sealed class CoverageConsumption
{
    private CoverageConsumption() { }
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public DateTime PeriodStartedAtUtc { get; private set; }
    public decimal Hours { get; private set; }
    public static CoverageConsumption Record(Guid ticketId, DateTime period, decimal hours) =>
        new() { Id = Guid.NewGuid(), TicketId = ticketId, PeriodStartedAtUtc = period, Hours = hours };
}
