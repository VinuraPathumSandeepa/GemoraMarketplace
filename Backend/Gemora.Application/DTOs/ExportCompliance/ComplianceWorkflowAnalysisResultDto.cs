namespace Gemora.Application.DTOs.ExportCompliance;

public class ComplianceWorkflowAnalysisResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }

    public Guid WorkflowId { get; set; }

    public Guid? AgentStepId { get; set; }

    public Guid? ValidationStepId { get; set; }

    public ComplianceAgentResultDto? Assessment { get; set; }
}
