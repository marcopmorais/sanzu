namespace Sanzu.Core.Entities;

public sealed class AdminPlatformConfig
{
    public Guid Id { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
