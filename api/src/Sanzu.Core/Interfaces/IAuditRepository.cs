using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IAuditRepository
{
    Task CreateAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEvent>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken);
}
