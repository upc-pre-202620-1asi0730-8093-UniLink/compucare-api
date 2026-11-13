using CompuCare.Application.Common;
using CompuCare.Domain.Common;
using CompuCare.Infrastructure.Persistence;

namespace CompuCare.Infrastructure.Notifications;
public sealed class NotificationWriter(CompuCareDbContext db) : INotificationWriter
{
    public async Task NotifyAsync(Guid userId, string message, CancellationToken ct = default) =>
        await db.Notifications.AddAsync(Notification.Send(userId, message), ct);
}
