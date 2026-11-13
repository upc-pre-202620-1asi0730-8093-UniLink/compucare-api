using System.Linq.Expressions;

namespace CompuCare.Application.Common;

/// <summary>DDD persistence port. Implemented only in Infrastructure, not Domain.</summary>
public interface IEntityStore<T> where T : class
{
    Task<T?> FirstAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default);
    Task<List<T>> ListAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
public interface ICurrentUser
{
    Guid UserId { get; }
    Guid? CompanyId { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
}
public interface IPasswordHasher
{
    string Hash(string secret);
    bool Verify(string secret, string storedHash);
}
public interface ITokenIssuer
{
    string Issue(Guid id, string role, Guid? companyId);
}
public interface INotificationWriter
{
    Task NotifyAsync(Guid userId, string message, CancellationToken ct = default);
}
public class AppException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
public static class Access
{
    public static void Require(ICurrentUser user, params string[] roles)
    {
        if (!user.IsAuthenticated) throw new AppException(401, "Authentication is required.");
        if (roles.Length > 0 && !roles.Contains(user.Role))
            throw new AppException(403, "You do not have permission for this operation.");
    }
    public static Guid Company(ICurrentUser user)
        => user.CompanyId ?? throw new AppException(403, "This account is not associated with a company.");
}
