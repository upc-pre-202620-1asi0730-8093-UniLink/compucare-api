using System.Net;
using System.Net.Mail;
using CompuCare.Application.Common;
using CompuCare.Application.IdentityAccess;
using Microsoft.Extensions.Configuration;

namespace CompuCare.Infrastructure.Notifications;

/// <summary>No silent fake emails in production; SMTP must be configured for real invitations.</summary>
public sealed class SmtpEmailSender(IConfiguration config, IRuntimeMode runtime) : IEmailSender
{
    public async Task SendAsync(string address, string subject, string body, CancellationToken ct = default)
    {
        if (runtime.IsDevelopment) return; // dev password is returned only by explicitly dev-only endpoints
        var server = config["Smtp:Host"];
        var from = config["Smtp:From"];
        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(from))
            throw new AppException(503, "Email delivery is not configured.");
        using var smtp = new SmtpClient(server, int.TryParse(config["Smtp:Port"], out var port) ? port : 587)
        { EnableSsl = true };
        var user = config["Smtp:User"];
        if (!string.IsNullOrEmpty(user))
            smtp.Credentials = new NetworkCredential(user, config["Smtp:Password"]);
        using var message = new MailMessage(from, address, subject, body);
        await smtp.SendMailAsync(message, ct);
    }
}
