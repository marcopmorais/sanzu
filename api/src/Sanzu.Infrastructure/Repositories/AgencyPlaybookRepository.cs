using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class AgencyPlaybookRepository : IAgencyPlaybookRepository
{
    private readonly SanzuDbContext _dbContext;

    public AgencyPlaybookRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AgencyPlaybook?> GetByIdAsync(
        Guid tenantId,
        Guid playbookId,
        CancellationToken cancellationToken)
    {
        return _dbContext.AgencyPlaybooks
            .Where(x => x.TenantId == tenantId && x.Id == playbookId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<AgencyPlaybook?> GetActiveAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return _dbContext.AgencyPlaybooks
            .Where(x => x.TenantId == tenantId && x.Status == PlaybookStatus.Active)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AgencyPlaybook>> ListAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.AgencyPlaybooks
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(AgencyPlaybook playbook, CancellationToken cancellationToken)
    {
        _dbContext.AgencyPlaybooks.Add(playbook);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AgencyPlaybook playbook, CancellationToken cancellationToken)
    {
        _dbContext.AgencyPlaybooks.Update(playbook);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
