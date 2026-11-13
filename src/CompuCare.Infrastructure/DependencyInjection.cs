using CompuCare.Application.ServiceRequests;
using CompuCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CompuCare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CompuCareDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IServiceRequestRepository, EfServiceRequestRepository>();
        return services;
    }
}
