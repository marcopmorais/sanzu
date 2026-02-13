using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class ExtractionCandidateRepository : IExtractionCandidateRepository
{
    private readonly SanzuDbContext _dbContext;

    public ExtractionCandidateRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateRangeAsync(IEnumerable<ExtractionCandidate> candidates, CancellationToken cancellationToken)
    {
        _dbContext.ExtractionCandidates.AddRange(candidates);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ExtractionCandidate>> GetByDocumentIdAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ExtractionCandidates
            .Where(x => x.DocumentId == documentId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
