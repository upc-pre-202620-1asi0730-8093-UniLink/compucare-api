using CompuCare.Domain.Common;

namespace CompuCare.Domain.IdentityAccess;

/// <summary>BC01 — Generic Domain. Aggregate root: no HTTP or crypto dependency.</summary>
public sealed class UserAccount
{
    private UserAccount() { }
    public Guid Id { get; private set; }
    public Guid? CompanyId { get; private set; }
    public string Name { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string PasswordHash { get; private set; } = "";
    public string Role { get; private set; } = "";
    public string Phone { get; private set; } = "";
    public string Position { get; private set; } = "";
    public bool Active { get; private set; } = true;

    public static UserAccount Register(string name, string email, string passwordHash, string role, Guid? companyId = null,
        string position = "")
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainRuleException("A valid name and email address are required.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainRuleException("A password hash is required.");
        if (!new[] { "admin", "company_manager", "employee", "technician", "sysadmin" }.Contains(role))
            throw new DomainRuleException("Unrecognized account role.");
        if (new[] { "admin", "company_manager", "employee" }.Contains(role) && !companyId.HasValue)
            throw new DomainRuleException("Company users must belong to a company.");
        return new UserAccount
        {
            Id = Guid.NewGuid(), Name = name.Trim(), Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash, Role = role, CompanyId = companyId, Position = position.Trim()
        };
    }

    public void UpdateProfile(string name, string? phone)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainRuleException("Name cannot be empty.");
        Name = name.Trim();
        Phone = phone?.Trim() ?? "";
    }

    public void ChangePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainRuleException("Password hash cannot be empty.");
        PasswordHash = passwordHash;
    }
}
