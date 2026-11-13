using System.Security.Cryptography;
using System.Text;
using CompuCare.Application.Common;
using CompuCare.Domain.CompaniesEquipment;
using CompuCare.Domain.IdentityAccess;
using CompuCare.Domain.SubscriptionsCoverage;

namespace CompuCare.Application.IdentityAccess;

public sealed record LoginResource(string Email, string Password);
public sealed record CompanyResource(string Name, string Ruc);
public sealed record AdminResource(string Name, string Email, string Password);
public sealed record DemoPaymentResource(string Holder, string Last4);
public sealed record CompanyRegistrationResource(CompanyResource Company, AdminResource Admin, Guid? PlanId, DemoPaymentResource? Payment);
public sealed record EmployeeRegistrationResource(string Name, string Email, string? Position);
public sealed record ProfilePatchResource(string Name, string? Phone);
public sealed record ChangePasswordResource(string CurrentPassword, string NewPassword);
public sealed record PasswordResetResource(string Token, string Password);
public sealed record EmailResource(string Email);
public sealed record UserResource(Guid Id, Guid? CompanyId, string Name, string Email, string Role, string Phone, string Position);
public sealed record LoginResult(string Token, string TokenType, UserResource User);
public sealed record RegistrationResult(UserResource User, CompanyResource Company);
public sealed record EmployeeResult(UserResource User, string? TempPassword);
public sealed record RecoveryResult(string? DevToken);

