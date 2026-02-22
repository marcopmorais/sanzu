using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
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

    public async Task<(IReadOnlyList<Organization> Items, int TotalCount)> SearchForPlatformAsync(
        TenantListRequest request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Organizations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var nameLower = request.Name.ToLowerInvariant();
            query = query.Where(o => o.Name.ToLower().Contains(nameLower));
        }

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<TenantStatus>(request.Status, ignoreCase: true, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.PlanTier))
        {
            query = query.Where(o => o.SubscriptionPlan == request.PlanTier);
        }

        if (request.SignupDateFrom.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= request.SignupDateFrom.Value);
        }

        if (request.SignupDateTo.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= request.SignupDateTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = query.OrderByDescending(o => o.CreatedAt);

        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var page = Math.Max(request.Page, 1);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
