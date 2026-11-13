using CompuCare.Application.SubscriptionsCoverage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompuCare.Api.SubscriptionsCoverage.Interfaces.Rest;

[ApiController, Route("api/v1/plans"), Tags("BC03 Subscriptions and Coverage")]
public sealed class PlansController(SubscriptionService service) : ControllerBase
{
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.PlansAsync(ct));
}
[ApiController, Route("api/v1/subscriptions"), Authorize, Tags("BC03 Subscriptions and Coverage")]
public sealed class SubscriptionsController(SubscriptionService service) : ControllerBase
{
    [HttpGet("balance")]
    public async Task<IActionResult> Balance(CancellationToken ct) => Ok(await service.BalanceAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeResource request, CancellationToken ct) =>
        StatusCode(201, await service.SubscribeAsync(request, ct));

    [HttpPost("renew")]
    public async Task<IActionResult> Renew(CancellationToken ct) => Ok(await service.RenewAsync(ct));
}
[ApiController, Route("api/v1/maintenances"), Authorize, Tags("BC03 Subscriptions and Coverage")]
public sealed class MaintenancesController(SubscriptionService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.MaintenancesAsync(ct));
    [HttpPost]
    public async Task<IActionResult> Schedule([FromBody] ScheduleMaintenanceResource request, CancellationToken ct) =>
        StatusCode(201, await service.ScheduleAsync(request, ct));
}