public interface IEmailSender
{
    Task SendAsync(string address, string subject, string body, CancellationToken ct = default);
}
public interface IRuntimeMode
{
    bool IsDevelopment { get; }
}
/// <summary>Orchestrates registration across BC01 and BC02 without domain objects crossing into HTTP.</summary>
public sealed class IamService(
    IEntityStore<UserAccount> users,
    IEntityStore<Company> companies,
    IEntityStore<Plan> plans,
    IEntityStore<Subscription> subscriptions,
    IEntityStore<PasswordResetToken> resetTokens,
    IPasswordHasher passwords,
    ITokenIssuer jwt,
    ICurrentUser current,
    IEmailSender email,
    IRuntimeMode runtime)
{
    public static UserResource Resource(UserAccount u) =>
        new(u.Id, u.CompanyId, u.Name, u.Email, u.Role, u.Phone, u.Position);

    public async Task<LoginResult> LoginAsync(LoginResource resource, CancellationToken ct)
    {
        var normalized = resource.Email?.Trim().ToLowerInvariant() ?? "";
        var user = await users.FirstAsync(u => u.Email == normalized && u.Active, ct);
        if (user is null || !passwords.Verify(resource.Password ?? "", user.PasswordHash))
            throw new AppException(401, "Invalid credentials.");
        return new LoginResult(jwt.Issue(user.Id, user.Role, user.CompanyId), "Bearer", Resource(user));
    }

    public async Task<RegistrationResult> RegisterAsync(CompanyRegistrationResource resource, CancellationToken ct)
    {
        if (resource.Company is null || resource.Admin is null ||
            resource.Company.Name is null || resource.Admin.Name is null ||
            resource.Admin.Email is null || resource.Admin.Password is null ||
            resource.Company.Ruc is null || resource.Company.Ruc.Length != 11 ||
            !resource.Company.Ruc.All(char.IsDigit))
            throw new AppException(400, "Company, RUC (11 digits) and administrator information are required.");
        var normalized = resource.Admin.Email.Trim().ToLowerInvariant();
        if (await companies.FirstAsync(x => x.TaxId == resource.Company.Ruc, ct) is not null)
            throw new AppException(409, "Company tax identifier is already registered.");
        if (await users.FirstAsync(x => x.Email == normalized, ct) is not null)
            throw new AppException(409, "Email is already registered.");
        Plan? selectedPlan = null;
        if (resource.PlanId.HasValue)
        {
            selectedPlan = await plans.FirstAsync(p => p.Id == resource.PlanId.Value, ct)
                ?? throw new AppException(404, "Selected plan not found.");
            if (resource.Payment is null || string.IsNullOrWhiteSpace(resource.Payment.Holder) ||
                resource.Payment.Last4?.Length != 4 || !resource.Payment.Last4.All(char.IsDigit))
                throw new AppException(400, "A fictitious cardholder and four test digits are required.");
        }
        var company = Company.Register(resource.Company.Name, resource.Company.Ruc);
        var admin = UserAccount.Register(resource.Admin.Name, normalized, passwords.Hash(resource.Admin.Password),
            "admin", company.Id, "Administrador");
        await companies.AddAsync(company, ct);
        await users.AddAsync(admin, ct);
        if (selectedPlan is not null)
            await subscriptions.AddAsync(Subscription.Activate(company.Id, selectedPlan), ct);
        await users.SaveAsync(ct);
        return new RegistrationResult(Resource(admin), new CompanyResource(company.Name, company.TaxId));
    }

    public async Task<List<UserResource>> EmployeesAsync(CancellationToken ct)
    {
        Access.Require(current, "admin", "company_manager");
        var companyId = Access.Company(current);
        var result = await users.ListAsync(u => u.CompanyId == companyId && u.Role == "employee", ct);
        return result.Select(Resource).ToList();
    }

    public async Task<EmployeeResult> CreateEmployeeAsync(EmployeeRegistrationResource resource, CancellationToken ct)
    {
        Access.Require(current, "admin", "company_manager");
        var companyId = Access.Company(current);
        var normalized = resource.Email?.Trim().ToLowerInvariant() ?? "";
        if (await users.FirstAsync(u => u.Email == normalized, ct) is not null)
            throw new AppException(409, "Email is already registered.");
        // Production must have an email provider; never silently create an inaccessible account.
        var tempPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
        var user = UserAccount.Register(resource.Name, normalized, passwords.Hash(tempPassword),
            "employee", companyId, resource.Position ?? "");
        await users.AddAsync(user, ct);
        await email.SendAsync(user.Email, "CompuCare invitation",
            $"Your temporary password is: {tempPassword}. Change it after your first sign-in.", ct);
        await users.SaveAsync(ct);
        return new EmployeeResult(Resource(user), runtime.IsDevelopment ? tempPassword : null);
    }

    public async Task<List<UserResource>> TechniciansAsync(CancellationToken ct)
    {
        Access.Require(current, "sysadmin");
        var result = await users.ListAsync(u => u.Role == "technician" && u.Active, ct);
        return result.Select(Resource).ToList();
    }

    public async Task<UserResource> MyProfileAsync(CancellationToken ct)
    {
        Access.Require(current);
        var user = await users.FirstAsync(u => u.Id == current.UserId, ct)
            ?? throw new AppException(401, "Session user not found.");
        return Resource(user);
    }

    public async Task<UserResource> UpdateProfileAsync(Guid id, ProfilePatchResource resource, CancellationToken ct)
    {
        Access.Require(current);
        if (id != current.UserId) throw new AppException(403, "You cannot edit another user.");
        var user = await users.FirstAsync(u => u.Id == id, ct)
            ?? throw new AppException(404, "User not found.");
        user.UpdateProfile(resource.Name, resource.Phone);
        await users.SaveAsync(ct);
        return Resource(user);
    }

    public async Task ChangePasswordAsync(Guid id, ChangePasswordResource resource, CancellationToken ct)
    {
        Access.Require(current);
        if (id != current.UserId) throw new AppException(403, "You cannot edit another user's password.");
        var user = await users.FirstAsync(u => u.Id == id, ct)
            ?? throw new AppException(404, "User not found.");
        if (!passwords.Verify(resource.CurrentPassword ?? "", user.PasswordHash))
            throw new AppException(400, "Current password does not match.");
        user.ChangePasswordHash(passwords.Hash(resource.NewPassword ?? ""));
        await users.SaveAsync(ct);
    }

    private static string Digest(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public async Task<RecoveryResult> RequestResetAsync(EmailResource resource, CancellationToken ct)
    {
        var normalized = resource.Email?.Trim().ToLowerInvariant() ?? "";
        var user = await users.FirstAsync(u => u.Email == normalized && u.Active, ct);
        if (user is null) return new RecoveryResult(null); // do not enumerate registered accounts
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        await resetTokens.AddAsync(PasswordResetToken.Issue(user.Id, Digest(token), DateTime.UtcNow.AddHours(1)), ct);
        await email.SendAsync(user.Email, "CompuCare password recovery",
            $"Your one-hour password reset token is: {token}", ct);
        await resetTokens.SaveAsync(ct);
        return new RecoveryResult(runtime.IsDevelopment ? token : null);
    }

    public async Task ResetPasswordAsync(PasswordResetResource resource, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(resource.Token)) throw new AppException(400, "Reset token required.");
        var digest = Digest(resource.Token);
        var reset = await resetTokens.FirstAsync(t => t.TokenHash == digest, ct)
            ?? throw new AppException(400, "Invalid or expired reset token.");
        var user = await users.FirstAsync(u => u.Id == reset.UserId, ct)
            ?? throw new AppException(400, "Invalid reset token.");
        reset.Consume();
        user.ChangePasswordHash(passwords.Hash(resource.Password ?? ""));
        await users.SaveAsync(ct);
    }
}
