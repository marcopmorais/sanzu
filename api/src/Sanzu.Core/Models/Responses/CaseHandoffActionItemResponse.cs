namespace Sanzu.Core.Models.Responses;

public sealed class CaseHandoffActionItemResponse
{
    public Guid StepId { get; init; }
    public string StepKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int Sequence { get; init; }
    public DateTime? DueDate { get; init; }
    public Guid? AssignedUserId { get; init; }
}
