using FluentAssertions;
using Moq;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Services;

namespace Sanzu.Tests.Unit.Services;

public sealed class AdminTenantServiceTests
{
    private readonly Mock<IOrganizationRepository> _orgRepoMock = new();
    private readonly Mock<ITenantHealthScoreRepository> _healthRepoMock = new();

    private AdminTenantService CreateService()
        => new(_orgRepoMock.Object, _healthRepoMock.Object);

    // ── AC #1: Maps Organization + TenantHealthScore → TenantListItemResponse correctly ──

    [Fact]
    public async Task ListTenants_Should_MapFieldsCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var org = new Organization
        {
            Id = tenantId,
            Name = "Test Agency",
            Status = TenantStatus.Active,
            SubscriptionPlan = "Professional",
            Location = "EU-West",
            CreatedAt = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        var healthScore = new TenantHealthScore
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OverallScore = 72,
            BillingScore = 80,
            CaseCompletionScore = 60,
            OnboardingScore = 75,
            HealthBand = HealthBand.Green,
            ComputedAt = DateTime.UtcNow
        };

        _orgRepoMock
            .Setup(r => r.SearchForPlatformAsync(It.IsAny<TenantListRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Organization> { org } as IReadOnlyList<Organization>, 1));

        _healthRepoMock
            .Setup(r => r.GetLatestForAllTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantHealthScore> { healthScore });

        var service = CreateService();
        var result = await service.ListTenantsAsync(new TenantListRequest(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Id.Should().Be(tenantId);
        item.Name.Should().Be("Test Agency");
        item.Status.Should().Be("Active");
        item.PlanTier.Should().Be("Professional");
        item.HealthScore.Should().Be(72);
        item.HealthBand.Should().Be("Green");
        item.SignupDate.Should().Be(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc));
        item.Region.Should().Be("EU-West");
    }

    // ── AC #1: Tenants without health scores get null healthScore/healthBand ──

    [Fact]
    public async Task ListTenants_Should_ReturnNullHealthFields_WhenNoScoreExists()
    {
        var tenantId = Guid.NewGuid();
        var org = new Organization
        {
            Id = tenantId,
            Name = "No Score Agency",
            Status = TenantStatus.Onboarding,
            Location = "",
            CreatedAt = DateTime.UtcNow
        };

        _orgRepoMock
            .Setup(r => r.SearchForPlatformAsync(It.IsAny<TenantListRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Organization> { org } as IReadOnlyList<Organization>, 1));

        _healthRepoMock
            .Setup(r => r.GetLatestForAllTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantHealthScore>());

        var service = CreateService();
        var result = await service.ListTenantsAsync(new TenantListRequest(), CancellationToken.None);

        var item = result.Items[0];
        item.HealthScore.Should().BeNull();
        item.HealthBand.Should().BeNull();
        item.Region.Should().BeNull("empty Location maps to null region");
    }

    // ── AC #7: Health score sort places null-score tenants last ──

    [Fact]
    public async Task ListTenants_Should_PlaceNullScoreTenantsLast_WhenSortByHealthScoreAsc()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var tenantNoScore = Guid.NewGuid();

        var orgs = new List<Organization>
        {
            new() { Id = tenantA, Name = "A", Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow },
            new() { Id = tenantB, Name = "B", Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow },
            new() { Id = tenantNoScore, Name = "NoScore", Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow }
        };

        var scores = new List<TenantHealthScore>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenantA, OverallScore = 80, HealthBand = HealthBand.Green, ComputedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), TenantId = tenantB, OverallScore = 30, HealthBand = HealthBand.Red, ComputedAt = DateTime.UtcNow }
        };

        _orgRepoMock
            .Setup(r => r.SearchForPlatformAsync(It.IsAny<TenantListRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((orgs as IReadOnlyList<Organization>, orgs.Count));

        _healthRepoMock
            .Setup(r => r.GetLatestForAllTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(scores);

        var service = CreateService();
        var result = await service.ListTenantsAsync(
            new TenantListRequest { Sort = "healthScore", Order = "asc" },
            CancellationToken.None);

        result.Items.Should().HaveCount(3);
        // B (30) first, A (80) second, NoScore last
        result.Items[0].Name.Should().Be("B");
        result.Items[1].Name.Should().Be("A");
        result.Items[2].Name.Should().Be("NoScore");
        result.Items[2].HealthScore.Should().BeNull();
    }
}
