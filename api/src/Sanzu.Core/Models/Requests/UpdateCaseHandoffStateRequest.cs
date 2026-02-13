namespace Sanzu.Core.Models.Requests;

public sealed class UpdateCaseHandoffStateRequest
{
    public string Status { get; init; } = string.Empty;
    public string? Notes { get; init; }
}
