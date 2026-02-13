using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Services;
using Sanzu.Core.Validators;
using Sanzu.Infrastructure.Data;
using Sanzu.Infrastructure.Repositories;

namespace Sanzu.Tests.Unit.Services;

public sealed class KpiAlertServiceTests
{
    [Fact]
    public async Task UpsertThreshold_ShouldPersistThresholdAndAudit_WhenActorIsSanzuAdmin()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithSanzuAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);

        var response = await service.UpsertThresholdAsync(
            actorUserId,
            new UpsertKpiThresholdRequest
            {
                MetricKey = "CasesCreated",
                ThresholdValue = 5,
                Severity = "High",
                RouteTarget = "ops@agency.pt",
                IsEnabled = true
            },
            CancellationToken.None);

        response.MetricKey.Should().Be(KpiMetricKey.CasesCreated);
        response.ThresholdValue.Should().Be(5);
        response.Severity.Should().Be(KpiAlertSeverity.High);
        response.RouteTarget.Should().Be("ops@agency.pt");
        response.UpdatedByUserId.Should().Be(actorUserId);

        dbContext.KpiThresholds.Should().Contain(
            x => x.MetricKey == KpiMetricKey.CasesCreated
                 && x.ThresholdValue == 5
                 && x.RouteTarget == "ops@agency.pt");
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "KpiThresholdConfigured");
    }

    [Fact]
    public async Task EvaluateThresholds_ShouldGenerateAlertAndPersistLog_WhenThresholdIsBreached()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithSanzuAdminAsync(dbContext, TenantStatus.Active);
        await SeedKpiUsageDataAsync(dbContext, tenantId, actorUserId);
        dbContext.KpiThresholds.Add(
            new KpiThresholdDefinition
            {
                Id = Guid.NewGuid(),
                MetricKey = KpiMetricKey.CasesCreated,
                ThresholdValue = 1,
                Severity = KpiAlertSeverity.High,
                RouteTarget = "ops@agency.pt",
                IsEnabled = true,
                UpdatedByUserId = actorUserId,
                UpdatedAt = DateTime.UtcNow
            });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var response = await service.EvaluateThresholdsAsync(
            actorUserId,
            new EvaluateKpiAlertsRequest
            {
                PeriodDays = 30,
                TenantLimit = 10,
                CaseLimit = 10
            },
            CancellationToken.None);

        response.GeneratedAlerts.Should().NotBeEmpty();
        response.GeneratedAlerts.Should().Contain(
            x => x.MetricKey == KpiMetricKey.CasesCreated
                 && x.Severity == KpiAlertSeverity.High
                 && x.RouteTarget == "ops@agency.pt");

        dbContext.KpiAlerts.Should().NotBeEmpty();
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "KpiThresholdAlertTriggered");
    }

    [Fact]
    public async Task UpsertThreshold_ShouldThrowAccessDenied_WhenActorIsNotSanzuAdmin()
    {
        var dbContext = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId, TenantStatus.Active);
        await SeedUserAsync(dbContext, actorUserId, tenantId);
        var service = CreateService(dbContext);

        var act = () => service.UpsertThresholdAsync(
            actorUserId,
            new UpsertKpiThresholdRequest
            {
                MetricKey = "CasesCreated",
                ThresholdValue = 3,
                Severity = "Medium",
                RouteTarget = "ops@agency.pt",
                IsEnabled = true
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantAccessDeniedException>();
    }

    [Fact]
    public async Task EvaluateThresholds_ShouldThrowValidation_WhenPeriodIsOutOfRange()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithSanzuAdminAsync(dbContext, TenantStatus.Active);
        await SeedKpiUsageDataAsync(dbContext, tenantId, actorUserId);
        var service = CreateService(dbContext);

        var act = () => service.EvaluateThresholdsAsync(
            actorUserId,
            new EvaluateKpiAlertsRequest
            {
                PeriodDays = 2,
                TenantLimit = 10,
                CaseLimit = 10
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private static KpiAlertService CreateService(SanzuDbContext dbContext)
    {
        return new KpiAlertService(
            new UserRoleRepository(dbContext),
            new KpiThresholdRepository(dbContext),
            new KpiAlertLogRepository(dbContext),
            new KpiDashboardService(
                new OrganizationRepository(dbContext),
                new UserRoleRepository(dbContext),
                new CaseRepository(dbContext),
                new CaseDocumentRepository(dbContext)),
            new AuditRepository(dbContext),
            new EfUnitOfWork(dbContext),
            new UpsertKpiThresholdRequestValidator(),
            new EvaluateKpiAlertsRequestValidator());
    }

    private static SanzuDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SanzuDbContext>()
            .UseInMemoryDatabase($"sanzu-kpi-alert-tests-{Guid.NewGuid()}")
            .Options;

        return new SanzuDbContext(options);
    }

    private static async Task<(Guid TenantId, Guid UserId)> SeedTenantWithSanzuAdminAsync(
        SanzuDbContext dbContext,
        TenantStatus status)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await SeedTenantAsync(dbContext, tenantId, status);
        await SeedUserAsync(dbContext, userId, tenantId);
        dbContext.UserRoles.Add(
            new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RoleType = PlatformRole.SanzuAdmin,
                TenantId = null,
                GrantedBy = userId,
                GrantedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
        return (tenantId, userId);
    }

    private static async Task SeedTenantAsync(SanzuDbContext dbContext, Guid tenantId, TenantStatus status)
    {
        dbContext.Organizations.Add(
            new Organization
            {
                Id = tenantId,
                Name = $"Tenant-{tenantId:N}",
                Location = "Lisbon",
                Status = status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedUserAsync(
        SanzuDbContext dbContext,
        Guid userId,
        Guid tenantId,
        string? email = null,
        string? fullName = null)
    {
        dbContext.Users.Add(
            new User
            {
                Id = userId,
                Email = email ?? $"user-{userId:N}@agency.pt",
                FullName = fullName ?? "Sanzu Admin",
                OrgId = tenantId,
                CreatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedKpiUsageDataAsync(SanzuDbContext dbContext, Guid tenantId, Guid actorUserId)
    {
        var now = DateTime.UtcNow;
        var caseId = Guid.NewGuid();

        dbContext.Cases.Add(
            new Case
            {
                Id = caseId,
                TenantId = tenantId,
                CaseNumber = "CASE-ALERT-001",
                DeceasedFullName = "Alert KPI",
                DateOfDeath = now.Date.AddDays(-3),
                CaseType = "GENERAL",
                Urgency = "NORMAL",
                Status = CaseStatus.Active,
                ManagerUserId = actorUserId,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-1)
            });

        dbContext.CaseDocuments.Add(
            new CaseDocument
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CaseId = caseId,
                FileName = "alert-doc.txt",
                ContentType = "text/plain",
                Content = System.Text.Encoding.UTF8.GetBytes("alert"),
                SizeBytes = 5,
                UploadedByUserId = actorUserId,
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1)
            });

        await dbContext.SaveChangesAsync();
    }
}
