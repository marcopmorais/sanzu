using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public TenantStatus Status { get; set; } = TenantStatus.Pending;
    public DateTime? OnboardingCompletedAt { get; set; }
    public string? DefaultLocale { get; set; }
    public string? DefaultTimeZone { get; set; }
    public string? DefaultCurrency { get; set; }
    public string? DefaultWorkflowKey { get; set; }
    public string? DefaultTemplateKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<TenantInvitation> TenantInvitations { get; set; } = new List<TenantInvitation>();
}
