using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class TenantOnboardingCompletionResponse
{
    public Guid TenantId { get; init; }
    public TenantStatus TenantStatus { get; init; }
    public DateTime OnboardingCompletedAt { get; init; }
}
