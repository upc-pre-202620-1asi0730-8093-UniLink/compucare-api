using CompuCare.Domain.CompaniesEquipment;
using CompuCare.Domain.IdentityAccess;
using CompuCare.Domain.QuotationsPayments;
using CompuCare.Domain.ServiceRequests;
using CompuCare.Domain.SubscriptionsCoverage;
using Microsoft.EntityFrameworkCore;

namespace CompuCare.Infrastructure.Persistence;

/// <summary>Initial database mapping for a modular monolith. NOT a finalized cross-BC schema.</summary>
public sealed class CompuCareDbContext(DbContextOptions<CompuCareDbContext> options) : DbContext(options)
{
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceRequest>(e =>
        {
            e.ToTable("service_requests"); e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(35);
            e.HasIndex(x => new { x.CompanyId, x.Status });
            e.HasIndex(x => x.TechnicianId);
        });
        modelBuilder.Entity<Subscription>(e =>
        {
            e.ToTable("subscriptions"); e.HasKey(x => x.Id);
            e.Ignore(x => x.AvailableHours);
            e.HasIndex(x => x.CompanyId);
        });
        modelBuilder.Entity<Company>(e =>
        {
            e.ToTable("companies"); e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.TaxId).HasMaxLength(20);
            e.HasIndex(x => x.TaxId).IsUnique();
        });
        modelBuilder.Entity<Equipment>(e =>
        {
            e.ToTable("equipment"); e.HasKey(x => x.Id);
            e.Property(x => x.SerialNumber).HasMaxLength(100);
            e.HasIndex(x => new { x.CompanyId, x.SerialNumber }).IsUnique();
        });
        modelBuilder.Entity<Quotation>(e =>
        {
            e.ToTable("quotations"); e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(35);
            e.HasIndex(x => x.RequestId);
        });
        modelBuilder.Entity<UserAccount>(e =>
        {
            e.ToTable("user_accounts"); e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(254);
            e.Property(x => x.PasswordHash).HasMaxLength(1024);
            e.Property(x => x.Role).HasMaxLength(40);
            e.HasIndex(x => x.Email).IsUnique();
        });
    }
}
