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

public sealed class AdminQueueControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminQueueControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListQueues_ShouldReturnAllQueues_WhenCalledBySanzuAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "queue-list@agency.pt");
        await GrantSanzuAdminAsync(signup.UserId);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get, "/api/v1/admin/queues", signup.UserId, Guid.Empty, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AdminQueueListResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Queues.Should().HaveCount(5);
        envelope.Data.Queues.Should().Contain(q => q.QueueId == "ADM_OnboardingStuck");
        envelope.Data.Queues.Should().Contain(q => q.QueueId == "ADM_FailedPayment");
    }

    [Fact]
    public async Task ListQueues_ShouldReturn403_WhenCalledByNonAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "queue-nonadmin@agency.pt");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get, "/api/v1/admin/queues", signup.UserId, signup.OrganizationId, "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetQueueItems_ShouldReturnItems_WhenQueueExists()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "queue-items@agency.pt");
        await GrantSanzuAdminAsync(signup.UserId);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get, "/api/v1/admin/queues/ADM_FailedPayment",
            signup.UserId, Guid.Empty, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AdminQueueItemsResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.QueueId.Should().Be("ADM_FailedPayment");
        envelope.Data.QueueName.Should().Be("Failed payment");
    }

    [Fact]
    public async Task GetQueueItems_ShouldReturn409_WhenQueueNotFound()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "queue-notfound@agency.pt");
        await GrantSanzuAdminAsync(signup.UserId);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get, "/api/v1/admin/queues/INVALID_QUEUE",
            signup.UserId, Guid.Empty, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetTenantEventStream_ShouldReturnSafeEvents_WhenTenantExists()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "queue-events@agency.pt");
        await ActivateTenantAsync(client, signup);
        await GrantSanzuAdminAsync(signup.UserId);

        // Create a case to generate events
        using var createCaseRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Queue Event Test",
                DateOfDeath = DateTime.UtcNow.AddDays(-3),
                CaseType = "General",
                Urgency = "Normal"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var createResponse = await client.SendAsync(createCaseRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/queues/events/{signup.OrganizationId}?limit=10",
            signup.UserId, Guid.Empty, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AdminEventStreamResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.TenantId.Should().Be(signup.OrganizationId);
        envelope.Data.Events.Should().NotBeEmpty();

        // Verify role-safe: no sensitive content in summaries
        var rawJson = await response.Content.ReadAsStringAsync();
        rawJson.Should().NotContain("Queue Event Test"); // no deceased name
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
            signup.UserId, signup.OrganizationId, "AgencyAdmin");
        (await client.SendAsync(defaultsRequest)).StatusCode.Should().Be(HttpStatusCode.OK);

        using var completionRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/complete",
            new CompleteTenantOnboardingRequest { ConfirmCompletion = true },
            signup.UserId, signup.OrganizationId, "AgencyAdmin");
        (await client.SendAsync(completionRequest)).StatusCode.Should().Be(HttpStatusCode.OK);

        using var activationRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/billing/activate",
            new ActivateTenantBillingRequest
            {
                PlanCode = "Growth",
                BillingCycle = "Monthly",
                PaymentMethodType = "Card",
                PaymentMethodReference = "pm_queue_tests",
                InvoiceProfileLegalName = "Queue Agency Lda",
                InvoiceProfileVatNumber = "PT123456789",
                InvoiceProfileBillingEmail = "billing@queue.pt",
                InvoiceProfileCountryCode = "PT"
            },
            signup.UserId, signup.OrganizationId, "AgencyAdmin");
        (await client.SendAsync(activationRequest)).StatusCode.Should().Be(HttpStatusCode.OK);
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
            Email = email, FullName = "Agency Admin", AgencyName = "Agency", Location = "Lisbon"
        };
        var signupResponse = await client.PostAsJsonAsync("/api/v1/tenants/signup", request);
        signupResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await signupResponse.Content.ReadFromJsonAsync<ApiEnvelope<CreateAgencyAccountResponse>>();
        return envelope!.Data!;
    }
}
