namespace Gemora.Application.DTOs.ExportCompliance;

public class ComplianceWorkflowExecutionResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }

    public Guid WorkflowId { get; set; }

    public Guid? WorkflowStepId { get; set; }

    public ComplianceAgentContextDto? Context { get; set; }
}
