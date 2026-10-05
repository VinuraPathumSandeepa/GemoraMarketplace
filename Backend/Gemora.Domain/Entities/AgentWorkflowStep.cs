using Gemora.Domain.Enums;

namespace Gemora.Domain.Entities;

public class AgentWorkflowStep
{
    public Guid Id { get; set; }

    public Guid WorkflowId { get; set; }

    public int StepNumber { get; set; }

    public string AgentName { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public AgentWorkflowStepStatus Status { get; set; } = AgentWorkflowStepStatus.Pending;

    public string? InputSummaryJson { get; set; }

    public string? OutputSummaryJson { get; set; }

    public string? ValidationResultJson { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public AgentWorkflow Workflow { get; set; } = null!;

    public ICollection<AgentToolCall> ToolCalls { get; set; } = new List<AgentToolCall>();
}
