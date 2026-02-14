using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IBillingRecordRepository
{
    Task CreateAsync(BillingRecord record, CancellationToken cancellationToken);
    Task<IReadOnlyList<BillingRecord>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<BillingRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<int> GetNextInvoiceNumberAsync(Guid tenantId, CancellationToken cancellationToken);
}
