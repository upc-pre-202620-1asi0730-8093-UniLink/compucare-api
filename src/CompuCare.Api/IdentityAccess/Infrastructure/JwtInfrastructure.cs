using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CompuCare.Application.Common;
using Microsoft.IdentityModel.Tokens;

namespace CompuCare.Api.IdentityAccess.Infrastructure;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public Guid UserId => Guid.TryParse(Principal?.FindFirst("sub")?.Value ??
        Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;
    public string Role => Principal?.FindFirst("role")?.Value ??
        Principal?.FindFirst(ClaimTypes.Role)?.Value ?? "";
    public Guid? CompanyId => Guid.TryParse(Principal?.FindFirst("companyId")?.Value, out var id) ? id : null;
}
public sealed class RuntimeMode(IHostEnvironment host) : IRuntimeMode
{
    public bool IsDevelopment => host.IsDevelopment();
}
public sealed class JwtTokenIssuer(IConfiguration configuration) : ITokenIssuer
{
    public string Issue(Guid id, string role, Guid? companyId)
    {
        var key = configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Missing Jwt__SigningKey.");
        if (Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("JWT signing key must contain at least 32 bytes.");
        var claims = new List<Claim> { new("sub", id.ToString()), new("role", role) };
        if (companyId.HasValue) claims.Add(new Claim("companyId", companyId.Value.ToString()));
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "CompuCare.Api",
            audience: configuration["Jwt:Audience"] ?? "CompuCare.Frontend",
            claims: claims, expires: expires, signingCredentials: credentials));
    }
}
