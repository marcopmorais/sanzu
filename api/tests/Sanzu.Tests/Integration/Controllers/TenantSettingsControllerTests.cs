using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Tests.Integration.Controllers;

public sealed class TenantSettingsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TenantSettingsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CaseDefaultsEndpoints_ShouldReturnVersionedDefaultsAndApplyToNewCases_WhenTenantAdminAuthorized()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "settings-admin-1@agency.pt");
        await SetDefaultsAndCompleteOnboardingAsync(client, signup);
        await ActivateBillingAsync(client, signup);

        using var updateRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/case-defaults",
            new UpdateTenantCaseDefaultsRequest
            {
                DefaultWorkflowKey = "workflow.v2",
                DefaultTemplateKey = "template.v5"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var updateResponse = await client.SendAsync(updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateEnvelope = await updateResponse.Content.ReadFromJsonAsync<ApiEnvelope<TenantCaseDefaultsResponse>>();
        updateEnvelope.Should().NotBeNull();
        updateEnvelope!.Data.Should().NotBeNull();
        updateEnvelope.Data!.DefaultWorkflowKey.Should().Be("workflow.v2");
        updateEnvelope.Data.DefaultTemplateKey.Should().Be("template.v5");
        updateEnvelope.Data.Version.Should().BeGreaterThan(0);

        using var getRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/case-defaults",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var getResponse = await client.SendAsync(getRequest);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getEnvelope = await getResponse.Content.ReadFromJsonAsync<ApiEnvelope<TenantCaseDefaultsResponse>>();
        getEnvelope.Should().NotBeNull();
        getEnvelope!.Data.Should().NotBeNull();
        getEnvelope.Data!.DefaultWorkflowKey.Should().Be("workflow.v2");
        getEnvelope.Data.DefaultTemplateKey.Should().Be("template.v5");
        getEnvelope.Data.Version.Should().Be(updateEnvelope.Data.Version);

        using var createCaseRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Settings Inheritance",
                DateOfDeath = DateTime.UtcNow.AddDays(-2),
                CaseType = "General",
                Urgency = "Normal"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var createCaseResponse = await client.SendAsync(createCaseRequest);
        createCaseResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createCaseEnvelope = await createCaseResponse.Content.ReadFromJsonAsync<ApiEnvelope<CreateCaseResponse>>();
        createCaseEnvelope.Should().NotBeNull();
        createCaseEnvelope!.Data.Should().NotBeNull();
        createCaseEnvelope.Data!.WorkflowKey.Should().Be("workflow.v2");
        createCaseEnvelope.Data.TemplateKey.Should().Be("template.v5");
    }

    [Fact]
    public async Task UpdateCaseDefaults_ShouldReturn403_WhenActorBelongsToDifferentTenant()
    {
        var client = _factory.CreateClient();
        var tenantA = await CreateTenantAsync(client, "settings-tenant-a@agency.pt");
        var tenantB = await CreateTenantAsync(client, "settings-tenant-b@agency.pt");

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{tenantA.OrganizationId}/settings/case-defaults",
            new UpdateTenantCaseDefaultsRequest
            {
                DefaultWorkflowKey = "workflow.denied.v1"
            },
            tenantB.UserId,
            tenantB.OrganizationId,
            "AgencyAdmin");
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsageIndicators_ShouldReturnCurrentAndHistoricalMetrics_WithPeriodFiltering()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "settings-usage-1@agency.pt");
        await SetDefaultsAndCompleteOnboardingAsync(client, signup);
        await ActivateBillingAsync(client, signup);

        using (var createCaseRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases",
                   new CreateCaseRequest
                   {
                       DeceasedFullName = "Usage Metrics",
                       DateOfDeath = DateTime.UtcNow.AddDays(-2),
                       CaseType = "General",
                       Urgency = "Normal"
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var createCaseResponse = await client.SendAsync(createCaseRequest);
            createCaseResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using var request30 = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/usage-indicators?periodDays=30",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var response30 = await client.SendAsync(request30);
        response30.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope30 = await response30.Content.ReadFromJsonAsync<ApiEnvelope<TenantUsageIndicatorsResponse>>();
        envelope30.Should().NotBeNull();
        envelope30!.Data.Should().NotBeNull();
        envelope30.Data!.PeriodDays.Should().Be(30);
        envelope30.Data.History.Should().HaveCount(30);
        envelope30.Data.Current.CasesCreated.Should().BeGreaterThanOrEqualTo(1);
        envelope30.Data.History.Should().Contain(x => x.CasesCreated > 0);

        using var request7 = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/usage-indicators?periodDays=7",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var response7 = await client.SendAsync(request7);
        response7.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope7 = await response7.Content.ReadFromJsonAsync<ApiEnvelope<TenantUsageIndicatorsResponse>>();
        envelope7.Should().NotBeNull();
        envelope7!.Data.Should().NotBeNull();
        envelope7.Data!.PeriodDays.Should().Be(7);
        envelope7.Data.History.Should().HaveCount(7);
    }

    [Fact]
    public async Task GetUsageIndicators_ShouldReturn400_WhenPeriodIsOutOfRange()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "settings-usage-invalid@agency.pt");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/usage-indicators?periodDays=0",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUsageIndicators_ShouldReturn403_WhenActorBelongsToDifferentTenant()
    {
        var client = _factory.CreateClient();
        var tenantA = await CreateTenantAsync(client, "settings-usage-tenant-a@agency.pt");
        var tenantB = await CreateTenantAsync(client, "settings-usage-tenant-b@agency.pt");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{tenantA.OrganizationId}/settings/usage-indicators?periodDays=30",
            tenantB.UserId,
            tenantB.OrganizationId,
            "AgencyAdmin");
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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

    private static async Task SetDefaultsAndCompleteOnboardingAsync(HttpClient client, CreateAgencyAccountResponse signup)
    {
        using var defaultsMsg = BuildAuthorizedJsonRequest(
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
        var defaultsResp = await client.SendAsync(defaultsMsg);
        defaultsResp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var completeMsg = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/complete",
            new CompleteTenantOnboardingRequest { ConfirmCompletion = true },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var completeResp = await client.SendAsync(completeMsg);
        completeResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task ActivateBillingAsync(HttpClient client, CreateAgencyAccountResponse signup)
    {
        using var activateMsg = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/billing/activate",
            new ActivateTenantBillingRequest
            {
                PlanCode = "Starter",
                BillingCycle = "Monthly",
                PaymentMethodType = "Card",
                PaymentMethodReference = "pm_settings",
                InvoiceProfileLegalName = "Agency Lda",
                InvoiceProfileVatNumber = "PT123456789",
                InvoiceProfileBillingEmail = "billing@agency.pt",
                InvoiceProfileCountryCode = "PT"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var activateResp = await client.SendAsync(activateMsg);
        activateResp.StatusCode.Should().Be(HttpStatusCode.OK);
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
}
