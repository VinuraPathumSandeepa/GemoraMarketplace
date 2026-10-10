using Gemora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<ProfileImage> ProfileImages => Set<ProfileImage>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }


    // ============================================================
    // DB SETS
    // ============================================================

    public DbSet<User> Users
        => Set<User>();

    public DbSet<GemListing> GemListings
        => Set<GemListing>();

    public DbSet<GemVerification> GemVerifications
        => Set<GemVerification>();

    public DbSet<EmailVerificationCode> EmailVerificationCodes
        => Set<EmailVerificationCode>();

    // Component 3 - Shipping & Insurance
    public DbSet<ShippingPlan> ShippingPlans => Set<ShippingPlan>();
    public DbSet<ShipmentTrackingEvent> ShipmentTrackingEvents => Set<ShipmentTrackingEvent>();
    public DbSet<InsuranceRecord> InsuranceRecords => Set<InsuranceRecord>();


    // ============================================================
    // COMPONENT 2 - MARKETPLACE / TRANSACTIONS
    // ============================================================

    public DbSet<Order> Orders
        => Set<Order>();

    public DbSet<OrderStatusHistory> OrderStatusHistories
        => Set<OrderStatusHistory>();

    public DbSet<PaymentTransaction> PaymentTransactions
        => Set<PaymentTransaction>();

    public DbSet<OrderDeliveryDetails> OrderDeliveryDetails
        => Set<OrderDeliveryDetails>();

    public DbSet<Shipment> Shipments
    => Set<Shipment>();

