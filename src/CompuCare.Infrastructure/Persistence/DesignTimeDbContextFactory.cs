using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CompuCare.Infrastructure.Persistence;

/// <summary>Allows dotnet ef migrations to inspect the schema without starting the web host.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CompuCareDbContext>
{
    public CompuCareDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__CompuCare");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Set ConnectionStrings__CompuCare to run dotnet ef.");
        var options = new DbContextOptionsBuilder<CompuCareDbContext>()
            .UseNpgsql(connection).Options;
        return new CompuCareDbContext(options);
    }
}
