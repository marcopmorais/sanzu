using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly SanzuDbContext _dbContext;

    public OrganizationRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateAsync(Organization organization, CancellationToken cancellationToken)
    {
        _dbContext.Organizations.Add(organization);
        return Task.CompletedTask;
    }

    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Organizations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Organization>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Organizations
            .IgnoreQueryFilters()
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Organization?> GetByIdForPlatformAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        return _dbContext.Organizations.AnyAsync(x => x.Name == name, cancellationToken);
    }
}
