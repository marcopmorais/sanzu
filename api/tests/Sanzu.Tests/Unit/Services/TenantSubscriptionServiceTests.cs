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

public sealed class TenantSubscriptionServiceTests
{
    [Fact]
    public async Task PreviewPlanChange_ShouldReturnPreview_WhenUpgrading()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedActiveTenantAsync(dbContext, "STARTER", "MONTHLY");
        var service = CreateService(dbContext);

        var result = await service.PreviewPlanChangeAsync(
            tenantId,
            userId,
            new PreviewPlanChangeRequest { PlanCode = "Growth", BillingCycle = "Monthly" },
            CancellationToken.None);

        result.CurrentPlan.Should().Be("STARTER");
        result.NewPlan.Should().Be("GROWTH");
        result.CurrentMonthlyPrice.Should().Be(149m);
        result.NewMonthlyPrice.Should().Be(399m);
        result.ProrationAmount.Should().BeGreaterThanOrEqualTo(0);
        result.Description.Should().Contain("Upgrade");
    }

    [Fact]
    public async Task PreviewPlanChange_ShouldReturnNegativeProration_WhenDowngrading()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedActiveTenantAsync(dbContext, "GROWTH", "MONTHLY");
        var service = CreateService(dbContext);

        var result = await service.PreviewPlanChangeAsync(
            tenantId,
            userId,
            new PreviewPlanChangeRequest { PlanCode = "Starter", BillingCycle = "Monthly" },
            CancellationToken.None);

        result.CurrentPlan.Should().Be("GROWTH");
        result.NewPlan.Should().Be("STARTER");
        result.ProrationAmount.Should().BeLessThanOrEqualTo(0);
        result.Description.Should().Contain("Downgrade");
    }

    [Fact]
    public async Task PreviewPlanChange_ShouldThrowStateException_WhenSamePlanAndCycle()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedActiveTenantAsync(dbContext, "STARTER", "MONTHLY");
        var service = CreateService(dbContext);

        var act = () => service.PreviewPlanChangeAsync(
            tenantId,
            userId,
            new PreviewPlanChangeRequest { PlanCode = "Starter", BillingCycle = "Monthly" },
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantOnboardingStateException>()
            .WithMessage("*same*");
    }

    [Fact]
    public async Task PreviewPlanChange_ShouldThrowStateException_WhenTenantNotActive()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedTenantAsync(dbContext, TenantStatus.Pending, "STARTER", "MONTHLY");
        var service = CreateService(dbContext);

        var act = () => service.PreviewPlanChangeAsync(
            tenantId,
            userId,
            new PreviewPlanChangeRequest { PlanCode = "Growth", BillingCycle = "Monthly" },
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantOnboardingStateException>()
            .WithMessage("*active*");
    }

    [Fact]
    public async Task ChangePlan_ShouldUpdatePlanAndWriteAudit_WhenUpgrading()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedActiveTenantAsync(dbContext, "STARTER", "MONTHLY");
        var service = CreateService(dbContext);

        var preview = await service.PreviewPlanChangeAsync(
            tenantId,
            userId,
            new PreviewPlanChangeRequest { PlanCode = "Growth", BillingCycle = "Monthly" },
            CancellationToken.None);

        var result = await service.ChangePlanAsync(
            tenantId,
            userId,
            new ChangePlanRequest
            {
                PlanCode = "Growth",
                BillingCycle = "Monthly",
                ConfirmedProrationAmount = preview.ProrationAmount
            },
            CancellationToken.None);

        result.PlanCode.Should().Be("GROWTH");
        result.PreviousPlan.Should().Be("STARTER");
        result.TenantId.Should().Be(tenantId);

        var tenant = await dbContext.Organizations.FindAsync(tenantId);
        tenant!.SubscriptionPlan.Should().Be("GROWTH");
        tenant.PreviousSubscriptionPlan.Should().Be("STARTER");

        var audit = await dbContext.AuditEvents.FirstAsync(x => x.EventType == "TenantSubscriptionPlanChanged");
        audit.Metadata.Should().Contain("STARTER");
        audit.Metadata.Should().Contain("GROWTH");
    }

    [Fact]
    public async Task ChangePlan_ShouldThrowConflict_WhenProrationMismatch()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedActiveTenantAsync(dbContext, "STARTER", "MONTHLY");
        var service = CreateService(dbContext);

        var act = () => service.ChangePlanAsync(
            tenantId,
            userId,
            new ChangePlanRequest
            {
                PlanCode = "Growth",
                BillingCycle = "Monthly",
                ConfirmedProrationAmount = 9999.99m
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantOnboardingConflictException>()
            .WithMessage("*proration*");
    }

    [Fact]
    public async Task ChangePlan_ShouldThrowStateException_WhenSamePlanAndCycle()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedActiveTenantAsync(dbContext, "GROWTH", "MONTHLY");
        var service = CreateService(dbContext);

        var act = () => service.ChangePlanAsync(
            tenantId,
            userId,
            new ChangePlanRequest
            {
                PlanCode = "Growth",
                BillingCycle = "Monthly",
                ConfirmedProrationAmount = 0m
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantOnboardingStateException>()
            .WithMessage("*same*");
    }

    [Fact]
    public async Task CancelSubscription_ShouldSuspendTenantAndWriteAudit_WhenValid()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedActiveTenantAsync(dbContext, "GROWTH", "MONTHLY");
        var service = CreateService(dbContext);

        var result = await service.CancelSubscriptionAsync(
            tenantId,
            userId,
            new CancelSubscriptionRequest
            {
                Reason = "We no longer need this service for our agency operations.",
                Confirmed = true
            },
            CancellationToken.None);

        result.TenantId.Should().Be(tenantId);
        result.TenantStatus.Should().Be(TenantStatus.Suspended);
        result.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var tenant = await dbContext.Organizations.FindAsync(tenantId);
        tenant!.Status.Should().Be(TenantStatus.Suspended);
        tenant.SubscriptionCancelledAt.Should().NotBeNull();
        tenant.SubscriptionCancellationReason.Should().Contain("no longer need");

        var audit = await dbContext.AuditEvents.FirstAsync(x => x.EventType == "TenantSubscriptionCancelled");
        audit.Metadata.Should().Contain("Suspended");
    }

    [Fact]
    public async Task CancelSubscription_ShouldThrowStateException_WhenAlreadyCancelled()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedActiveTenantAsync(dbContext, "GROWTH", "MONTHLY");

        var tenant = await dbContext.Organizations.FindAsync(tenantId);
        tenant!.SubscriptionCancelledAt = DateTime.UtcNow.AddDays(-1);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var act = () => service.CancelSubscriptionAsync(
            tenantId,
            userId,
            new CancelSubscriptionRequest
            {
                Reason = "We no longer need this service for our agency operations.",
                Confirmed = true
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantOnboardingStateException>()
            .WithMessage("*already*cancelled*");
    }

    [Fact]
    public async Task CancelSubscription_ShouldThrowStateException_WhenTenantNotActive()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedTenantAsync(dbContext, TenantStatus.Pending, "STARTER", "MONTHLY");
        var service = CreateService(dbContext);

        var act = () => service.CancelSubscriptionAsync(
            tenantId,
            userId,
            new CancelSubscriptionRequest
            {
                Reason = "We no longer need this service for our agency operations.",
                Confirmed = true
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantOnboardingStateException>()
            .WithMessage("*active*");
    }

    [Fact]
    public async Task ChangePlan_ShouldHandleBillingCycleChange_WhenOnlyBillingCycleChanges()
    {
        var dbContext = CreateContext();
        var (tenantId, userId) = await SeedActiveTenantAsync(dbContext, "STARTER", "MONTHLY");
        var service = CreateService(dbContext);

        var preview = await service.PreviewPlanChangeAsync(
            tenantId,
            userId,
            new PreviewPlanChangeRequest { PlanCode = "Starter", BillingCycle = "Annual" },
            CancellationToken.None);

        var result = await service.ChangePlanAsync(
            tenantId,
            userId,
            new ChangePlanRequest
            {
                PlanCode = "Starter",
                BillingCycle = "Annual",
                ConfirmedProrationAmount = preview.ProrationAmount
            },
            CancellationToken.None);

        result.PlanCode.Should().Be("STARTER");
        result.BillingCycle.Should().Be("ANNUAL");
        result.PreviousBillingCycle.Should().Be("MONTHLY");
    }

    private static SanzuDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SanzuDbContext>()
            .UseInMemoryDatabase($"sanzu-sub-tests-{Guid.NewGuid()}")
            .Options;

        return new SanzuDbContext(options);
    }

    private static async Task<(Guid TenantId, Guid UserId)> SeedActiveTenantAsync(
        SanzuDbContext dbContext,
        string plan,
        string billingCycle)
    {
        return await SeedTenantAsync(dbContext, TenantStatus.Active, plan, billingCycle);
    }

    private static async Task<(Guid TenantId, Guid UserId)> SeedTenantAsync(
        SanzuDbContext dbContext,
        TenantStatus status,
        string plan,
        string billingCycle)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var org = new Organization
        {
            Id = tenantId,
            Name = "Test Agency",
            Location = "Lisbon",
            Status = status,
            SubscriptionPlan = plan,
            SubscriptionBillingCycle = billingCycle,
            SubscriptionActivatedAt = DateTime.UtcNow.AddDays(-15)
        };
        dbContext.Organizations.Add(org);

        var user = new User
        {
            Id = userId,
            Email = $"admin-{Guid.NewGuid():N}@agency.pt",
            FullName = "Admin User",
            OrgId = tenantId
        };
        dbContext.Users.Add(user);

        var role = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleType = PlatformRole.AgencyAdmin,
            TenantId = tenantId,
            GrantedBy = userId
        };
        dbContext.UserRoles.Add(role);

        await dbContext.SaveChangesAsync();
        return (tenantId, userId);
    }

    private static TenantSubscriptionService CreateService(SanzuDbContext dbContext)
    {
        return new TenantSubscriptionService(
            new OrganizationRepository(dbContext),
            new UserRoleRepository(dbContext),
            new AuditRepository(dbContext),
            new EfUnitOfWork(dbContext),
            new PreviewPlanChangeRequestValidator(),
            new ChangePlanRequestValidator(),
            new CancelSubscriptionRequestValidator());
    }
}
