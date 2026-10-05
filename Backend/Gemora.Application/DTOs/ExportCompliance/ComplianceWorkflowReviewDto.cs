namespace Gemora.Application.DTOs.ExportCompliance;

public class ComplianceWorkflowReviewDto
{
    public Guid WorkflowId { get; set; }

    public string WorkflowStatus { get; set; } = string.Empty;

    public string ApprovalStatus { get; set; } = string.Empty;

    public int CurrentStep { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? FinalSummary { get; set; }

    public ComplianceAgentResultDto? Assessment { get; set; }

    public IReadOnlyList<ComplianceWorkflowStepDto> Steps { get; set; }
        = Array.Empty<ComplianceWorkflowStepDto>();
}
