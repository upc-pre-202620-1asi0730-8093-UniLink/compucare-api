using CompuCare.Domain.SubscriptionsCoverage;
using CompuCare.Domain.CompaniesEquipment;
using CompuCare.Domain.IdentityAccess;
using CompuCare.Application.Common;
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
    /// <summary>Optional account fixtures. Password is read ONLY from untracked configuration.</summary>
    public static async Task SeedDemoAccountsAsync(CompuCareDbContext db, IPasswordHasher hasher,
        string secret, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 12)
            throw new InvalidOperationException("DemoSeed__Password must have at least twelve characters.");
        const string taxId = "20123456789";
        var company = await db.Companies.FirstOrDefaultAsync(x => x.TaxId == taxId, ct);
        if (company is null)
        {
            company = Company.Register("CompuCare demo company", taxId);
            await db.Companies.AddAsync(company, ct);
        }
        async Task<UserAccount> Ensure(string mail, string name, string role, Guid? companyId)
        {
            var previous = await db.UserAccounts.FirstOrDefaultAsync(x => x.Email == mail, ct);
            if (previous is not null) return previous;
            var user = UserAccount.Register(name, mail, hasher.Hash(secret), role, companyId);
            await db.UserAccounts.AddAsync(user, ct);
            return user;
        }

        await Ensure("soporte@unilink.pe", "UniLink Support", "sysadmin", null);
        await Ensure("tecnico@unilink.pe", "Technical Support", "technician", null);
        await Ensure("admin@mype.pe", "Company Administrator", "admin", company.Id);
        var employee = await Ensure("empleada@mype.pe", "Company Employee", "employee", company.Id);
        var location = await db.Locations.FirstOrDefaultAsync(x => x.CompanyId == company.Id, ct);
        if (location is null)
        {
            location = Location.Register(company.Id, "Lima Central Office", "Desk 1");
            await db.Locations.AddAsync(location, ct);
        }
        if (!await db.Equipments.AnyAsync(x => x.CompanyId == company.Id, ct))
        {
            var device = Equipment.Register(company.Id, "DEMO-COMPUCARE-001", "Office workstation", "Desktop", location.Id);
            device.AssignTo(employee.Id, true);
            await db.Equipments.AddAsync(device, ct);
        }
        if (!await db.Subscriptions.AnyAsync(x => x.CompanyId == company.Id, ct))
        {
            var basicPlan = await db.Plans.FirstAsync(x => x.Code == "basico", ct);
            await db.Subscriptions.AddAsync(Subscription.Activate(company.Id, basicPlan), ct);
        }
        await db.SaveChangesAsync(ct);
    }

}
