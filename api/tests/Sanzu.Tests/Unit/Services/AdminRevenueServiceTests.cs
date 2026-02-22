using FluentAssertions;
using Moq;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Services;

namespace Sanzu.Tests.Unit.Services;

public sealed class AdminRevenueServiceTests
{
    private readonly Mock<IOrganizationRepository> _orgRepo = new();
    private readonly Mock<IBillingRecordRepository> _billingRepo = new();
    private readonly AdminRevenueService _sut;

    public AdminRevenueServiceTests()
    {
        _sut = new AdminRevenueService(_orgRepo.Object, _billingRepo.Object);
    }

    // ── GetRevenueOverviewAsync ──

    [Fact]
    public async Task GetRevenueOverview_Should_ComputeMrrFromActiveTenants()
    {
        _orgRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization>
            {
                MakeOrg(TenantStatus.Active, "Starter"),
                MakeOrg(TenantStatus.Active, "Professional"),
                MakeOrg(TenantStatus.Active, "Enterprise"),
                MakeOrg(TenantStatus.Terminated, "Starter") // should be excluded
            });

        var result = await _sut.GetRevenueOverviewAsync(CancellationToken.None);

        result.Mrr.Should().Be(149m + 399m + 0m); // Starter + Professional + Enterprise
        result.Arr.Should().Be(result.Mrr * 12);
    }

    [Fact]
    public async Task GetRevenueOverview_Should_ComputePlanBreakdownPercentages()
    {
        _orgRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization>
            {
                MakeOrg(TenantStatus.Active, "Starter"),
                MakeOrg(TenantStatus.Active, "Starter"),
                MakeOrg(TenantStatus.Active, "Professional")
            });

        var result = await _sut.GetRevenueOverviewAsync(CancellationToken.None);

        result.PlanBreakdown.Should().HaveCount(2);
        var starter = result.PlanBreakdown.First(p => p.PlanName == "Starter");
        starter.TenantCount.Should().Be(2);
        starter.Mrr.Should().Be(298m);
    }

    [Fact]
    public async Task GetRevenueOverview_Should_ComputeChurnRate()
    {
        var now = DateTime.UtcNow;
        _orgRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization>
            {
                MakeOrg(TenantStatus.Active, "Starter"),
                MakeOrg(TenantStatus.Active, "Professional"),
                MakeOrg(TenantStatus.Terminated, "Starter", updatedAt: now.AddDays(-5)) // recently terminated
            });

        var result = await _sut.GetRevenueOverviewAsync(CancellationToken.None);

        // Active at start ≈ 2 active + 1 recently terminated = 3
        // Churn = 1/3 * 100 = 33.3%
        result.ChurnRate.Should().Be(33.3m);
    }

    [Fact]
    public async Task GetRevenueOverview_Should_ReturnZeros_WhenNoOrgs()
    {
        _orgRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization>());

        var result = await _sut.GetRevenueOverviewAsync(CancellationToken.None);

        result.Mrr.Should().Be(0m);
        result.Arr.Should().Be(0m);
        result.ChurnRate.Should().Be(0m);
        result.GrowthRate.Should().Be(0m);
        result.PlanBreakdown.Should().BeEmpty();
    }

    // ── GetRevenueTrendsAsync ──

    [Fact]
    public async Task GetRevenueTrends_Should_GroupByMonthlyPeriod()
    {
        var now = DateTime.UtcNow;
        _billingRepo.Setup(r => r.GetAllInPeriodForPlatformAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BillingRecord>
            {
                MakeBillingRecord(now.AddMonths(-1), 149m),
                MakeBillingRecord(now.AddMonths(-1).AddDays(1), 399m),
                MakeBillingRecord(now, 149m)
            });

        var result = await _sut.GetRevenueTrendsAsync("monthly", CancellationToken.None);

        result.DataPoints.Should().NotBeEmpty();
        result.DataPoints.Should().BeInAscendingOrder(p => p.PeriodLabel);
    }

    // ── GetBillingHealthAsync ──

    [Fact]
    public async Task GetBillingHealth_Should_IdentifyFailedPayments()
    {
        var failedOrg = MakeOrg(TenantStatus.Active, "Starter");
        failedOrg.FailedPaymentAttempts = 2;
        failedOrg.LastPaymentFailedAt = DateTime.UtcNow.AddDays(-1);

        _orgRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization> { failedOrg });
        _billingRepo.Setup(r => r.GetAllForPlatformAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BillingRecord>());

        var result = await _sut.GetBillingHealthAsync(CancellationToken.None);

        result.FailedPaymentCount.Should().Be(1);
        result.FailedPayments.Should().HaveCount(1);
        result.FailedPayments[0].TenantId.Should().Be(failedOrg.Id);
    }

    // ── GetRevenueExportDataAsync ──

    [Fact]
    public async Task GetRevenueExportData_Should_ReturnRowsForNonTerminatedTenants()
    {
        _orgRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization>
            {
                MakeOrg(TenantStatus.Active, "Starter"),
                MakeOrg(TenantStatus.Active, "Professional"),
                MakeOrg(TenantStatus.Terminated, "Starter") // excluded
            });
        _billingRepo.Setup(r => r.GetAllForPlatformAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BillingRecord>());

        var result = await _sut.GetRevenueExportDataAsync(CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].MrrContribution.Should().Be(149m);
        result[1].MrrContribution.Should().Be(399m);
    }

    [Fact]
    public async Task GetRevenueExportData_Should_DeriveCorrectBillingStatus()
    {
        var failedOrg = MakeOrg(TenantStatus.Active, "Starter");
        failedOrg.FailedPaymentAttempts = 2;
        failedOrg.LastPaymentFailedAt = DateTime.UtcNow.AddDays(-1);

        _orgRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization> { failedOrg });
        _billingRepo.Setup(r => r.GetAllForPlatformAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BillingRecord>());

        var result = await _sut.GetRevenueExportDataAsync(CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].BillingStatus.Should().Be("Failed");
    }

    // ── GetBillingHealthExportDataAsync ──

    [Fact]
    public async Task GetBillingHealthExportData_Should_CombineAllIssueTypes()
    {
        var failedOrg = MakeOrg(TenantStatus.Active, "Starter");
        failedOrg.FailedPaymentAttempts = 1;
        failedOrg.LastPaymentFailedAt = DateTime.UtcNow.AddDays(-2);

        var graceOrg = MakeOrg(TenantStatus.Active, "Professional");
        graceOrg.NextPaymentRetryAt = DateTime.UtcNow.AddDays(5);

        _orgRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization> { failedOrg, graceOrg });
        _billingRepo.Setup(r => r.GetAllForPlatformAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BillingRecord>());

        var result = await _sut.GetBillingHealthExportDataAsync(CancellationToken.None);

        result.Should().Contain(r => r.IssueType == "FailedPayment");
        result.Should().Contain(r => r.IssueType == "GracePeriod");
    }

    // ── Helpers ──

    private static Organization MakeOrg(TenantStatus status, string? plan, DateTime? updatedAt = null)
    {
        var now = DateTime.UtcNow;
        return new Organization
        {
            Id = Guid.NewGuid(),
            Name = $"Org-{Guid.NewGuid():N}",
            Location = "Test",
            Status = status,
            SubscriptionPlan = plan,
            CreatedAt = now.AddDays(-60),
            UpdatedAt = updatedAt ?? now
        };
    }

    private static BillingRecord MakeBillingRecord(DateTime cycleStart, decimal amount)
    {
        return new BillingRecord
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            InvoiceNumber = $"INV-{Guid.NewGuid():N}",
            BillingCycleStart = cycleStart,
            BillingCycleEnd = cycleStart.AddMonths(1),
            TotalAmount = amount,
            Currency = "EUR",
            Status = "Paid",
            CreatedAt = cycleStart
        };
    }
}
