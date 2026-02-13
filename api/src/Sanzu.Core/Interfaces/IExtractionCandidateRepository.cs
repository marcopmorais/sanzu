using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IExtractionCandidateRepository
{
    Task CreateRangeAsync(IEnumerable<ExtractionCandidate> candidates, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExtractionCandidate>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken);
}
