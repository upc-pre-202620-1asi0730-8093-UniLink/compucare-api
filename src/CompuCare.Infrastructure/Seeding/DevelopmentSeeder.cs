using CompuCare.Domain.SubscriptionsCoverage;
using CompuCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CompuCare.Infrastructure.Seeding;

/// <summary>Local plan catalog. Real users are created through IAM registration.</summary>
public static class DevelopmentSeeder
{
    public static async Task SeedPlansAsync(CompuCareDbContext db, CancellationToken ct = default)
    {
        if (await db.Plans.AnyAsync(ct)) return;
        await db.Plans.AddRangeAsync(
            Plan.Publish("basico", "Básico", 99m, 5m, 1),
            Plan.Publish("empresarial", "Empresarial", 249m, 15m, 2),
            Plan.Publish("integral", "Integral", 499m, 30m, 4));
        await db.SaveChangesAsync(ct);
    }
}
