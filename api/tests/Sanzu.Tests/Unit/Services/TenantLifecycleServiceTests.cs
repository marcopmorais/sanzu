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

public sealed class TenantLifecycleServiceTests
{
    [Fact]
    public async Task UpdateTenantLifecycleState_ShouldPersistStatusAndWriteAudit_WhenTransitionIsValid()
    {
        var dbContext = CreateContext();
        var (tenantId, sanzuAdminUserId) = await SeedTenantWithSanzuAdminAsync(dbContext, TenantStatus.Pending);
        var service = CreateService(dbContext);

        var result = await service.UpdateTenantLifecycleStateAsync(
            tenantId,
            sanzuAdminUserId,
            new UpdateTenantLifecycleStateRequest
            {
                TargetStatus = "Suspended",
                Reason = "Manual risk mitigation."
            },
            CancellationToken.None);

        result.TenantId.Should().Be(tenantId);
        result.PreviousStatus.Should().Be(TenantStatus.Pending);
        result.CurrentStatus.Should().Be(TenantStatus.Suspended);
        result.Reason.Should().Be("Manual risk mitigation.");
        result.ChangedByUserId.Should().Be(sanzuAdminUserId);

        var tenant = await dbContext.Organizations.SingleAsync(x => x.Id == tenantId);
        tenant.Status.Should().Be(TenantStatus.Suspended);
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "TenantLifecycleStateChanged"
                 && x.ActorUserId == sanzuAdminUserId
                 && x.Metadata.Contains("Manual risk mitigation."));
    }

    [Fact]
    public async Task UpdateTenantLifecycleState_ShouldThrowAccessDenied_WhenActorIsNotSanzuAdmin()
    {
        var dbContext = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId, TenantStatus.Active);
        await SeedUserAsync(dbContext, actorUserId, tenantId);
        var service = CreateService(dbContext);

        var act = () => service.UpdateTenantLifecycleStateAsync(
            tenantId,
            actorUserId,
            new UpdateTenantLifecycleStateRequest
            {
                TargetStatus = "Suspended",
                Reason = "Unauthorized actor."
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantAccessDeniedException>();
    }

    [Fact]
    public async Task UpdateTenantLifecycleState_ShouldThrowStateException_WhenTransitionIsInvalid()
    {
        var dbContext = CreateContext();
        var (tenantId, sanzuAdminUserId) = await SeedTenantWithSanzuAdminAsync(dbContext, TenantStatus.Pending);
        var service = CreateService(dbContext);

        var act = () => service.UpdateTenantLifecycleStateAsync(
            tenantId,
            sanzuAdminUserId,
            new UpdateTenantLifecycleStateRequest
            {
                TargetStatus = "PaymentIssue",
                Reason = "Should fail."
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantLifecycleStateException>()
            .WithMessage("*Invalid tenant lifecycle transition*");
    }

    [Fact]
    public async Task UpdateTenantLifecycleState_ShouldThrowValidation_WhenRequestIsInvalid()
    {
        var dbContext = CreateContext();
        var (tenantId, sanzuAdminUserId) = await SeedTenantWithSanzuAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);

        var act = () => service.UpdateTenantLifecycleStateAsync(
            tenantId,
            sanzuAdminUserId,
            new UpdateTenantLifecycleStateRequest
            {
                TargetStatus = string.Empty,
                Reason = string.Empty
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private static TenantLifecycleService CreateService(SanzuDbContext dbContext)
    {
        return new TenantLifecycleService(
            new OrganizationRepository(dbContext),
            new UserRoleRepository(dbContext),
            new AuditRepository(dbContext),
            new EfUnitOfWork(dbContext),
            new UpdateTenantLifecycleStateRequestValidator());
    }

    private static SanzuDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SanzuDbContext>()
            .UseInMemoryDatabase($"sanzu-tenant-lifecycle-tests-{Guid.NewGuid()}")
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
}
