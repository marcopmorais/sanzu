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

public sealed class TenantUsageIndicatorsServiceTests
{
    [Fact]
    public async Task GetUsageIndicators_ShouldReturnCurrentAndHistoricalMetrics_WhenUsageDataExists()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        await SeedUsageDataAsync(dbContext, tenantId, actorUserId);
        var service = CreateService(dbContext);

        var result = await service.GetUsageIndicatorsAsync(
            tenantId,
            actorUserId,
            7,
            CancellationToken.None);

        result.TenantId.Should().Be(tenantId);
        result.PeriodDays.Should().Be(7);
        result.History.Should().HaveCount(7);
        result.Current.CasesCreated.Should().Be(1);
        result.Current.CasesClosed.Should().Be(1);
        result.Current.ActiveCases.Should().Be(1);
        result.Current.DocumentsUploaded.Should().Be(2);
        result.History.Should().Contain(x => x.DocumentsUploaded > 0);
    }

    [Fact]
    public async Task GetUsageIndicators_ShouldThrowAccessDenied_WhenActorHasNoTenantAdminRole()
    {
        var dbContext = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId, TenantStatus.Active);
        await SeedUserAsync(dbContext, actorUserId, tenantId);
        var service = CreateService(dbContext);

        var act = () => service.GetUsageIndicatorsAsync(
            tenantId,
            actorUserId,
            30,
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantAccessDeniedException>();
    }

    [Fact]
    public async Task GetUsageIndicators_ShouldThrowValidation_WhenPeriodIsOutOfRange()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);

        var act = () => service.GetUsageIndicatorsAsync(
            tenantId,
            actorUserId,
            0,
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private static TenantUsageIndicatorsService CreateService(SanzuDbContext dbContext)
    {
        return new TenantUsageIndicatorsService(
            new OrganizationRepository(dbContext),
            new UserRoleRepository(dbContext),
            new CaseRepository(dbContext),
            new CaseDocumentRepository(dbContext));
    }

    private static SanzuDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SanzuDbContext>()
            .UseInMemoryDatabase($"sanzu-usage-tests-{Guid.NewGuid()}")
            .Options;

        return new SanzuDbContext(options);
    }

    private static async Task<(Guid TenantId, Guid UserId)> SeedTenantWithAdminAsync(
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
                RoleType = PlatformRole.AgencyAdmin,
                TenantId = tenantId,
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
                FullName = fullName ?? "Manager User",
                OrgId = tenantId,
                CreatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedUsageDataAsync(SanzuDbContext dbContext, Guid tenantId, Guid actorUserId)
    {
        var now = DateTime.UtcNow;

        var activeCaseId = Guid.NewGuid();
        var closedCaseId = Guid.NewGuid();
        dbContext.Cases.AddRange(
            new Case
            {
                Id = activeCaseId,
                TenantId = tenantId,
                CaseNumber = "CASE-00001",
                DeceasedFullName = "Usage Active",
                DateOfDeath = now.Date.AddDays(-2),
                CaseType = "GENERAL",
                Urgency = "NORMAL",
                Status = CaseStatus.Active,
                ManagerUserId = actorUserId,
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1)
            },
            new Case
            {
                Id = closedCaseId,
                TenantId = tenantId,
                CaseNumber = "CASE-00002",
                DeceasedFullName = "Usage Closed",
                DateOfDeath = now.Date.AddDays(-15),
                CaseType = "GENERAL",
                Urgency = "NORMAL",
                Status = CaseStatus.Closed,
                ManagerUserId = actorUserId,
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now.AddDays(-5),
                ClosedAt = now.AddDays(-5)
            });

        dbContext.CaseDocuments.AddRange(
            new CaseDocument
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CaseId = activeCaseId,
                FileName = "doc-1.txt",
                ContentType = "text/plain",
                Content = System.Text.Encoding.UTF8.GetBytes("doc-1"),
                SizeBytes = 5,
                UploadedByUserId = actorUserId,
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1)
            },
            new CaseDocument
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CaseId = closedCaseId,
                FileName = "doc-2.txt",
                ContentType = "text/plain",
                Content = System.Text.Encoding.UTF8.GetBytes("doc-2"),
                SizeBytes = 5,
                UploadedByUserId = actorUserId,
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-5)
            });

        await dbContext.SaveChangesAsync();
    }
}
