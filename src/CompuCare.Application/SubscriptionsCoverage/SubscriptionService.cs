using CompuCare.Application.Common;
using CompuCare.Domain.CompaniesEquipment;
using CompuCare.Domain.SubscriptionsCoverage;

namespace CompuCare.Application.SubscriptionsCoverage;

public sealed record PlanResource(Guid Id, string Code, string Name, decimal Price, decimal Hours, int PreventiveVisits);
public sealed record SubscribeResource(Guid PlanId, BillingResource Billing);
public sealed record BillingResource(string Name, string Email);
public sealed record BalanceResource(Guid Id, Guid CompanyId, Guid PlanId, string PlanName,
    decimal HoursTotal, decimal HoursUsed, decimal HoursRemaining, int PreventiveTotal,
    int PreventiveUsed, int PreventiveRemaining, decimal PercentUsed, DateTime StartedAt,
    DateTime RenewsAt, string Status);
public sealed record ScheduleMaintenanceResource(string Date, string Time, Guid[] EquipmentIds);
public sealed record MaintenanceResource(Guid Id, Guid CompanyId, string Date, string Time, Guid[] EquipmentIds, string Status);

public sealed class SubscriptionService(
    IEntityStore<Plan> plans,
    IEntityStore<Subscription> subscriptions,
    IEntityStore<MaintenanceVisit> visits,
    IEntityStore<Equipment> equipments,
    ICurrentUser current)
{
    private void RequireManager() => Access.Require(current, "admin", "company_manager");
    private Guid Company => Access.Company(current);
    public static BalanceResource Balance(Subscription s) => new(
        s.Id, s.CompanyId, s.PlanId, s.PlanName, s.MonthlyHours, s.UsedHours, s.AvailableHours,
        s.PreventiveTotal, s.PreventiveUsed, s.PreventiveRemaining,
        s.MonthlyHours == 0 ? 0 : Math.Round(100m * s.UsedHours / s.MonthlyHours),
        s.StartedAtUtc, s.RenewsAtUtc, s.Status);

    public async Task<List<PlanResource>> PlansAsync(CancellationToken ct)
    {
        var items = await plans.ListAsync(p => true, ct);
        return items.OrderBy(x => x.Price).Select(p => new PlanResource(
            p.Id, p.Code, p.Name, p.Price, p.Hours, p.PreventiveVisits)).ToList();
    }

    public async Task<BalanceResource> BalanceAsync(CancellationToken ct)
    {
        RequireManager();
        var sub = await subscriptions.FirstAsync(x => x.CompanyId == Company, ct)
            ?? throw new AppException(404, "No active subscription.");
        return Balance(sub);
    }

    public async Task<BalanceResource> SubscribeAsync(SubscribeResource request, CancellationToken ct)
    {
        RequireManager();
        if (request.Billing is null || string.IsNullOrWhiteSpace(request.Billing.Name) ||
            string.IsNullOrWhiteSpace(request.Billing.Email) || !request.Billing.Email.Contains('@'))
            throw new AppException(400, "Valid billing details are required.");
        var plan = await plans.FirstAsync(x => x.Id == request.PlanId, ct)
            ?? throw new AppException(404, "Plan not found.");
        var sub = await subscriptions.FirstAsync(x => x.CompanyId == Company, ct);
        if (sub is null) { sub = Subscription.Activate(Company, plan); await subscriptions.AddAsync(sub, ct); }
        else sub.Renew(plan);
        await subscriptions.SaveAsync(ct);
        return Balance(sub);
    }

    public async Task<BalanceResource> RenewAsync(CancellationToken ct)
    {
        RequireManager();
        var sub = await subscriptions.FirstAsync(x => x.CompanyId == Company, ct)
            ?? throw new AppException(404, "No active subscription.");
        var plan = await plans.FirstAsync(x => x.Id == sub.PlanId, ct)
            ?? throw new AppException(404, "Plan not found.");
        sub.Renew(plan);
        await subscriptions.SaveAsync(ct);
        return Balance(sub);
    }
    public async Task<List<MaintenanceResource>> MaintenancesAsync(CancellationToken ct)
    {
        RequireManager();
        var items = await visits.ListAsync(x => x.CompanyId == Company, ct);
        return items.OrderByDescending(x => x.Date).Select(Map).ToList();
    }
    private static MaintenanceResource Map(MaintenanceVisit x) => new(x.Id, x.CompanyId, x.Date, x.Time, x.EquipmentIds, x.Status);

    public async Task<MaintenanceResource> ScheduleAsync(ScheduleMaintenanceResource request, CancellationToken ct)
    {
        RequireManager();
        var companyId = Company;
        if (request.EquipmentIds is null || request.EquipmentIds.Length == 0)
            throw new AppException(400, "At least one device is required.");
        var todayInPeru = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
        if (!DateOnly.TryParseExact(request.Date, "yyyy-MM-dd", out var date) || date < todayInPeru)
            throw new AppException(400, "Maintenance date cannot be in the past.");
        var sub = await subscriptions.FirstAsync(x => x.CompanyId == companyId, ct)
            ?? throw new AppException(400, "Subscription required.");
        var valid = await equipments.ListAsync(e => e.CompanyId == companyId, ct);
        if (request.EquipmentIds.Distinct().Count() != request.EquipmentIds.Length ||
            request.EquipmentIds.Any(id => valid.All(x => x.Id != id)))
            throw new AppException(400, "All selected devices must belong to your company.");
        var visit = MaintenanceVisit.Schedule(companyId, sub.Id, request.Date, request.Time, request.EquipmentIds);
        sub.BookPreventive();
        await visits.AddAsync(visit, ct);
        await subscriptions.SaveAsync(ct);
        return Map(visit);
    }
}
