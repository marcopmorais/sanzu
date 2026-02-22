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

public sealed class AdminCommsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminCommsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SendCommunication_Should_Return201()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, $"/api/v1/admin/tenants/{tenantId}/actions/send-communication", userId, tenantId, "SanzuAdmin");
        request.Content = JsonContent(new { subject = "Billing reminder", body = "Please update your payment method.", messageType = "billing" });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task SendCommunication_Should_Return403_ForSanzuViewer()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuViewer");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, $"/api/v1/admin/tenants/{tenantId}/actions/send-communication", userId, tenantId, "SanzuViewer");
        request.Content = JsonContent(new { subject = "Test", body = "Test", messageType = "support" });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetTenantComms_Should_Return200()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuOps");
        await SeedCommunicationAsync(tenantId, userId);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, $"/api/v1/admin/tenants/{tenantId}/comms", userId, tenantId, "SanzuOps");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<List<CommDto>>>(JsonOptions);
        envelope!.Data.Should().NotBeEmpty();
        envelope.Data!.Should().OnlyContain(c => c.TenantId == tenantId);
    }

    [Fact]
    public async Task SearchComms_Should_Return200()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        await SeedCommunicationAsync(tenantId, userId);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/comms", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchComms_Should_FilterByType()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        await SeedCommunicationAsync(tenantId, userId, "escalation");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/comms?type=escalation", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<List<CommDto>>>(JsonOptions);
        envelope!.Data!.Should().OnlyContain(c => c.MessageType == "escalation");
    }

    [Fact]
    public async Task SearchComms_Should_FilterByTenantId()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        await SeedCommunicationAsync(tenantId, userId);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, $"/api/v1/admin/comms?tenantId={tenantId}", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<List<CommDto>>>(JsonOptions);
        envelope!.Data!.Should().OnlyContain(c => c.TenantId == tenantId);
    }

    // ── Helpers ──

    private sealed class CommDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid SenderUserId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    private async Task<(Guid tenantId, Guid userId)> SeedTenantAndAdminAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var tenantId = Guid.NewGuid();
        db.Organizations.Add(new Organization
        {
            Id = tenantId, Name = $"Comms-{Guid.NewGuid():N}", Location = "Test",
            Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, OrgId = tenantId, Email = $"comms.{userId:N}@sanzu.pt", FullName = "Comms Admin", CreatedAt = DateTime.UtcNow });
        db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), UserId = userId, RoleType = Enum.Parse<PlatformRole>(role), TenantId = null, GrantedBy = userId, GrantedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();
        return (tenantId, userId);
    }

    private async Task SeedCommunicationAsync(Guid tenantId, Guid senderUserId, string messageType = "support")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        db.TenantCommunications.Add(new TenantCommunication
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SenderUserId = senderUserId,
            MessageType = messageType,
            Subject = "Test communication",
            Body = "Test body",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
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
