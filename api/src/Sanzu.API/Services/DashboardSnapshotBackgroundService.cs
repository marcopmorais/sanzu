using Sanzu.Core.Interfaces;

namespace Sanzu.API.Services;

public sealed class DashboardSnapshotBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DashboardSnapshotBackgroundService> _logger;
    private readonly TimeSpan _interval;

    public DashboardSnapshotBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DashboardSnapshotBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var intervalMinutes = configuration.GetValue("DashboardSnapshot:IntervalMinutes", 10);
        _interval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "DashboardSnapshotBackgroundService starting with interval {Interval}",
            _interval);

        // Run immediately on startup (satisfies AC1)
        await RunSnapshotAsync(stoppingToken);

        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunSnapshotAsync(stoppingToken);
        }
    }

    private async Task RunSnapshotAsync(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
            return;

        try
        {
            _logger.LogInformation("DashboardSnapshot computation started at {Time}", DateTime.UtcNow);

            using var scope = _scopeFactory.CreateScope();
            var snapshotService = scope.ServiceProvider.GetRequiredService<IDashboardSnapshotService>();
            var repository = scope.ServiceProvider.GetRequiredService<IDashboardSnapshotRepository>();

            var snapshot = await snapshotService.ComputeSnapshotAsync(stoppingToken);

            var isStale = await snapshotService.IsStaleAsync(stoppingToken);
            snapshot.IsStale = isStale;

            await repository.CreateOrUpdateAsync(snapshot, stoppingToken);

            _logger.LogInformation(
                "DashboardSnapshot persisted: {TotalTenants} tenants, avg score {AvgScore}, stale={IsStale}",
                snapshot.TotalTenants,
                snapshot.AvgHealthScore,
                snapshot.IsStale);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful shutdown — no action needed
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing dashboard snapshot; will retry on next scheduled run");
        }
    }
}
