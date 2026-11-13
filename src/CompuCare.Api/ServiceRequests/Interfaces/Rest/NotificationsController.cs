using CompuCare.Application.Common;
using CompuCare.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompuCare.Api.ServiceRequests.Interfaces.Rest;

[ApiController, Route("api/v1/notifications"), Authorize, Tags("Notifications")]
public sealed class NotificationsController(IEntityStore<Notification> store, ICurrentUser current) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        Access.Require(current);
        var data = await store.ListAsync(x => x.UserId == current.UserId, ct);
        return Ok(data.OrderByDescending(x => x.CreatedAt).Select(x => new {
            x.Id, x.UserId, x.Message, x.CreatedAt, x.Read
        }));
    }
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        Access.Require(current);
        var value = await store.FirstAsync(x => x.Id == id && x.UserId == current.UserId, ct)
            ?? throw new AppException(404, "Notification not found.");
        value.MarkRead();
        await store.SaveAsync(ct);
        return Ok(new { value.Id, value.Read });
    }
}
