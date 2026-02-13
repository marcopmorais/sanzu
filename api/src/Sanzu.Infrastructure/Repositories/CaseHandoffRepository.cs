using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class CaseHandoffRepository : ICaseHandoffRepository
{
    private readonly SanzuDbContext _dbContext;

    public CaseHandoffRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateAsync(CaseHandoff handoff, CancellationToken cancellationToken)
    {
        _dbContext.CaseHandoffs.Add(handoff);
        return Task.CompletedTask;
    }

    public Task<CaseHandoff?> GetByIdAsync(Guid handoffId, CancellationToken cancellationToken)
    {
        return _dbContext.CaseHandoffs.FirstOrDefaultAsync(x => x.Id == handoffId, cancellationToken);
    }

    public Task<CaseHandoff?> GetLatestByCaseIdAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return _dbContext.CaseHandoffs
            .Where(x => x.CaseId == caseId)
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
