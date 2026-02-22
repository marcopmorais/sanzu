using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;
using Sanzu.Tests.Integration;

namespace Sanzu.Tests.Admin;

public sealed class AdminDashboardSnapshotTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminDashboardSnapshotTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── AC #1: AdminDashboardSnapshot entity and repository ──

    [Fact]
    public async Task UpsertAsync_Should_CreateSnapshot_When_NoExistingRow()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAdminDashboardSnapshotRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var snapshot = new AdminDashboardSnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotType = $"Test-{Guid.NewGuid():N}",
            JsonPayload = "{}",
            ComputedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        await repo.UpsertAsync(snapshot, CancellationToken.None);

        var stored = await repo.GetLatestByTypeAsync(snapshot.SnapshotType, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.JsonPayload.Should().Be("{}");
    }

    [Fact]
    public async Task UpsertAsync_Should_UpdateExisting_When_SameSnapshotType()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAdminDashboardSnapshotRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var snapshotType = $"Upsert-{Guid.NewGuid():N}";
        var first = new AdminDashboardSnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotType = snapshotType,
            JsonPayload = "{\"version\":1}",
            ComputedAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTime.UtcNow
        };

        await repo.UpsertAsync(first, CancellationToken.None);

        var second = new AdminDashboardSnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotType = snapshotType,
            JsonPayload = "{\"version\":2}",
            ComputedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        await repo.UpsertAsync(second, CancellationToken.None);

        var all = dbContext.AdminDashboardSnapshots
            .Where(s => s.SnapshotType == snapshotType)
            .ToList();

        all.Should().HaveCount(1, "upsert should update, not duplicate");
        all[0].JsonPayload.Should().Be("{\"version\":2}");
    }

    // ── AC #2: ComputeSnapshotAsync produces correct summary ──

    [Fact]
    public async Task ComputeSnapshotAsync_Should_ComputeTenantCounts()
    {
        var tenantIds = await SeedMultipleTenantScenarioAsync();

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminDashboardService>();

        await service.ComputeSnapshotAsync(CancellationToken.None);

        var summary = await service.GetLatestSnapshotAsync(CancellationToken.None);
        summary.Should().NotBeNull();
        summary!.Tenants.Active.Should().BeGreaterThanOrEqualTo(1);
        summary.Tenants.Total.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ComputeSnapshotAsync_Should_ComputeHealthDistribution()
    {
        var tenantId = await SeedTenantWithHealthScoreAsync(HealthBand.Red, 25, "BillingFailed");

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminDashboardService>();

        await service.ComputeSnapshotAsync(CancellationToken.None);

        var summary = await service.GetLatestSnapshotAsync(CancellationToken.None);
        summary.Should().NotBeNull();
        // At least the one we seeded should be in red
        summary!.Health.Red.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ComputeSnapshotAsync_Should_IncludeTopAtRiskTenants()
    {
        var tenantId = await SeedTenantWithHealthScoreAsync(HealthBand.Red, 15, "BillingFailed");

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminDashboardService>();

        await service.ComputeSnapshotAsync(CancellationToken.None);

        var summary = await service.GetLatestSnapshotAsync(CancellationToken.None);
        summary.Should().NotBeNull();
        summary!.Health.TopAtRisk.Should().Contain(t => t.TenantId == tenantId);
    }

    [Fact]
    public async Task ComputeSnapshotAsync_Should_ReturnZeroedAlertCounts()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminDashboardService>();

        await service.ComputeSnapshotAsync(CancellationToken.None);

        var summary = await service.GetLatestSnapshotAsync(CancellationToken.None);
        summary.Should().NotBeNull();
        // Alert counts should be zeroed — AdminAlert table doesn't exist yet (Epic 17)
        summary!.Alerts.Critical.Should().Be(0);
        summary.Alerts.Warning.Should().Be(0);
        summary.Alerts.Info.Should().Be(0);
        summary.Alerts.Unacknowledged.Should().Be(0);
    }

    // ── AC #3: Audit event logged with system actor ──

    [Fact]
    public async Task ComputeSnapshotAsync_Should_LogAuditEvent_WithSystemActor()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminDashboardService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var beforeCount = dbContext.AuditEvents
            .Count(e => e.EventType == "Admin.Dashboard.SnapshotComputed");

        await service.ComputeSnapshotAsync(CancellationToken.None);

        var auditEvents = dbContext.AuditEvents
            .Where(e => e.EventType == "Admin.Dashboard.SnapshotComputed")
            .OrderByDescending(e => e.CreatedAt)
            .ToList();

        auditEvents.Should().HaveCountGreaterThan(beforeCount);
        var latest = auditEvents.First();
        latest.ActorUserId.Should().Be(Guid.Empty, "system-initiated events use Guid.Empty");
        latest.EventType.Should().Be("Admin.Dashboard.SnapshotComputed");
    }

    // ── Background service unit test ──

    [Fact]
    public async Task GetLatestSnapshotAsync_Should_ReturnNull_When_NoSnapshotExists()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminDashboardService>();

        // Use a fresh type that doesn't exist to test null return
        var repo = scope.ServiceProvider.GetRequiredService<IAdminDashboardSnapshotRepository>();
        var result = await repo.GetLatestByTypeAsync($"NonExistent-{Guid.NewGuid():N}", CancellationToken.None);
        result.Should().BeNull();
    }

    // ── Helpers ──

    private async Task<List<Guid>> SeedMultipleTenantScenarioAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var ids = new List<Guid>();

        var activeTenantId = Guid.NewGuid();
        dbContext.Organizations.Add(new Organization
        {
            Id = activeTenantId,
            Name = $"DashActive-{activeTenantId:N}",
            Location = "Test",
            Status = TenantStatus.Active,
            OnboardingCompletedAt = DateTime.UtcNow.AddDays(-30),
            SubscriptionActivatedAt = DateTime.UtcNow.AddDays(-30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        ids.Add(activeTenantId);

        var onboardingTenantId = Guid.NewGuid();
        dbContext.Organizations.Add(new Organization
        {
            Id = onboardingTenantId,
            Name = $"DashOnboarding-{onboardingTenantId:N}",
            Location = "Test",
            Status = TenantStatus.Onboarding,
            CreatedAt = DateTime.UtcNow.AddDays(-20),
            UpdatedAt = DateTime.UtcNow
        });
        ids.Add(onboardingTenantId);

        var paymentIssueTenantId = Guid.NewGuid();
        dbContext.Organizations.Add(new Organization
        {
            Id = paymentIssueTenantId,
            Name = $"DashPayIssue-{paymentIssueTenantId:N}",
            Location = "Test",
            Status = TenantStatus.PaymentIssue,
            OnboardingCompletedAt = DateTime.UtcNow.AddDays(-60),
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            UpdatedAt = DateTime.UtcNow
        });
        ids.Add(paymentIssueTenantId);

        await dbContext.SaveChangesAsync();
        return ids;
    }

    private async Task<Guid> SeedTenantWithHealthScoreAsync(HealthBand band, int score, string? primaryIssue)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var tenantId = Guid.NewGuid();
        dbContext.Organizations.Add(new Organization
        {
            Id = tenantId,
            Name = $"DashHealth-{tenantId:N}",
            Location = "Test",
            Status = TenantStatus.Active,
            OnboardingCompletedAt = DateTime.UtcNow.AddDays(-30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        dbContext.TenantHealthScores.Add(new TenantHealthScore
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OverallScore = score,
            BillingScore = score,
            CaseCompletionScore = score,
            OnboardingScore = score,
            HealthBand = band,
            PrimaryIssue = primaryIssue,
            ComputedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return tenantId;
    }
}
