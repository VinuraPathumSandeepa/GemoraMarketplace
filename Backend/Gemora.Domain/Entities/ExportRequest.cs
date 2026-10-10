using Gemora.Domain.Enums;

namespace Gemora.Domain.Entities;

public class ExportRequest
{
    public Guid Id { get; set; }

    public Guid RequestedByUserId { get; set; }

    public string OriginCountry { get; set; } = string.Empty;

    public string DestinationCountry { get; set; } = string.Empty;

    public decimal DeclaredValue { get; set; }

    public string Currency { get; set; } = "USD";

    public string? Purpose { get; set; }

    public ExportRequestStatus Status { get; set; } = ExportRequestStatus.Draft;

    public Guid? ReviewedByUserId { get; set; }

    public string? ReviewNotes { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User RequestedByUser { get; set; } = null!;

    public User? ReviewedByUser { get; set; }

    public ICollection<ComplianceDocument> ComplianceDocuments { get; set; } = new List<ComplianceDocument>();
}
