using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface ICaseDocumentRepository
{
    Task CreateAsync(CaseDocument document, CancellationToken cancellationToken);
    Task<CaseDocument?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken);
}
