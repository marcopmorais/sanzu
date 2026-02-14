using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class WorkflowStepInstance
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    public string StepKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public WorkflowStepStatus Status { get; set; } = WorkflowStepStatus.NotStarted;
    public DateTime? DueDate { get; set; }
    public string? DeadlineSource { get; set; }
    public Guid? AssignedUserId { get; set; }
    public bool IsReadinessOverridden { get; set; }
    public string? ReadinessOverrideRationale { get; set; }
    public Guid? ReadinessOverrideByUserId { get; set; }
    public DateTime? ReadinessOverriddenAt { get; set; }
    public BlockedReasonCode? BlockedReasonCode { get; set; }
    public string? BlockedReasonDetail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Case? Case { get; set; }
    public ICollection<WorkflowStepDependency> Dependencies { get; set; } = new List<WorkflowStepDependency>();
    public ICollection<WorkflowStepDependency> Dependents { get; set; } = new List<WorkflowStepDependency>();
}
