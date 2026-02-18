using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IRemediationRepository
{
    Task CreateAsync(RemediationAction action, CancellationToken cancellationToken);
    Task<RemediationAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
