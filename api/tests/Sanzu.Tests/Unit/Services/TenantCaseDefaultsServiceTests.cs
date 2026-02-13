using FluentAssertions;
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

public sealed class TenantCaseDefaultsServiceTests
{
    [Fact]
    public async Task UpdateCaseDefaults_ShouldPersistDefaultsAndWriteAudit_WhenActorIsTenantAdmin()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);

        var result = await service.UpdateCaseDefaultsAsync(
            tenantId,
            actorUserId,
            new UpdateTenantCaseDefaultsRequest
            {
                DefaultWorkflowKey = "workflow.v2",
                DefaultTemplateKey = "template.v7"
            },
            CancellationToken.None);

        result.TenantId.Should().Be(tenantId);
        result.DefaultWorkflowKey.Should().Be("workflow.v2");
        result.DefaultTemplateKey.Should().Be("template.v7");
        result.Version.Should().BeGreaterThan(0);

        var tenant = await dbContext.Organizations.SingleAsync(x => x.Id == tenantId);
        tenant.DefaultWorkflowKey.Should().Be("workflow.v2");
        tenant.DefaultTemplateKey.Should().Be("template.v7");
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantCaseDefaultsUpdated");
    }

    [Fact]
    public async Task GetCaseDefaults_ShouldReturnConfiguredValues_WhenActorIsTenantAdmin()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var tenant = await dbContext.Organizations.SingleAsync(x => x.Id == tenantId);
        tenant.DefaultWorkflowKey = "workflow.v1";
        tenant.DefaultTemplateKey = "template.v1";
        tenant.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetCaseDefaultsAsync(tenantId, actorUserId, CancellationToken.None);

        result.TenantId.Should().Be(tenantId);
        result.DefaultWorkflowKey.Should().Be("workflow.v1");
        result.DefaultTemplateKey.Should().Be("template.v1");
        result.Version.Should().Be(tenant.UpdatedAt.ToUniversalTime().Ticks);
    }

    [Fact]
    public async Task UpdateCaseDefaults_ShouldThrowAccessDenied_WhenActorHasNoTenantAdminRole()
    {
        var dbContext = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId, TenantStatus.Active);
        await SeedUserAsync(dbContext, actorUserId, tenantId);
        var service = CreateService(dbContext);

        var act = () => service.UpdateCaseDefaultsAsync(
            tenantId,
            actorUserId,
            new UpdateTenantCaseDefaultsRequest
            {
                DefaultWorkflowKey = "workflow.denied"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantAccessDeniedException>();
    }

    private static TenantCaseDefaultsService CreateService(SanzuDbContext dbContext)
    {
        return new TenantCaseDefaultsService(
            new OrganizationRepository(dbContext),
            new UserRoleRepository(dbContext),
            new AuditRepository(dbContext),
            new EfUnitOfWork(dbContext),
            new UpdateTenantCaseDefaultsRequestValidator());
    }

    private static SanzuDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SanzuDbContext>()
            .UseInMemoryDatabase($"sanzu-tenant-defaults-tests-{Guid.NewGuid()}")
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
}
