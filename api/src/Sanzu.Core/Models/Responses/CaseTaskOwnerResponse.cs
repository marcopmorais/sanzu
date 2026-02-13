namespace Sanzu.Core.Models.Responses;

public sealed class CaseTaskOwnerResponse
{
    public Guid StepId { get; init; }
    public string StepKey { get; init; } = string.Empty;
    public Guid? AssignedUserId { get; init; }
}
