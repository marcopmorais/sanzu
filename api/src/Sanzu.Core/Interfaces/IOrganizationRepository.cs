using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IOrganizationRepository
{
    Task CreateAsync(Organization organization, CancellationToken cancellationToken);
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Organization>> GetAllAsync(CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);
    Task<Organization?> GetByIdForPlatformAsync(Guid id, CancellationToken cancellationToken);
}
