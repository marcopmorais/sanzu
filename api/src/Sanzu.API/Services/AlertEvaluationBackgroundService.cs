using Sanzu.Core.Interfaces;

namespace Sanzu.API.Services;

public sealed class AlertEvaluationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertEvaluationBackgroundService> _logger;
    private readonly TimeSpan _interval;

    public AlertEvaluationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AlertEvaluationBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var intervalMinutes = configuration.GetValue("Alerts:EvaluationIntervalMinutes", 10);
        _interval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IAdminAlertService>();
                await service.EvaluateAlertRulesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Alert evaluation failed");
            }
        }
    }
}
