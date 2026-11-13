using CompuCare.Application.IdentityAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompuCare.Api.IdentityAccess.Interfaces.Rest;

[ApiController, Route("api/v1/auth"), Tags("BC01 Identity and Access")]
public sealed class AuthController(IamService service) : ControllerBase
{
    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginResource request, CancellationToken ct) =>
        Ok(await service.LoginAsync(request, ct));

    [HttpPost("register"), AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] CompanyRegistrationResource request, CancellationToken ct) =>
        StatusCode(201, await service.RegisterAsync(request, ct));

    [HttpPost("forgot-password"), AllowAnonymous]
    public async Task<IActionResult> Forgot([FromBody] EmailResource request, CancellationToken ct)
    {
        var result = await service.RequestResetAsync(request, ct);
        return Ok(new { message = "If the account exists, recovery instructions were sent.", devToken = result.DevToken });
    }

    [HttpPost("reset-password"), AllowAnonymous]
    public async Task<IActionResult> Reset([FromBody] PasswordResetResource request, CancellationToken ct)
    {
        await service.ResetPasswordAsync(request, ct);
        return Ok(new { message = "Password updated." });
    }

    [HttpGet("me"), Authorize]
    public async Task<IActionResult> Me(CancellationToken ct) => Ok(await service.MyProfileAsync(ct));
}

[ApiController, Route("api/v1/employees"), Authorize, Tags("BC01 Identity and Access")]
public sealed class EmployeesController(IamService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.EmployeesAsync(ct));
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmployeeRegistrationResource request, CancellationToken ct)
    {
        var result = await service.CreateEmployeeAsync(request, ct);
        var user = result.User;
        return StatusCode(201, new { user.Id, user.CompanyId, user.Name, user.Email, user.Role, user.Phone,
            user.Position, result.TempPassword });
    }
}
[ApiController, Route("api/v1/technicians"), Authorize, Tags("BC01 Identity and Access")]
public sealed class TechniciansController(IamService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.TechniciansAsync(ct));
}
[ApiController, Route("api/v1/users"), Authorize, Tags("BC01 Identity and Access")]
public sealed class UsersController(IamService service) : ControllerBase
{
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, [FromBody] ProfilePatchResource request, CancellationToken ct)
        => Ok(await service.UpdateProfileAsync(id, request, ct));
    [HttpPut("{id:guid}/password")]
    public async Task<IActionResult> Password(Guid id, [FromBody] ChangePasswordResource request, CancellationToken ct)
    {
        await service.ChangePasswordAsync(id, request, ct);
        return Ok(new { message = "Password updated." });
    }
}
