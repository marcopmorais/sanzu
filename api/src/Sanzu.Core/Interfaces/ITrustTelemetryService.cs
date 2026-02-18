using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface ITrustTelemetryService
{
    Task<TrustTelemetryResponse> GetTenantTelemetryAsync(
        Guid tenantId,
        Guid actorUserId,
        int periodDays,
        CancellationToken cancellationToken);

    Task<TrustTelemetryResponse> GetPlatformTelemetryAsync(
        Guid actorUserId,
        int periodDays,
        CancellationToken cancellationToken);
}
