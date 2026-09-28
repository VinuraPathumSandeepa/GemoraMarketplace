namespace Gemora.Application.DTOs.ExportCompliance;

public class OfficerExportRequestResponseDto
{
    public Guid Id { get; set; }

    public Guid RequestedByUserId { get; set; }

    public string RequesterName { get; set; } = string.Empty;

    public string RequesterEmail { get; set; } = string.Empty;

    public string OriginCountry { get; set; } = string.Empty;

    public string DestinationCountry { get; set; } = string.Empty;

    public decimal DeclaredValue { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string? Purpose { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ReviewNotes { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public IReadOnlyList<ComplianceDocumentResponseDto> Documents { get; set; }
        = new List<ComplianceDocumentResponseDto>();

    public ComplianceCheckResultDto? DeterministicCompliance { get; set; }

    public ComplianceWorkflowReviewDto? AgentWorkflow { get; set; }
}
