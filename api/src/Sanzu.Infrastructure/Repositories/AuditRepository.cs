using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class AuditRepository : IAuditRepository
{
    private readonly SanzuDbContext _dbContext;

    public AuditRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        _dbContext.AuditEvents.Add(auditEvent);
        return Task.CompletedTask;
    }
}
