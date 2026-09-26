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

    public DbSet<GemVerification> GemVerifications =>
        Set<GemVerification>();


    // ============================================================
    // MODEL CONFIGURATION
    // ============================================================

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        // ========================================================
        // USER CONFIGURATION
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

            // Every user must have a unique email address.
            entity.HasIndex(u => u.Email)
                .IsUnique();
        });


        // ========================================================
        // GEM LISTING CONFIGURATION
        // ========================================================

        modelBuilder.Entity<GemListing>(entity =>
        {
            entity.HasKey(g => g.Id);

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
                .IsRequired()
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
                .IsRequired()
                .HasPrecision(18, 2);

            entity.Property(g => g.Currency)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(g => g.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(g => g.CreatedAt)
                .IsRequired();

            // ----------------------------------------------------
            // INDEXES
            // ----------------------------------------------------

            entity.HasIndex(g => g.Status);

            entity.HasIndex(g => g.SellerId);


            // ----------------------------------------------------
            // SELLER RELATIONSHIP
            //
            // User (Seller) 1 -------- * GemListing
            // ----------------------------------------------------

            entity.HasOne(g => g.Seller)
                .WithMany(u => u.GemListings)
                .HasForeignKey(g => g.SellerId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ========================================================
        // GEM VERIFICATION CONFIGURATION
        // ========================================================

        modelBuilder.Entity<GemVerification>(entity =>
        {
            entity.HasKey(v => v.Id);


            // ----------------------------------------------------
            // HUMAN VERIFICATION DATA
            // ----------------------------------------------------

            entity.Property(v => v.Decision)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(v => v.ReviewNotes)
                .HasMaxLength(2000);


            // ----------------------------------------------------
            // AI VERIFICATION DATA
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
            // GEM LISTING RELATIONSHIP
            //
            // GemListing 1 -------- * GemVerification
            //
            // If a GemListing is deleted, its verification
            // history is also deleted.
            // ----------------------------------------------------

            entity.HasOne(v => v.GemListing)
                .WithMany(g => g.Verifications)
                .HasForeignKey(v => v.GemListingId)
                .OnDelete(DeleteBehavior.Cascade);


            // ----------------------------------------------------
            // GEMOLOGIST RELATIONSHIP
            //
            // User (Gemologist) 1 -------- * GemVerification
            //
            // GemologistId is nullable because the verification
            // record can exist before a human reviews it.
            // ----------------------------------------------------

            entity.HasOne(v => v.Gemologist)
                .WithMany(u => u.GemVerifications)
                .HasForeignKey(v => v.GemologistId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}