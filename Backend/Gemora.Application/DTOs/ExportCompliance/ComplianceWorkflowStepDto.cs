namespace Gemora.Application.DTOs.ExportCompliance;

public class ComplianceWorkflowStepDto
{
    public int StepNumber { get; set; }

    public string ActorType { get; set; } = string.Empty;

    public string Actor { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? ErrorCode { get; set; }

    public IReadOnlyList<ComplianceWorkflowToolCallDto> ToolCalls { get; set; }
        = Array.Empty<ComplianceWorkflowToolCallDto>();
}
