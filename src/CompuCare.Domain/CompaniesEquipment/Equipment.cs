using CompuCare.Domain.Common;

namespace CompuCare.Domain.CompaniesEquipment;

/// <summary>BC02 — Supporting. Assignment eligibility is verified by an application port.</summary>
public sealed class Equipment
{
    private Equipment() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid? AssignedEmployeeId { get; private set; }
    public string SerialNumber { get; private set; } = string.Empty;

    public static Equipment Register(Guid companyId, string serialNumber)
    {
        if (companyId == Guid.Empty || string.IsNullOrWhiteSpace(serialNumber))
            throw new DomainRuleException("A company and serial number are required.");
        return new Equipment { Id = Guid.NewGuid(), CompanyId = companyId, SerialNumber = serialNumber.Trim() };
    }
    public void AssignTo(Guid employeeId, bool belongsToSameCompany)
    {
        if (employeeId == Guid.Empty || !belongsToSameCompany)
            throw new DomainRuleException("Equipment must be assigned to an employee of its company.");
        AssignedEmployeeId = employeeId;
    }
}
