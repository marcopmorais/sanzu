namespace Sanzu.Core.Interfaces;

public interface IHealthScoreInput
{
    string Name { get; }
    int Weight { get; }
    Task<HealthScoreInputResult> ComputeAsync(Guid tenantId, CancellationToken cancellationToken);
}

public sealed class HealthScoreInputResult
{
    public int Score { get; init; }
    public int? FloorCap { get; init; }
    public string? FloorReason { get; init; }
}
