using Sanzu.Core.Enums;
using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IUserRoleRepository
{
    Task CreateAsync(UserRole userRole, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserRole>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> HasRoleAsync(Guid userId, Guid tenantId, PlatformRole roleType, CancellationToken cancellationToken);
}
