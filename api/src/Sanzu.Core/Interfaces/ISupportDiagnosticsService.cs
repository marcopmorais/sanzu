using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface ISupportDiagnosticsService
{
    Task<SupportDiagnosticSessionResponse> StartDiagnosticSessionAsync(
        Guid tenantId,
        Guid actorUserId,
        StartSupportDiagnosticSessionRequest request,
        CancellationToken cancellationToken);

    Task<SupportDiagnosticSummaryResponse> GetDiagnosticSummaryAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid sessionId,
        CancellationToken cancellationToken);
}
