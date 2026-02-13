namespace Sanzu.Core.Entities;

public sealed class WorkflowStepDependency
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    public Guid StepId { get; set; }
    public Guid DependsOnStepId { get; set; }
    public DateTime CreatedAt { get; set; }

    public WorkflowStepInstance? Step { get; set; }
    public WorkflowStepInstance? DependsOnStep { get; set; }
}
