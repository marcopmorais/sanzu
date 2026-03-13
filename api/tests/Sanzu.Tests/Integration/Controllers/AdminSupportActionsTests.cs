using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;
using Sanzu.Tests.Integration;

namespace Sanzu.Tests.Integration.Controllers;

public sealed class AdminSupportActionsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminSupportActionsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Override Blocked Step ──

    [Fact]
    public async Task OverrideBlockedStep_Should_Return204()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        var (caseId, stepId) = await SeedBlockedStepAsync(tenantId);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, $"/api/v1/admin/tenants/{tenantId}/actions/override-blocked-step", userId, tenantId, "SanzuAdmin");
        request.Content = JsonContent(new { caseId, stepId, rationale = "Customer escalation — unblocking per manager approval" });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task OverrideBlockedStep_Should_Return400_WhenRationaleMissing()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        var (caseId, stepId) = await SeedBlockedStepAsync(tenantId);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, $"/api/v1/admin/tenants/{tenantId}/actions/override-blocked-step", userId, tenantId, "SanzuAdmin");
        request.Content = JsonContent(new { caseId, stepId, rationale = "" });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OverrideBlockedStep_Should_Return403_ForSanzuViewer()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuViewer");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, $"/api/v1/admin/tenants/{tenantId}/actions/override-blocked-step", userId, tenantId, "SanzuViewer");
        request.Content = JsonContent(new { caseId = Guid.NewGuid(), stepId = Guid.NewGuid(), rationale = "Test" });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OverrideBlockedStep_Should_Return403_ForSanzuFinance()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuFinance");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, $"/api/v1/admin/tenants/{tenantId}/actions/override-blocked-step", userId, tenantId, "SanzuFinance");
        request.Content = JsonContent(new { caseId = Guid.NewGuid(), stepId = Guid.NewGuid(), rationale = "Test" });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Extend Grace Period ──

    [Fact]
    public async Task ExtendGracePeriod_Should_Return204()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, $"/api/v1/admin/tenants/{tenantId}/actions/extend-grace-period", userId, tenantId, "SanzuAdmin");
        request.Content = JsonContent(new { days = 14, rationale = "Customer requested extension during migration" });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ExtendGracePeriod_Should_Return400_WhenRationaleMissing()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuFinance");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, $"/api/v1/admin/tenants/{tenantId}/actions/extend-grace-period", userId, tenantId, "SanzuFinance");
        request.Content = JsonContent(new { days = 7, rationale = "" });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExtendGracePeriod_Should_Return403_ForSanzuOps()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuOps");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, $"/api/v1/admin/tenants/{tenantId}/actions/extend-grace-period", userId, tenantId, "SanzuOps");
        request.Content = JsonContent(new { days = 7, rationale = "Test" });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Re-Onboarding ──

    [Fact]
    public async Task ReOnboard_Should_Return204()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuOps");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, $"/api/v1/admin/tenants/{tenantId}/actions/re-onboard", userId, tenantId, "SanzuOps");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReOnboard_Should_Return403_ForSanzuSupport()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuSupport");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, $"/api/v1/admin/tenants/{tenantId}/actions/re-onboard", userId, tenantId, "SanzuSupport");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Impersonation ──

    [Fact]
    public async Task Impersonate_Should_Return200WithToken()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, $"/api/v1/admin/tenants/{tenantId}/actions/impersonate", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<ImpersonationResponseDto>>(JsonOptions);
        envelope!.Data!.Token.Should().NotBeNullOrEmpty();
        envelope.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        envelope.Data.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task Impersonate_Should_Return403_ForSanzuViewer()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuViewer");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, $"/api/v1/admin/tenants/{tenantId}/actions/impersonate", userId, tenantId, "SanzuViewer");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Impersonate_Should_Return403_ForSanzuFinance()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuFinance");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, $"/api/v1/admin/tenants/{tenantId}/actions/impersonate", userId, tenantId, "SanzuFinance");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Helpers ──

    private sealed class ImpersonationResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
    }

    private async Task<(Guid tenantId, Guid userId)> SeedTenantAndAdminAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var tenantId = Guid.NewGuid();
        db.Organizations.Add(new Organization
        {
            Id = tenantId, Name = $"Support-{Guid.NewGuid():N}", Location = "Test",
            Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, OrgId = tenantId, Email = $"support.{userId:N}@sanzu.pt", FullName = "Support Admin", CreatedAt = DateTime.UtcNow });
        db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), UserId = userId, RoleType = Enum.Parse<PlatformRole>(role), TenantId = null, GrantedBy = userId, GrantedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();
        return (tenantId, userId);
    }

    private async Task<(Guid caseId, Guid stepId)> SeedBlockedStepAsync(Guid tenantId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var caseId = Guid.NewGuid();
        db.Cases.Add(new Case
        {
            Id = caseId, TenantId = tenantId, CaseNumber = $"CASE-{Random.Shared.Next(10000, 99999)}",
            DeceasedFullName = "Test Case", Status = CaseStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            ManagerUserId = Guid.Empty
        });

        var stepId = Guid.NewGuid();
        db.WorkflowStepInstances.Add(new WorkflowStepInstance
        {
            Id = stepId, TenantId = tenantId, CaseId = caseId, StepKey = "blocked-step",
            Title = "Blocked Step", Sequence = 1, Status = WorkflowStepStatus.Blocked,
            BlockedReasonCode = BlockedReasonCode.ExternalDependency, BlockedReasonDetail = "Upstream step not complete",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return (caseId, stepId);
    }

    private static StringContent JsonContent(object obj)
        => new(JsonSerializer.Serialize(obj, JsonOptions), Encoding.UTF8, "application/json");

    private static HttpRequestMessage BuildAuthorizedRequest(HttpMethod method, string uri, Guid userId, Guid tenantId, string role)
    {
        var message = new HttpRequestMessage(method, uri);
        message.Headers.Add("X-User-Id", userId.ToString());
        message.Headers.Add("X-Tenant-Id", tenantId.ToString());
        message.Headers.Add("X-User-Role", role);
        return message;
    }
}
