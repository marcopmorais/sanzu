using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sanzu.API.Services;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;

namespace Sanzu.Tests.Unit.Services;

public sealed class DashboardSnapshotBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_CallsComputeSnapshot_ImmediatelyOnStartup()
    {
        var snapshotService = new Mock<IDashboardSnapshotService>();
        var repo = new Mock<IDashboardSnapshotRepository>();
        var snapshot = MakeSnapshot();

        snapshotService.Setup(s => s.ComputeSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        snapshotService.Setup(s => s.IsStaleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.CreateOrUpdateAsync(It.IsAny<DashboardSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        var scopeFactory = BuildScopeFactory(snapshotService.Object, repo.Object);
        var service = CreateService(scopeFactory);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(150);
        await service.StopAsync(CancellationToken.None);

        snapshotService.Verify(s => s.ComputeSnapshotAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        repo.Verify(r => r.CreateOrUpdateAsync(It.IsAny<DashboardSnapshot>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_LogsError_AndDoesNotCrash_WhenComputeThrows()
    {
        var snapshotService = new Mock<IDashboardSnapshotService>();
        var repo = new Mock<IDashboardSnapshotRepository>();
        var logger = new Mock<ILogger<DashboardSnapshotBackgroundService>>();

        snapshotService.Setup(s => s.ComputeSnapshotAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB timeout"));

        var scopeFactory = BuildScopeFactory(snapshotService.Object, repo.Object);
        var service = CreateService(scopeFactory, logger.Object);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(150);
        await service.StopAsync(CancellationToken.None);

        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("snapshot")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_StopsGracefully_WhenCancellationRequested()
    {
        var snapshotService = new Mock<IDashboardSnapshotService>();
        var repo = new Mock<IDashboardSnapshotRepository>();
        var snapshot = MakeSnapshot();

        snapshotService.Setup(s => s.ComputeSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        snapshotService.Setup(s => s.IsStaleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.CreateOrUpdateAsync(It.IsAny<DashboardSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        var scopeFactory = BuildScopeFactory(snapshotService.Object, repo.Object);
        var service = CreateService(scopeFactory);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50);

        var stopTask = service.StopAsync(CancellationToken.None);
        var completed = await Task.WhenAny(stopTask, Task.Delay(2000));

        completed.Should().Be(stopTask, "service should stop gracefully within 2 seconds");
    }

    [Fact]
    public async Task ExecuteAsync_SetsIsStale_True_WhenServiceReportsStale()
    {
        var snapshotService = new Mock<IDashboardSnapshotService>();
        var repo = new Mock<IDashboardSnapshotRepository>();
        var snapshot = MakeSnapshot();

        snapshotService.Setup(s => s.ComputeSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        snapshotService.Setup(s => s.IsStaleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);  // report stale
        repo.Setup(r => r.CreateOrUpdateAsync(It.IsAny<DashboardSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        var scopeFactory = BuildScopeFactory(snapshotService.Object, repo.Object);
        var service = CreateService(scopeFactory);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(150);
        await service.StopAsync(CancellationToken.None);

        repo.Verify(r => r.CreateOrUpdateAsync(
            It.Is<DashboardSnapshot>(s => s.IsStale),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static IServiceScopeFactory BuildScopeFactory(
        IDashboardSnapshotService snapshotService,
        IDashboardSnapshotRepository repo)
    {
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IDashboardSnapshotService)))
            .Returns(snapshotService);
        serviceProvider.Setup(sp => sp.GetService(typeof(IDashboardSnapshotRepository)))
            .Returns(repo);

        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        return scopeFactory.Object;
    }

    private static DashboardSnapshotBackgroundService CreateService(
        IServiceScopeFactory scopeFactory,
        ILogger<DashboardSnapshotBackgroundService>? logger = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DashboardSnapshot:IntervalMinutes"] = "60"
            })
            .Build();

        return new DashboardSnapshotBackgroundService(
            scopeFactory,
            logger ?? NullLogger<DashboardSnapshotBackgroundService>.Instance,
            config);
    }

    private static DashboardSnapshot MakeSnapshot(DateTime? computedAt = null)
        => new()
        {
            Id = Guid.NewGuid(),
            ComputedAt = computedAt ?? DateTime.UtcNow,
            IsStale = false,
            TotalTenants = 3,
            ActiveTenants = 2,
            GreenTenants = 1,
            YellowTenants = 1,
            RedTenants = 0,
            TotalRevenueMtd = 1500m,
            OpenAlerts = 2,
            AvgHealthScore = 72m,
            Metadata = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
}
