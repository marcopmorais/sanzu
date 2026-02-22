using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class AuditRepository : IAuditRepository
{
    private readonly SanzuDbContext _dbContext;

    public AuditRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        _dbContext.AuditEvents.Add(auditEvent);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<AuditEvent>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return await _dbContext.AuditEvents
            .Where(x => x.CaseId == caseId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEvent>> GetByTenantIdInPeriodAsync(
        Guid tenantId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken)
    {
        return await _dbContext.AuditEvents
            .Join(
                _dbContext.Cases.IgnoreQueryFilters().Where(c => c.TenantId == tenantId),
                audit => audit.CaseId,
                caseEntity => caseEntity.Id,
                (audit, _) => audit)
            .Where(x => x.CreatedAt >= periodStart && x.CreatedAt < periodEnd)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEvent>> GetAllInPeriodAsync(
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken)
    {
        return await _dbContext.AuditEvents
            .Where(x => x.CreatedAt >= periodStart && x.CreatedAt < periodEnd)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<AuditSearchResult> SearchAsync(
        Guid? actorUserId,
        string? eventType,
        Guid? caseId,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.AuditEvents.AsNoTracking();

        if (actorUserId.HasValue)
            query = query.Where(x => x.ActorUserId == actorUserId.Value);

        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(x => x.EventType.Contains(eventType));

        if (caseId.HasValue)
            query = query.Where(x => x.CaseId == caseId.Value);

        if (dateFrom.HasValue)
            query = query.Where(x => x.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.CreatedAt <= dateTo.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var offset = 0;
        if (!string.IsNullOrEmpty(cursor) && TryDecodeOffsetCursor(cursor, out var cursorOffset))
            offset = cursorOffset;

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(offset)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        string? nextCursor = null;
        if (items.Count > pageSize)
        {
            items = items.Take(pageSize).ToList();
            nextCursor = EncodeOffsetCursor(offset + pageSize);
        }

        return new AuditSearchResult
        {
            Items = items,
            NextCursor = nextCursor,
            TotalCount = totalCount
        };
    }

    internal static string EncodeOffsetCursor(int offset)
    {
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(offset.ToString(CultureInfo.InvariantCulture)));
    }

    internal static bool TryDecodeOffsetCursor(string cursor, out int offset)
    {
        offset = 0;
        try
        {
            var raw = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            return int.TryParse(raw, CultureInfo.InvariantCulture, out offset);
        }
        catch
        {
            return false;
        }
    }
}
