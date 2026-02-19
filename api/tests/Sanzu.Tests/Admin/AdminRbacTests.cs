using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;
using Sanzu.Tests.Integration;

namespace Sanzu.Tests.Admin;

public sealed class AdminRbacTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminRbacTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── AC #1: PlatformRole enum has all 6 expected values ──

    [Fact]
    public void PlatformRole_Should_HaveAllSixValues()
    {
        var values = Enum.GetValues<PlatformRole>();
        values.Should().HaveCount(6);
        values.Should().Contain(PlatformRole.AgencyAdmin);
        values.Should().Contain(PlatformRole.SanzuAdmin);
        values.Should().Contain(PlatformRole.SanzuOps);
        values.Should().Contain(PlatformRole.SanzuFinance);
        values.Should().Contain(PlatformRole.SanzuSupport);
        values.Should().Contain(PlatformRole.SanzuViewer);
    }

    // ── AC #2: GET /admin/me/permissions returns correct matrix per role ──

    [Theory]
    [InlineData("SanzuAdmin", true, 14, 6)]
    [InlineData("SanzuOps", true, 9, 5)]
    [InlineData("SanzuFinance", true, 7, 3)]
    [InlineData("SanzuSupport", true, 7, 2)]
    [InlineData("SanzuViewer", false, 4, 2)]
    public async Task GetPermissions_Should_ReturnCorrectMatrix_When_RoleIsAdminRole(
        string role,
        bool expectedCanTakeActions,
        int expectedEndpointCount,
        int expectedWidgetCount)
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync(role);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/me/permissions",
            userId,
            tenantId,
            role);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AdminPermissionsResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Role.Should().Be(role);
        envelope.Data.CanTakeActions.Should().Be(expectedCanTakeActions);
        envelope.Data.AccessibleEndpoints.Should().HaveCount(expectedEndpointCount);
        envelope.Data.AccessibleWidgets.Should().HaveCount(expectedWidgetCount);
    }

    // ── AC #1: AdminViewer policy allows all 5 admin roles ──

    [Theory]
    [InlineData("SanzuAdmin")]
    [InlineData("SanzuOps")]
    [InlineData("SanzuFinance")]
    [InlineData("SanzuSupport")]
    [InlineData("SanzuViewer")]
    public async Task AdminViewerPolicy_Should_AllowAllAdminRoles(string role)
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync(role);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/me/permissions",
            userId,
            tenantId,
            role);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── AC #3: AgencyAdmin (non-internal role) gets 403 ──

    [Fact]
    public async Task GetPermissions_Should_Return403_When_RoleIsAgencyAdmin()
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync("AgencyAdmin");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/me/permissions",
            userId,
            tenantId,
            "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── AC #4: Unauthenticated request gets 401 ──

    [Fact]
    public async Task GetPermissions_Should_Return401_When_Unauthenticated()
    {
        var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/me/permissions");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── AC #2: Audit event logged on permissions access ──

    [Fact]
    public async Task GetPermissions_Should_LogAuditEvent_When_CalledByAdminRole()
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync("SanzuAdmin");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/me/permissions",
            userId,
            tenantId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "Admin.Permissions.GetPermissions"
                 && x.ActorUserId == userId);
    }

    // ── Policy boundary tests ──

    [Fact]
    public async Task SanzuOps_Should_HaveAccessToOpsEndpoints()
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync("SanzuOps");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/me/permissions",
            userId,
            tenantId,
            "SanzuOps");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AdminPermissionsResponse>>();

        envelope!.Data!.AccessibleEndpoints.Should().Contain("/admin/tenants/{id}/actions/*");
        envelope.Data.AccessibleEndpoints.Should().NotContain("/admin/revenue");
        envelope.Data.AccessibleEndpoints.Should().NotContain("/admin/config/*");
        envelope.Data.AccessibleEndpoints.Should().NotContain("/admin/team");
        envelope.Data.AccessibleEndpoints.Should().NotContain("/admin/platform/*");
    }

    [Fact]
    public async Task SanzuFinance_Should_HaveAccessToFinanceEndpoints()
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync("SanzuFinance");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/me/permissions",
            userId,
            tenantId,
            "SanzuFinance");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AdminPermissionsResponse>>();

        envelope!.Data!.AccessibleEndpoints.Should().Contain("/admin/revenue");
        envelope.Data.AccessibleEndpoints.Should().Contain("/admin/tenants/{id}/billing");
        envelope.Data.AccessibleEndpoints.Should().NotContain("/admin/tenants/{id}/cases");
        envelope.Data.AccessibleEndpoints.Should().NotContain("/admin/tenants/{id}/actions/*");
    }

    [Fact]
    public async Task SanzuViewer_Should_NotHaveCanTakeActions()
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync("SanzuViewer");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/me/permissions",
            userId,
            tenantId,
            "SanzuViewer");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AdminPermissionsResponse>>();

        envelope!.Data!.CanTakeActions.Should().BeFalse();
        envelope.Data.AccessibleWidgets.Should().HaveCount(2);
        envelope.Data.AccessibleWidgets.Should().Contain("TenantSummary");
        envelope.Data.AccessibleWidgets.Should().Contain("HealthOverview");
    }

    // ── Helpers ──

    private async Task<(Guid UserId, Guid TenantId)> SeedUserWithRoleAsync(string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        dbContext.Organizations.Add(
            new Organization
            {
                Id = tenantId,
                Name = $"TestOrg-{userId:N}",
                Location = "Test",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        dbContext.Users.Add(
            new User
            {
                Id = userId,
                OrgId = tenantId,
                Email = $"test.{roleName.ToLowerInvariant()}.{userId:N}@sanzu.pt",
                FullName = $"Test {roleName}",
                CreatedAt = DateTime.UtcNow
            });

        if (Enum.TryParse<PlatformRole>(roleName, out var platformRole))
        {
            dbContext.UserRoles.Add(
                new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RoleType = platformRole,
                    TenantId = roleName == "AgencyAdmin" ? tenantId : null,
                    GrantedBy = userId,
                    GrantedAt = DateTime.UtcNow
                });
        }

        await dbContext.SaveChangesAsync();
        return (userId, tenantId);
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
}
