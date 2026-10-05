using Gemora.Domain.Enums;

namespace Gemora.Domain.Entities;

public class AgentWorkflow
{
    public Guid Id { get; set; }

    public string Objective { get; set; } = string.Empty;

    public string WorkflowType { get; set; } = string.Empty;

    public Guid TriggeredByUserId { get; set; }

    public AgentWorkflowStatus Status { get; set; } = AgentWorkflowStatus.Pending;

    public int CurrentStep { get; set; } = 0;

    public string? PlanJson { get; set; }

    public AgentApprovalStatus ApprovalStatus { get; set; } = AgentApprovalStatus.NotRequired;

    public string? FinalSummary { get; set; }

    public string? RootEntityType { get; set; }

    public Guid? RootEntityId { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public User TriggeredByUser { get; set; } = null!;

    public ICollection<AgentWorkflowStep> Steps { get; set; } = new List<AgentWorkflowStep>();
}
