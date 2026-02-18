using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface IRecoveryPlanService
{
    Task<RecoveryPlanResponse> GenerateRecoveryPlanAsync(
        Guid actorUserId, Guid tenantId, RequestRecoveryPlanRequest request, CancellationToken cancellationToken);

    Task<RecoveryPlanResponse> GeneratePlatformRecoveryPlanAsync(
        Guid actorUserId, Guid tenantId, Guid caseId, CancellationToken cancellationToken);
}
