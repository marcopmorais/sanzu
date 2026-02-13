using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface ICaseHandoffRepository
{
    Task CreateAsync(CaseHandoff handoff, CancellationToken cancellationToken);
    Task<CaseHandoff?> GetByIdAsync(Guid handoffId, CancellationToken cancellationToken);
    Task<CaseHandoff?> GetLatestByCaseIdAsync(Guid caseId, CancellationToken cancellationToken);
}
