using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class GlossaryTerm
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Term { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public string? WhyThisMatters { get; set; }
    public string Locale { get; set; } = "pt-PT";
    public GlossaryVisibility Visibility { get; set; } = GlossaryVisibility.Public;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

