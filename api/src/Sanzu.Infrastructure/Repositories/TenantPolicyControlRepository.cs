using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class TenantPolicyControlRepository : ITenantPolicyControlRepository
{
    private readonly SanzuDbContext _dbContext;

    public TenantPolicyControlRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TenantPolicyControl?> GetByTenantAndControlAsync(
        Guid tenantId,
        TenantPolicyControlType controlType,
        CancellationToken cancellationToken)
    {
        return _dbContext.TenantPolicyControls.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.ControlType == controlType,
            cancellationToken);
    }

    public Task<bool> IsControlEnabledAsync(
        Guid tenantId,
        TenantPolicyControlType controlType,
        CancellationToken cancellationToken)
    {
        return _dbContext.TenantPolicyControls.AnyAsync(
            x => x.TenantId == tenantId && x.ControlType == controlType && x.IsEnabled,
            cancellationToken);
    }

    public Task CreateAsync(TenantPolicyControl control, CancellationToken cancellationToken)
    {
        _dbContext.TenantPolicyControls.Add(control);
        return Task.CompletedTask;
    }
}
