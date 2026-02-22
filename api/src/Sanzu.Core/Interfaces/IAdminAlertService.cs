using Sanzu.Core.Entities;
using Sanzu.Core.Enums;

namespace Sanzu.Core.Interfaces;

public interface IAdminAlertService
{
    Task EvaluateAlertRulesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminAlert>> GetAlertsAsync(
        AlertStatus? status,
        AlertSeverity? severity,
        string? alertType,
        CancellationToken cancellationToken);
    Task<AdminAlert?> GetAlertByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AcknowledgeAlertAsync(Guid alertId, Guid actorUserId, CancellationToken cancellationToken);
    Task ResolveAlertAsync(Guid alertId, Guid actorUserId, CancellationToken cancellationToken);
}
