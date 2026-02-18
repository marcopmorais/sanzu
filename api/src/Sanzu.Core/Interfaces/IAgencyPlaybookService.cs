using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface IAgencyPlaybookService
{
    Task<IReadOnlyList<PlaybookResponse>> ListAsync(
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<PlaybookResponse> GetByIdAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid playbookId,
        CancellationToken cancellationToken);

    Task<PlaybookResponse> CreateAsync(
        Guid tenantId,
        Guid actorUserId,
        CreatePlaybookRequest request,
        CancellationToken cancellationToken);

    Task<PlaybookResponse> UpdateAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid playbookId,
        UpdatePlaybookRequest request,
        CancellationToken cancellationToken);

    Task<PlaybookResponse> ActivateAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid playbookId,
        CancellationToken cancellationToken);
}
