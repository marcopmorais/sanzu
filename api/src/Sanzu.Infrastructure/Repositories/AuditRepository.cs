using Microsoft.EntityFrameworkCore;
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

    public async Task<IReadOnlyList<AuditEvent>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return await _dbContext.AuditEvents
            .Where(x => x.CaseId == caseId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
