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

public sealed class AdminAlertsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminAlertsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAlerts_Should_Return200WithAlertList()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        await SeedAlertAsync(tenantId);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/alerts", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<List<AdminAlertResponse>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAlerts_Should_FilterByStatus()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        await SeedAlertAsync(tenantId, AlertStatus.Resolved);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/alerts?status=Fired", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<List<AdminAlertResponse>>>(JsonOptions);
        envelope!.Data!.Should().OnlyContain(a => a.Status == "Fired");
    }

    [Fact]
    public async Task GetAlerts_Should_FilterBySeverity()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        await SeedAlertAsync(tenantId, severity: AlertSeverity.Critical);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/alerts?severity=Critical", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<List<AdminAlertResponse>>>(JsonOptions);
        envelope!.Data!.Should().OnlyContain(a => a.Severity == "Critical");
    }

    [Fact]
    public async Task PatchAlert_Should_AcknowledgeAlert()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        var alertId = await SeedAlertAsync(tenantId);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Patch, $"/api/v1/admin/alerts/{alertId}", userId, tenantId, "SanzuAdmin");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { status = "Acknowledged" }, JsonOptions),
            Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var alert = db.AdminAlerts.First(a => a.Id == alertId);
        alert.Status.Should().Be(AlertStatus.Acknowledged);
    }

    [Fact]
    public async Task PatchAlert_Should_ResolveAlert()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        var alertId = await SeedAlertAsync(tenantId, AlertStatus.Acknowledged);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Patch, $"/api/v1/admin/alerts/{alertId}", userId, tenantId, "SanzuAdmin");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { status = "Resolved" }, JsonOptions),
            Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PatchAlert_Should_Return404_ForNonExistentAlert()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Patch, $"/api/v1/admin/alerts/{Guid.NewGuid()}", userId, tenantId, "SanzuAdmin");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { status = "Acknowledged" }, JsonOptions),
            Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── RBAC ──

    [Fact]
    public async Task GetAlerts_Should_Return401_WhenUnauthenticated()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admin/alerts");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("SanzuAdmin")]
    [InlineData("SanzuOps")]
    [InlineData("SanzuFinance")]
    [InlineData("SanzuSupport")]
    [InlineData("SanzuViewer")]
    public async Task GetAlerts_Should_Return200_ForAllAdminRoles(string role)
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync(role);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/alerts", userId, tenantId, role);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PatchAlert_Should_Return403_ForViewerRole()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuViewer");
        var alertId = await SeedAlertAsync(tenantId);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Patch, $"/api/v1/admin/alerts/{alertId}", userId, tenantId, "SanzuViewer");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { status = "Acknowledged" }, JsonOptions),
            Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAlerts_Should_Return403_ForAgencyAdmin()
    {
        var tenantId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
            db.Organizations.Add(new Organization { Id = tenantId, Name = $"Agency-{tenantId:N}", Location = "Test", Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/alerts", Guid.NewGuid(), tenantId, "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Helpers ──

    private async Task<(Guid tenantId, Guid userId)> SeedTenantAndAdminAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var tenantId = Guid.NewGuid();
        db.Organizations.Add(new Organization
        {
            Id = tenantId, Name = $"AlertCtrl-{Guid.NewGuid():N}", Location = "Test",
            Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, OrgId = tenantId, Email = $"alert.{userId:N}@sanzu.pt", FullName = "Alert Admin", CreatedAt = DateTime.UtcNow });
        db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), UserId = userId, RoleType = Enum.Parse<PlatformRole>(role), TenantId = null, GrantedBy = userId, GrantedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();
        return (tenantId, userId);
    }

    private async Task<Guid> SeedAlertAsync(Guid tenantId, AlertStatus status = AlertStatus.Fired, AlertSeverity severity = AlertSeverity.Warning)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var alertId = Guid.NewGuid();
        db.AdminAlerts.Add(new AdminAlert
        {
            Id = alertId,
            TenantId = tenantId,
            AlertType = "TestAlert",
            Severity = severity,
            Title = "Test alert",
            Detail = "Test detail",
            Status = status,
            RoutedToRole = "SanzuOps",
            FiredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return alertId;
    }

    private static HttpRequestMessage BuildAuthorizedRequest(HttpMethod method, string uri, Guid userId, Guid tenantId, string role)
    {
        var message = new HttpRequestMessage(method, uri);
        message.Headers.Add("X-User-Id", userId.ToString());
        message.Headers.Add("X-Tenant-Id", tenantId.ToString());
        message.Headers.Add("X-User-Role", role);
        return message;
    }
}
