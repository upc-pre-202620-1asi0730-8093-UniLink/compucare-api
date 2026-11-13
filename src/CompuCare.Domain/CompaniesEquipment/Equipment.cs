using CompuCare.Domain.Common;

namespace CompuCare.Domain.CompaniesEquipment;

/// <summary>BC02 Supporting — Equipment aggregate root; cross-BC user references are IDs.</summary>
public sealed class Equipment
{
    private Equipment() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string SerialNumber { get; private set; } = "";
    public string Type { get; private set; } = "";
    public Guid? LocationId { get; private set; }
    public Guid? AssignedEmployeeId { get; private set; }

    public static Equipment Register(Guid companyId, string serialNumber) =>
        Register(companyId, serialNumber, $"Device {serialNumber}", "Other");

    public static Equipment Register(Guid companyId, string serialNumber, string name, string type, Guid? locationId = null)
    {
        if (companyId == Guid.Empty || string.IsNullOrWhiteSpace(serialNumber) ||
            string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
            throw new DomainRuleException("Company, serial number, name and equipment type are required.");
        if (name.Length > 160 || serialNumber.Length > 100)
            throw new DomainRuleException("Equipment fields exceed the allowed length.");
        var id = Guid.NewGuid();
        return new Equipment
        {
            Id = id, CompanyId = companyId, Name = name.Trim(), Type = type.Trim(),
            SerialNumber = serialNumber.Trim(), LocationId = locationId,
            Code = $"EQ-{id.ToString("N")[..8].ToUpperInvariant()}"
        };
    }

    public void AssignTo(Guid employeeId, bool belongsToSameCompany)
    {
        if (employeeId == Guid.Empty || !belongsToSameCompany)
            throw new DomainRuleException("Equipment must be assigned to an employee of its company.");
        AssignedEmployeeId = employeeId;
    }
    public void Unassign() => AssignedEmployeeId = null;
    public void ChangeLocation(Guid? locationId, bool activeAndSameCompany)
    {
        if (locationId.HasValue && !activeAndSameCompany)
            throw new DomainRuleException("Equipment location must be active and belong to the same company.");
        LocationId = locationId;
    }
}
