using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Infrastructure.Data;
using Sanzu.Infrastructure.Repositories;
using Sanzu.Infrastructure.Services;

namespace Sanzu.Tests.Unit.Services;

public sealed class DashboardSnapshotServiceTests
{
    // ── ComputeSnapshotAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ComputeSnapshotAsync_ReturnsCorrectTenantCount_WhenTenantsExist()
    {
        var db = CreateContext();
        await SeedTenantsAsync(db, 3);
        var service = CreateService(db);

        var result = await service.ComputeSnapshotAsync();

        result.TotalTenants.Should().Be(3);
    }

    [Fact]
    public async Task ComputeSnapshotAsync_ReturnsHealthBandDistribution_WhenScoresExist()
    {
        var db = CreateContext();
        var tenantIds = await SeedTenantsAsync(db, 3);
        await SeedHealthScoreAsync(db, tenantIds[0], HealthBand.Green, 85, DateTime.UtcNow.AddHours(-1));
        await SeedHealthScoreAsync(db, tenantIds[1], HealthBand.Yellow, 55, DateTime.UtcNow.AddHours(-1));
        await SeedHealthScoreAsync(db, tenantIds[2], HealthBand.Red, 25, DateTime.UtcNow.AddHours(-1));
        var service = CreateService(db);

        var result = await service.ComputeSnapshotAsync();

        result.GreenTenants.Should().Be(1);
        result.YellowTenants.Should().Be(1);
        result.RedTenants.Should().Be(1);
    }

    [Fact]
    public async Task ComputeSnapshotAsync_CountsActiveTenants_BasedOnRecentHealthScore()
    {
        var db = CreateContext();
        var tenantIds = await SeedTenantsAsync(db, 3);
        // Two recent scores, one old
        await SeedHealthScoreAsync(db, tenantIds[0], HealthBand.Green, 80, DateTime.UtcNow.AddDays(-5));
        await SeedHealthScoreAsync(db, tenantIds[1], HealthBand.Yellow, 60, DateTime.UtcNow.AddDays(-15));
        await SeedHealthScoreAsync(db, tenantIds[2], HealthBand.Red, 30, DateTime.UtcNow.AddDays(-45));
        var service = CreateService(db);

        var result = await service.ComputeSnapshotAsync();

        result.ActiveTenants.Should().Be(2);
    }

    [Fact]
    public async Task ComputeSnapshotAsync_CalculatesAvgHealthScore_FromLatestScorePerTenant()
    {
        var db = CreateContext();
        var tenantIds = await SeedTenantsAsync(db, 2);
        // Old score for tenant 0 — should be ignored in favour of latest
        await SeedHealthScoreAsync(db, tenantIds[0], HealthBand.Red, 20, DateTime.UtcNow.AddDays(-10));
        await SeedHealthScoreAsync(db, tenantIds[0], HealthBand.Green, 80, DateTime.UtcNow.AddDays(-1));
        await SeedHealthScoreAsync(db, tenantIds[1], HealthBand.Green, 60, DateTime.UtcNow.AddDays(-1));
        var service = CreateService(db);

        var result = await service.ComputeSnapshotAsync();

        result.AvgHealthScore.Should().Be(70m); // (80 + 60) / 2
    }

    [Fact]
    public async Task ComputeSnapshotAsync_SumsMtdRevenue_FromCurrentMonthBillingRecords()
    {
        var db = CreateContext();
        var tenantIds = await SeedTenantsAsync(db, 1);
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        await SeedBillingRecordAsync(db, tenantIds[0], monthStart.AddDays(1), 500m);
        await SeedBillingRecordAsync(db, tenantIds[0], monthStart.AddDays(3), 300m);
        // Previous month — should not be included
        await SeedBillingRecordAsync(db, tenantIds[0], monthStart.AddDays(-5), 999m);
        var service = CreateService(db);

        var result = await service.ComputeSnapshotAsync();

        result.TotalRevenueMtd.Should().Be(800m);
    }

    [Fact]
    public async Task ComputeSnapshotAsync_SetsIsStale_False()
    {
        var db = CreateContext();
        var service = CreateService(db);

        var result = await service.ComputeSnapshotAsync();

        result.IsStale.Should().BeFalse();
    }

    [Fact]
    public async Task ComputeSnapshotAsync_ReturnsZeroAggregates_WhenDatabaseIsEmpty()
    {
        var db = CreateContext();
        var service = CreateService(db);

        var result = await service.ComputeSnapshotAsync();

        result.TotalTenants.Should().Be(0);
        result.ActiveTenants.Should().Be(0);
        result.GreenTenants.Should().Be(0);
        result.YellowTenants.Should().Be(0);
        result.RedTenants.Should().Be(0);
        result.TotalRevenueMtd.Should().Be(0m);
        result.OpenAlerts.Should().Be(0);
        result.AvgHealthScore.Should().Be(0m);
    }

