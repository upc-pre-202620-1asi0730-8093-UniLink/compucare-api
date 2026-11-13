using CompuCare.Domain.Common;
namespace CompuCare.Domain.IdentityAccess;

/// <summary>One-time password reset token. Only a SHA-256 digest is persisted.</summary>
public sealed class PasswordResetToken
{
    private PasswordResetToken() { }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = "";
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }
    public static PasswordResetToken Issue(Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(tokenHash) || expiresAtUtc <= DateTime.UtcNow)
            throw new DomainRuleException("Invalid password reset token request.");
        return new PasswordResetToken { Id = Guid.NewGuid(), UserId = userId, TokenHash = tokenHash, ExpiresAtUtc = expiresAtUtc };
    }
    public void Consume()
    {
        if (UsedAtUtc.HasValue || ExpiresAtUtc <= DateTime.UtcNow)
            throw new DomainRuleException("Password reset token expired or already used.");
        UsedAtUtc = DateTime.UtcNow;
    }
}
