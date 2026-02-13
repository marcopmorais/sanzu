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
}
