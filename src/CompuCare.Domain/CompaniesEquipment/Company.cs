using CompuCare.Domain.Common;

namespace CompuCare.Domain.CompaniesEquipment;

/// <summary>BC02 — Supporting. Initial aggregate model.</summary>
public sealed class Company
{
    private Company() { }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string TaxId { get; private set; } = string.Empty;

    public static Company Register(string name, string taxId)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(taxId))
            throw new DomainRuleException("Company name and tax identifier are required.");
        return new Company { Id = Guid.NewGuid(), Name = name.Trim(), TaxId = taxId.Trim() };
    }
}