public DbSet<FulfillmentStatusHistory>
    FulfillmentStatusHistories
        => Set<FulfillmentStatusHistory>();


    // ============================================================
    // EXPORT / COMPLIANCE
    // ============================================================

    public DbSet<ExportRequest> ExportRequests
        => Set<ExportRequest>();

    public DbSet<ComplianceDocument> ComplianceDocuments
        => Set<ComplianceDocument>();


    // ============================================================
    // AGENT WORKFLOW
    // ============================================================

    public DbSet<AgentWorkflow> AgentWorkflows
        => Set<AgentWorkflow>();

    public DbSet<AgentWorkflowStep> AgentWorkflowSteps
        => Set<AgentWorkflowStep>();

    public DbSet<AgentToolCall> AgentToolCalls
        => Set<AgentToolCall>();


    // ============================================================
    // MODEL CONFIGURATION
    // ============================================================

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProfileImage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(32).IsRequired();
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<WishlistItem>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.GemListingId });
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<GemListing>().WithMany().HasForeignKey(x => x.GemListingId).OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<Shipment>(
    entity =>
    {
        entity.ToTable(
            "Shipments");


        entity.HasKey(
            s => s.Id);


        entity.Property(
                s => s.CourierName)
            .IsRequired(false)
            .HasMaxLength(150);


        entity.Property(
                s => s.TrackingNumber)
            .IsRequired(false)
            .HasMaxLength(150);


        entity.Property(
                s => s.TrackingUrl)
            .HasMaxLength(1000);


        entity.Property(
            s =>
                s.ExpectedDeliveryDate);


        entity.Property(
                s => s.DispatchNote)
            .HasMaxLength(1000);


        entity.Property(
                s => s.Status)
            .IsRequired()
            .HasMaxLength(50);


        entity.Property(
                s => s.CreatedAt)
            .IsRequired();


        entity.Property(
            s => s.UpdatedAt);


        entity.Property(
            s => s.HandedOverAt);


        entity.Property(
            s => s.DeliveredAt);


        entity.HasIndex(
                s => s.OrderId)
            .IsUnique();


        entity.HasIndex(
                s => s.TrackingNumber)
            .IsUnique();


        entity.HasOne(
                s => s.Order)
            .WithOne(
                o => o.Shipment)
            .HasForeignKey<Shipment>(
                s => s.OrderId)
            .OnDelete(
                DeleteBehavior.Cascade);
    });


        modelBuilder.Entity<FulfillmentStatusHistory>(entity =>
        {
            entity.ToTable("FulfillmentStatusHistories");

            entity.HasKey(h => h.Id);

            entity.Property(h => h.PreviousStatus)
                .HasMaxLength(50);

            entity.Property(h => h.NewStatus)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(h => h.Note)
                .HasMaxLength(1000);

            entity.Property(h => h.CreatedAt)
                .IsRequired();

            entity.HasIndex(h => h.OrderId);

            entity.HasIndex(h => h.ChangedByUserId);

            entity.HasIndex(h => h.CreatedAt);

            entity.HasOne(h => h.Order)
                .WithMany(o =>
                    o.FulfillmentStatusHistory)
                .HasForeignKey(h => h.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.ChangedByUser)
                .WithMany()
                .HasForeignKey(h =>
                    h.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });







        // ========================================================
        // USER
        // ========================================================

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(u => u.Id);


            // ----------------------------------------------------
            // ID
            // ----------------------------------------------------

            entity.Property(u => u.Id)
                .IsRequired();


            // ----------------------------------------------------
            // PERSONAL INFORMATION
            // ----------------------------------------------------

            entity.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(u => u.PasswordHash)
                .IsRequired();


            // ----------------------------------------------------
            // ROLE
            // ----------------------------------------------------

            entity.Property(u => u.Role)
                .IsRequired()
                .HasMaxLength(50);


            // ----------------------------------------------------
            // CONTACT / LOCATION
            // ----------------------------------------------------

            entity.Property(u => u.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(u => u.CountryCode)
                .HasMaxLength(2);

            entity.Property(u => u.Region)
                .HasMaxLength(100);


            // ----------------------------------------------------
            // PROFILE IMAGE
            // ----------------------------------------------------

            entity.Property(u => u.ProfileImageUrl)
                .HasMaxLength(500);


            // ----------------------------------------------------
            // EMAIL VERIFICATION
            // ----------------------------------------------------

            entity.Property(u => u.IsEmailVerified)
                .IsRequired();

            entity.Property(u => u.EmailVerifiedAt);


            // ----------------------------------------------------
            // AUDIT
            // ----------------------------------------------------

            entity.Property(u => u.CreatedAt)
                .IsRequired();


            // ----------------------------------------------------
            // INDEXES
            // ----------------------------------------------------

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.HasIndex(u => u.PhoneNumber);
        });


        // ========================================================
        // GEM LISTING
        // ========================================================

        modelBuilder.Entity<GemListing>(entity =>
        {
            entity.ToTable("GemListings");

            entity.HasKey(g => g.Id);


            // ----------------------------------------------------
            // OWNER
            // ----------------------------------------------------

            entity.Property(g => g.SellerId)
                .IsRequired();


            // ----------------------------------------------------
            // BASIC LISTING INFORMATION
            // ----------------------------------------------------

            entity.Property(g => g.Title)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(g => g.GemType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(g => g.Description)
                .IsRequired()
                .HasMaxLength(2000);


            // ----------------------------------------------------
            // GEM CHARACTERISTICS
            // ----------------------------------------------------

            entity.Property(g => g.CaratWeight)
                .HasPrecision(10, 2);

            entity.Property(g => g.Color)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(g => g.Clarity)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(g => g.Cut)
                .IsRequired()
                .HasMaxLength(100);


            // ----------------------------------------------------
            // PRICE
            // ----------------------------------------------------

            entity.Property(g => g.Price)
                .HasPrecision(18, 2);

            entity.Property(g => g.Currency)
                .IsRequired()
                .HasMaxLength(10);


            // ----------------------------------------------------
            // IMAGE EVIDENCE
            // ----------------------------------------------------

            entity.Property(g => g.PrimaryImageUrl)
                .HasMaxLength(1000);


            // ----------------------------------------------------
            // CERTIFICATE EVIDENCE
            // ----------------------------------------------------

            entity.Property(g => g.CertificateNumber)
                .HasMaxLength(200);

            entity.Property(g => g.CertificateAuthority)
                .HasMaxLength(200);

            entity.Property(g => g.CertificateUrl)
                .HasMaxLength(1000);


            // ----------------------------------------------------
            // LISTING LOCATION
            // ----------------------------------------------------

            entity.Property(g => g.CountryCode)
                .HasMaxLength(2);

            entity.Property(g => g.Region)
                .HasMaxLength(100);


            // ----------------------------------------------------
            // STATUS
            // ----------------------------------------------------

            entity.Property(g => g.Status)
                .IsRequired()
                .HasMaxLength(50);


            // ----------------------------------------------------
            // AUDIT
            // ----------------------------------------------------

            entity.Property(g => g.CreatedAt)
                .IsRequired();

            entity.Property(g => g.UpdatedAt);


            // ----------------------------------------------------
            // INDEXES
            // ----------------------------------------------------

            entity.HasIndex(g => g.SellerId);

            entity.HasIndex(g => g.Status);

            entity.HasIndex(g => g.CertificateNumber);

            entity.HasIndex(g => g.CountryCode);

            entity.HasIndex(g => new
            {
                g.CountryCode,
                g.Region
            });


            // ----------------------------------------------------
            // SELLER RELATIONSHIP
            // ----------------------------------------------------

            entity.HasOne(g => g.Seller)
                .WithMany(u => u.GemListings)
                .HasForeignKey(g => g.SellerId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ========================================================
        // GEM VERIFICATION
        // ========================================================

        modelBuilder.Entity<GemVerification>(entity =>
        {
            entity.ToTable("GemVerifications");

            entity.HasKey(v => v.Id);


            // ----------------------------------------------------
            // RELATIONSHIP IDS
            // ----------------------------------------------------

            entity.Property(v => v.GemListingId)
                .IsRequired();

            entity.Property(v => v.GemologistId);


            // ----------------------------------------------------
            // HUMAN REVIEW
            // ----------------------------------------------------

            entity.Property(v => v.Decision)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(v => v.ReviewNotes)
                .HasMaxLength(2000);


            // ----------------------------------------------------
            // AI WORKFLOW
            // ----------------------------------------------------

            entity.Property(v => v.AiStatus)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(v => v.AiSuggestedGemType)
                .HasMaxLength(100);

            entity.Property(v => v.AiConfidenceScore)
                .HasPrecision(5, 2);

            entity.Property(v => v.AiFindings)
                .HasMaxLength(4000);

            entity.Property(v => v.AiRiskFlags)
                .HasMaxLength(2000);


            // ----------------------------------------------------
            // AUDIT
            // ----------------------------------------------------

            entity.Property(v => v.CreatedAt)
                .IsRequired();

            entity.Property(v => v.AiProcessedAt);

            entity.Property(v => v.ReviewedAt);


            // ----------------------------------------------------
            // INDEXES
            // ----------------------------------------------------

            entity.HasIndex(v => v.GemListingId);

            entity.HasIndex(v => v.GemologistId);

            entity.HasIndex(v => v.Decision);

            entity.HasIndex(v => v.AiStatus);


            // ----------------------------------------------------
            // GEM LISTING RELATIONSHIP
            // ----------------------------------------------------

            entity.HasOne(v => v.GemListing)
                .WithMany(g => g.Verifications)
                .HasForeignKey(v => v.GemListingId)
                .OnDelete(DeleteBehavior.Cascade);


            // ----------------------------------------------------
            // GEMOLOGIST RELATIONSHIP
            // ----------------------------------------------------

            entity.HasOne(v => v.Gemologist)
                .WithMany(u => u.GemVerifications)
                .HasForeignKey(v => v.GemologistId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ========================================================
        // ORDER
        // ========================================================

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.Property(o => o.IsLegacyDuplicate).HasDefaultValue(false);
            entity.HasIndex(o => o.GemListingId, "IX_Orders_ReservedGem")
                .IsUnique().HasFilter("NOT \"IsLegacyDuplicate\" AND \"Status\" NOT IN ('Pending', 'Rejected', 'Cancelled', 'Refunded', 'Failed')");
            entity.HasIndex(o => new { o.GemListingId, o.BuyerId }, "IX_Orders_ActiveBuyerGem")
                .IsUnique().HasFilter("NOT \"IsLegacyDuplicate\" AND \"Status\" NOT IN ('Rejected', 'Cancelled', 'Refunded', 'Failed')");
            entity.HasIndex(o => new { o.Status, o.PaymentDueAt });


            entity.HasKey(o => o.Id);


            // ----------------------------------------------------
            // ID
            // ----------------------------------------------------

            entity.Property(o => o.Id)
                .IsRequired();


            // ----------------------------------------------------
            // RELATIONSHIP IDS
            // ----------------------------------------------------

            entity.Property(o => o.BuyerId)
                .IsRequired();

            entity.Property(o => o.SellerId)
                .IsRequired();

            // Nullable according to existing database structure.
            entity.Property(o => o.GemListingId);


            // ----------------------------------------------------
            // AMOUNT
            // ----------------------------------------------------

            entity.Property(o => o.TotalAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            entity.Property(o => o.Currency)
                .IsRequired()
                .HasMaxLength(20);


            // ----------------------------------------------------
            // ORDER STATUS
            // ----------------------------------------------------

            entity.Property(o => o.Status)
                .IsRequired()
                .HasMaxLength(50);


            // ----------------------------------------------------
            // FULFILLMENT STATUS
            // ----------------------------------------------------

            entity.Property(o => o.FulfillmentStatus)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(o => o.HandedOverAt);

            entity.Property(o => o.DeliveredAt);


            // ----------------------------------------------------
            // LEGACY SHIPPING FIELDS
            //
            // These remain temporarily for backwards
            // compatibility while OrderDeliveryDetails is used
            // for the new structured delivery system.
            // ----------------------------------------------------

            entity.Property(o => o.ShippingAddress)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(o => o.ShippingRegion)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(o => o.ShippingCountryCode)
                .IsRequired()
                .HasMaxLength(2);


            // ----------------------------------------------------
            // AUDIT
            // ----------------------------------------------------

            entity.Property(o => o.CreatedAt)
                .IsRequired();

            entity.Property(o => o.UpdatedAt);

            entity.Property(o => o.PaidAt);


            // ----------------------------------------------------
            // INDEXES
            // ----------------------------------------------------

            entity.HasIndex(o => o.BuyerId);

            entity.HasIndex(o => o.SellerId);

            entity.HasIndex(o => o.GemListingId);

            entity.HasIndex(o => o.Status);


            // ----------------------------------------------------
            // BUYER RELATIONSHIP
            // ----------------------------------------------------

            entity.HasOne(o => o.Buyer)
                .WithMany(u => u.BuyerOrders)
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);


            // ----------------------------------------------------
            // SELLER RELATIONSHIP
            // ----------------------------------------------------

            entity.HasOne(o => o.Seller)
                .WithMany(u => u.SellerOrders)
                .HasForeignKey(o => o.SellerId)
                .OnDelete(DeleteBehavior.Restrict);


            // ----------------------------------------------------
            // GEM LISTING RELATIONSHIP
            // ----------------------------------------------------

            entity.HasOne(o => o.GemListing)
                .WithMany(g => g.Orders)
                .HasForeignKey(o => o.GemListingId)
                .OnDelete(DeleteBehavior.Restrict);


            // ----------------------------------------------------
            // STRUCTURED DELIVERY RELATIONSHIP
            // ----------------------------------------------------

            entity.HasOne(o => o.DeliveryDetails)
                .WithOne(d => d.Order)
                .HasForeignKey<OrderDeliveryDetails>(
                    d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // ========================================================
        // ORDER DELIVERY DETAILS
        // ========================================================

        modelBuilder.Entity<OrderDeliveryDetails>(entity =>
        {
            entity.ToTable("OrderDeliveryDetails");

            entity.HasKey(d => d.Id);


            // ----------------------------------------------------
            // IDS
            // ----------------------------------------------------

            entity.Property(d => d.Id)
                .IsRequired();

            entity.Property(d => d.OrderId)
                .IsRequired();


            // ----------------------------------------------------
            // RECIPIENT
            // ----------------------------------------------------

            entity.Property(d => d.RecipientName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(d => d.RecipientPhone)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(d => d.AlternatePhone)
                .HasMaxLength(30);


            // ----------------------------------------------------
            // ADDRESS
            // ----------------------------------------------------

            entity.Property(d => d.AddressLine1)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(d => d.AddressLine2)
                .HasMaxLength(250);

            entity.Property(d => d.City)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(d => d.District)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(d => d.Region)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(d => d.PostalCode)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(d => d.CountryCode)
                .IsRequired()
                .HasMaxLength(2);


            // ----------------------------------------------------
            // DELIVERY INFORMATION
            // ----------------------------------------------------

            entity.Property(d => d.NearestLandmark)
                .HasMaxLength(250);

            entity.Property(d => d.DeliveryInstructions)
                .HasMaxLength(1000);

            entity.Property(d => d.SignatureRequired)
                .IsRequired();


            // ----------------------------------------------------
            // AUDIT / LOCKING
            // ----------------------------------------------------

            entity.Property(d => d.CreatedAt)
                .IsRequired();

            entity.Property(d => d.UpdatedAt);

            entity.Property(d => d.LastUpdatedByUserId);

            entity.Property(d => d.LockedAt);


            // ----------------------------------------------------
            // INDEX
            //
            // One delivery record per order.
            // ----------------------------------------------------

            entity.HasIndex(d => d.OrderId)
                .IsUnique();


            // ----------------------------------------------------
            // ORDER RELATIONSHIP
            // ----------------------------------------------------

            entity.HasOne(d => d.Order)
                .WithOne(o => o.DeliveryDetails)
                .HasForeignKey<OrderDeliveryDetails>(
                    d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // ========================================================
        // PAYMENT TRANSACTION
        // ========================================================

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("PaymentTransactions");

            entity.HasKey(p => p.Id);


            entity.Property(p => p.Provider)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(p => p.ExternalReference)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.Amount)
                .IsRequired()
                .HasPrecision(18, 2);

            entity.Property(p => p.Currency)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(p => p.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(p => p.CreatedAt)
                .IsRequired();

            entity.Property(p => p.UpdatedAt);


            // ----------------------------------------------------
            // INDEXES
            // ----------------------------------------------------

            entity.HasIndex(p => p.ExternalReference)
                .IsUnique();

            entity.HasIndex(p => p.OrderId);


            // ----------------------------------------------------
            // ORDER RELATIONSHIP
            // ----------------------------------------------------

            entity.HasOne(p => p.Order)
                .WithMany()
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ========================================================
        // ORDER STATUS HISTORY
        // ========================================================

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.ToTable("OrderStatusHistories");

            entity.HasKey(h => h.Id);


            entity.Property(h => h.OrderId)
                .IsRequired();

            entity.Property(h => h.ChangedByUserId);

            entity.Property(h => h.PreviousStatus)
                .HasMaxLength(50);

            entity.Property(h => h.NewStatus)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(h => h.Reason)
                .HasMaxLength(500);

            entity.Property(h => h.CreatedAt)
                .IsRequired();


            // ----------------------------------------------------
            // INDEXES
            // ----------------------------------------------------

            entity.HasIndex(h => h.OrderId);

            entity.HasIndex(h => h.ChangedByUserId);

            entity.HasIndex(h => h.CreatedAt);


            // ----------------------------------------------------
            // ORDER RELATIONSHIP
            // ----------------------------------------------------

            entity.HasOne(h => h.Order)
                .WithMany(o => o.StatusHistory)
                .HasForeignKey(h => h.OrderId)
                .OnDelete(DeleteBehavior.Cascade);


            // ----------------------------------------------------
            // USER RELATIONSHIP
            // ----------------------------------------------------

            entity.HasOne(h => h.ChangedByUser)
                .WithMany(u => u.OrderStatusChanges)
                .HasForeignKey(h => h.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });


        // ========================================================
        // EMAIL VERIFICATION CODE
        // ========================================================

        modelBuilder.Entity<EmailVerificationCode>(entity =>
        {
            entity.ToTable("EmailVerificationCodes");

            entity.HasKey(e => e.Id);


            // ----------------------------------------------------
            // IDENTIFIERS
            // ----------------------------------------------------

            entity.Property(e => e.Id)
                .IsRequired();

            entity.Property(e => e.UserId)
                .IsRequired();


            // ----------------------------------------------------
            // OTP SECURITY
            // ----------------------------------------------------

            entity.Property(e => e.CodeHash)
                .IsRequired()
                .HasMaxLength(255);


            // ----------------------------------------------------
            // OTP STATE
            // ----------------------------------------------------

            entity.Property(e => e.ExpiresAt)
                .IsRequired();

            entity.Property(e => e.AttemptCount)
                .IsRequired();

            entity.Property(e => e.UsedAt);


            // ----------------------------------------------------
            // AUDIT
            // ----------------------------------------------------

            entity.Property(e => e.CreatedAt)
                .IsRequired();


            // ----------------------------------------------------
            // INDEXES
            // ----------------------------------------------------

            entity.HasIndex(e => e.UserId);

            entity.HasIndex(e => new
            {
                e.UserId,
                e.CreatedAt
            });


            // ----------------------------------------------------
            // USER RELATIONSHIP
            // ----------------------------------------------------

            entity.HasOne(e => e.User)
                .WithMany(u => u.EmailVerificationCodes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // ============================================================
        // ORDER (Component 3)
        // ============================================================

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");

            entity.HasKey(o => o.Id);

            entity.Property(o => o.BuyerId).IsRequired();
            entity.Property(o => o.SellerId).IsRequired();
            entity.Property(o => o.TotalAmount).HasPrecision(18, 2);
            entity.Property(o => o.Currency).IsRequired().HasMaxLength(20);
            entity.Property(o => o.Status).IsRequired().HasMaxLength(50);
            entity.Property(o => o.ShippingAddress).IsRequired().HasMaxLength(500);
            entity.Property(o => o.ShippingRegion).IsRequired().HasMaxLength(100);
            entity.Property(o => o.ShippingCountryCode).IsRequired().HasMaxLength(2);

            entity.HasIndex(o => o.BuyerId);
            entity.HasIndex(o => o.SellerId);
            entity.HasIndex(o => o.Status);
            entity.HasIndex(o => o.CreatedAt);

            entity.HasOne(o => o.Buyer)
                .WithMany(u => u.BuyerOrders)
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Seller)
                .WithMany(u => u.SellerOrders)
                .HasForeignKey(o => o.SellerId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ============================================================
        // SHIPMENT (Component 3)
        // ============================================================

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.ToTable("Shipments");

            entity.HasKey(s => s.Id);

            entity.Property(s => s.OrderId).IsRequired();
            entity.Property(s => s.SellerId).IsRequired();
            entity.Property(s => s.BuyerId).IsRequired();
            entity.Property(s => s.OriginAddress).IsRequired().HasMaxLength(500);
            entity.Property(s => s.OriginRegion).IsRequired().HasMaxLength(100);
            entity.Property(s => s.OriginCountryCode).IsRequired().HasMaxLength(2);
            entity.Property(s => s.DestinationAddress).IsRequired().HasMaxLength(500);
            entity.Property(s => s.DestinationRegion).IsRequired().HasMaxLength(100);
            entity.Property(s => s.DestinationCountryCode).IsRequired().HasMaxLength(2);
            entity.Property(s => s.DeclaredValue).HasPrecision(18, 2);
            entity.Property(s => s.Currency).IsRequired().HasMaxLength(20);
            entity.Property(s => s.PackageDescription).IsRequired().HasMaxLength(2000);
            entity.Property(s => s.PackageWeight).HasPrecision(10, 2);
            entity.Property(s => s.PackageDimensions).HasMaxLength(200);
            entity.Property(s => s.SpecialHandlingNotes).HasMaxLength(2000);
            entity.Property(s => s.PreferredService).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Status).IsRequired().HasMaxLength(50);
            entity.Property(s => s.RiskLevel).HasMaxLength(50);
            entity.Property(s => s.TrackingNumber).HasMaxLength(200);
            entity.Property(s => s.CourierName).HasMaxLength(200);

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
        // SHIPPING PLAN (Component 3)
        // ============================================================

        modelBuilder.Entity<ShippingPlan>(entity =>
        {
            entity.ToTable("ShippingPlans");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.ShipmentId).IsRequired();
            entity.Property(p => p.RiskLevel).IsRequired().HasMaxLength(50);
            entity.Property(p => p.RiskReasons).HasMaxLength(4000);
            entity.Property(p => p.RecommendedServiceType).IsRequired().HasMaxLength(100);
            entity.Property(p => p.RecommendedCoverageAmount).HasPrecision(18, 2);
            entity.Property(p => p.HandlingRequirements).HasMaxLength(2000);
            entity.Property(p => p.RequiredDocuments).HasMaxLength(2000);
            entity.Property(p => p.Warnings).HasMaxLength(2000);
            entity.Property(p => p.AdminNotes).HasMaxLength(2000);

            entity.HasIndex(p => p.ShipmentId).IsUnique();
            entity.HasIndex(p => p.IsApproved);

            entity.HasOne(p => p.Shipment)
                .WithOne(s => s.ShippingPlan)
                .HasForeignKey<ShippingPlan>(p => p.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // ============================================================
        // SHIPMENT TRACKING EVENT (Component 3)
        // ============================================================

        modelBuilder.Entity<ShipmentTrackingEvent>(entity =>
        {
            entity.ToTable("ShipmentTrackingEvents");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ShipmentId).IsRequired();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);

            entity.HasIndex(e => e.ShipmentId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.OccurredAt);

            entity.HasOne(e => e.Shipment)
                .WithMany(s => s.TrackingEvents)
                .HasForeignKey(e => e.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // ============================================================
        // INSURANCE RECORD (Component 3)
        // ============================================================

        modelBuilder.Entity<InsuranceRecord>(entity =>
        {
            entity.ToTable("InsuranceRecords");

            entity.HasKey(i => i.Id);

            entity.Property(i => i.ShipmentId).IsRequired();
            entity.Property(i => i.CoverageAmount).HasPrecision(18, 2);
            entity.Property(i => i.Currency).IsRequired().HasMaxLength(20);
            entity.Property(i => i.CoverageType).IsRequired().HasMaxLength(50);
            entity.Property(i => i.PolicyNumber).HasMaxLength(200);
            entity.Property(i => i.ProviderName).HasMaxLength(200);
            entity.Property(i => i.Status).IsRequired().HasMaxLength(50);

            entity.HasIndex(i => i.ShipmentId).IsUnique();
            entity.HasIndex(i => i.Status);

            entity.HasOne(i => i.Shipment)
                .WithOne(s => s.InsuranceRecord)
                .HasForeignKey<InsuranceRecord>(i => i.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // ========================================================
        // EXPORT REQUEST
        // ========================================================

        modelBuilder.Entity<ExportRequest>(entity =>
        {
            entity.HasKey(e => e.Id);


            entity.Property(e => e.OriginCountry)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.DestinationCountry)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.DeclaredValue)
                .HasPrecision(18, 2);

            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(e => e.Purpose)
                .HasMaxLength(500);

            entity.Property(e => e.Status)
                .IsRequired();

            entity.Property(e => e.ReviewNotes)
                .HasMaxLength(1000);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();


            // ----------------------------------------------------
            // RELATIONSHIPS
            // ----------------------------------------------------

            entity.HasOne(e => e.RequestedByUser)
                .WithMany()
                .HasForeignKey(e => e.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReviewedByUser)
                .WithMany()
                .HasForeignKey(e => e.ReviewedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.ComplianceDocuments)
                .WithOne(c => c.ExportRequest)
                .HasForeignKey(c => c.ExportRequestId)
                .OnDelete(DeleteBehavior.Cascade);


            // ----------------------------------------------------
            // INDEXES
            // ----------------------------------------------------

            entity.HasIndex(e => e.RequestedByUserId);

            entity.HasIndex(e => e.Status);

            entity.HasIndex(e => e.DestinationCountry);
        });


        // ========================================================
        // COMPLIANCE DOCUMENT
        // ========================================================

        modelBuilder.Entity<ComplianceDocument>(entity =>
        {
            entity.HasKey(c => c.Id);


            entity.Property(c => c.DocumentType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(c => c.DocumentNumber)
                .HasMaxLength(150);

            entity.Property(c => c.Issuer)
                .HasMaxLength(150);

            entity.Property(c => c.FileUrl)
                .HasMaxLength(1000);

            entity.Property(c => c.Status)
                .IsRequired();

            entity.Property(c => c.UploadedAt)
                .IsRequired();


            // ----------------------------------------------------
            // RELATIONSHIPS
            // ----------------------------------------------------

            entity.HasOne(c => c.UploadedByUser)
                .WithMany()
                .HasForeignKey(c => c.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);


            // ----------------------------------------------------
            // INDEXES
            // ----------------------------------------------------

            entity.HasIndex(c => c.ExportRequestId);

            entity.HasIndex(c => c.DocumentType);

            entity.HasIndex(c => c.UploadedByUserId);
        });


        // ========================================================
        // AGENT WORKFLOW
        // ========================================================

        modelBuilder.Entity<AgentWorkflow>(entity =>
        {
            entity.HasKey(w => w.Id);


            entity.Property(w => w.Objective)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(w => w.WorkflowType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(w => w.PlanJson)
                .HasColumnType("jsonb");

            entity.Property(w => w.FinalSummary)
                .HasMaxLength(4000);

            entity.Property(w => w.RootEntityType)
                .HasMaxLength(100);

            entity.Property(w => w.ErrorCode)
                .HasMaxLength(100);

            entity.Property(w => w.ErrorMessage)
                .HasMaxLength(2000);

            entity.Property(w => w.Status)
                .IsRequired();

            entity.Property(w => w.ApprovalStatus)
                .IsRequired();

            entity.Property(w => w.CreatedAt)
                .IsRequired();

            entity.Property(w => w.UpdatedAt)
                .IsRequired();


            // ----------------------------------------------------
            // RELATIONSHIPS
            // ----------------------------------------------------

            entity.HasOne(w => w.TriggeredByUser)
                .WithMany()
                .HasForeignKey(w => w.TriggeredByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(w => w.Steps)
                .WithOne(s => s.Workflow)
                .HasForeignKey(s => s.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);


            // ----------------------------------------------------
            // INDEXES
            // ----------------------------------------------------

            entity.HasIndex(w => w.TriggeredByUserId);

            entity.HasIndex(w => w.Status);

            entity.HasIndex(w => w.WorkflowType);

            entity.HasIndex(w => new
            {
                w.RootEntityType,
                w.RootEntityId
            });
        });


        // ========================================================
        // AGENT WORKFLOW STEP
        // ========================================================

        modelBuilder.Entity<AgentWorkflowStep>(entity =>
        {
            entity.HasKey(s => s.Id);


            entity.Property(s => s.AgentName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(s => s.Action)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(s => s.Status)
                .IsRequired();

            entity.Property(s => s.InputSummaryJson)
                .HasColumnType("jsonb");

            entity.Property(s => s.OutputSummaryJson)
                .HasColumnType("jsonb");

            entity.Property(s => s.ValidationResultJson)
                .HasColumnType("jsonb");

            entity.Property(s => s.ErrorCode)
                .HasMaxLength(100);

            entity.Property(s => s.ErrorMessage)
                .HasMaxLength(2000);


            // ----------------------------------------------------
            // RELATIONSHIPS
            // ----------------------------------------------------

            entity.HasOne(s => s.Workflow)
                .WithMany(w => w.Steps)
                .HasForeignKey(s => s.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(s => s.ToolCalls)
                .WithOne(t => t.WorkflowStep)
                .HasForeignKey(t => t.WorkflowStepId)
                .OnDelete(DeleteBehavior.Cascade);


            // ----------------------------------------------------
            // INDEXES
            // ----------------------------------------------------

            entity.HasIndex(s => s.WorkflowId);

            entity.HasIndex(s => s.Status);

            entity.HasIndex(s => new
            {
                s.WorkflowId,
                s.StepNumber
            })
            .IsUnique();
        });


        // ========================================================
        // AGENT TOOL CALL
        // ========================================================

        modelBuilder.Entity<AgentToolCall>(entity =>
        {
            entity.HasKey(t => t.Id);


            entity.Property(t => t.ToolName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(t => t.AttemptNumber)
                .IsRequired();

            entity.Property(t => t.Succeeded)
                .IsRequired();

            entity.Property(t => t.DurationMs)
                .IsRequired();

            entity.Property(t => t.InputSummaryJson)
                .HasColumnType("jsonb");

            entity.Property(t => t.OutputSummaryJson)
                .HasColumnType("jsonb");

            entity.Property(t => t.ErrorCode)
                .HasMaxLength(100);

            entity.Property(t => t.ErrorMessage)
                .HasMaxLength(2000);

            entity.Property(t => t.CreatedAt)
                .IsRequired();


            // ----------------------------------------------------
            // RELATIONSHIP
            // ----------------------------------------------------

            entity.HasOne(t => t.WorkflowStep)
                .WithMany(s => s.ToolCalls)
                .HasForeignKey(t => t.WorkflowStepId)
                .OnDelete(DeleteBehavior.Cascade);


            // ----------------------------------------------------
            // INDEXES
            // ----------------------------------------------------

            entity.HasIndex(t => t.WorkflowStepId);

            entity.HasIndex(t => t.ToolName);

            entity.HasIndex(t => new
            {
                t.WorkflowStepId,
                t.ToolName,
                t.AttemptNumber
            });
        });
    }

}
