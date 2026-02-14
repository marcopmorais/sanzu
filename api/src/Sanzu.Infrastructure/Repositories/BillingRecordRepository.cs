using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class BillingRecordRepository : IBillingRecordRepository
{
    private readonly SanzuDbContext _dbContext;

    public BillingRecordRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateAsync(BillingRecord record, CancellationToken cancellationToken)
    {
        _dbContext.BillingRecords.Add(record);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<BillingRecord>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.BillingRecords
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<BillingRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.BillingRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<int> GetNextInvoiceNumberAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var count = await _dbContext.BillingRecords
            .CountAsync(x => x.TenantId == tenantId, cancellationToken);

        return count + 1;
    }
}
