using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Tests.Integration.Controllers;

public sealed class CaseParticipantsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CaseParticipantsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task InviteParticipant_ShouldReturn201AndCreatePendingInvitation_WhenCaseIsActive()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "case-participants-admin-1@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Invite Participant Case");
        await MoveCaseToActiveAsync(client, signup, createdCase.CaseId);

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/participants",
            new InviteCaseParticipantRequest
            {
                Email = "family.editor@agency.pt",
                Role = "Editor",
                ExpirationDays = 7
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<InviteCaseParticipantResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Participant.Status.Should().Be(CaseParticipantStatus.Pending);
        envelope.Data.Participant.Role.Should().Be(CaseRole.Editor);
        envelope.Data.InvitationToken.Should().NotBeNullOrWhiteSpace();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.CaseParticipants.Should().Contain(x => x.Id == envelope.Data.Participant.ParticipantId && x.Status == CaseParticipantStatus.Pending);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "CaseParticipantInvited" && x.CaseId == createdCase.CaseId);
    }

    [Fact]
    public async Task InviteParticipant_ShouldReturn409_WhenCaseIsNotActive()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "case-participants-admin-2@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Draft Participant Case");

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/participants",
            new InviteCaseParticipantRequest
            {
                Email = "family.reader@agency.pt",
                Role = "Reader"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AcceptInvitation_ShouldReturn200AndProvisionParticipantAccess_WhenTokenMatches()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "case-participants-admin-3@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Acceptance Participant Case");
        await MoveCaseToActiveAsync(client, signup, createdCase.CaseId);
        var invitedUser = await CreateTenantUserAsync(signup.OrganizationId, "family.accept@agency.pt");

        using var inviteRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/participants",
            new InviteCaseParticipantRequest
            {
                Email = invitedUser.Email,
                Role = "Editor"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var inviteResponse = await client.SendAsync(inviteRequest);
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var inviteEnvelope = await inviteResponse.Content.ReadFromJsonAsync<ApiEnvelope<InviteCaseParticipantResponse>>();
        inviteEnvelope.Should().NotBeNull();
        inviteEnvelope!.Data.Should().NotBeNull();

        using var acceptRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/participants/{inviteEnvelope.Data!.Participant.ParticipantId}/accept",
            new AcceptCaseParticipantInvitationRequest
            {
                InvitationToken = inviteEnvelope.Data.InvitationToken
            },
            invitedUser.Id,
            signup.OrganizationId,
            role: null);

        var acceptResponse = await client.SendAsync(acceptRequest);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var acceptedEnvelope = await acceptResponse.Content.ReadFromJsonAsync<ApiEnvelope<CaseParticipantResponse>>();
        acceptedEnvelope.Should().NotBeNull();
        acceptedEnvelope!.Data.Should().NotBeNull();
        acceptedEnvelope.Data!.Status.Should().Be(CaseParticipantStatus.Accepted);
        acceptedEnvelope.Data.ParticipantUserId.Should().Be(invitedUser.Id);
        acceptedEnvelope.Data.Role.Should().Be(CaseRole.Editor);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.CaseParticipants.Should().Contain(
            x => x.Id == acceptedEnvelope.Data.ParticipantId
                 && x.Status == CaseParticipantStatus.Accepted
                 && x.ParticipantUserId == invitedUser.Id);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "CaseParticipantAccepted" && x.CaseId == createdCase.CaseId);
    }

    [Fact]
    public async Task UpdateParticipantRole_ShouldReturn200AndPersistRole_WhenAdminUpdatesRole()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "case-participants-admin-4@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Role Update Participant Case");
        await MoveCaseToActiveAsync(client, signup, createdCase.CaseId);

        using var inviteRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/participants",
            new InviteCaseParticipantRequest
            {
                Email = "family.role@agency.pt",
                Role = "Editor"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var inviteResponse = await client.SendAsync(inviteRequest);
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var inviteEnvelope = await inviteResponse.Content.ReadFromJsonAsync<ApiEnvelope<InviteCaseParticipantResponse>>();
        inviteEnvelope.Should().NotBeNull();
        inviteEnvelope!.Data.Should().NotBeNull();

        using var updateRoleRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/participants/{inviteEnvelope.Data!.Participant.ParticipantId}/role",
            new UpdateCaseParticipantRoleRequest
            {
                Role = "Reader"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var updateRoleResponse = await client.SendAsync(updateRoleRequest);
        updateRoleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateRoleEnvelope = await updateRoleResponse.Content.ReadFromJsonAsync<ApiEnvelope<CaseParticipantResponse>>();
        updateRoleEnvelope.Should().NotBeNull();
        updateRoleEnvelope!.Data.Should().NotBeNull();
        updateRoleEnvelope.Data!.Role.Should().Be(CaseRole.Reader);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.CaseParticipants.Should().Contain(x => x.Id == updateRoleEnvelope.Data.ParticipantId && x.Role == CaseRole.Reader);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "CaseParticipantRoleUpdated" && x.CaseId == createdCase.CaseId);
    }

    private async Task ActivateTenantAsync(HttpClient client, CreateAgencyAccountResponse signup)
    {
        using var defaultsRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/defaults",
            new UpdateTenantOnboardingDefaultsRequest
            {
                DefaultLocale = "pt-PT",
                DefaultTimeZone = "Europe/Lisbon",
                DefaultCurrency = "EUR"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var defaultsResponse = await client.SendAsync(defaultsRequest);
        defaultsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var completionRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/complete",
            new CompleteTenantOnboardingRequest { ConfirmCompletion = true },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var completionResponse = await client.SendAsync(completionRequest);
        completionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var activationRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/billing/activate",
            new ActivateTenantBillingRequest
            {
                PlanCode = "Growth",
                BillingCycle = "Monthly",
                PaymentMethodType = "Card",
                PaymentMethodReference = "pm_case_participant_tests",
                InvoiceProfileLegalName = "Agency Lda",
                InvoiceProfileVatNumber = "PT123456789",
                InvoiceProfileBillingEmail = "billing@agency.pt",
                InvoiceProfileCountryCode = "PT"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var activationResponse = await client.SendAsync(activationRequest);
        activationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<CreateCaseResponse> CreateCaseAsync(
        HttpClient client,
        CreateAgencyAccountResponse signup,
        string deceasedFullName)
    {
        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = deceasedFullName,
                DateOfDeath = DateTime.UtcNow.AddDays(-2),
                CaseType = "General",
                Urgency = "Normal"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CreateCaseResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        return envelope.Data!;
    }

    private static async Task MoveCaseToActiveAsync(HttpClient client, CreateAgencyAccountResponse signup, Guid caseId)
    {
        foreach (var targetStatus in new[] { "Intake", "Active" })
        {
            using var request = BuildAuthorizedJsonRequest(
                HttpMethod.Patch,
                $"/api/v1/tenants/{signup.OrganizationId}/cases/{caseId}/lifecycle",
                new UpdateCaseLifecycleRequest
                {
                    TargetStatus = targetStatus
                },
                signup.UserId,
                signup.OrganizationId,
                "AgencyAdmin");

            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    private async Task<User> CreateTenantUserAsync(Guid tenantId, string email)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            FullName = "Family Participant",
            OrgId = tenantId,
            CreatedAt = DateTime.UtcNow
        };

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private static HttpRequestMessage BuildAuthorizedJsonRequest(
        HttpMethod method,
        string uri,
        object payload,
        Guid userId,
        Guid tenantId,
        string? role)
    {
        var message = new HttpRequestMessage(method, uri)
        {
            Content = JsonContent.Create(payload)
        };

        message.Headers.Add("X-User-Id", userId.ToString());
        message.Headers.Add("X-Tenant-Id", tenantId.ToString());
        if (!string.IsNullOrWhiteSpace(role))
        {
            message.Headers.Add("X-User-Role", role);
        }

        return message;
    }

    private static async Task<CreateAgencyAccountResponse> CreateTenantAsync(HttpClient client, string email)
    {
        var request = new CreateAgencyAccountRequest
        {
            Email = email,
            FullName = "Case Participant Admin",
            AgencyName = "Case Participant Agency",
            Location = "Lisbon"
        };

        var signupResponse = await client.PostAsJsonAsync("/api/v1/tenants/signup", request);
        signupResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var envelope = await signupResponse.Content.ReadFromJsonAsync<ApiEnvelope<CreateAgencyAccountResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        return envelope.Data!;
    }
}