    [Fact]
    public async Task ComputeSnapshotAsync_CountsOpenAlerts_FromKpiAlertLog()
    {
        var db = CreateContext();
        var tenantIds = await SeedTenantsAsync(db, 1);
        var userId = Guid.NewGuid();
        await SeedUserAsync(db, userId, tenantIds[0]);
        await SeedKpiAlertAsync(db, userId);
        await SeedKpiAlertAsync(db, userId);
        var service = CreateService(db);

        var result = await service.ComputeSnapshotAsync();

        result.OpenAlerts.Should().Be(2);
    }

    [Fact]
    public async Task ComputeSnapshotAsync_SetsComputedAt_ToCurrentUtcTime()
    {
        var db = CreateContext();
        var service = CreateService(db);
        var before = DateTime.UtcNow;

        var result = await service.ComputeSnapshotAsync();

        result.ComputedAt.Should().BeOnOrAfter(before);
        result.ComputedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    // ── IsStaleAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task IsStaleAsync_ReturnsFalse_WhenSnapshotIsWithin15Minutes()
    {
        var db = CreateContext();
        var repo = new DashboardSnapshotRepository(db);
        await repo.CreateOrUpdateAsync(new DashboardSnapshot
        {
            Id = Guid.NewGuid(),
            ComputedAt = DateTime.UtcNow.AddMinutes(-5),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        var service = CreateService(db);

        var result = await service.IsStaleAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsStaleAsync_ReturnsTrue_WhenSnapshotIsOlderThan15Minutes()
    {
        var db = CreateContext();
        var repo = new DashboardSnapshotRepository(db);
        await repo.CreateOrUpdateAsync(new DashboardSnapshot
        {
            Id = Guid.NewGuid(),
            ComputedAt = DateTime.UtcNow.AddMinutes(-20),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        var service = CreateService(db);

        var result = await service.IsStaleAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsStaleAsync_ReturnsTrue_WhenNoSnapshotExists()
    {
        var db = CreateContext();
        var service = CreateService(db);

        var result = await service.IsStaleAsync();

        result.Should().BeTrue();
    }

    // ── GetLatestAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetLatestAsync_ReturnsNull_WhenNoSnapshotExists()
    {
        var db = CreateContext();
        var service = CreateService(db);

        var result = await service.GetLatestAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsSnapshot_WhenOneExists()
    {
        var db = CreateContext();
        var repo = new DashboardSnapshotRepository(db);
        var snapshot = new DashboardSnapshot
        {
            Id = Guid.NewGuid(),
            ComputedAt = DateTime.UtcNow.AddMinutes(-3),
            TotalTenants = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await repo.CreateOrUpdateAsync(snapshot);
        var service = CreateService(db);

        var result = await service.GetLatestAsync();

        result.Should().NotBeNull();
        result!.TotalTenants.Should().Be(5);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static DashboardSnapshotService CreateService(SanzuDbContext db)
        => new(db, new DashboardSnapshotRepository(db));

    private static SanzuDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SanzuDbContext>()
            .UseInMemoryDatabase($"sanzu-dashboard-snapshot-{Guid.NewGuid()}")
            .Options;
        return new SanzuDbContext(options);
    }

    private static async Task<List<Guid>> SeedTenantsAsync(SanzuDbContext db, int count)
    {
        var ids = new List<Guid>();
        for (var i = 0; i < count; i++)
        {
            var id = Guid.NewGuid();
            ids.Add(id);
            db.Organizations.Add(new Organization
            {
                Id = id,
                Name = $"Tenant {i}",
                Location = "Lisbon",
                Status = TenantStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-60),
                UpdatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
        return ids;
    }

    private static async Task SeedHealthScoreAsync(
        SanzuDbContext db,
        Guid tenantId,
        HealthBand band,
        int score,
        DateTime computedAt)
    {
        db.TenantHealthScores.Add(new TenantHealthScore
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OverallScore = score,
            BillingScore = score,
            CaseCompletionScore = score,
            OnboardingScore = score,
            HealthBand = band,
            ComputedAt = computedAt
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedBillingRecordAsync(
        SanzuDbContext db,
        Guid tenantId,
        DateTime cycleStart,
        decimal amount)
    {
        db.BillingRecords.Add(new BillingRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceNumber = $"INV-{Guid.NewGuid():N}",
            BillingCycleStart = cycleStart,
            BillingCycleEnd = cycleStart.AddMonths(1),
            PlanCode = "PRO",
            BillingCycle = "MONTHLY",
            BaseAmount = amount,
            TotalAmount = amount,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedUserAsync(SanzuDbContext db, Guid userId, Guid tenantId)
    {
        db.Users.Add(new User
        {
            Id = userId,
            Email = $"user-{userId:N}@test.com",
            FullName = "Test User",
            OrgId = tenantId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedKpiAlertAsync(SanzuDbContext db, Guid triggeredByUserId)
    {
        db.KpiAlerts.Add(new KpiAlertLog
        {
            Id = Guid.NewGuid(),
            ThresholdId = Guid.NewGuid(),
            MetricKey = KpiMetricKey.CasesCreated,
            ThresholdValue = 10,
            ActualValue = 5,
            Severity = KpiAlertSeverity.Warning,
            RouteTarget = "ops",
            TriggeredByUserId = triggeredByUserId,
            TriggeredAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
