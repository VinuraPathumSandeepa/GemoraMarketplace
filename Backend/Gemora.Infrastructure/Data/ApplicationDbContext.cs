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

    public DbSet<ExportRequest> ExportRequests { get; set; }

    public DbSet<ComplianceDocument> ComplianceDocuments { get; set; }

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
        // EXPORT REQUEST CONFIGURATION
        // ==========================================

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

            // Relationships
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

            // Indexes
            entity.HasIndex(e => e.RequestedByUserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DestinationCountry);
        });

        // ==========================================
        // COMPLIANCE DOCUMENT CONFIGURATION
        // ==========================================

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

            // Relationships
            entity.HasOne(c => c.UploadedByUser)
                .WithMany()
                .HasForeignKey(c => c.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(c => c.ExportRequestId);
            entity.HasIndex(c => c.DocumentType);
            entity.HasIndex(c => c.UploadedByUserId);
        });
    }
}