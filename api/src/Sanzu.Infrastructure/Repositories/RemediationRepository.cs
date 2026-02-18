using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class RemediationRepository : IRemediationRepository
{
    private readonly SanzuDbContext _dbContext;

    public RemediationRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateAsync(RemediationAction action, CancellationToken cancellationToken)
    {
        _dbContext.RemediationActions.Add(action);
        return Task.CompletedTask;
    }

    public Task<RemediationAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.RemediationActions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
