using CompuCare.Domain.Common;
using CompuCare.Domain.CompaniesEquipment;
using CompuCare.Domain.IdentityAccess;
using CompuCare.Domain.QuotationsPayments;
using CompuCare.Domain.ServiceRequests;
using CompuCare.Domain.SubscriptionsCoverage;
using Microsoft.EntityFrameworkCore;

namespace CompuCare.Infrastructure.Persistence;

/// <summary>EF Core infrastructure of a DDD modular monolith; BC domain objects have no EF dependencies.</summary>
public sealed class CompuCareDbContext(DbContextOptions<CompuCareDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<PasswordResetToken> ResetTokens => Set<PasswordResetToken>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<EquipmentAssignment> EquipmentAssignments => Set<EquipmentAssignment>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<MaintenanceVisit> MaintenanceVisits => Set<MaintenanceVisit>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);

        m.Entity<UserAccount>(e =>
        {
            e.ToTable("user_accounts"); e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(254).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(1024).IsRequired();
            e.Property(x => x.Role).HasMaxLength(40).IsRequired();
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Position).HasMaxLength(140);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.Role });
        });
        m.Entity<PasswordResetToken>(e =>
        {
            e.ToTable("password_reset_tokens"); e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.UserId);
        });
        m.Entity<Company>(e =>
        {
            e.ToTable("companies"); e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.TaxId).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.TaxId).IsUnique();
        });
        m.Entity<Location>(e =>
        {
            e.ToTable("locations"); e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Desk).HasMaxLength(140).IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.Name, x.Desk }).IsUnique();
        });
        m.Entity<Equipment>(e =>
        {
            e.ToTable("equipment"); e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(30).IsRequired();
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.Type).HasMaxLength(90).IsRequired();
            e.Property(x => x.SerialNumber).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.SerialNumber }).IsUnique();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.AssignedEmployeeId);
        });
        m.Entity<EquipmentAssignment>(e =>
        {
            e.ToTable("equipment_assignments"); e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EquipmentId, x.At });
        });
        m.Entity<Plan>(e =>
        {
            e.ToTable("plans"); e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(60).IsRequired();
            e.Property(x => x.Name).HasMaxLength(90).IsRequired();
            e.Property(x => x.Price).HasPrecision(12, 2);
            e.Property(x => x.Hours).HasPrecision(10, 2);
            e.HasIndex(x => x.Code).IsUnique();
        });
        m.Entity<Subscription>(e =>
        {
            e.ToTable("subscriptions"); e.HasKey(x => x.Id);
            e.Property(x => x.PlanName).HasMaxLength(90);
            e.Property(x => x.MonthlyHours).HasPrecision(10, 2);
            e.Property(x => x.UsedHours).HasPrecision(10, 2);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.Ignore(x => x.AvailableHours);
            e.Ignore(x => x.PreventiveRemaining);
            e.HasIndex(x => x.CompanyId).IsUnique();
            e.OwnsMany(x => x.Consumptions, item =>
            {
                item.ToTable("coverage_consumptions");
                item.WithOwner().HasForeignKey("subscription_id");
                item.HasKey(x => x.Id);
                item.Property(x => x.Hours).HasPrecision(10, 2);
                item.HasIndex("subscription_id", nameof(CoverageConsumption.TicketId),
                    nameof(CoverageConsumption.PeriodStartedAtUtc)).IsUnique();
            });
            e.Navigation(x => x.Consumptions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        m.Entity<MaintenanceVisit>(e =>
        {
            e.ToTable("maintenance_visits"); e.HasKey(x => x.Id);
            e.Property(x => x.Date).HasMaxLength(10).IsRequired();
            e.Property(x => x.Time).HasMaxLength(8).IsRequired();
            e.Property(x => x.Status).HasMaxLength(40).IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.Date });
        });
        m.Entity<ServiceRequest>(e =>
        {
            e.ToTable("service_requests"); e.HasKey(x => x.Id);
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Diagnosis).HasMaxLength(4000);
            e.Property(x => x.WorkDone).HasMaxLength(4000);
            e.Property(x => x.WorkHours).HasPrecision(10, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(35);
            e.HasIndex(x => new { x.CompanyId, x.Status });
            e.HasIndex(x => x.TechnicianId);
            e.OwnsMany(x => x.Timeline, line =>
            {
                line.ToTable("ticket_timeline");
                line.WithOwner().HasForeignKey("service_request_id");
                line.HasKey(x => x.Id);
                line.Property(x => x.Status).HasMaxLength(55).IsRequired();
            });
            e.Navigation(x => x.Timeline).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        m.Entity<Quotation>(e =>
        {
            e.ToTable("quotations"); e.HasKey(x => x.Id);
            e.Property(x => x.Total).HasPrecision(12, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(35);
            e.Property(x => x.Receipt).HasMaxLength(50);
            e.HasIndex(x => new { x.RequestId, x.Status });
            e.HasIndex(x => new { x.CompanyId, x.Status });
            e.OwnsMany(x => x.Items, line =>
            {
                line.ToTable("quotation_lines");
                line.WithOwner().HasForeignKey("quotation_id");
                line.HasKey(x => x.Id);
                line.Property(x => x.Description).HasMaxLength(500).IsRequired();
                line.Property(x => x.Cost).HasPrecision(12, 2);
            });
            e.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        m.Entity<Notification>(e =>
        {
            e.ToTable("notifications"); e.HasKey(x => x.Id);
            e.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            e.HasIndex(x => new { x.UserId, x.CreatedAt });
        });
    }
}
