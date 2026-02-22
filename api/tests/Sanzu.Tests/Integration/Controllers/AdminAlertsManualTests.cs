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

public sealed class AdminAlertsManualTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminAlertsManualTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Manual Alert Creation ──

    [Fact]
    public async Task CreateManualAlert_Should_Return201()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, "/api/v1/admin/alerts/manual", userId, tenantId, "SanzuAdmin");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { tenantId = tenantId, note = "Follow up on billing", dueDate = DateTime.UtcNow.AddDays(7) }, JsonOptions),
            Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AdminAlertResponse>>(JsonOptions);
        envelope!.Data!.AlertType.Should().Be("ManualFollowUp");
        envelope.Data.Severity.Should().Be("Info");
        envelope.Data.Title.Should().Contain("Follow up on billing");
    }

    [Fact]
    public async Task CreateManualAlert_Should_Return403_ForViewerRole()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuViewer");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, "/api/v1/admin/alerts/manual", userId, tenantId, "SanzuViewer");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { note = "Test", dueDate = DateTime.UtcNow.AddDays(1) }, JsonOptions),
            Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("SanzuAdmin")]
    [InlineData("SanzuOps")]
    public async Task CreateManualAlert_Should_Return201_ForOpsRoles(string role)
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync(role);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, "/api/v1/admin/alerts/manual", userId, tenantId, role);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { note = "Ops follow-up", dueDate = DateTime.UtcNow.AddDays(3) }, JsonOptions),
            Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── Delivery Config ──

    [Fact]
    public async Task ConfigureDelivery_Should_Return204_ForSanzuAdmin()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, "/api/v1/admin/alerts/delivery-config", userId, tenantId, "SanzuAdmin");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { channel = "email", target = "alerts@sanzu.pt" }, JsonOptions),
            Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ConfigureDelivery_Should_Return403_ForNonAdminRoles()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuOps");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, "/api/v1/admin/alerts/delivery-config", userId, tenantId, "SanzuOps");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { channel = "slack", target = "https://hooks.slack.com/test" }, JsonOptions),
            Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ConfigureDelivery_Should_UpsertExistingConfig()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();

        // First config
        using var req1 = BuildAuthorizedRequest(HttpMethod.Post, "/api/v1/admin/alerts/delivery-config", userId, tenantId, "SanzuAdmin");
        req1.Content = new StringContent(
            JsonSerializer.Serialize(new { channel = "slack", target = "https://hooks.slack.com/old" }, JsonOptions),
            Encoding.UTF8, "application/json");
        await client.SendAsync(req1);

        // Update
        using var req2 = BuildAuthorizedRequest(HttpMethod.Post, "/api/v1/admin/alerts/delivery-config", userId, tenantId, "SanzuAdmin");
        req2.Content = new StringContent(
            JsonSerializer.Serialize(new { channel = "slack", target = "https://hooks.slack.com/new" }, JsonOptions),
            Encoding.UTF8, "application/json");
        var response = await client.SendAsync(req2);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify only one config for "slack"
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var configs = db.AlertDeliveryConfigs.Where(c => c.Channel == "slack").ToList();
        configs.Should().HaveCount(1);
        configs[0].Target.Should().Be("https://hooks.slack.com/new");
    }

    // ── Helpers ──

    private async Task<(Guid tenantId, Guid userId)> SeedTenantAndAdminAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var tenantId = Guid.NewGuid();
        db.Organizations.Add(new Organization
        {
            Id = tenantId, Name = $"ManualAlert-{Guid.NewGuid():N}", Location = "Test",
            Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, OrgId = tenantId, Email = $"manual.{userId:N}@sanzu.pt", FullName = "Manual Admin", CreatedAt = DateTime.UtcNow });
        db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), UserId = userId, RoleType = Enum.Parse<PlatformRole>(role), TenantId = null, GrantedBy = userId, GrantedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();
        return (tenantId, userId);
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
