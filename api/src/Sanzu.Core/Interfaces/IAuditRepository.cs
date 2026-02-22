using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IAuditRepository
{
    Task CreateAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEvent>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEvent>> GetByTenantIdInPeriodAsync(Guid tenantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEvent>> GetAllInPeriodAsync(DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken);

    Task<AuditSearchResult> SearchAsync(
        Guid? actorUserId,
        string? eventType,
        Guid? caseId,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken);
}

public sealed class AuditSearchResult
{
    public IReadOnlyList<AuditEvent> Items { get; init; } = [];
    public string? NextCursor { get; init; }
    public int TotalCount { get; init; }
}
