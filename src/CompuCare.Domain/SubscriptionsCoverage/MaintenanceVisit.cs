using CompuCare.Domain.Common;

namespace CompuCare.Domain.SubscriptionsCoverage;

public sealed class MaintenanceVisit
{
    private MaintenanceVisit() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public string Date { get; private set; } = "";
    public string Time { get; private set; } = "";
    public Guid[] EquipmentIds { get; private set; } = [];
    public string Status { get; private set; } = "Agendado";
    public static MaintenanceVisit Schedule(Guid companyId, Guid subscriptionId, string date, string time, Guid[] equipmentIds)
    {
        if (companyId == Guid.Empty || subscriptionId == Guid.Empty ||
            !DateOnly.TryParseExact(date, "yyyy-MM-dd", out _) ||
            !TimeOnly.TryParse(time, out _) ||
            equipmentIds is null || equipmentIds.Length == 0 || equipmentIds.Any(x => x == Guid.Empty) ||
            equipmentIds.Distinct().Count() != equipmentIds.Length)
            throw new DomainRuleException("A valid date, time and non-duplicated equipment list are required.");
        return new MaintenanceVisit { Id = Guid.NewGuid(), CompanyId = companyId, SubscriptionId = subscriptionId,
            Date = date, Time = time, EquipmentIds = equipmentIds.ToArray() };
    }
}
