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

public sealed class FleetControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FleetControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetFleetPosture_ShouldReturnTenants_WhenCalledBySanzuAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "fleet-admin@agency.pt");
        await ActivateTenantAsync(client, signup);

        // Grant SanzuAdmin role
        await GrantSanzuAdminAsync(signup.UserId);

        using var fleetRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/fleet",
            signup.UserId,
            Guid.Empty,
            "SanzuAdmin");

        var response = await client.SendAsync(fleetRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<FleetPostureResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.TotalTenants.Should().BeGreaterThanOrEqualTo(1);
        envelope.Data.Tenants.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFleetPosture_ShouldReturn403_WhenCalledByNonAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "fleet-nonadmin@agency.pt");

        using var fleetRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/fleet",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(fleetRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetFleetPosture_ShouldFilterBySearch_WhenSearchProvided()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "fleet-search@agency.pt");
        await ActivateTenantAsync(client, signup);
        await GrantSanzuAdminAsync(signup.UserId);

        using var fleetRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/fleet?search=Agency",
            signup.UserId,
            Guid.Empty,
            "SanzuAdmin");

        var response = await client.SendAsync(fleetRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<FleetPostureResponse>>();
        envelope!.Data!.Tenants.Should().AllSatisfy(t =>
            (t.TenantName.Contains("Agency", StringComparison.OrdinalIgnoreCase)
             || t.Location.Contains("Agency", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue());
    }

    [Fact]
    public async Task GetTenantDrilldown_ShouldReturnPosture_WhenTenantExists()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "fleet-drilldown@agency.pt");
        await ActivateTenantAsync(client, signup);
        await GrantSanzuAdminAsync(signup.UserId);

        // Create a case to have data
        using var createCaseRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Fleet Drilldown Test",
                DateOfDeath = DateTime.UtcNow.AddDays(-5),
                CaseType = "General",
                Urgency = "Normal"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var createResponse = await client.SendAsync(createCaseRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var drilldownRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/fleet/{signup.OrganizationId}",
            signup.UserId,
            Guid.Empty,
            "SanzuAdmin");

        var response = await client.SendAsync(drilldownRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<TenantDrilldownResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.TenantId.Should().Be(signup.OrganizationId);
        envelope.Data.Metrics.Should().NotBeNull();
        envelope.Data.Metrics.TotalCases.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetTenantDrilldown_ShouldNotExposeSensitiveData()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "fleet-safe@agency.pt");
        await ActivateTenantAsync(client, signup);
        await GrantSanzuAdminAsync(signup.UserId);

        using var drilldownRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/fleet/{signup.OrganizationId}",
            signup.UserId,
            Guid.Empty,
            "SanzuAdmin");

        var response = await client.SendAsync(drilldownRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rawJson = await response.Content.ReadAsStringAsync();
        // Role-safe: no family-sensitive data (deceased names, dates of death, etc.)
        rawJson.Should().NotContain("deceasedFullName");
        rawJson.Should().NotContain("dateOfDeath");
    }

    private async Task GrantSanzuAdminAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = null,
            RoleType = PlatformRole.SanzuAdmin,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = userId
        });
        await dbContext.SaveChangesAsync();
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
                PaymentMethodReference = "pm_fleet_tests",
                InvoiceProfileLegalName = "Fleet Agency Lda",
                InvoiceProfileVatNumber = "PT123456789",
                InvoiceProfileBillingEmail = "billing@fleet.pt",
                InvoiceProfileCountryCode = "PT"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var activationResponse = await client.SendAsync(activationRequest);
        activationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static HttpRequestMessage BuildAuthorizedJsonRequest(
        HttpMethod method, string uri, object payload, Guid userId, Guid tenantId, string role)
    {
        var message = new HttpRequestMessage(method, uri) { Content = JsonContent.Create(payload) };
        message.Headers.Add("X-User-Id", userId.ToString());
        message.Headers.Add("X-Tenant-Id", tenantId.ToString());
        message.Headers.Add("X-User-Role", role);
        return message;
    }

    private static HttpRequestMessage BuildAuthorizedRequest(
        HttpMethod method, string uri, Guid userId, Guid tenantId, string role)
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
        return envelope!.Data!;
    }
}
