using FluentAssertions;
using Moq;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Services;

namespace Sanzu.Tests.Unit.Services;

public sealed class AdminAlertServiceTests
{
    private readonly Mock<IAdminAlertRepository> _alertRepo = new();
    private readonly Mock<IOrganizationRepository> _orgRepo = new();
    private readonly Mock<ITenantHealthScoreRepository> _healthRepo = new();
    private readonly Mock<ICaseRepository> _caseRepo = new();
    private readonly Mock<IAuditRepository> _auditRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly AdminAlertService _sut;

    public AdminAlertServiceTests()
    {
        _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        _sut = new AdminAlertService(
            _alertRepo.Object,
            _orgRepo.Object,
            _healthRepo.Object,
            _caseRepo.Object,
            _auditRepo.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task EvaluateAlertRules_Should_FireHealthDropAlert_WhenScoreBelowThreshold()
    {
        var orgId = Guid.NewGuid();
        _orgRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization>
            {
                new() { Id = orgId, Name = "TestOrg", Status = TenantStatus.Active, Location = "Test", CreatedAt = DateTime.UtcNow.AddDays(-60) }
            });

        _healthRepo.Setup(r => r.GetLatestForAllTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantHealthScore>
            {
                new() { Id = Guid.NewGuid(), TenantId = orgId, OverallScore = 20, HealthBand = HealthBand.Red, ComputedAt = DateTime.UtcNow }
            });

        _alertRepo.Setup(r => r.ExistsFiredAsync("HealthDrop", orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _caseRepo.Setup(r => r.GetByTenantIdWithStepsForPlatformAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Case>());

        await _sut.EvaluateAlertRulesAsync(CancellationToken.None);

        _alertRepo.Verify(r => r.CreateAsync(
            It.Is<AdminAlert>(a => a.AlertType == "HealthDrop" && a.Severity == AlertSeverity.Warning),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateAlertRules_Should_NotFireDuplicate_WhenAlreadyFired()
    {
        var orgId = Guid.NewGuid();
        _orgRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization>
            {
                new() { Id = orgId, Name = "TestOrg", Status = TenantStatus.Active, Location = "Test", FailedPaymentAttempts = 2, LastPaymentFailedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow.AddDays(-60) }
            });

        _healthRepo.Setup(r => r.GetLatestForAllTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantHealthScore>());

        // Already fired
        _alertRepo.Setup(r => r.ExistsFiredAsync("BillingFailure", orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _alertRepo.Setup(r => r.ExistsFiredAsync("CaseStuck", It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _caseRepo.Setup(r => r.GetByTenantIdWithStepsForPlatformAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Case>());

        await _sut.EvaluateAlertRulesAsync(CancellationToken.None);

        _alertRepo.Verify(r => r.CreateAsync(
            It.Is<AdminAlert>(a => a.AlertType == "BillingFailure"),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateAlertRules_Should_FireBillingFailureAlert_WhenPaymentFailed()
    {
        var orgId = Guid.NewGuid();
        _orgRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization>
            {
                new() { Id = orgId, Name = "FailedOrg", Status = TenantStatus.Active, Location = "Test", FailedPaymentAttempts = 3, LastPaymentFailedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow.AddDays(-60) }
            });

        _healthRepo.Setup(r => r.GetLatestForAllTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantHealthScore>());

        _alertRepo.Setup(r => r.ExistsFiredAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _caseRepo.Setup(r => r.GetByTenantIdWithStepsForPlatformAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Case>());

        await _sut.EvaluateAlertRulesAsync(CancellationToken.None);

        _alertRepo.Verify(r => r.CreateAsync(
            It.Is<AdminAlert>(a => a.AlertType == "BillingFailure" && a.Severity == AlertSeverity.Critical && a.RoutedToRole == "SanzuFinance"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcknowledgeAlert_Should_UpdateStatusAndSetOwner()
    {
        var alertId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var alert = new AdminAlert
        {
            Id = alertId,
            AlertType = "HealthDrop",
            Status = AlertStatus.Fired,
            FiredAt = DateTime.UtcNow.AddHours(-1)
        };

        _alertRepo.Setup(r => r.GetByIdAsync(alertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        await _sut.AcknowledgeAlertAsync(alertId, userId, CancellationToken.None);

        alert.Status.Should().Be(AlertStatus.Acknowledged);
        alert.OwnedByUserId.Should().Be(userId);
        alert.AcknowledgedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _alertRepo.Verify(r => r.UpdateAsync(alert, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAlert_Should_UpdateStatusAndSetResolvedAt()
    {
        var alertId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var alert = new AdminAlert
        {
            Id = alertId,
            AlertType = "BillingFailure",
            Status = AlertStatus.Acknowledged,
            FiredAt = DateTime.UtcNow.AddHours(-2)
        };

        _alertRepo.Setup(r => r.GetByIdAsync(alertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        await _sut.ResolveAlertAsync(alertId, userId, CancellationToken.None);

        alert.Status.Should().Be(AlertStatus.Resolved);
        alert.ResolvedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task EvaluateAlertRules_Should_FireOnboardingStalledAlert()
    {
        var orgId = Guid.NewGuid();
        _orgRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization>
            {
                new()
                {
                    Id = orgId, Name = "StalledOrg", Status = TenantStatus.Active, Location = "Test",
                    OnboardingCompletedAt = null,
                    CreatedAt = DateTime.UtcNow.AddDays(-30) // > 21 days threshold
                }
            });

        _healthRepo.Setup(r => r.GetLatestForAllTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantHealthScore>());

        _alertRepo.Setup(r => r.ExistsFiredAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _caseRepo.Setup(r => r.GetByTenantIdWithStepsForPlatformAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Case>());

        await _sut.EvaluateAlertRulesAsync(CancellationToken.None);

        _alertRepo.Verify(r => r.CreateAsync(
            It.Is<AdminAlert>(a => a.AlertType == "OnboardingStalled" && a.RoutedToRole == "SanzuOps"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
