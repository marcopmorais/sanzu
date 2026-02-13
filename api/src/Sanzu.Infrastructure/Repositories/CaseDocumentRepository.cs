using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class CaseDocumentRepository : ICaseDocumentRepository
{
    private readonly SanzuDbContext _dbContext;

    public CaseDocumentRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateAsync(CaseDocument document, CancellationToken cancellationToken)
    {
        _dbContext.CaseDocuments.Add(document);
        return Task.CompletedTask;
    }

    public Task<CaseDocument?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return _dbContext.CaseDocuments.FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);
    }

    public async Task<IReadOnlyList<CaseDocument>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return await _dbContext.CaseDocuments
            .Where(x => x.CaseId == caseId)
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CaseDocumentVersion>> GetVersionsAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CaseDocumentVersions
            .Where(x => x.DocumentId == documentId)
            .OrderBy(x => x.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    public Task CreateVersionAsync(CaseDocumentVersion version, CancellationToken cancellationToken)
    {
        _dbContext.CaseDocumentVersions.Add(version);
        return Task.CompletedTask;
    }
}
