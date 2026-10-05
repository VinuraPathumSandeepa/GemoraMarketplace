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

    // ============================================================
    // DB SETS
    // ============================================================

    public DbSet<User> Users => Set<User>();

    public DbSet<GemListing> GemListings => Set<GemListing>();

    public DbSet<GemVerification> GemVerifications
        => Set<GemVerification>();

    public DbSet<EmailVerificationCode> EmailVerificationCodes
        => Set<EmailVerificationCode>();


    // ============================================================
    // COMPONENT 2
    // ============================================================

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderStatusHistory> OrderStatusHistories
        => Set<OrderStatusHistory>();


    public DbSet<PaymentTransaction> PaymentTransactions =>
        Set<PaymentTransaction>();







    // ============================================================
    // MODEL CONFIGURATION
    // ============================================================

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        // ============================================================
        // USER
        // ============================================================

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(u => u.Id);


            // --------------------------------------------------------
            // ID
            // --------------------------------------------------------

            entity.Property(u => u.Id)
                .IsRequired();


            // --------------------------------------------------------
            // PERSONAL INFORMATION
            // --------------------------------------------------------

            entity.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(100);


            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);


            entity.Property(u => u.PasswordHash)
                .IsRequired();


            // --------------------------------------------------------
            // ROLE
            //
            // IMPORTANT:
            // Role is system controlled.
            // Users must not be allowed to edit this through
            // their personal profile.
            // --------------------------------------------------------

            entity.Property(u => u.Role)
                .IsRequired()
                .HasMaxLength(50);


            // --------------------------------------------------------
            // CONTACT / LOCATION
            // --------------------------------------------------------

            entity.Property(u => u.PhoneNumber)
                .HasMaxLength(20);


            entity.Property(u => u.CountryCode)
                .HasMaxLength(2);


            entity.Property(u => u.Region)
                .HasMaxLength(100);


            // --------------------------------------------------------
            // PROFILE IMAGE
            //
            // Nullable because a user may not upload a profile photo.
            // In that case the React UI will display initials.
            //
            // Example:
            // /uploads/profiles/user-guid.webp
            // --------------------------------------------------------

            entity.Property(u => u.ProfileImageUrl)
                .HasMaxLength(500);


            // --------------------------------------------------------
            // EMAIL VERIFICATION
            // --------------------------------------------------------

            entity.Property(u => u.IsEmailVerified)
                .IsRequired();


            entity.Property(u => u.EmailVerifiedAt);


            // --------------------------------------------------------
            // AUDIT
            // --------------------------------------------------------

            entity.Property(u => u.CreatedAt)
                .IsRequired();


            // --------------------------------------------------------
            // INDEXES
            // --------------------------------------------------------

            entity.HasIndex(u => u.Email)
                .IsUnique();


            entity.HasIndex(u => u.PhoneNumber);
        });





        modelBuilder.Entity<PaymentTransaction>(entity =>
{
    entity.ToTable("PaymentTransactions");

    entity.HasKey(p => p.Id);

    entity.Property(p => p.Provider)
        .HasMaxLength(50)
        .IsRequired();

    entity.Property(p => p.ExternalReference)
        .HasMaxLength(100)
        .IsRequired();

    entity.Property(p => p.Amount)
        .HasPrecision(18, 2)
        .IsRequired();

    entity.Property(p => p.Currency)
        .HasMaxLength(10)
        .IsRequired();

    entity.Property(p => p.Status)
        .HasMaxLength(50)
        .IsRequired();

    entity.HasIndex(p => p.ExternalReference)
        .IsUnique();

    entity.HasIndex(p => p.OrderId);

    entity.HasOne(p => p.Order)
        .WithMany()
        .HasForeignKey(p => p.OrderId)
        .OnDelete(DeleteBehavior.Restrict);
});







        // ============================================================
        // GEM LISTING
        // ============================================================

        modelBuilder.Entity<GemListing>(entity =>
        {
            entity.ToTable("GemListings");

            entity.HasKey(g => g.Id);


            // --------------------------------------------------------
            // OWNER
            // --------------------------------------------------------

            entity.Property(g => g.SellerId)
                .IsRequired();


            // --------------------------------------------------------
            // BASIC LISTING INFORMATION
            // --------------------------------------------------------

            entity.Property(g => g.Title)
                .IsRequired()
                .HasMaxLength(150);


            entity.Property(g => g.GemType)
                .IsRequired()
                .HasMaxLength(100);


            entity.Property(g => g.Description)
                .IsRequired()
                .HasMaxLength(2000);


            // --------------------------------------------------------
            // GEM CHARACTERISTICS
            // --------------------------------------------------------

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


            // --------------------------------------------------------
            // PRICE
            // --------------------------------------------------------

            entity.Property(g => g.Price)
                .HasPrecision(18, 2);


            entity.Property(g => g.Currency)
                .IsRequired()
                .HasMaxLength(10);


            // --------------------------------------------------------
            // IMAGE EVIDENCE
            // --------------------------------------------------------

            entity.Property(g => g.PrimaryImageUrl)
                .HasMaxLength(1000);


            // --------------------------------------------------------
            // CERTIFICATE EVIDENCE
            // --------------------------------------------------------

            entity.Property(g => g.CertificateNumber)
                .HasMaxLength(200);


            entity.Property(g => g.CertificateAuthority)
                .HasMaxLength(200);


            entity.Property(g => g.CertificateUrl)
                .HasMaxLength(1000);


            // --------------------------------------------------------
            // LISTING LOCATION
            // --------------------------------------------------------

            entity.Property(g => g.CountryCode)
                .HasMaxLength(2);


            entity.Property(g => g.Region)
                .HasMaxLength(100);


            // --------------------------------------------------------
            // STATUS
            // --------------------------------------------------------

            entity.Property(g => g.Status)
                .IsRequired()
                .HasMaxLength(50);



            // --------------------------------------------------------
            // AUDIT
            // --------------------------------------------------------

            entity.Property(g => g.CreatedAt)
                .IsRequired();


            entity.Property(g => g.UpdatedAt);


            // --------------------------------------------------------
            // INDEXES
            // --------------------------------------------------------

            entity.HasIndex(g => g.SellerId);


            entity.HasIndex(g => g.Status);


            entity.HasIndex(g => g.CertificateNumber);


            entity.HasIndex(g => g.CountryCode);


            entity.HasIndex(g => new
            {
                g.CountryCode,
                g.Region
            });


            // --------------------------------------------------------
            // SELLER RELATIONSHIP
            // --------------------------------------------------------

            entity.HasOne(g => g.Seller)
                .WithMany(u => u.GemListings)
                .HasForeignKey(g => g.SellerId)
                .OnDelete(DeleteBehavior.Restrict);
        });





        // ============================================================
        // GEM VERIFICATION
        //
        // IMPORTANT:
        // These values match the existing database schema.
        // Keep the current configured lengths and delete behaviour.
        // ============================================================

        modelBuilder.Entity<GemVerification>(entity =>
        {
            entity.ToTable("GemVerifications");

            entity.HasKey(v => v.Id);


            // --------------------------------------------------------
            // RELATIONSHIP IDS
            // --------------------------------------------------------

            entity.Property(v => v.GemListingId)
                .IsRequired();


            entity.Property(v => v.GemologistId);


            // --------------------------------------------------------
            // HUMAN REVIEW
            // --------------------------------------------------------

            entity.Property(v => v.Decision)
                .IsRequired()
                .HasMaxLength(50);


            entity.Property(v => v.ReviewNotes)
                .HasMaxLength(2000);


            // --------------------------------------------------------
            // AI WORKFLOW
            // --------------------------------------------------------

            entity.Property(v => v.AiStatus)
                .IsRequired()
                .HasMaxLength(50);


            // KEEP 100 - existing database schema
            entity.Property(v => v.AiSuggestedGemType)
                .HasMaxLength(100);


            entity.Property(v => v.AiConfidenceScore)
                .HasPrecision(5, 2);


            // KEEP 4000 - existing database schema
            entity.Property(v => v.AiFindings)
                .HasMaxLength(4000);


            // KEEP 2000 - existing database schema
            entity.Property(v => v.AiRiskFlags)
                .HasMaxLength(2000);


            // --------------------------------------------------------
            // AUDIT
            // --------------------------------------------------------

            entity.Property(v => v.CreatedAt)
                .IsRequired();


            entity.Property(v => v.AiProcessedAt);


            entity.Property(v => v.ReviewedAt);


            // --------------------------------------------------------
            // INDEXES
            // --------------------------------------------------------

            entity.HasIndex(v => v.GemListingId);


            entity.HasIndex(v => v.GemologistId);


            entity.HasIndex(v => v.Decision);


            entity.HasIndex(v => v.AiStatus);


            // --------------------------------------------------------
            // GEM LISTING RELATIONSHIP
            // --------------------------------------------------------

            entity.HasOne(v => v.GemListing)
                .WithMany(g => g.Verifications)
                .HasForeignKey(v => v.GemListingId)
                .OnDelete(DeleteBehavior.Cascade);


            // --------------------------------------------------------
            // GEMOLOGIST RELATIONSHIP
            //
            // KEEP Restrict - existing database schema
            // --------------------------------------------------------

            entity.HasOne(v => v.Gemologist)
                .WithMany(u => u.GemVerifications)
                .HasForeignKey(v => v.GemologistId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================================================
        // COMPONENT 2 - ORDER STATUS HISTORY
        // ============================================================

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

    entity.HasIndex(h => h.OrderId);
    entity.HasIndex(h => h.ChangedByUserId);
    entity.HasIndex(h => h.CreatedAt);

    entity.HasOne(h => h.Order)
        .WithMany(o => o.StatusHistory)
        .HasForeignKey(h => h.OrderId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(h => h.ChangedByUser)
        .WithMany(u => u.OrderStatusChanges)
        .HasForeignKey(h => h.ChangedByUserId)
        .OnDelete(DeleteBehavior.SetNull);
});

        // ============================================================
        // COMPONENT 2 - ORDERS / TRANSACTIONS
        // ============================================================

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");

            entity.HasKey(o => o.Id);


            // --------------------------------------------------------
            // ID
            // PostgreSQL type = uuid
            // --------------------------------------------------------

            entity.Property(o => o.Id)
                .IsRequired();


            // --------------------------------------------------------
            // RELATIONSHIP IDS
            // --------------------------------------------------------

            entity.Property(o => o.BuyerId)
                .IsRequired();

            entity.Property(o => o.SellerId)
                .IsRequired();


            // GemListingId is nullable in the existing database.
            entity.Property(o => o.GemListingId);


            // --------------------------------------------------------
            // AMOUNT
            // --------------------------------------------------------

            entity.Property(o => o.TotalAmount)
                .IsRequired()
                .HasPrecision(18, 2);


            entity.Property(o => o.Currency)
                .IsRequired()
                .HasMaxLength(20);


            // --------------------------------------------------------
            // STATUS
            // --------------------------------------------------------

            entity.Property(o => o.Status)
                .IsRequired()
                .HasMaxLength(50);


            // --------------------------------------------------------
            // SHIPPING
            // --------------------------------------------------------

            entity.Property(o => o.ShippingAddress)
                .IsRequired()
                .HasMaxLength(500);


            entity.Property(o => o.ShippingRegion)
                .IsRequired()
                .HasMaxLength(100);


            entity.Property(o => o.ShippingCountryCode)
                .IsRequired()
                .HasMaxLength(2);


            // --------------------------------------------------------
            // AUDIT
            // --------------------------------------------------------

            entity.Property(o => o.CreatedAt)
                .IsRequired();

            entity.Property(o => o.UpdatedAt);

            entity.Property(o => o.PaidAt);


            // --------------------------------------------------------
            // INDEXES
            // --------------------------------------------------------

            entity.HasIndex(o => o.BuyerId);

            entity.HasIndex(o => o.SellerId);

            entity.HasIndex(o => o.GemListingId);

            entity.HasIndex(o => o.Status);


            // --------------------------------------------------------
            // BUYER
            // --------------------------------------------------------

            entity.HasOne(o => o.Buyer)
                .WithMany(u => u.BuyerOrders)
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);


            // --------------------------------------------------------
            // SELLER
            // --------------------------------------------------------

            entity.HasOne(o => o.Seller)
                .WithMany(u => u.SellerOrders)
                .HasForeignKey(o => o.SellerId)
                .OnDelete(DeleteBehavior.Restrict);


            // --------------------------------------------------------
            // GEM LISTING
            // --------------------------------------------------------

            entity.HasOne(o => o.GemListing)
                .WithMany(g => g.Orders)
                .HasForeignKey(o => o.GemListingId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ============================================================
        // EMAIL VERIFICATION CODE
        // ============================================================

        modelBuilder.Entity<EmailVerificationCode>(entity =>
        {
            entity.ToTable("EmailVerificationCodes");

            entity.HasKey(e => e.Id);


            // --------------------------------------------------------
            // IDENTIFIERS
            // --------------------------------------------------------

            entity.Property(e => e.Id)
                .IsRequired();


            entity.Property(e => e.UserId)
                .IsRequired();


            // --------------------------------------------------------
            // OTP SECURITY
            //
            // Only the hash of the verification code is stored.
            // --------------------------------------------------------

            entity.Property(e => e.CodeHash)
                .IsRequired()
                .HasMaxLength(255);


            // --------------------------------------------------------
            // OTP STATE
            // --------------------------------------------------------

            entity.Property(e => e.ExpiresAt)
                .IsRequired();


            entity.Property(e => e.AttemptCount)
                .IsRequired();


            entity.Property(e => e.UsedAt);


            // --------------------------------------------------------
            // AUDIT
            // --------------------------------------------------------

            entity.Property(e => e.CreatedAt)
                .IsRequired();


            // --------------------------------------------------------
            // INDEXES
            // --------------------------------------------------------

            entity.HasIndex(e => e.UserId);


            entity.HasIndex(e => new
            {
                e.UserId,
                e.CreatedAt
            });


            // --------------------------------------------------------
            // USER RELATIONSHIP
            // --------------------------------------------------------

            entity.HasOne(e => e.User)
                .WithMany(u => u.EmailVerificationCodes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}