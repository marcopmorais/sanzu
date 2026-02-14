using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface ITenantSubscriptionService
{
    Task<PlanChangePreviewResponse> PreviewPlanChangeAsync(
        Guid tenantId,
        Guid actorUserId,
        PreviewPlanChangeRequest request,
        CancellationToken cancellationToken);

    Task<ChangePlanResponse> ChangePlanAsync(
        Guid tenantId,
        Guid actorUserId,
        ChangePlanRequest request,
        CancellationToken cancellationToken);

    Task<CancelSubscriptionResponse> CancelSubscriptionAsync(
        Guid tenantId,
        Guid actorUserId,
        CancelSubscriptionRequest request,
        CancellationToken cancellationToken);
}
