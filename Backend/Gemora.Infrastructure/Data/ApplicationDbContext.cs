using Gemora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

<<<<<<< Updated upstream
    public DbSet<User> Users { get; set; }
=======
    public DbSet<User> Users => Set<User>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<GemListing> GemListings => Set<GemListing>();

    public DbSet<GemVerification> GemVerifications
        => Set<GemVerification>();

    public DbSet<EmailVerificationCode> EmailVerificationCodes
        => Set<EmailVerificationCode>();
>>>>>>> Stashed changes

    public DbSet<Shipment> Shipments => Set<Shipment>();

    public DbSet<ShippingPlan> ShippingPlans => Set<ShippingPlan>();

    public DbSet<InsuranceRecord> InsuranceRecords => Set<InsuranceRecord>();

    public DbSet<ShipmentTrackingEvent> ShipmentTrackingEvents => Set<ShipmentTrackingEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==========================================
        // USER CONFIGURATION
        // ==========================================

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.Property(u => u.Role)
                .IsRequired()
                .HasMaxLength(50);

            // Database-level unique email protection
            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.CreatedAt)
                .IsRequired();
        });
    }
}