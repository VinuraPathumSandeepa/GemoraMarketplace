using Gemora.Domain.Enums;

namespace Gemora.Domain.Entities;

public class ComplianceDocument
{
    public Guid Id { get; set; }

    public Guid ExportRequestId { get; set; }

    public Guid UploadedByUserId { get; set; }

    public string DocumentType { get; set; } = string.Empty;

    public string? DocumentNumber { get; set; }

    public string? Issuer { get; set; }

    public DateTime? IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string? FileUrl { get; set; }

    public ComplianceDocumentStatus Status { get; set; } = ComplianceDocumentStatus.Pending;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ExportRequest ExportRequest { get; set; } = null!;

    public User UploadedByUser { get; set; } = null!;
}
