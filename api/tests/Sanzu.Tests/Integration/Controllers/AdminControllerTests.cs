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

public sealed class AdminControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateTenantLifecycleState_ShouldReturn200AndPersistStatusAndAudit_WhenActorIsSanzuAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "admin-lifecycle-tenant@agency.pt");
        var sanzuAdminUserId = await SeedSanzuAdminAsync(signup.OrganizationId);

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/admin/tenants/{signup.OrganizationId}/lifecycle",
            new UpdateTenantLifecycleStateRequest
            {
                TargetStatus = "Suspended",
                Reason = "Fraud risk review in progress."
            },
            sanzuAdminUserId,
            signup.OrganizationId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<TenantLifecycleStateResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.TenantId.Should().Be(signup.OrganizationId);
        envelope.Data.PreviousStatus.Should().Be(TenantStatus.Pending);
        envelope.Data.CurrentStatus.Should().Be(TenantStatus.Suspended);
        envelope.Data.Reason.Should().Be("Fraud risk review in progress.");
        envelope.Data.ChangedByUserId.Should().Be(sanzuAdminUserId);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var tenant = await dbContext.Organizations.FindAsync(signup.OrganizationId);
        tenant.Should().NotBeNull();
        tenant!.Status.Should().Be(TenantStatus.Suspended);
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "TenantLifecycleStateChanged"
                 && x.ActorUserId == sanzuAdminUserId
                 && x.Metadata.Contains("Fraud risk review in progress."));
    }

    [Fact]
    public async Task UpdateTenantLifecycleState_ShouldReturn403_WhenActorIsNotSanzuAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "admin-lifecycle-forbidden@agency.pt");

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/admin/tenants/{signup.OrganizationId}/lifecycle",
            new UpdateTenantLifecycleStateRequest
            {
                TargetStatus = "Suspended",
                Reason = "Unauthorized attempt."
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateTenantLifecycleState_ShouldReturn409_WhenTransitionIsInvalid()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "admin-lifecycle-invalid@agency.pt");
        var sanzuAdminUserId = await SeedSanzuAdminAsync(signup.OrganizationId);

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/admin/tenants/{signup.OrganizationId}/lifecycle",
            new UpdateTenantLifecycleStateRequest
            {
                TargetStatus = "PaymentIssue",
                Reason = "Invalid transition check."
            },
            sanzuAdminUserId,
            signup.OrganizationId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task StartDiagnosticSession_ShouldReturn201AndPersistSession_WhenActorIsSanzuAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "admin-diagnostics-start@agency.pt");
        var sanzuAdminUserId = await SeedSanzuAdminAsync(signup.OrganizationId);

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/admin/tenants/{signup.OrganizationId}/diagnostics/sessions",
            new StartSupportDiagnosticSessionRequest
            {
                Scope = "TenantOperationalRead",
                DurationMinutes = 30,
                Reason = "Escalated support case."
            },
            sanzuAdminUserId,
            signup.OrganizationId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<SupportDiagnosticSessionResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.TenantId.Should().Be(signup.OrganizationId);
        envelope.Data.Scope.Should().Be(SupportDiagnosticScope.TenantOperationalRead);
        envelope.Data.DurationMinutes.Should().Be(30);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.SupportDiagnosticSessions.Should().Contain(x => x.Id == envelope.Data.SessionId);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "SupportDiagnosticSessionStarted");
    }

    [Fact]
    public async Task GetDiagnosticSummary_ShouldReturn200_WhenSessionIsActive()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "admin-diagnostics-summary@agency.pt");
        var sanzuAdminUserId = await SeedSanzuAdminAsync(signup.OrganizationId);

        using (var startRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Post,
                   $"/api/v1/admin/tenants/{signup.OrganizationId}/diagnostics/sessions",
                   new StartSupportDiagnosticSessionRequest
                   {
                       Scope = "TenantStatusRead",
                       DurationMinutes = 30,
                       Reason = "Summary check."
                   },
                   sanzuAdminUserId,
                   signup.OrganizationId,
                   "SanzuAdmin"))
        {
            var startResponse = await client.SendAsync(startRequest);
            startResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var startEnvelope = await startResponse.Content.ReadFromJsonAsync<ApiEnvelope<SupportDiagnosticSessionResponse>>();
            startEnvelope.Should().NotBeNull();
            startEnvelope!.Data.Should().NotBeNull();

            using var summaryRequest = BuildAuthorizedRequest(
                HttpMethod.Get,
                $"/api/v1/admin/tenants/{signup.OrganizationId}/diagnostics/sessions/{startEnvelope.Data!.SessionId}/summary",
                sanzuAdminUserId,
                signup.OrganizationId,
                "SanzuAdmin");

            var summaryResponse = await client.SendAsync(summaryRequest);
            summaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var summaryEnvelope = await summaryResponse.Content.ReadFromJsonAsync<ApiEnvelope<SupportDiagnosticSummaryResponse>>();
            summaryEnvelope.Should().NotBeNull();
            summaryEnvelope!.Data.Should().NotBeNull();
            summaryEnvelope.Data!.TenantId.Should().Be(signup.OrganizationId);
            summaryEnvelope.Data.Scope.Should().Be(SupportDiagnosticScope.TenantStatusRead);
        }
    }

    [Fact]
    public async Task GetDiagnosticSummary_ShouldReturn409_WhenSessionIsExpired()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "admin-diagnostics-expired@agency.pt");
        var sanzuAdminUserId = await SeedSanzuAdminAsync(signup.OrganizationId);
        var sessionId = await SeedExpiredDiagnosticSessionAsync(signup.OrganizationId, sanzuAdminUserId);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/tenants/{signup.OrganizationId}/diagnostics/sessions/{sessionId}/summary",
            sanzuAdminUserId,
            signup.OrganizationId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<Guid> SeedSanzuAdminAsync(Guid tenantId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var userId = Guid.NewGuid();

        dbContext.Users.Add(
            new User
            {
                Id = userId,
                OrgId = tenantId,
                Email = $"sanzu.admin.{userId:N}@sanzu.pt",
                FullName = "Sanzu Admin",
                CreatedAt = DateTime.UtcNow
            });

        dbContext.UserRoles.Add(
            new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RoleType = PlatformRole.SanzuAdmin,
                TenantId = null,
                GrantedBy = userId,
                GrantedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
        return userId;
    }

    private async Task<Guid> SeedExpiredDiagnosticSessionAsync(Guid tenantId, Guid actorUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var now = DateTime.UtcNow;
        var sessionId = Guid.NewGuid();

        dbContext.SupportDiagnosticSessions.Add(
            new SupportDiagnosticSession
            {
                Id = sessionId,
                TenantId = tenantId,
                RequestedByUserId = actorUserId,
                Scope = SupportDiagnosticScope.TenantOperationalRead,
                Reason = "Expired session seed",
                StartedAt = now.AddMinutes(-40),
                ExpiresAt = now.AddMinutes(-10)
            });

        await dbContext.SaveChangesAsync();
        return sessionId;
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
