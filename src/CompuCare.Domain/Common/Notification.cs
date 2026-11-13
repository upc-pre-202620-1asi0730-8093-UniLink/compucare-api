namespace CompuCare.Domain.Common;
/// <summary>Cross-cutting in-app notification (not proof of email delivery).</summary>
public sealed class Notification
{
    private Notification() { }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Message { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }
    public bool Read { get; private set; }
    public static Notification Send(Guid userId, string message) =>
        new() { Id = Guid.NewGuid(), UserId = userId, Message = message, CreatedAt = DateTime.UtcNow };
    public void MarkRead() => Read = true;
}
