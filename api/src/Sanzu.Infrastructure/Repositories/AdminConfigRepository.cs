using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class AdminConfigRepository : IAdminConfigRepository
{
    private readonly SanzuDbContext _context;

    public AdminConfigRepository(SanzuDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AdminPlatformConfig>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.AdminPlatformConfigs
            .AsNoTracking()
            .OrderBy(c => c.ConfigKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminPlatformConfig?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        return await _context.AdminPlatformConfigs
            .FirstOrDefaultAsync(c => c.ConfigKey == key, cancellationToken);
    }

    public async Task UpsertAsync(string key, string value, CancellationToken cancellationToken)
    {
        var existing = await _context.AdminPlatformConfigs
            .FirstOrDefaultAsync(c => c.ConfigKey == key, cancellationToken);

        if (existing is not null)
        {
            existing.ConfigValue = value;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.AdminPlatformConfigs.Add(new AdminPlatformConfig
            {
                Id = Guid.NewGuid(),
                ConfigKey = key,
                ConfigValue = value,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
