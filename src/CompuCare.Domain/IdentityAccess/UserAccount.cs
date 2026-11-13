namespace CompuCare.Domain.IdentityAccess;

/// <summary>BC01 — Generic. Password hashing and tokens belong in secure adapters; not implemented yet.</summary>
public sealed class UserAccount
{
    private UserAccount() { }
    public Guid Id { get; private set; }
    public Guid? CompanyId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
}
