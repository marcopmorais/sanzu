using Sanzu.Core.Entities;
using Sanzu.Core.Enums;

namespace Sanzu.Core.Interfaces;

public interface IAdminAlertRepository
{
    Task CreateAsync(AdminAlert alert, CancellationToken cancellationToken);
    Task<AdminAlert?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminAlert>> GetAllAsync(
        AlertStatus? status,
        AlertSeverity? severity,
        string? alertType,
        CancellationToken cancellationToken);
    Task<bool> ExistsFiredAsync(string alertType, Guid? tenantId, CancellationToken cancellationToken);
    Task UpdateAsync(AdminAlert alert, CancellationToken cancellationToken);
}
