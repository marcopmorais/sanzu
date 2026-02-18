using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface IRemediationService
{
    Task<RemediationImpactPreviewResponse> PreviewAsync(
        Guid actorUserId,
        string actionType,
        Guid tenantId,
        CancellationToken cancellationToken);

    Task<RemediationActionResponse> CommitAsync(
        Guid actorUserId,
        CommitRemediationRequest request,
        CancellationToken cancellationToken);

    Task<RemediationActionResponse> VerifyAsync(
        Guid actorUserId,
        Guid remediationId,
        VerifyRemediationRequest request,
        CancellationToken cancellationToken);

    Task<RemediationActionResponse> ResolveAsync(
        Guid actorUserId,
        Guid remediationId,
        ResolveRemediationRequest request,
        CancellationToken cancellationToken);

    Task<RemediationActionResponse> GetByIdAsync(
        Guid actorUserId,
        Guid remediationId,
        CancellationToken cancellationToken);
}
