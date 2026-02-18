namespace Sanzu.Core.Models.Requests;

public sealed class RequestRecoveryPlanRequest
{
    public Guid CaseId { get; set; }
    public Guid? WorkflowStepId { get; set; }
}
