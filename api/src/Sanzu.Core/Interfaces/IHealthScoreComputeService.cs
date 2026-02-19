using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface IHealthScoreComputeService
{
    Task ComputeForAllTenantsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<TenantHealthScoreResponse>> GetLatestScoresAsync(CancellationToken cancellationToken);
}
