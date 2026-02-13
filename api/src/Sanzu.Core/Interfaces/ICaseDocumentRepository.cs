using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface ICaseDocumentRepository
{
    Task CreateAsync(CaseDocument document, CancellationToken cancellationToken);
    Task<CaseDocument?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CaseDocument>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CaseDocument>> GetByCaseIdForPlatformAsync(Guid caseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CaseDocumentVersion>> GetVersionsAsync(Guid documentId, CancellationToken cancellationToken);
    Task CreateVersionAsync(CaseDocumentVersion version, CancellationToken cancellationToken);
}
