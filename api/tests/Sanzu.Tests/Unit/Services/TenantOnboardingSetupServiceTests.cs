using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Notifications;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Services;
using Sanzu.Core.Validators;
using Sanzu.Infrastructure.Data;
using Sanzu.Infrastructure.Repositories;

namespace Sanzu.Tests.Unit.Services;

public sealed class TenantOnboardingSetupServiceTests
{
    [Fact]
    public async Task UpdateOnboardingProfile_ShouldMoveTenantToOnboardingAndWriteAudit_WhenTenantIsPending()
    {
        var dbContext = CreateContext();
        var seed = await SeedTenantAdminAsync(dbContext);
        var service = CreateService(dbContext);

        var response = await service.UpdateOnboardingProfileAsync(
            seed.TenantId,
            seed.AdminUserId,
            new UpdateTenantOnboardingProfileRequest
            {
                AgencyName = "Updated Agency",
                Location = "Porto"
            },
            CancellationToken.None);

        var tenant = await dbContext.Organizations.SingleAsync(x => x.Id == seed.TenantId);
        tenant.Status.Should().Be(TenantStatus.Onboarding);
        tenant.Name.Should().Be("Updated Agency");
        response.TenantStatus.Should().Be(TenantStatus.Onboarding);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantOnboardingProfileUpdated");
    }

    [Fact]
    public async Task CreateInvitation_ShouldReturnConflict_WhenExistingTenantMemberUsesSameEmail()
    {
        var dbContext = CreateContext();
        var seed = await SeedTenantAdminAsync(dbContext);
        dbContext.Users.Add(
            new User
            {
                Id = Guid.NewGuid(),
                Email = "member@agency.pt",
                FullName = "Existing Member",
                OrgId = seed.TenantId
            });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var act = () => service.CreateInvitationAsync(
            seed.TenantId,
            seed.AdminUserId,
            new CreateTenantInvitationRequest
            {
                Email = "member@agency.pt",
                RoleType = PlatformRole.AgencyAdmin,
                ExpirationDays = 7
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantOnboardingConflictException>();
    }

    [Fact]
    public async Task CompleteOnboarding_ShouldFail_WhenDefaultsAreMissing()
    {
        var dbContext = CreateContext();
        var seed = await SeedTenantAdminAsync(dbContext);
        var service = CreateService(dbContext);

        var act = () => service.CompleteOnboardingAsync(
            seed.TenantId,
            seed.AdminUserId,
            new CompleteTenantOnboardingRequest { ConfirmCompletion = true },
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantOnboardingStateException>();
    }

    [Fact]
    public async Task CompleteOnboarding_ShouldSetCompletionMarkerAndWriteAudit_WhenDefaultsExist()
    {
        var dbContext = CreateContext();
        var seed = await SeedTenantAdminAsync(dbContext);
        var tenant = await dbContext.Organizations.SingleAsync(x => x.Id == seed.TenantId);
        tenant.DefaultLocale = "pt-PT";
        tenant.DefaultTimeZone = "Europe/Lisbon";
        tenant.DefaultCurrency = "EUR";
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.CompleteOnboardingAsync(
            seed.TenantId,
            seed.AdminUserId,
            new CompleteTenantOnboardingRequest { ConfirmCompletion = true },
            CancellationToken.None);

        result.OnboardingCompletedAt.Should().NotBe(default);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantOnboardingCompleted");
    }

    private static SanzuDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SanzuDbContext>()
            .UseInMemoryDatabase($"sanzu-onboarding-tests-{Guid.NewGuid()}")
            .Options;

        return new SanzuDbContext(options);
    }

    private static async Task<(Guid TenantId, Guid AdminUserId)> SeedTenantAdminAsync(SanzuDbContext dbContext)
    {
        var tenantId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        dbContext.Organizations.Add(
            new Organization
            {
                Id = tenantId,
                Name = "Agency",
                Location = "Lisbon",
                Status = TenantStatus.Pending
            });

        dbContext.Users.Add(
            new User
            {
                Id = adminUserId,
                Email = "admin@agency.pt",
                FullName = "Admin",
                OrgId = tenantId
            });

        dbContext.UserRoles.Add(
            new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = adminUserId,
                TenantId = tenantId,
                RoleType = PlatformRole.AgencyAdmin,
                GrantedBy = adminUserId
            });

        await dbContext.SaveChangesAsync();
        return (tenantId, adminUserId);
    }

    private static TenantOnboardingService CreateService(SanzuDbContext dbContext)
    {
        return new TenantOnboardingService(
            new OrganizationRepository(dbContext),
            new UserRepository(dbContext),
            new UserRoleRepository(dbContext),
            new AuditRepository(dbContext),
            new TenantInvitationRepository(dbContext),
            new NullNotificationSender(),
            new EfUnitOfWork(dbContext),
            new CreateAgencyAccountRequestValidator(),
            new UpdateTenantOnboardingProfileRequestValidator(),
            new UpdateTenantOnboardingDefaultsRequestValidator(),
            new CreateTenantInvitationRequestValidator(),
            new CompleteTenantOnboardingRequestValidator());
    }

    private sealed class NullNotificationSender : ITenantInvitationNotificationSender
    {
        public Task SendTenantInviteAsync(TenantInvitationNotification notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
