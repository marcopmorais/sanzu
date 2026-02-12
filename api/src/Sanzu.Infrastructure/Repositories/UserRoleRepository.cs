using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class UserRoleRepository : IUserRoleRepository
{
    private readonly SanzuDbContext _dbContext;

    public UserRoleRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateAsync(UserRole userRole, CancellationToken cancellationToken)
    {
        _dbContext.UserRoles.Add(userRole);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<UserRole>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.UserRoles
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.GrantedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasRoleAsync(Guid userId, Guid tenantId, PlatformRole roleType, CancellationToken cancellationToken)
    {
        return _dbContext.UserRoles.AnyAsync(
            x => x.UserId == userId && x.TenantId == tenantId && x.RoleType == roleType,
            cancellationToken);
    }
}
