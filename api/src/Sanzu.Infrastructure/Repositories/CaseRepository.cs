using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class CaseRepository : ICaseRepository
{
    private readonly SanzuDbContext _dbContext;

    public CaseRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateAsync(Case caseEntity, CancellationToken cancellationToken)
    {
        _dbContext.Cases.Add(caseEntity);
        return Task.CompletedTask;
    }

    public Task<Case?> GetByIdAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return _dbContext.Cases.FirstOrDefaultAsync(x => x.Id == caseId, cancellationToken);
    }

    public async Task<IReadOnlyList<Case>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Cases
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextCaseSequenceAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var count = await _dbContext.Cases.CountAsync(x => x.TenantId == tenantId, cancellationToken);
        return count + 1;
    }
}
