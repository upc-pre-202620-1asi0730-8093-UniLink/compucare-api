using CompuCare.Application.QuotationsPayments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompuCare.Api.QuotationsPayments.Interfaces.Rest;

[ApiController, Route("api/v1/quotes"), Authorize, Tags("BC05 Quotations and Payments")]
public sealed class QuotationsController(QuotationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await service.QuotesAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] IssueQuotationResource request, CancellationToken ct) =>
        StatusCode(201, await service.CreateAsync(request, ct));

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] QuotationStatusResource request, CancellationToken ct) =>
        Ok(await service.UpdateStatusAsync(id, request, ct));

    [HttpPost("{id:guid}/pay")]
    public async Task<IActionResult> Pay(Guid id, [FromBody] TestCardResource request, CancellationToken ct) =>
        Ok(await service.PayAsync(id, request, ct));
}
