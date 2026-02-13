using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class ProcessAliasRepository : IProcessAliasRepository
{
    private readonly SanzuDbContext _dbContext;

    public ProcessAliasRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateAsync(ProcessAlias alias, CancellationToken cancellationToken)
    {
        _dbContext.ProcessAliases.Add(alias);
        return Task.CompletedTask;
    }

    public Task<ProcessAlias?> GetLatestByCaseIdAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return _dbContext.ProcessAliases
            .Where(x => x.CaseId == caseId)
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ProcessAlias?> GetCurrentByCaseIdAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return _dbContext.ProcessAliases
            .Where(
                x => x.CaseId == caseId
                     && (x.Status == ProcessAliasStatus.Active || x.Status == ProcessAliasStatus.Deactivated))
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ExistsByAliasEmailAsync(string aliasEmail, CancellationToken cancellationToken)
    {
        var normalized = aliasEmail.Trim().ToLowerInvariant();
        return _dbContext.ProcessAliases.AnyAsync(x => x.AliasEmail == normalized, cancellationToken);
    }
}
