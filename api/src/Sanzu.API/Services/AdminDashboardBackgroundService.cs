using Sanzu.Core.Interfaces;

namespace Sanzu.API.Services;

public sealed class AdminDashboardBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AdminDashboardBackgroundService> _logger;
    private readonly TimeSpan _interval;

    public AdminDashboardBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AdminDashboardBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var intervalMinutes = configuration.GetValue("Dashboard:SnapshotIntervalMinutes", 5);
        _interval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AdminDashboardBackgroundService starting with interval {Interval}", _interval);

        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dashboardService = scope.ServiceProvider.GetRequiredService<IAdminDashboardService>();
                await dashboardService.ComputeSnapshotAsync(stoppingToken);

                _logger.LogInformation("Dashboard snapshot computed successfully");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing dashboard snapshot");
            }
        }
    }
}
