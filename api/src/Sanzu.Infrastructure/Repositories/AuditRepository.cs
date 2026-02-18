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

    public async Task<IReadOnlyList<AuditEvent>> GetByTenantIdInPeriodAsync(
        Guid tenantId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken)
    {
        return await _dbContext.AuditEvents
            .Join(
                _dbContext.Cases.IgnoreQueryFilters().Where(c => c.TenantId == tenantId),
                audit => audit.CaseId,
                caseEntity => caseEntity.Id,
                (audit, _) => audit)
            .Where(x => x.CreatedAt >= periodStart && x.CreatedAt < periodEnd)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEvent>> GetAllInPeriodAsync(
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken)
    {
        return await _dbContext.AuditEvents
            .Where(x => x.CreatedAt >= periodStart && x.CreatedAt < periodEnd)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
