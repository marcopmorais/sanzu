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

public sealed class TelemetryControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TelemetryControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetTenantTelemetry_ShouldReturnMetrics_WhenCalledByAgencyAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "telemetry-tenant@agency.pt");
        await ActivateTenantAsync(client, signup);

        // Create a case to generate audit events
        using var createCaseRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Telemetry Test",
                DateOfDeath = DateTime.UtcNow.AddDays(-5),
                CaseType = "General",
                Urgency = "Normal"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var createResponse = await client.SendAsync(createCaseRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Get tenant telemetry
        using var telemetryRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/telemetry?periodDays=30",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(telemetryRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<TrustTelemetryResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.TenantId.Should().Be(signup.OrganizationId);
        envelope.Data.PeriodDays.Should().Be(30);
        envelope.Data.Metrics.Should().NotBeNull();
        envelope.Data.Metrics.CasesCreated.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetTenantTelemetry_ShouldReturn403_WhenCalledByNonAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "telemetry-nonadmin@agency.pt");
        await ActivateTenantAsync(client, signup);

        using var telemetryRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/telemetry?periodDays=30",
            signup.UserId,
            signup.OrganizationId,
            "CaseWorker");

        var response = await client.SendAsync(telemetryRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPlatformTelemetry_ShouldReturnMetrics_WhenCalledBySanzuAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "telemetry-platform@agency.pt");
        await ActivateTenantAsync(client, signup);

        // Create a case to have some data
        using var createCaseRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Platform Telemetry",
                DateOfDeath = DateTime.UtcNow.AddDays(-3),
                CaseType = "General",
                Urgency = "Normal"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var createResponse = await client.SendAsync(createCaseRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Set up SanzuAdmin role
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = signup.UserId,
            TenantId = null,
            RoleType = PlatformRole.SanzuAdmin,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = signup.UserId
        });
        await dbContext.SaveChangesAsync();

        // Get platform telemetry
        using var telemetryRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/telemetry?periodDays=30",
            signup.UserId,
            Guid.Empty,
            "SanzuAdmin");

        var response = await client.SendAsync(telemetryRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<TrustTelemetryResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.TenantId.Should().BeNull();
        envelope.Data.PeriodDays.Should().Be(30);
        envelope.Data.Metrics.Should().NotBeNull();
        envelope.Data.EventSummary.Should().NotBeNull();
        envelope.Data.BlockedByReason.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTenantTelemetry_ShouldIncludeBlockedByReason_WhenBlockedTasksExist()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "telemetry-blocked@agency.pt");
        await ActivateTenantAsync(client, signup);

        // Create a case
        using var createCaseRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Blocked Test",
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

        // Add a blocked workflow step directly
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.WorkflowStepInstances.Add(new WorkflowStepInstance
        {
            Id = Guid.NewGuid(),
            TenantId = signup.OrganizationId,
            CaseId = caseId,
            StepKey = "test-step",
            Title = "Test Step",
            Sequence = 1,
            Status = WorkflowStepStatus.Blocked,
            BlockedReasonCode = BlockedReasonCode.EvidenceMissing,
            BlockedReasonDetail = "Missing death certificate",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        // Get tenant telemetry
        using var telemetryRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/telemetry?periodDays=30",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(telemetryRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<TrustTelemetryResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Metrics.TasksBlocked.Should().BeGreaterThanOrEqualTo(1);
        envelope.Data.BlockedByReason.Should().Contain(
            x => x.ReasonCategory == "EvidenceMissing" && x.Count >= 1);
    }

    [Fact]
    public async Task GetTenantTelemetry_ShouldIncludeEventSummary_WithPlaybookAppliedEvent()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "telemetry-events@agency.pt");
        await ActivateTenantAsync(client, signup);

        // Create and activate a playbook
        using var createPlaybookRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/playbooks",
            new CreatePlaybookRequest { Name = "Telemetry Playbook" },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var playbookResponse = await client.SendAsync(createPlaybookRequest);
        playbookResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var playbookEnvelope = await playbookResponse.Content.ReadFromJsonAsync<ApiEnvelope<PlaybookResponse>>();
        var playbook = playbookEnvelope!.Data!;

        using var activateRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/playbooks/{playbook.Id}/activate",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var activateResponse = await client.SendAsync(activateRequest);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Create a case (should trigger PlaybookApplied event)
        using var createCaseRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Event Summary Test",
                DateOfDeath = DateTime.UtcNow.AddDays(-1),
                CaseType = "General",
                Urgency = "Normal"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var caseResponse = await client.SendAsync(createCaseRequest);
        caseResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Get telemetry
        using var telemetryRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/telemetry?periodDays=30",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(telemetryRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<TrustTelemetryResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Metrics.PlaybooksApplied.Should().BeGreaterThanOrEqualTo(1);
        envelope.Data.EventSummary.Should().Contain(
            x => x.EventType == "PlaybookApplied" && x.Count >= 1);
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
                PaymentMethodReference = "pm_telemetry_tests",
                InvoiceProfileLegalName = "Telemetry Agency Lda",
                InvoiceProfileVatNumber = "PT123456789",
                InvoiceProfileBillingEmail = "billing@telemetry.pt",
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
