using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Requests;

public sealed class UpdatePlaybookRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? ChangeNotes { get; init; }
    public PlaybookStatus? Status { get; init; }
}
