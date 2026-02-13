using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface ICaseRepository
{
    Task CreateAsync(Case caseEntity, CancellationToken cancellationToken);
    Task<Case?> GetByIdAsync(Guid caseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Case>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<int> GetNextCaseSequenceAsync(Guid tenantId, CancellationToken cancellationToken);
}
