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

        // ============================================================
        // ORDER
        // ============================================================

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");

            entity.HasKey(o => o.Id);

            entity.Property(o => o.Id)
                .IsRequired();

            entity.Property(o => o.BuyerId)
                .IsRequired();

            entity.Property(o => o.SellerId)
                .IsRequired();

            entity.Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(o => o.Currency)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(o => o.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(o => o.ShippingAddress)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(o => o.ShippingRegion)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(o => o.ShippingCountryCode)
                .IsRequired()
                .HasMaxLength(2);

            entity.Property(o => o.CreatedAt)
                .IsRequired();

            entity.HasIndex(o => o.BuyerId);
            entity.HasIndex(o => o.SellerId);
            entity.HasIndex(o => o.GemListingId);
            entity.HasIndex(o => o.Status);

            entity.HasOne(o => o.Buyer)
                .WithMany(u => u.PurchasedOrders)
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Seller)
                .WithMany(u => u.SoldOrders)
                .HasForeignKey(o => o.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.GemListing)
                .WithMany(g => g.Orders)
                .HasForeignKey(o => o.GemListingId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ============================================================
        // SHIPMENT
        // ============================================================

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.ToTable("Shipments");

            entity.HasKey(s => s.Id);

            entity.Property(s => s.Id)
                .IsRequired();

            entity.Property(s => s.OrderId)
                .IsRequired();

            entity.Property(s => s.SellerId)
                .IsRequired();

            entity.Property(s => s.BuyerId)
                .IsRequired();

            entity.Property(s => s.OriginAddress)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(s => s.OriginRegion)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(s => s.OriginCountryCode)
                .IsRequired()
                .HasMaxLength(2);

            entity.Property(s => s.DestinationAddress)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(s => s.DestinationRegion)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(s => s.DestinationCountryCode)
                .IsRequired()
                .HasMaxLength(2);

            entity.Property(s => s.DeclaredValue)
                .HasPrecision(18, 2);

            entity.Property(s => s.Currency)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(s => s.PackageDescription)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(s => s.PackageWeight)
                .HasPrecision(10, 2);

            entity.Property(s => s.PackageDimensions)
                .HasMaxLength(200);

            entity.Property(s => s.PreferredService)
                .HasMaxLength(100);

            entity.Property(s => s.SpecialHandlingNotes)
                .HasMaxLength(2000);

            entity.Property(s => s.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(s => s.RiskLevel)
                .HasMaxLength(50);

            entity.Property(s => s.TrackingNumber)
                .HasMaxLength(200);

            entity.Property(s => s.CourierName)
                .HasMaxLength(200);

            entity.Property(s => s.CreatedAt)
                .IsRequired();

            entity.HasIndex(s => s.OrderId);
            entity.HasIndex(s => s.SellerId);
            entity.HasIndex(s => s.BuyerId);
            entity.HasIndex(s => s.Status);

            entity.HasOne(s => s.Seller)
                .WithMany(u => u.SellerShipments)
                .HasForeignKey(s => s.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Buyer)
                .WithMany(u => u.BuyerShipments)
                .HasForeignKey(s => s.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================================================
        // SHIPPING PLAN
        // ============================================================

        modelBuilder.Entity<ShippingPlan>(entity =>
        {
            entity.ToTable("ShippingPlans");

            entity.HasKey(sp => sp.Id);

            entity.Property(sp => sp.Id)
                .IsRequired();

            entity.Property(sp => sp.ShipmentId)
                .IsRequired();

            entity.Property(sp => sp.RiskLevel)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(sp => sp.RiskReasons)
                .HasMaxLength(4000);

            entity.Property(sp => sp.RecommendedServiceType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(sp => sp.RecommendedCoverageAmount)
                .HasPrecision(18, 2);

            entity.Property(sp => sp.HandlingRequirements)
                .HasMaxLength(2000);

            entity.Property(sp => sp.RequiredDocuments)
                .HasMaxLength(2000);

            entity.Property(sp => sp.Warnings)
                .HasMaxLength(2000);

            entity.Property(sp => sp.AdminNotes)
                .HasMaxLength(2000);

            entity.Property(sp => sp.CreatedAt)
                .IsRequired();

            entity.HasIndex(sp => sp.ShipmentId);
            entity.HasIndex(sp => sp.IsApproved);

            entity.HasOne(sp => sp.Shipment)
                .WithOne(s => s.ShippingPlan)
                .HasForeignKey<ShippingPlan>(sp => sp.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============================================================
        // INSURANCE RECORD
        // ============================================================

        modelBuilder.Entity<InsuranceRecord>(entity =>
        {
            entity.ToTable("InsuranceRecords");

            entity.HasKey(ir => ir.Id);

            entity.Property(ir => ir.Id)
                .IsRequired();

            entity.Property(ir => ir.ShipmentId)
                .IsRequired();

            entity.Property(ir => ir.CoverageAmount)
                .HasPrecision(18, 2);

            entity.Property(ir => ir.Currency)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(ir => ir.CoverageType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(ir => ir.PolicyNumber)
                .HasMaxLength(200);

            entity.Property(ir => ir.ProviderName)
                .HasMaxLength(200);

            entity.Property(ir => ir.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(ir => ir.CreatedAt)
                .IsRequired();

            entity.HasIndex(ir => ir.ShipmentId);
            entity.HasIndex(ir => ir.Status);

            entity.HasOne(ir => ir.Shipment)
                .WithOne(s => s.InsuranceRecord)
                .HasForeignKey<InsuranceRecord>(ir => ir.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============================================================
        // SHIPMENT TRACKING EVENT
        // ============================================================

        modelBuilder.Entity<ShipmentTrackingEvent>(entity =>
        {
            entity.ToTable("ShipmentTrackingEvents");

            entity.HasKey(te => te.Id);

            entity.Property(te => te.Id)
                .IsRequired();

            entity.Property(te => te.ShipmentId)
                .IsRequired();

            entity.Property(te => te.EventType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(te => te.Location)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(te => te.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(te => te.EventTimestamp)
                .IsRequired();

            entity.Property(te => te.CreatedAt)
                .IsRequired();

            entity.HasIndex(te => te.ShipmentId);
            entity.HasIndex(te => te.EventType);
            entity.HasIndex(te => te.EventTimestamp);

            entity.HasOne(te => te.Shipment)
                .WithMany(s => s.TrackingEvents)
                .HasForeignKey(te => te.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}