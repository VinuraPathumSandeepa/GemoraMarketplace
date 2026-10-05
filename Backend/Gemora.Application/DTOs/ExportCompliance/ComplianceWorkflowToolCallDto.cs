namespace Gemora.Application.DTOs.ExportCompliance;

public class ComplianceWorkflowToolCallDto
{
    public string ToolName { get; set; } = string.Empty;

    public int AttemptNumber { get; set; }

    public bool Succeeded { get; set; }

    public long DurationMs { get; set; }

    public string? ErrorCode { get; set; }

    public DateTime CreatedAt { get; set; }
}
