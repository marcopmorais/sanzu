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

public sealed class PlaybooksControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlaybooksControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreatePlaybook_ShouldReturn201AndPersist_WhenActorIsAgencyAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "playbook-create@agency.pt");

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/playbooks",
            new CreatePlaybookRequest
            {
                Name = "Standard Estate Handling",
                Description = "Default handling for estate cases.",
                ChangeNotes = "Initial version."
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<PlaybookResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Name.Should().Be("Standard Estate Handling");
        envelope.Data.Version.Should().Be(1);
        envelope.Data.Status.Should().Be(PlaybookStatus.Draft);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AgencyPlaybooks.Should().Contain(x => x.Id == envelope.Data.Id && x.TenantId == signup.OrganizationId);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "PlaybookCreated");
    }

    [Fact]
    public async Task CreatePlaybook_ShouldReturn403_WhenActorIsNotAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "playbook-forbidden@agency.pt");

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/playbooks",
            new CreatePlaybookRequest
            {
                Name = "Unauthorized Playbook"
            },
            signup.UserId,
            signup.OrganizationId,
            "CaseWorker");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreatePlaybook_ShouldReturn400_WhenNameIsEmpty()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "playbook-invalid@agency.pt");

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/playbooks",
            new CreatePlaybookRequest
            {
                Name = string.Empty
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListPlaybooks_ShouldReturnAllVersions()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "playbook-list@agency.pt");

        // Create two playbooks
        await CreatePlaybookViaApi(client, signup, "Playbook A");
        await CreatePlaybookViaApi(client, signup, "Playbook B");

        using var listRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/playbooks",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(listRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<IReadOnlyList<PlaybookResponse>>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Count.Should().Be(2);
    }

    [Fact]
    public async Task ActivatePlaybook_ShouldSetActiveAndArchivePrevious()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "playbook-activate@agency.pt");

        var first = await CreatePlaybookViaApi(client, signup, "Playbook v1");
        var second = await CreatePlaybookViaApi(client, signup, "Playbook v2");

        // Activate first
        using var activateFirst = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/playbooks/{first.Id}/activate",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var activateFirstResponse = await client.SendAsync(activateFirst);
        activateFirstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Activate second — first should become Archived
        using var activateSecond = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/playbooks/{second.Id}/activate",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var activateSecondResponse = await client.SendAsync(activateSecond);
        activateSecondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await activateSecondResponse.Content.ReadFromJsonAsync<ApiEnvelope<PlaybookResponse>>();
        envelope!.Data!.Status.Should().Be(PlaybookStatus.Active);

        // Verify first is now archived
        using var getFirst = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/playbooks/{first.Id}",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var getFirstResponse = await client.SendAsync(getFirst);
        var firstEnvelope = await getFirstResponse.Content.ReadFromJsonAsync<ApiEnvelope<PlaybookResponse>>();
        firstEnvelope!.Data!.Status.Should().Be(PlaybookStatus.Archived);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "PlaybookArchived");
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "PlaybookActivated");
    }

    [Fact]
    public async Task UpdatePlaybook_ShouldReject_WhenPlaybookIsActive()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "playbook-update-active@agency.pt");

        var playbook = await CreatePlaybookViaApi(client, signup, "Locked Playbook");

        // Activate it
        using var activateReq = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/playbooks/{playbook.Id}/activate",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        await client.SendAsync(activateReq);

        // Try to update
        using var updateReq = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/playbooks/{playbook.Id}",
            new UpdatePlaybookRequest { Name = "Updated Name" },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(updateReq);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreatePlaybook_ShouldIncrementVersion()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "playbook-version@agency.pt");

        var first = await CreatePlaybookViaApi(client, signup, "Versioned Playbook");
        var second = await CreatePlaybookViaApi(client, signup, "Versioned Playbook v2");

        first.Version.Should().Be(1);
        second.Version.Should().Be(2);
    }

    private async Task<PlaybookResponse> CreatePlaybookViaApi(
        HttpClient client,
        CreateAgencyAccountResponse signup,
        string name)
    {
        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/playbooks",
            new CreatePlaybookRequest { Name = name },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<PlaybookResponse>>();
        return envelope!.Data!;
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
