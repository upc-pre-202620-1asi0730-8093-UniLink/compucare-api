using CompuCare.Application.CompaniesEquipment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompuCare.Api.CompaniesEquipment.Interfaces.Rest;

[ApiController, Route("api/v1/locations"), Authorize, Tags("BC02 Companies and Equipment")]
public sealed class LocationsController(CompanyEquipmentService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => Ok(await service.LocationsAsync(ct));
    [HttpPost] public async Task<IActionResult> Post([FromBody] CreateLocationResource request, CancellationToken ct) =>
        StatusCode(201, await service.CreateLocationAsync(request, ct));
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchLocationResource request, CancellationToken ct) =>
        Ok(await service.PatchLocationAsync(id, request, ct));
}
[ApiController, Route("api/v1/equipments"), Authorize, Tags("BC02 Companies and Equipment")]
public sealed class EquipmentsController(CompanyEquipmentService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? status, [FromQuery] string? type,
        [FromQuery] Guid? locationId, [FromQuery] string? q, CancellationToken ct) =>
        Ok(await service.EquipmentsAsync(status, type, locationId, q, ct));

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateEquipmentResource request, CancellationToken ct) =>
        StatusCode(201, await service.CreateEquipmentAsync(request, ct));

    [HttpPut("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignEquipmentResource request, CancellationToken ct) =>
        Ok(await service.AssignAsync(id, request, ct));

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> History(Guid id, CancellationToken ct) =>
        Ok(await service.HistoryAsync(id, ct));
}
