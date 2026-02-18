using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface IAdminQueueService
{
    Task<AdminQueueListResponse> ListQueuesAsync(
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<AdminQueueItemsResponse> GetQueueItemsAsync(
        Guid actorUserId,
        string queueId,
        CancellationToken cancellationToken);

    Task<AdminEventStreamResponse> GetTenantEventStreamAsync(
        Guid actorUserId,
        Guid tenantId,
        int limit,
        CancellationToken cancellationToken);
}
