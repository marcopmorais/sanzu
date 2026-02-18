namespace Sanzu.Core.Models.Requests;

public sealed class CreatePlaybookRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ChangeNotes { get; init; }
}
