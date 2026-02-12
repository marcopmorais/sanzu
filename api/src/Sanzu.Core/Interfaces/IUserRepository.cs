using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IUserRepository
{
    Task CreateAsync(User user, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailInOrganizationAsync(Guid organizationId, string email, CancellationToken cancellationToken);
}
