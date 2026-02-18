using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface ICopilotDraftService
{
    Task<CopilotDraftResponse> GenerateDraftAsync(
        Guid actorUserId, Guid tenantId, RequestCopilotDraftRequest request, CancellationToken cancellationToken);

    Task<CopilotDraftAcceptedResponse> AcceptDraftAsync(
        Guid actorUserId, Guid tenantId, AcceptCopilotDraftRequest request, CancellationToken cancellationToken);
}
