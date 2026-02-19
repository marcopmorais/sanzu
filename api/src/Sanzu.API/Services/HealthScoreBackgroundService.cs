using Sanzu.Core.Interfaces;

namespace Sanzu.API.Services;

public sealed class HealthScoreBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HealthScoreBackgroundService> _logger;
    private readonly TimeSpan _interval;
    private readonly int _retentionDays;
    private DateTime _lastCleanup = DateTime.MinValue;

    public HealthScoreBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<HealthScoreBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var intervalMinutes = configuration.GetValue("HealthScore:IntervalMinutes", 15);
        _interval = TimeSpan.FromMinutes(intervalMinutes);
        _retentionDays = configuration.GetValue("HealthScore:RetentionDays", 90);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HealthScoreBackgroundService starting with interval {Interval}", _interval);

        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var computeService = scope.ServiceProvider.GetRequiredService<IHealthScoreComputeService>();
                await computeService.ComputeForAllTenantsAsync(stoppingToken);

                _logger.LogInformation("Health scores computed successfully");

                // Daily cleanup
                if (DateTime.UtcNow - _lastCleanup > TimeSpan.FromHours(24))
                {
                    var healthScoreRepository = scope.ServiceProvider.GetRequiredService<ITenantHealthScoreRepository>();
                    var cutoff = DateTime.UtcNow.AddDays(-_retentionDays);
                    await healthScoreRepository.DeleteOlderThanAsync(cutoff, stoppingToken);
                    _lastCleanup = DateTime.UtcNow;

                    _logger.LogInformation("Health score cleanup completed, removed entries older than {Cutoff}", cutoff);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing health scores");
            }
        }
    }
}
