using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Tests.Integration.Controllers;

public sealed class TenantOnboardingControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TenantOnboardingControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateOnboardingProfile_ShouldReturn200AndMoveTenantToOnboarding_WhenAuthorizedAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "onboarding-admin-1@agency.pt");

        var request = new UpdateTenantOnboardingProfileRequest
        {
            AgencyName = "Agency Updated",
            Location = "Porto"
        };

        using var message = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/profile",
            request,
            signup.UserId,
            signup.OrganizationId);

        var response = await client.SendAsync(message);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<TenantOnboardingProfileResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.TenantStatus.Should().Be(Core.Enums.TenantStatus.Onboarding);
    }

    [Fact]
    public async Task CreateOnboardingInvitation_ShouldReturnConflict_WhenDuplicatePendingInviteExists()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "onboarding-admin-2@agency.pt");
        var invitationRequest = new CreateTenantInvitationRequest
        {
            Email = "invitee@agency.pt",
            RoleType = Core.Enums.PlatformRole.AgencyAdmin,
            ExpirationDays = 7
        };

        using var first = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/invitations",
            invitationRequest,
            signup.UserId,
            signup.OrganizationId);
        var firstResponse = await client.SendAsync(first);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var second = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/invitations",
            invitationRequest,
            signup.UserId,
            signup.OrganizationId);
        var secondResponse = await client.SendAsync(second);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CompleteOnboarding_ShouldReturn401_WhenRequestIsUnauthenticated()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "onboarding-admin-3@agency.pt");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/complete",
            new CompleteTenantOnboardingRequest { ConfirmCompletion = true });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateOnboardingDefaults_ShouldReturn403_WhenActorBelongsToDifferentTenant()
    {
        var client = _factory.CreateClient();
        var tenantA = await CreateTenantAsync(client, "tenant-a-admin@agency.pt");
        var tenantB = await CreateTenantAsync(client, "tenant-b-admin@agency.pt");

        var request = new UpdateTenantOnboardingDefaultsRequest
        {
            DefaultLocale = "pt-PT",
            DefaultTimeZone = "Europe/Lisbon",
            DefaultCurrency = "EUR"
        };

        using var message = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{tenantA.OrganizationId}/onboarding/defaults",
            request,
            tenantB.UserId,
            tenantB.OrganizationId);

        var response = await client.SendAsync(message);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CompleteOnboarding_ShouldPersistCompletionMarkerAndAudit_WhenDefaultsExist()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "onboarding-admin-4@agency.pt");

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
            signup.OrganizationId);
        var defaultsResponse = await client.SendAsync(defaultsRequest);
        defaultsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var completionRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/complete",
            new CompleteTenantOnboardingRequest { ConfirmCompletion = true },
            signup.UserId,
            signup.OrganizationId);
        var completionResponse = await client.SendAsync(completionRequest);
        completionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var organization = await dbContext.Organizations.FindAsync(signup.OrganizationId);
        organization.Should().NotBeNull();
        organization!.OnboardingCompletedAt.Should().NotBeNull();
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantOnboardingDefaultsUpdated");
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantOnboardingCompleted");
    }

    private static HttpRequestMessage BuildAuthorizedJsonRequest(
        HttpMethod method,
        string uri,
        object payload,
        Guid userId,
        Guid tenantId)
    {
        var message = new HttpRequestMessage(method, uri)
        {
            Content = JsonContent.Create(payload)
        };

        message.Headers.Add("X-User-Id", userId.ToString());
        message.Headers.Add("X-Tenant-Id", tenantId.ToString());
        message.Headers.Add("X-User-Role", "AgencyAdmin");
        return message;
    }

    private static async Task<CreateAgencyAccountResponse> CreateTenantAsync(HttpClient client, string email)
    {
        var request = new CreateAgencyAccountRequest
        {
            Email = email,
            FullName = "Agency Admin",
            AgencyName = "Agency",
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
