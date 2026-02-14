using FluentValidation;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Notifications;
using Sanzu.Core.Services;
using Sanzu.Core.Validators;
using Sanzu.Infrastructure.Data;
using Sanzu.Infrastructure.Repositories;

namespace Sanzu.Tests.Unit.Services;

public sealed class TenantOnboardingServiceTests
{
    [Fact]
    public async Task CreateAgencyAccount_ShouldCreateOrgInPendingState_WhenValidInput()
    {
        var dbContext = CreateContext();
        var service = CreateService(dbContext);
        var request = BuildValidRequest();

        var result = await service.CreateAgencyAccountAsync(request, CancellationToken.None);

        var organization = await dbContext.Organizations.FirstAsync(x => x.Id == result.OrganizationId);
        organization.Status.Should().Be(TenantStatus.Pending);
    }

    [Fact]
    public async Task CreateAgencyAccount_ShouldAssignAgencyAdminRole_WhenAccountCreated()
    {
        var dbContext = CreateContext();
        var service = CreateService(dbContext);
        var request = BuildValidRequest();

        var result = await service.CreateAgencyAccountAsync(request, CancellationToken.None);

        var role = await dbContext.UserRoles.SingleAsync(x => x.UserId == result.UserId);
        role.RoleType.Should().Be(PlatformRole.AgencyAdmin);
        role.TenantId.Should().Be(result.OrganizationId);
    }

    [Fact]
    public async Task CreateAgencyAccount_ShouldWriteAuditEvent_WhenAccountCreated()
    {
        var dbContext = CreateContext();
        var service = CreateService(dbContext);
        var request = BuildValidRequest();

        var result = await service.CreateAgencyAccountAsync(request, CancellationToken.None);

        var auditEvent = await dbContext.AuditEvents.SingleAsync(x => x.ActorUserId == result.UserId);
        auditEvent.EventType.Should().Be("TenantCreated");
        auditEvent.Metadata.Should().Contain(result.OrganizationId.ToString());
        auditEvent.Metadata.Should().Contain(result.UserId.ToString());
    }

    [Fact]
    public async Task CreateAgencyAccount_ShouldReturnError_WhenEmailAlreadyExists()
    {
        var dbContext = CreateContext();
        var service = CreateService(dbContext);
        var request = BuildValidRequest();

        await service.CreateAgencyAccountAsync(request, CancellationToken.None);
        var act = () => service.CreateAgencyAccountAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateEmailException>();
    }

    [Fact]
    public async Task UpdateCaseDefaults_ShouldIncrementVersionAndWriteAudit_WhenDefaultsChange()
    {
        var dbContext = CreateContext();
        var service = CreateService(dbContext);
        var signup = await service.CreateAgencyAccountAsync(BuildValidRequest(), CancellationToken.None);
        var tenant = await dbContext.Organizations.SingleAsync(x => x.Id == signup.OrganizationId);
        tenant.Status = TenantStatus.Active;
        await dbContext.SaveChangesAsync();

        var result = await service.UpdateCaseDefaultsAsync(
            signup.OrganizationId,
            signup.UserId,
            new UpdateTenantCaseDefaultsRequest
            {
                DefaultWorkflowKey = "workflow.v2",
                DefaultTemplateKey = "template.v4"
            },
            CancellationToken.None);

        result.TenantId.Should().Be(signup.OrganizationId);
        result.DefaultWorkflowKey.Should().Be("workflow.v2");
        result.DefaultTemplateKey.Should().Be("template.v4");
        result.Version.Should().Be(1);

        var persistedTenant = await dbContext.Organizations.SingleAsync(x => x.Id == signup.OrganizationId);
        persistedTenant.DefaultWorkflowKey.Should().Be("workflow.v2");
        persistedTenant.DefaultTemplateKey.Should().Be("template.v4");
        persistedTenant.CaseDefaultsVersion.Should().Be(1);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantCaseDefaultsUpdated");
    }

    [Fact]
    public async Task GetCaseDefaults_ShouldReturnCurrentDefaultsAndVersion_WhenTenantAdminAuthorized()
    {
        var dbContext = CreateContext();
        var service = CreateService(dbContext);
        var signup = await service.CreateAgencyAccountAsync(BuildValidRequest(), CancellationToken.None);
        var tenant = await dbContext.Organizations.SingleAsync(x => x.Id == signup.OrganizationId);
        tenant.DefaultWorkflowKey = "workflow.v1";
        tenant.DefaultTemplateKey = "template.v2";
        tenant.CaseDefaultsVersion = 3;
        tenant.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var result = await service.GetCaseDefaultsAsync(
            signup.OrganizationId,
            signup.UserId,
            CancellationToken.None);

        result.TenantId.Should().Be(signup.OrganizationId);
        result.DefaultWorkflowKey.Should().Be("workflow.v1");
        result.DefaultTemplateKey.Should().Be("template.v2");
        result.Version.Should().Be(3);
    }

    private static SanzuDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SanzuDbContext>()
            .UseInMemoryDatabase($"sanzu-tests-{Guid.NewGuid()}")
            .Options;

        return new SanzuDbContext(options);
    }

    private static TenantOnboardingService CreateService(SanzuDbContext dbContext)
    {
        IValidator<CreateAgencyAccountRequest> validator = new CreateAgencyAccountRequestValidator();

        return new TenantOnboardingService(
            new OrganizationRepository(dbContext),
            new UserRepository(dbContext),
            new UserRoleRepository(dbContext),
            new AuditRepository(dbContext),
            new TenantInvitationRepository(dbContext),
            new NullNotificationSender(),
            new EfUnitOfWork(dbContext),
            validator,
            new UpdateTenantOnboardingProfileRequestValidator(),
            new UpdateTenantOnboardingDefaultsRequestValidator(),
            new UpdateTenantCaseDefaultsRequestValidator(),
            new CreateTenantInvitationRequestValidator(),
            new CompleteTenantOnboardingRequestValidator(),
            new ActivateTenantBillingRequestValidator());
    }

    private static CreateAgencyAccountRequest BuildValidRequest()
    {
        return new CreateAgencyAccountRequest
        {
            Email = "owner@agency.pt",
            FullName = "Agency Owner",
            AgencyName = "Lisbon Agency",
            Location = "Lisbon"
        };
    }

    private sealed class NullNotificationSender : Sanzu.Core.Interfaces.ITenantInvitationNotificationSender
    {
        public Task SendTenantInviteAsync(TenantInvitationNotification notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
