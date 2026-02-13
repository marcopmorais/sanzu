namespace Sanzu.Core.Enums;

public enum WorkflowStepStatus
{
    NotStarted = 0,
    Blocked = 1,
    Ready = 2,
    InProgress = 3,
    AwaitingEvidence = 4,
    Complete = 5,
    Skipped = 6,
    Overdue = 7
}
