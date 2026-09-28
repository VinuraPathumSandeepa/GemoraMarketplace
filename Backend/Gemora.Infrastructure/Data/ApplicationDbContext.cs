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

    public DbSet<User> Users => Set<User>();

    public DbSet<GemListing> GemListings => Set<GemListing>();

    public DbSet<GemVerification> GemVerifications
        => Set<GemVerification>();

    public DbSet<EmailVerificationCode> EmailVerificationCodes
        => Set<EmailVerificationCode>();

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

            entity.Property(u => u.Id)
                .IsRequired();

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

            // New contact/location fields
            entity.Property(u => u.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(u => u.CountryCode)
                .HasMaxLength(2);

            entity.Property(u => u.Region)
                .HasMaxLength(100);

            entity.Property(u => u.IsEmailVerified)
                .IsRequired();

            entity.Property(u => u.EmailVerifiedAt);

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.HasIndex(u => u.PhoneNumber);
        });

        // ============================================================
        // GEM LISTING
        // ============================================================

        modelBuilder.Entity<GemListing>(entity =>
        {
            entity.ToTable("GemListings");

            entity.HasKey(g => g.Id);

            entity.Property(g => g.SellerId)
                .IsRequired();

            entity.Property(g => g.Title)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(g => g.GemType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(g => g.Description)
                .IsRequired()
                .HasMaxLength(2000);

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

            entity.Property(g => g.Price)
                .HasPrecision(18, 2);

            entity.Property(g => g.Currency)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(g => g.PrimaryImageUrl)
                .HasMaxLength(1000);

            entity.Property(g => g.CertificateNumber)
                .HasMaxLength(200);

            entity.Property(g => g.CertificateAuthority)
                .HasMaxLength(200);

            entity.Property(g => g.CertificateUrl)
                .HasMaxLength(1000);

            // New listing location fields
            entity.Property(g => g.CountryCode)
                .HasMaxLength(2);

            entity.Property(g => g.Region)
                .HasMaxLength(100);

            entity.Property(g => g.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(g => g.CreatedAt)
                .IsRequired();

            entity.Property(g => g.UpdatedAt);

            entity.HasIndex(g => g.SellerId);

            entity.HasIndex(g => g.Status);

            entity.HasIndex(g => g.CertificateNumber);

            // New location indexes
            entity.HasIndex(g => g.CountryCode);

            entity.HasIndex(g => new
            {
                g.CountryCode,
                g.Region
            });

            entity.HasOne(g => g.Seller)
                .WithMany(u => u.GemListings)
                .HasForeignKey(g => g.SellerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================================================
        // GEM VERIFICATION
        //
        // IMPORTANT:
        // These values match your EXISTING database schema.
        // Do not change them as part of the OTP/location migration.
        // ============================================================

        modelBuilder.Entity<GemVerification>(entity =>
        {
            entity.ToTable("GemVerifications");

            entity.HasKey(v => v.Id);

            entity.Property(v => v.GemListingId)
                .IsRequired();

            entity.Property(v => v.GemologistId);

            entity.Property(v => v.Decision)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(v => v.ReviewNotes)
                .HasMaxLength(2000);

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

            entity.Property(v => v.CreatedAt)
                .IsRequired();

            entity.Property(v => v.AiProcessedAt);

            entity.Property(v => v.ReviewedAt);

            entity.HasIndex(v => v.GemListingId);

            entity.HasIndex(v => v.GemologistId);

            entity.HasIndex(v => v.Decision);

            entity.HasIndex(v => v.AiStatus);

            entity.HasOne(v => v.GemListing)
                .WithMany(g => g.Verifications)
                .HasForeignKey(v => v.GemListingId)
                .OnDelete(DeleteBehavior.Cascade);

            // KEEP Restrict - existing database schema
            entity.HasOne(v => v.Gemologist)
                .WithMany(u => u.GemVerifications)
                .HasForeignKey(v => v.GemologistId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================================================
        // EMAIL VERIFICATION CODE
        // ============================================================

        modelBuilder.Entity<EmailVerificationCode>(entity =>
        {
            entity.ToTable("EmailVerificationCodes");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .IsRequired();

            entity.Property(e => e.UserId)
                .IsRequired();

            // Only the hash of the OTP is stored.
            entity.Property(e => e.CodeHash)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.ExpiresAt)
                .IsRequired();

            entity.Property(e => e.AttemptCount)
                .IsRequired();

            entity.Property(e => e.UsedAt);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasIndex(e => e.UserId);

            entity.HasIndex(e => new
            {
                e.UserId,
                e.CreatedAt
            });

            entity.HasOne(e => e.User)
                .WithMany(u => u.EmailVerificationCodes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}