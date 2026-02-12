using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IAuditRepository
{
    Task CreateAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}
