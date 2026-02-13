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

public sealed class TenantPolicyControlServiceTests
{
    [Fact]
    public async Task ApplyTenantPolicyControl_ShouldPersistControlAndAudit_WhenActorIsSanzuAdmin()
    {
        var dbContext = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId, TenantStatus.Active);
        await SeedUserAsync(dbContext, actorUserId, tenantId);
        await SeedSanzuAdminRoleAsync(dbContext, actorUserId);
        var service = CreateService(dbContext);

        var response = await service.ApplyTenantPolicyControlAsync(
            tenantId,
            actorUserId,
            new ApplyTenantPolicyControlRequest
            {
                ControlType = "RiskHold",
                IsEnabled = true,
                ReasonCode = "RISK_ESCALATION"
            },
            CancellationToken.None);

        response.TenantId.Should().Be(tenantId);
        response.ControlType.Should().Be(TenantPolicyControlType.RiskHold);
        response.IsEnabled.Should().BeTrue();
        response.ReasonCode.Should().Be("RISK_ESCALATION");
        response.AppliedByUserId.Should().Be(actorUserId);

        dbContext.TenantPolicyControls.Should().Contain(
            x => x.TenantId == tenantId
                 && x.ControlType == TenantPolicyControlType.RiskHold
                 && x.IsEnabled
                 && x.ReasonCode == "RISK_ESCALATION");

        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "TenantPolicyControlApplied"
                 && x.ActorUserId == actorUserId
                 && x.Metadata.Contains("RISK_ESCALATION"));
    }

    [Fact]
    public async Task ApplyTenantPolicyControl_ShouldThrowAccessDenied_WhenActorIsNotSanzuAdmin()
    {
        var dbContext = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId, TenantStatus.Active);
        await SeedUserAsync(dbContext, actorUserId, tenantId);
        var service = CreateService(dbContext);

        var act = () => service.ApplyTenantPolicyControlAsync(
            tenantId,
            actorUserId,
            new ApplyTenantPolicyControlRequest
            {
                ControlType = "ComplianceFlag",
                IsEnabled = true,
                ReasonCode = "COMPLIANCE_REVIEW"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantAccessDeniedException>();
    }

    [Fact]
    public async Task ApplyTenantPolicyControl_ShouldUpdateExistingControl_WhenControlAlreadyExists()
    {
        var dbContext = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId, TenantStatus.Active);
        await SeedUserAsync(dbContext, actorUserId, tenantId);
        await SeedSanzuAdminRoleAsync(dbContext, actorUserId);

        dbContext.TenantPolicyControls.Add(
            new TenantPolicyControl
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ControlType = TenantPolicyControlType.RiskHold,
                IsEnabled = true,
                ReasonCode = "INITIAL_RISK",
                AppliedByUserId = actorUserId,
                AppliedAt = DateTime.UtcNow.AddMinutes(-30),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-30)
            });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var response = await service.ApplyTenantPolicyControlAsync(
            tenantId,
            actorUserId,
            new ApplyTenantPolicyControlRequest
            {
                ControlType = "RiskHold",
                IsEnabled = false,
                ReasonCode = "RISK_CLEARED"
            },
            CancellationToken.None);

        response.IsEnabled.Should().BeFalse();
        response.ReasonCode.Should().Be("RISK_CLEARED");
        dbContext.TenantPolicyControls.Should().ContainSingle(
            x => x.TenantId == tenantId && x.ControlType == TenantPolicyControlType.RiskHold);
    }

    [Fact]
    public async Task ApplyTenantPolicyControl_ShouldThrowValidation_WhenPayloadIsInvalid()
    {
        var dbContext = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId, TenantStatus.Active);
        await SeedUserAsync(dbContext, actorUserId, tenantId);
        await SeedSanzuAdminRoleAsync(dbContext, actorUserId);
        var service = CreateService(dbContext);

        var act = () => service.ApplyTenantPolicyControlAsync(
            tenantId,
            actorUserId,
            new ApplyTenantPolicyControlRequest
            {
                ControlType = string.Empty,
                IsEnabled = true,
                ReasonCode = "bad reason"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private static TenantPolicyControlService CreateService(SanzuDbContext dbContext)
    {
        return new TenantPolicyControlService(
            new OrganizationRepository(dbContext),
            new UserRoleRepository(dbContext),
            new TenantPolicyControlRepository(dbContext),
            new AuditRepository(dbContext),
            new EfUnitOfWork(dbContext),
            new ApplyTenantPolicyControlRequestValidator());
    }

    private static SanzuDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SanzuDbContext>()
            .UseInMemoryDatabase($"sanzu-tenant-policy-controls-tests-{Guid.NewGuid()}")
            .Options;

        return new SanzuDbContext(options);
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
}
