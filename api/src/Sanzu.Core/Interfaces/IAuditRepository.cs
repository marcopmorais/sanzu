using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IAuditRepository
{
    Task CreateAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEvent>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEvent>> GetByTenantIdInPeriodAsync(Guid tenantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEvent>> GetAllInPeriodAsync(DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken);
}
