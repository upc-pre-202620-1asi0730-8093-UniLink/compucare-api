using CompuCare.Application.Common;
using CompuCare.Application.IdentityAccess;
using CompuCare.Application.ServiceRequests;
using CompuCare.Infrastructure.Notifications;
using CompuCare.Infrastructure.Persistence;
using CompuCare.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CompuCare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CompuCareDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped(typeof(IEntityStore<>), typeof(EfEntityStore<>));
        services.AddScoped<IServiceRequestRepository, EfServiceRequestRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<INotificationWriter, NotificationWriter>();
        return services;
    }
}
