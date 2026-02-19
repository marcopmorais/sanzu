using Sanzu.Core.Interfaces;

namespace Sanzu.Core.Services;

public sealed class BillingStatusInput : IHealthScoreInput
{
    private readonly IOrganizationRepository _organizationRepository;

    public BillingStatusInput(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public string Name => "Billing";
    public int Weight => 40;

    public async Task<HealthScoreInputResult> ComputeAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var org = await _organizationRepository.GetByIdForPlatformAsync(tenantId, cancellationToken);
        if (org is null)
        {
            return new HealthScoreInputResult { Score = 0 };
        }

        if (org.FailedPaymentAttempts > 0)
        {
            var score = Math.Max(0, 100 - (org.FailedPaymentAttempts * 30));
            return new HealthScoreInputResult
            {
                Score = score,
                FloorCap = 30,
                FloorReason = "BillingFailed"
            };
        }

        if (org.SubscriptionPlan is not null && org.PaymentMethodType is not null)
        {
            return new HealthScoreInputResult { Score = 100 };
        }

        return new HealthScoreInputResult { Score = 50 };
    }
}
