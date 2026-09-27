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

    public DbSet<User> Users { get; set; }
    public DbSet<Shipment> Shipments { get; set; }
    public DbSet<InsuranceRecord> InsuranceRecords { get; set; }
    public DbSet<ShipmentTrackingEvent> ShipmentTrackingEvents { get; set; }
    public DbSet<ShippingPlan> ShippingPlans { get; set; }

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

        // ==========================================
        // SHIPMENT CONFIGURATION
        // ==========================================

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.ShipmentNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(s => s.Origin)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(s => s.Destination)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(s => s.Currency)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(s => s.PackageDescription)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(s => s.SelectedService)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(s => s.CourierName)
                .HasMaxLength(100);

            entity.Property(s => s.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(s => s.OrderId);
            entity.HasIndex(s => s.BuyerUserId);
            entity.HasIndex(s => s.SellerUserId);
            entity.HasIndex(s => s.Status);

            entity.HasMany(s => s.TrackingEvents)
                .WithOne(t => t.Shipment)
                .HasForeignKey(t => t.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(s => s.InsuranceRecords)
                .WithOne(i => i.Shipment)
                .HasForeignKey(i => i.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.ShippingPlan)
                .WithOne(p => p.Shipment)
                .HasForeignKey<ShippingPlan>(p => p.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InsuranceRecord>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Property(i => i.Provider)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(i => i.PolicyReference)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(i => i.CoverageType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(i => i.Currency)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(i => i.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(i => i.ShipmentId);
        });

        modelBuilder.Entity<ShipmentTrackingEvent>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(t => t.LocationText)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(t => t.Description)
                .IsRequired();

            entity.HasIndex(t => t.ShipmentId);
            entity.HasIndex(t => t.OccurredAt);
        });

        modelBuilder.Entity<ShippingPlan>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.RiskLevel)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(p => p.RecommendedServiceType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.Requirements)
                .HasColumnType("jsonb");

            entity.Property(p => p.Warnings)
                .HasColumnType("jsonb");

            entity.HasIndex(p => p.ShipmentId)
                .IsUnique();
        });
    }
}