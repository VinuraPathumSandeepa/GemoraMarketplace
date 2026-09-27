namespace Gemora.Domain.Entities;

public class AgentToolCall
{
    public Guid Id { get; set; }

    public Guid WorkflowStepId { get; set; }

    public string ToolName { get; set; } = string.Empty;

    public int AttemptNumber { get; set; } = 1;

    public string? InputSummaryJson { get; set; }

    public string? OutputSummaryJson { get; set; }

    public bool Succeeded { get; set; }

    public long DurationMs { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public AgentWorkflowStep WorkflowStep { get; set; } = null!;
}
