namespace Sanzu.Core.Models.Requests;

public sealed class CreateCaseRequest
{
    public string DeceasedFullName { get; init; } = string.Empty;
    public DateTime DateOfDeath { get; init; }
    public string? CaseType { get; init; }
    public string? Urgency { get; init; }
    public string? Notes { get; init; }
}
