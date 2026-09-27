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

    public DbSet<AgentWorkflow> AgentWorkflows { get; set; }

    public DbSet<AgentWorkflowStep> AgentWorkflowSteps { get; set; }

    public DbSet<AgentToolCall> AgentToolCalls { get; set; }

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

        // ==========================================
        // AGENT WORKFLOW CONFIGURATION
        // ==========================================

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

            // Relationships
            entity.HasOne(w => w.TriggeredByUser)
                .WithMany()
                .HasForeignKey(w => w.TriggeredByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(w => w.Steps)
                .WithOne(s => s.Workflow)
                .HasForeignKey(s => s.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(w => w.TriggeredByUserId);
            entity.HasIndex(w => w.Status);
            entity.HasIndex(w => w.WorkflowType);
            entity.HasIndex(w => new { w.RootEntityType, w.RootEntityId });
        });

        // ==========================================
        // AGENT WORKFLOW STEP CONFIGURATION
        // ==========================================

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

            // Relationships
            entity.HasOne(s => s.Workflow)
                .WithMany(w => w.Steps)
                .HasForeignKey(s => s.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(s => s.ToolCalls)
                .WithOne(t => t.WorkflowStep)
                .HasForeignKey(t => t.WorkflowStepId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(s => s.WorkflowId);
            entity.HasIndex(s => s.Status);
            entity.HasIndex(s => new { s.WorkflowId, s.StepNumber })
                .IsUnique();
        });

        // ==========================================
        // AGENT TOOL CALL CONFIGURATION
        // ==========================================

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

            // Relationships
            entity.HasOne(t => t.WorkflowStep)
                .WithMany(s => s.ToolCalls)
                .HasForeignKey(t => t.WorkflowStepId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(t => t.WorkflowStepId);
            entity.HasIndex(t => t.ToolName);
            entity.HasIndex(t => new { t.WorkflowStepId, t.ToolName, t.AttemptNumber });
        });
    }
}