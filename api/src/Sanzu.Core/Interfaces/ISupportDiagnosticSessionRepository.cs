using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface ISupportDiagnosticSessionRepository
{
    Task CreateAsync(SupportDiagnosticSession session, CancellationToken cancellationToken);
    Task<SupportDiagnosticSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<int> CountStartedSinceAsync(Guid tenantId, DateTime sinceUtc, CancellationToken cancellationToken);
}
