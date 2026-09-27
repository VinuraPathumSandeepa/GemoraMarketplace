namespace Gemora.Application.DTOs.ExportCompliance;

public class ComplianceDocumentResponseDto
{
    public Guid Id { get; set; }

    public Guid ExportRequestId { get; set; }

    public string DocumentType { get; set; } = string.Empty;

    public string? DocumentNumber { get; set; }

    public string? Issuer { get; set; }

    public DateTime? IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string? FileUrl { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }
}
