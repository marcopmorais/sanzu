using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class GlossaryTermResponse
{
    public string Key { get; init; } = string.Empty;
    public string Term { get; init; } = string.Empty;
    public string Definition { get; init; } = string.Empty;
    public string? WhyThisMatters { get; init; }
    public string Locale { get; init; } = "pt-PT";
    public GlossaryVisibility Visibility { get; init; } = GlossaryVisibility.Public;
    public DateTime UpdatedAt { get; init; }
}

