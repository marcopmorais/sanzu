using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class ProcessEmailRepository : IProcessEmailRepository
{
    private readonly SanzuDbContext _dbContext;

    public ProcessEmailRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateAsync(ProcessEmail email, CancellationToken cancellationToken)
    {
        _dbContext.ProcessEmails.Add(email);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ProcessEmail>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return await _dbContext.ProcessEmails
            .Where(x => x.CaseId == caseId)
            .OrderByDescending(x => x.SentAt)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
