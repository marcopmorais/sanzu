using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Requests;

public sealed class UpsertGlossaryTermRequest
{
    public string Term { get; init; } = string.Empty;
    public string Definition { get; init; } = string.Empty;
    public string? WhyThisMatters { get; init; }
    public string? Locale { get; init; }
    public GlossaryVisibility Visibility { get; init; } = GlossaryVisibility.Public;
}

