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

public sealed class CasePlaybookApplicationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CasePlaybookApplicationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateCase_ShouldApplyActivePlaybook_WhenPlaybookExists()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "case-playbook-apply@agency.pt");
        await ActivateTenantAsync(client, signup);

        // Create and activate a playbook
        var playbook = await CreateAndActivatePlaybookAsync(client, signup, "Estate Standard v1");

        // Create a case — should have the playbook applied
        using var createCaseRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Maria Silva",
                DateOfDeath = DateTime.UtcNow.AddDays(-5),
                CaseType = "General",
                Urgency = "Normal"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(createCaseRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CreateCaseResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.PlaybookId.Should().Be(playbook.Id);
        envelope.Data.PlaybookVersion.Should().Be(playbook.Version);

        // Verify the PlaybookApplied audit event
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "PlaybookApplied"
                 && x.CaseId == envelope.Data.CaseId);
    }

    [Fact]
    public async Task CreateCase_ShouldSucceedWithNullPlaybook_WhenNoPlaybookIsActive()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "case-no-playbook@agency.pt");
        await ActivateTenantAsync(client, signup);

        // Create a case without any playbook — should succeed with null playbook fields
        using var createCaseRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Antonio Costa",
                DateOfDeath = DateTime.UtcNow.AddDays(-3),
                CaseType = "General",
                Urgency = "Normal"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(createCaseRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CreateCaseResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.PlaybookId.Should().BeNull();
        envelope.Data.PlaybookVersion.Should().BeNull();

        // Verify no PlaybookApplied audit event
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().NotContain(
            x => x.EventType == "PlaybookApplied"
                 && x.CaseId == envelope.Data.CaseId);
    }

    [Fact]
    public async Task CaseTimeline_ShouldIncludePlaybookAppliedEvent_WhenPlaybookWasApplied()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "case-playbook-timeline@agency.pt");
        await ActivateTenantAsync(client, signup);

        var playbook = await CreateAndActivatePlaybookAsync(client, signup, "Timeline Playbook v1");

        // Create case with playbook
        using var createCaseRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Pedro Santos",
                DateOfDeath = DateTime.UtcNow.AddDays(-1),
                CaseType = "General",
                Urgency = "Normal"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var createResponse = await client.SendAsync(createCaseRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<ApiEnvelope<CreateCaseResponse>>();
        var caseId = createEnvelope!.Data!.CaseId;

        // Get timeline
        using var timelineRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{caseId}/timeline",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var timelineResponse = await client.SendAsync(timelineRequest);
        timelineResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var timelineEnvelope = await timelineResponse.Content.ReadFromJsonAsync<ApiEnvelope<CaseTimelineResponse>>();
        timelineEnvelope.Should().NotBeNull();
        timelineEnvelope!.Data.Should().NotBeNull();
        timelineEnvelope.Data!.Events.Should().Contain(
            e => e.EventType == "PlaybookApplied"
                 && e.Description.Contains("Timeline Playbook v1"));
    }

    [Fact]
    public async Task CaseDetails_ShouldExposePlaybookFields_WhenPlaybookWasApplied()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "case-playbook-details@agency.pt");
        await ActivateTenantAsync(client, signup);

        var playbook = await CreateAndActivatePlaybookAsync(client, signup, "Details Playbook v1");

        using var createCaseRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Ana Ferreira",
                DateOfDeath = DateTime.UtcNow.AddDays(-2),
                CaseType = "General",
                Urgency = "Normal"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var createResponse = await client.SendAsync(createCaseRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<ApiEnvelope<CreateCaseResponse>>();
        var caseId = createEnvelope!.Data!.CaseId;

        // Get case details
        using var detailsRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{caseId}",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var detailsResponse = await client.SendAsync(detailsRequest);
        detailsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detailsEnvelope = await detailsResponse.Content.ReadFromJsonAsync<ApiEnvelope<CaseDetailsResponse>>();
        detailsEnvelope.Should().NotBeNull();
        detailsEnvelope!.Data.Should().NotBeNull();
        detailsEnvelope.Data!.PlaybookId.Should().Be(playbook.Id);
        detailsEnvelope.Data.PlaybookVersion.Should().Be(playbook.Version);
    }

    private async Task<PlaybookResponse> CreateAndActivatePlaybookAsync(
        HttpClient client,
        CreateAgencyAccountResponse signup,
        string name)
    {
        using var createRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/playbooks",
            new CreatePlaybookRequest { Name = name },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var createResponse = await client.SendAsync(createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<ApiEnvelope<PlaybookResponse>>();
        var playbook = createEnvelope!.Data!;

        using var activateRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/playbooks/{playbook.Id}/activate",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var activateResponse = await client.SendAsync(activateRequest);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var activateEnvelope = await activateResponse.Content.ReadFromJsonAsync<ApiEnvelope<PlaybookResponse>>();
        return activateEnvelope!.Data!;
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
                PaymentMethodReference = "pm_playbook_tests",
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

    private static HttpRequestMessage BuildAuthorizedJsonRequest(
        HttpMethod method,
        string uri,
        object payload,
        Guid userId,
        Guid tenantId,
        string role)
    {
        var message = new HttpRequestMessage(method, uri)
        {
            Content = JsonContent.Create(payload)
        };

        message.Headers.Add("X-User-Id", userId.ToString());
        message.Headers.Add("X-Tenant-Id", tenantId.ToString());
        message.Headers.Add("X-User-Role", role);
        return message;
    }

    private static HttpRequestMessage BuildAuthorizedRequest(
        HttpMethod method,
        string uri,
        Guid userId,
        Guid tenantId,
        string role)
    {
        var message = new HttpRequestMessage(method, uri);
        message.Headers.Add("X-User-Id", userId.ToString());
        message.Headers.Add("X-Tenant-Id", tenantId.ToString());
        message.Headers.Add("X-User-Role", role);
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
