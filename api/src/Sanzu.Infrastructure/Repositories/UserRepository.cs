using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly SanzuDbContext _dbContext;

    public UserRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateAsync(User user, CancellationToken cancellationToken)
    {
        _dbContext.Users.Add(user);
        return Task.CompletedTask;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _dbContext.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _dbContext.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken);
    }

    public Task<bool> ExistsByEmailInOrganizationAsync(Guid organizationId, string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _dbContext.Users.AnyAsync(
            x => x.OrgId == organizationId && x.Email == normalizedEmail,
            cancellationToken);
    }
}
