namespace Gemora.Domain.Enums;

public enum AgentWorkflowStatus
{
    Pending = 0,
    Planning = 1,
    Running = 2,
    WaitingForApproval = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6
}
