using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IAdminConfigRepository
{
    Task<IReadOnlyList<AdminPlatformConfig>> GetAllAsync(CancellationToken cancellationToken);
    Task<AdminPlatformConfig?> GetByKeyAsync(string key, CancellationToken cancellationToken);
    Task UpsertAsync(string key, string value, CancellationToken cancellationToken);
}
