using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Services;
using Sanzu.Infrastructure.Data;
using Sanzu.Infrastructure.Repositories;

namespace Sanzu.Tests.Unit.Services;

public sealed class KpiDashboardServiceTests
{
    [Fact]
    public async Task GetDashboard_ShouldReturnCurrentBaselineAndDrilldown_WhenActorIsSanzuAdmin()
    {
        var dbContext = CreateContext();
        var actorUserId = Guid.NewGuid();
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantAId, "Tenant Alpha", TenantStatus.Active);
        await SeedTenantAsync(dbContext, tenantBId, "Tenant Beta", TenantStatus.Active);
        await SeedUserAsync(dbContext, actorUserId, tenantAId);
        await SeedSanzuAdminRoleAsync(dbContext, actorUserId);
        await SeedKpiDataAsync(dbContext, tenantAId, tenantBId, actorUserId);
        var service = CreateService(dbContext);

        var result = await service.GetDashboardAsync(
            actorUserId,
            periodDays: 30,
            tenantLimit: 10,
            caseLimit: 10,
            cancellationToken: CancellationToken.None);

        result.PeriodDays.Should().Be(30);
        result.Current.TenantsTotal.Should().BeGreaterThanOrEqualTo(2);
        result.Current.CasesCreated.Should().BeGreaterThanOrEqualTo(2);
        result.Current.DocumentsUploaded.Should().BeGreaterThanOrEqualTo(2);
        result.TenantContributions.Should().Contain(x => x.TenantId == tenantAId);
        result.TenantContributions.Should().Contain(x => x.TenantId == tenantBId);
        result.CaseContributions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDashboard_ShouldThrowAccessDenied_WhenActorIsNotSanzuAdmin()
    {
        var dbContext = CreateContext();
        var actorUserId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId, "Tenant Access", TenantStatus.Active);
        await SeedUserAsync(dbContext, actorUserId, tenantId);
        var service = CreateService(dbContext);

        var act = () => service.GetDashboardAsync(
            actorUserId,
            periodDays: 30,
            tenantLimit: 10,
            caseLimit: 10,
            cancellationToken: CancellationToken.None);

        await act.Should().ThrowAsync<TenantAccessDeniedException>();
    }

    [Fact]
    public async Task GetDashboard_ShouldThrowValidation_WhenPeriodIsOutOfRange()
    {
        var dbContext = CreateContext();
        var actorUserId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId, "Tenant Validation", TenantStatus.Active);
        await SeedUserAsync(dbContext, actorUserId, tenantId);
        await SeedSanzuAdminRoleAsync(dbContext, actorUserId);
        var service = CreateService(dbContext);

        var act = () => service.GetDashboardAsync(
            actorUserId,
            periodDays: 3,
            tenantLimit: 10,
            caseLimit: 10,
            cancellationToken: CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private static KpiDashboardService CreateService(SanzuDbContext dbContext)
    {
        return new KpiDashboardService(
            new OrganizationRepository(dbContext),
            new UserRoleRepository(dbContext),
            new CaseRepository(dbContext),
            new CaseDocumentRepository(dbContext));
    }

    private static SanzuDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SanzuDbContext>()
            .UseInMemoryDatabase($"sanzu-kpi-dashboard-tests-{Guid.NewGuid()}")
            .Options;

        return new SanzuDbContext(options);
    }

    private static async Task SeedTenantAsync(
        SanzuDbContext dbContext,
        Guid tenantId,
        string tenantName,
        TenantStatus status)
    {
        dbContext.Organizations.Add(
            new Organization
            {
                Id = tenantId,
                Name = tenantName,
                Location = "Lisbon",
                Status = status,
                CreatedAt = DateTime.UtcNow.AddDays(-120),
                UpdatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedUserAsync(SanzuDbContext dbContext, Guid userId, Guid tenantId)
    {
        dbContext.Users.Add(
            new User
            {
                Id = userId,
                Email = $"user-{userId:N}@agency.pt",
                FullName = "Platform Admin",
                OrgId = tenantId,
                CreatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedSanzuAdminRoleAsync(SanzuDbContext dbContext, Guid userId)
    {
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
    }

    private static async Task SeedKpiDataAsync(
        SanzuDbContext dbContext,
        Guid tenantAId,
        Guid tenantBId,
        Guid actorUserId)
    {
        var now = DateTime.UtcNow;
        var caseAId = Guid.NewGuid();
        var caseBId = Guid.NewGuid();

        dbContext.Cases.AddRange(
            new Case
            {
                Id = caseAId,
                TenantId = tenantAId,
                CaseNumber = "CASE-10001",
                DeceasedFullName = "Alpha Case",
                DateOfDeath = now.Date.AddDays(-2),
                CaseType = "GENERAL",
                Urgency = "NORMAL",
                Status = CaseStatus.Active,
                ManagerUserId = actorUserId,
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-1)
            },
            new Case
            {
                Id = caseBId,
                TenantId = tenantBId,
                CaseNumber = "CASE-20001",
                DeceasedFullName = "Beta Case",
                DateOfDeath = now.Date.AddDays(-9),
                CaseType = "GENERAL",
                Urgency = "NORMAL",
                Status = CaseStatus.Closed,
                ManagerUserId = actorUserId,
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now.AddDays(-2),
                ClosedAt = now.AddDays(-2)
            });

        dbContext.CaseDocuments.AddRange(
            new CaseDocument
            {
                Id = Guid.NewGuid(),
                TenantId = tenantAId,
                CaseId = caseAId,
                FileName = "alpha-doc.txt",
                ContentType = "text/plain",
                Content = System.Text.Encoding.UTF8.GetBytes("alpha"),
                SizeBytes = 5,
                UploadedByUserId = actorUserId,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2)
            },
            new CaseDocument
            {
                Id = Guid.NewGuid(),
                TenantId = tenantBId,
                CaseId = caseBId,
                FileName = "beta-doc.txt",
                ContentType = "text/plain",
                Content = System.Text.Encoding.UTF8.GetBytes("beta"),
                SizeBytes = 4,
                UploadedByUserId = actorUserId,
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1)
            });

        await dbContext.SaveChangesAsync();
    }
}
