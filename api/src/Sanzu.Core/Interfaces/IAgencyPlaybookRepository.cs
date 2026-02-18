using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IAgencyPlaybookRepository
{
    Task<AgencyPlaybook?> GetByIdAsync(Guid tenantId, Guid playbookId, CancellationToken cancellationToken);
    Task<AgencyPlaybook?> GetActiveAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgencyPlaybook>> ListAsync(Guid tenantId, CancellationToken cancellationToken);
    Task CreateAsync(AgencyPlaybook playbook, CancellationToken cancellationToken);
    Task UpdateAsync(AgencyPlaybook playbook, CancellationToken cancellationToken);
}
