using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface IGlossaryService
{
    Task<GlossaryLookupResponse> SearchAsync(
        Guid tenantId,
        Guid actorUserId,
        string? query,
        string? locale,
        CancellationToken cancellationToken);

    Task<GlossaryTermResponse> GetTermAsync(
        Guid tenantId,
        Guid actorUserId,
        string key,
        string? locale,
        CancellationToken cancellationToken);

    Task<GlossaryTermResponse> UpsertAsync(
        Guid tenantId,
        Guid actorUserId,
        string key,
        UpsertGlossaryTermRequest request,
        CancellationToken cancellationToken);
}

