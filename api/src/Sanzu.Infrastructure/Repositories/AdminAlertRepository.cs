using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class AdminAlertRepository : IAdminAlertRepository
{
    private readonly SanzuDbContext _context;

    public AdminAlertRepository(SanzuDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(AdminAlert alert, CancellationToken cancellationToken)
    {
        _context.AdminAlerts.Add(alert);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminAlert?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.AdminAlerts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminAlert>> GetAllAsync(
        AlertStatus? status,
        AlertSeverity? severity,
        string? alertType,
        CancellationToken cancellationToken)
    {
        var query = _context.AdminAlerts.AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (severity.HasValue)
            query = query.Where(a => a.Severity == severity.Value);

        if (!string.IsNullOrEmpty(alertType))
            query = query.Where(a => a.AlertType == alertType);

        return await query
            .OrderByDescending(a => a.FiredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsFiredAsync(string alertType, Guid? tenantId, CancellationToken cancellationToken)
    {
        return await _context.AdminAlerts
            .AnyAsync(a =>
                a.AlertType == alertType
                && a.TenantId == tenantId
                && a.Status == AlertStatus.Fired,
                cancellationToken);
    }

    public async Task UpdateAsync(AdminAlert alert, CancellationToken cancellationToken)
    {
        _context.AdminAlerts.Update(alert);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
