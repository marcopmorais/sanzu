using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;
using Sanzu.Tests.Integration;

namespace Sanzu.Tests.Integration.Services;

public sealed class AdminAlertServiceIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminAlertServiceIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EvaluateAlertRules_Should_CreateBillingFailureAlert_WhenOrgHasFailedPayments()
    {
        var orgId = await SeedOrgWithFailedPaymentAsync();

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminAlertService>();
        await service.EvaluateAlertRulesAsync(CancellationToken.None);

        var alertRepo = scope.ServiceProvider.GetRequiredService<IAdminAlertRepository>();
        var alerts = await alertRepo.GetAllAsync(AlertStatus.Fired, null, "BillingFailure", CancellationToken.None);

        alerts.Should().Contain(a => a.TenantId == orgId);
    }

    [Fact]
    public async Task EvaluateAlertRules_Should_NotCreateDuplicate_OnSecondRun()
    {
        var orgId = await SeedOrgWithFailedPaymentAsync("NoDup");

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminAlertService>();

        // First run
        await service.EvaluateAlertRulesAsync(CancellationToken.None);

        // Second run
        await service.EvaluateAlertRulesAsync(CancellationToken.None);

        var alertRepo = scope.ServiceProvider.GetRequiredService<IAdminAlertRepository>();
        var alerts = await alertRepo.GetAllAsync(AlertStatus.Fired, null, "BillingFailure", CancellationToken.None);
        alerts.Count(a => a.TenantId == orgId).Should().Be(1, "should not duplicate");
    }

    [Fact]
    public async Task AcknowledgeAlert_Should_ChangeStatusToAcknowledged()
    {
        using var scope = _factory.Services.CreateScope();
        var alertRepo = scope.ServiceProvider.GetRequiredService<IAdminAlertRepository>();

        var alertId = Guid.NewGuid();
        await alertRepo.CreateAsync(new AdminAlert
        {
            Id = alertId,
            AlertType = "TestAlert",
            Severity = AlertSeverity.Warning,
            Title = "Test",
            Detail = "Test detail",
            Status = AlertStatus.Fired,
            RoutedToRole = "SanzuOps",
            FiredAt = DateTime.UtcNow
        }, CancellationToken.None);

        var service = scope.ServiceProvider.GetRequiredService<IAdminAlertService>();
        var userId = Guid.NewGuid();
        await service.AcknowledgeAlertAsync(alertId, userId, CancellationToken.None);

        var updated = await alertRepo.GetByIdAsync(alertId, CancellationToken.None);
        updated!.Status.Should().Be(AlertStatus.Acknowledged);
        updated.OwnedByUserId.Should().Be(userId);
    }

    [Fact]
    public async Task ResolveAlert_Should_ChangeStatusToResolved()
    {
        using var scope = _factory.Services.CreateScope();
        var alertRepo = scope.ServiceProvider.GetRequiredService<IAdminAlertRepository>();

        var alertId = Guid.NewGuid();
        await alertRepo.CreateAsync(new AdminAlert
        {
            Id = alertId,
            AlertType = "TestResolve",
            Severity = AlertSeverity.Critical,
            Title = "Resolve me",
            Detail = "Detail",
            Status = AlertStatus.Acknowledged,
            RoutedToRole = "SanzuFinance",
            FiredAt = DateTime.UtcNow.AddHours(-1),
            AcknowledgedAt = DateTime.UtcNow
        }, CancellationToken.None);

        var service = scope.ServiceProvider.GetRequiredService<IAdminAlertService>();
        await service.ResolveAlertAsync(alertId, Guid.NewGuid(), CancellationToken.None);

        var updated = await alertRepo.GetByIdAsync(alertId, CancellationToken.None);
        updated!.Status.Should().Be(AlertStatus.Resolved);
        updated.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExistsFiredAsync_Should_ReturnTrue_ForExistingFiredAlert()
    {
        using var scope = _factory.Services.CreateScope();
        var alertRepo = scope.ServiceProvider.GetRequiredService<IAdminAlertRepository>();

        var tenantId = Guid.NewGuid();
        await alertRepo.CreateAsync(new AdminAlert
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AlertType = "ExistsCheck",
            Severity = AlertSeverity.Info,
            Title = "Check",
            Detail = "",
            Status = AlertStatus.Fired,
            RoutedToRole = "SanzuOps",
            FiredAt = DateTime.UtcNow
        }, CancellationToken.None);

        var exists = await alertRepo.ExistsFiredAsync("ExistsCheck", tenantId, CancellationToken.None);
        exists.Should().BeTrue();
    }

    private async Task<Guid> SeedOrgWithFailedPaymentAsync(string suffix = "")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var orgId = Guid.NewGuid();
        db.Organizations.Add(new Organization
        {
            Id = orgId,
            Name = $"AlertTest{suffix}-{orgId:N}",
            Location = "Test",
            Status = TenantStatus.Active,
            FailedPaymentAttempts = 2,
            LastPaymentFailedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return orgId;
    }
}
