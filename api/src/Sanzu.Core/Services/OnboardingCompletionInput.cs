using Sanzu.Core.Interfaces;

namespace Sanzu.Core.Services;

public sealed class OnboardingCompletionInput : IHealthScoreInput
{
    private readonly IOrganizationRepository _organizationRepository;

    public OnboardingCompletionInput(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public string Name => "Onboarding";
    public int Weight => 25;

    public async Task<HealthScoreInputResult> ComputeAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var org = await _organizationRepository.GetByIdForPlatformAsync(tenantId, cancellationToken);
        if (org is null)
        {
            return new HealthScoreInputResult { Score = 0 };
        }

        var score = 0;

        if (org.OnboardingCompletedAt is not null)
        {
            score += 80;
        }
        else
        {
            return new HealthScoreInputResult { Score = 20 };
        }

        if (org.SubscriptionActivatedAt is not null)
        {
            score += 10;
        }

        var hasProfile = org.DefaultLocale is not null
                         || org.DefaultTimeZone is not null
                         || org.DefaultCurrency is not null;
        if (hasProfile)
        {
            score += 10;
        }

        return new HealthScoreInputResult { Score = Math.Clamp(score, 0, 100) };
    }
}
