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
    // DATABASE TABLES
    // ============================================================

    public DbSet<User> Users => Set<User>();

    public DbSet<GemListing> GemListings => Set<GemListing>();

    public DbSet<GemVerification> GemVerifications
        => Set<GemVerification>();


    // ============================================================
    // MODEL CONFIGURATION
    // ============================================================

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        // ========================================================
        // USER
        // ========================================================

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

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            entity.HasIndex(u => u.Email)
                .IsUnique();
        });


        // ========================================================
        // GEM LISTING
        // ========================================================

        modelBuilder.Entity<GemListing>(entity =>
        {
            entity.HasKey(g => g.Id);


            // ----------------------------------------------------
            // BASIC GEM INFORMATION
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
            // GEM IMAGE
            // ----------------------------------------------------

            entity.Property(g => g.PrimaryImageUrl)
                .HasMaxLength(1000);


            // ----------------------------------------------------
            // CERTIFICATE / SUPPORTING EVIDENCE
            // ----------------------------------------------------

            entity.Property(g => g.CertificateNumber)
                .HasMaxLength(200);

            entity.Property(g => g.CertificateAuthority)
                .HasMaxLength(200);

            entity.Property(g => g.CertificateUrl)
                .HasMaxLength(1000);


            // ----------------------------------------------------
            // WORKFLOW
            // ----------------------------------------------------

            entity.Property(g => g.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(g => g.CreatedAt)
                .IsRequired();


            // ----------------------------------------------------
            // INDEXES
            // ----------------------------------------------------

            entity.HasIndex(g => g.SellerId);

            entity.HasIndex(g => g.Status);

            // New evidence index
            entity.HasIndex(g => g.CertificateNumber);


            // ----------------------------------------------------
            // SELLER -> GEM LISTINGS
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
            entity.HasKey(v => v.Id);


            // ----------------------------------------------------
            // HUMAN VERIFICATION
            // ----------------------------------------------------

            entity.Property(v => v.Decision)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(v => v.ReviewNotes)
                .HasMaxLength(2000);


            // ----------------------------------------------------
            // AI VERIFICATION
            // ----------------------------------------------------

            entity.Property(v => v.AiSuggestedGemType)
                .HasMaxLength(100);

            entity.Property(v => v.AiConfidenceScore)
                .HasPrecision(5, 2);

            entity.Property(v => v.AiFindings)
                .HasMaxLength(4000);

            entity.Property(v => v.AiRiskFlags)
                .HasMaxLength(2000);

            entity.Property(v => v.AiStatus)
                .IsRequired()
                .HasMaxLength(50);


            // ----------------------------------------------------
            // AUDIT INFORMATION
            // ----------------------------------------------------

            entity.Property(v => v.CreatedAt)
                .IsRequired();


            // ----------------------------------------------------
            // INDEXES
            // ----------------------------------------------------

            entity.HasIndex(v => v.GemListingId);

            entity.HasIndex(v => v.GemologistId);

            entity.HasIndex(v => v.Decision);

            entity.HasIndex(v => v.AiStatus);


            // ----------------------------------------------------
            // GEM LISTING -> VERIFICATIONS
            // ----------------------------------------------------

            entity.HasOne(v => v.GemListing)
                .WithMany(g => g.Verifications)
                .HasForeignKey(v => v.GemListingId)
                .OnDelete(DeleteBehavior.Cascade);


            // ----------------------------------------------------
            // GEMOLOGIST -> VERIFICATIONS
            // ----------------------------------------------------

            entity.HasOne(v => v.Gemologist)
                .WithMany(u => u.GemVerifications)
                .HasForeignKey(v => v.GemologistId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}