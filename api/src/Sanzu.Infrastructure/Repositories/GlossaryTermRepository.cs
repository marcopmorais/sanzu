using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class GlossaryTermRepository : IGlossaryTermRepository
{
    private readonly SanzuDbContext _dbContext;

    public GlossaryTermRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<GlossaryTerm?> GetByKeyAsync(
        Guid tenantId,
        string key,
        string locale,
        CancellationToken cancellationToken)
    {
        return _dbContext.GlossaryTerms
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Key == key && x.Locale == locale)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GlossaryTerm>> SearchAsync(
        Guid tenantId,
        string query,
        string locale,
        CancellationToken cancellationToken)
    {
        // Simple contains matching; can be upgraded to full-text later.
        var normalized = query.Trim();

        return await _dbContext.GlossaryTerms
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Locale == locale)
            .Where(x => x.Key.Contains(normalized) || x.Term.Contains(normalized))
            .OrderBy(x => x.Term)
            .Take(25)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(GlossaryTerm term, CancellationToken cancellationToken)
    {
        _dbContext.GlossaryTerms.Add(term);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(GlossaryTerm term, CancellationToken cancellationToken)
    {
        _dbContext.GlossaryTerms.Update(term);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

