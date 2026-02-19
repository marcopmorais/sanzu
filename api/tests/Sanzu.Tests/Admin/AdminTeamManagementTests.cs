using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;
using Sanzu.Tests.Integration;

namespace Sanzu.Tests.Admin;

public sealed class AdminTeamManagementTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminTeamManagementTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── AC #1: SanzuAdmin can list team members ──

    [Fact]
    public async Task ListTeam_Should_ReturnOk_When_SanzuAdmin()
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync("SanzuAdmin");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/team",
            userId,
            tenantId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<List<AdminTeamMemberResponse>>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Should().Contain(m => m.UserId == userId && m.Role == "SanzuAdmin");
    }

    // ── AC #4: Non-SanzuAdmin roles get 403 ──

    [Theory]
    [InlineData("SanzuOps")]
    [InlineData("SanzuFinance")]
    [InlineData("SanzuSupport")]
    [InlineData("SanzuViewer")]
    public async Task ListTeam_Should_Return403_When_NonSanzuAdmin(string role)
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync(role);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/team",
            userId,
            tenantId,
            role);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── AC #2: SanzuAdmin can grant admin role ──

    [Fact]
    public async Task GrantRole_Should_Return204_When_ValidRole()
    {
        var client = _factory.CreateClient();
        var (adminId, tenantId) = await SeedUserWithRoleAsync("SanzuAdmin");
        var targetUserId = await SeedPlainUserAsync(tenantId);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/admin/team/{targetUserId}/roles",
            adminId,
            tenantId,
            "SanzuAdmin");

        request.Content = JsonContent.Create(new GrantAdminRoleRequest { Role = "SanzuSupport" });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify role was created
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.UserRoles.Should().Contain(
            r => r.UserId == targetUserId
                 && r.RoleType == PlatformRole.SanzuSupport
                 && r.TenantId == null);
    }

    // ── AC #2: Audit event logged for grant ──

    [Fact]
    public async Task GrantRole_Should_LogAuditEvent_When_RoleGranted()
    {
        var client = _factory.CreateClient();
        var (adminId, tenantId) = await SeedUserWithRoleAsync("SanzuAdmin");
        var targetUserId = await SeedPlainUserAsync(tenantId);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/admin/team/{targetUserId}/roles",
            adminId,
            tenantId,
            "SanzuAdmin");

        request.Content = JsonContent.Create(new GrantAdminRoleRequest { Role = "SanzuOps" });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "Admin.Team.RoleGranted"
                 && x.ActorUserId == adminId);
    }

    // ── AC #3: SanzuAdmin can revoke admin role ──

    [Fact]
    public async Task RevokeRole_Should_Return204_When_RoleExists()
    {
        var client = _factory.CreateClient();
        var (adminId, tenantId) = await SeedUserWithRoleAsync("SanzuAdmin");
        var (targetUserId, _) = await SeedUserWithRoleAsync("SanzuViewer");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/v1/admin/team/{targetUserId}/roles/SanzuViewer",
            adminId,
            tenantId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify audit event
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "Admin.Team.RoleRevoked"
                 && x.ActorUserId == adminId);
    }

    // ── Privilege escalation prevention ──

    [Fact]
    public async Task GrantRole_Should_Return400_When_GrantingSanzuAdmin()
    {
        var client = _factory.CreateClient();
        var (adminId, tenantId) = await SeedUserWithRoleAsync("SanzuAdmin");
        var targetUserId = await SeedPlainUserAsync(tenantId);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/admin/team/{targetUserId}/roles",
            adminId,
            tenantId,
            "SanzuAdmin");

        request.Content = JsonContent.Create(new GrantAdminRoleRequest { Role = "SanzuAdmin" });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GrantRole_Should_Return400_When_GrantingAgencyAdmin()
    {
        var client = _factory.CreateClient();
        var (adminId, tenantId) = await SeedUserWithRoleAsync("SanzuAdmin");
        var targetUserId = await SeedPlainUserAsync(tenantId);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/admin/team/{targetUserId}/roles",
            adminId,
            tenantId,
            "SanzuAdmin");

        request.Content = JsonContent.Create(new GrantAdminRoleRequest { Role = "AgencyAdmin" });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListTeam_Should_Return401_When_Unauthenticated()
    {
        var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/team");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
                Email = $"team.{roleName.ToLowerInvariant()}.{userId:N}@sanzu.pt",
                FullName = $"Team Test {roleName}",
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

    private async Task<Guid> SeedPlainUserAsync(Guid tenantId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var userId = Guid.NewGuid();

        dbContext.Users.Add(
            new User
            {
                Id = userId,
                OrgId = tenantId,
                Email = $"plain.{userId:N}@sanzu.pt",
                FullName = "Plain User",
                CreatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
        return userId;
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
