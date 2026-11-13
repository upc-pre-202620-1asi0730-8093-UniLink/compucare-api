using CompuCare.Application.ServiceRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompuCare.Api.ServiceRequests.Interfaces.Rest;

[ApiController, Route("api/v1/tickets"), Authorize, Tags("BC04 Service Requests")]
public sealed class TicketsController(ServiceRequestService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await service.TicketsAsync(ct));
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ReportIssueResource request, CancellationToken ct) =>
        StatusCode(201, await service.ReportAsync(request, ct));
    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) => Ok(await service.CancelAsync(id, ct));
    [HttpPut("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignTechnicianResource request, CancellationToken ct) =>
        Ok(await service.AssignAsync(id, request, ct));
    [HttpPut("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct) => Ok(await service.StartAsync(id, ct));
    [HttpPut("{id:guid}/diagnosis")]
    public async Task<IActionResult> Diagnose(Guid id, [FromBody] DiagnoseResource request, CancellationToken ct) =>
        Ok(await service.DiagnoseAsync(id, request, ct));
}
