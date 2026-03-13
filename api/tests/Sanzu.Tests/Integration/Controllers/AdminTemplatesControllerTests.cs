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

public sealed class AdminTemplatesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminTemplatesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetTemplates_Should_Return200()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/config/templates", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<List<TemplateDto>>>(JsonOptions);
        envelope!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateTemplate_Should_Return201()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, "/api/v1/admin/config/templates", userId, tenantId, "SanzuAdmin");
        request.Content = JsonContent(new
        {
            name = "Welcome Email",
            subject = "Welcome to Sanzu!",
            body = "Dear tenant, welcome aboard.",
            messageType = "onboarding"
        });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<TemplateDto>>(JsonOptions);
        envelope!.Data!.Name.Should().Be("Welcome Email");
        envelope.Data!.MessageType.Should().Be("onboarding");
        envelope.Data!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateTemplate_Should_Return400_WhenNameMissing()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, "/api/v1/admin/config/templates", userId, tenantId, "SanzuAdmin");
        request.Content = JsonContent(new
        {
            name = "",
            subject = "Some subject",
            body = "Body text",
            messageType = "support"
        });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTemplate_Should_Return200()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");
        var templateId = await SeedTemplateAsync();

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Put, $"/api/v1/admin/config/templates/{templateId}", userId, tenantId, "SanzuAdmin");
        request.Content = JsonContent(new
        {
            name = "Updated Template",
            subject = "Updated Subject",
            body = "Updated body text",
            messageType = "billing"
        });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<TemplateDto>>(JsonOptions);
        envelope!.Data!.Name.Should().Be("Updated Template");
        envelope.Data!.Subject.Should().Be("Updated Subject");
        envelope.Data!.MessageType.Should().Be("billing");
    }

    [Fact]
    public async Task UpdateTemplate_Should_Return404_WhenNotFound()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Put, $"/api/v1/admin/config/templates/{Guid.NewGuid()}", userId, tenantId, "SanzuAdmin");
        request.Content = JsonContent(new { name = "Test", subject = "Test" });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTemplate_Should_Return403_ForNonAdmin()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuOps");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, "/api/v1/admin/config/templates", userId, tenantId, "SanzuOps");
        request.Content = JsonContent(new { name = "Test", subject = "Test", body = "Body", messageType = "support" });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateAndList_Should_ShowNewTemplate()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");
        var uniqueName = $"Template-{Guid.NewGuid():N}";

        var client = _factory.CreateClient();

        // Create
        using var createReq = BuildAuthorizedRequest(HttpMethod.Post, "/api/v1/admin/config/templates", userId, tenantId, "SanzuAdmin");
        createReq.Content = JsonContent(new { name = uniqueName, subject = "Sub", body = "Body", messageType = "escalation" });
        var createResp = await client.SendAsync(createReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // List
        using var listReq = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/config/templates", userId, tenantId, "SanzuAdmin");
        var listResp = await client.SendAsync(listReq);
        var envelope = await listResp.Content.ReadFromJsonAsync<ApiEnvelope<List<TemplateDto>>>(JsonOptions);
        envelope!.Data!.Should().Contain(t => t.Name == uniqueName);
    }

    // ── Helpers ──

    private sealed class TemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }

    private async Task<(Guid tenantId, Guid userId)> SeedAdminAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var tenantId = Guid.NewGuid();
        db.Organizations.Add(new Organization
        {
            Id = tenantId, Name = $"Templates-{Guid.NewGuid():N}", Location = "Test",
            Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, OrgId = tenantId, Email = $"tpl.{userId:N}@sanzu.pt", FullName = "Template Admin", CreatedAt = DateTime.UtcNow });
        db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), UserId = userId, RoleType = Enum.Parse<PlatformRole>(role), TenantId = null, GrantedBy = userId, GrantedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();
        return (tenantId, userId);
    }

    private async Task<Guid> SeedTemplateAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var template = new CommunicationTemplate
        {
            Id = Guid.NewGuid(),
            Name = $"Seed-{Guid.NewGuid():N}",
            Subject = "Seed Subject",
            Body = "Seed Body",
            MessageType = "support",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.CommunicationTemplates.Add(template);
        await db.SaveChangesAsync();
        return template.Id;
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
