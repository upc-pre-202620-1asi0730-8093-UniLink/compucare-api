using CompuCare.Domain.Common;

namespace CompuCare.Domain.CompaniesEquipment;

public sealed class Location
{
    private Location() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = "";
    public string Desk { get; private set; } = "";
    public bool Active { get; private set; } = true;
    public static Location Register(Guid companyId, string name, string desk)
    {
        if (companyId == Guid.Empty || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(desk))
            throw new DomainRuleException("Company, site and desk are required.");
        return new Location { Id = Guid.NewGuid(), CompanyId = companyId, Name = name.Trim(), Desk = desk.Trim(), Active = true };
    }
    public void SetActive(bool active) => Active = active;
    public void Rename(string name, string desk)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(desk))
            throw new DomainRuleException("Site and desk must not be empty.");
        Name = name.Trim(); Desk = desk.Trim();
    }
}
