using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;
using Sanzu.Tests.Integration;

namespace Sanzu.Tests.Integration.Controllers;

public sealed class AdminAuditControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminAuditControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Search Endpoint ──

    [Fact]
    public async Task Search_Should_Return200WithPaginatedResults()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        await SeedAuditEventsAsync(userId, 3);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/audit", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AuditSearchResponse>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Items.Should().NotBeEmpty();
        envelope.Data.TotalCount.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task Search_Should_FilterByActorUserId()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        var specificActor = Guid.NewGuid();
        await SeedSpecificAuditEventAsync(specificActor, "Test.Event", null);
        await SeedAuditEventsAsync(userId, 2);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, $"/api/v1/admin/audit?actorUserId={specificActor}", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AuditSearchResponse>>(JsonOptions);
        envelope!.Data!.Items.Should().OnlyContain(x => x.ActorUserId == specificActor);
    }

    [Fact]
    public async Task Search_Should_FilterByEventType()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        await SeedSpecificAuditEventAsync(userId, "Admin.Tenant.Update", null);
        await SeedSpecificAuditEventAsync(userId, "Admin.Case.Create", null);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/audit?eventType=Tenant", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AuditSearchResponse>>(JsonOptions);
        envelope!.Data!.Items.Should().OnlyContain(x => x.EventType.Contains("Tenant"));
    }

    [Fact]
    public async Task Search_Should_FilterByDateRange()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        var now = DateTime.UtcNow;
        await SeedSpecificAuditEventAsync(userId, "Admin.Recent.Event", null, now.AddHours(-1));
        await SeedSpecificAuditEventAsync(userId, "Admin.Old.Event", null, now.AddDays(-30));

        var client = _factory.CreateClient();
        var from = now.AddDays(-2).ToString("O");
        var to = now.ToString("O");
        using var request = BuildAuthorizedRequest(HttpMethod.Get, $"/api/v1/admin/audit?dateFrom={Uri.EscapeDataString(from)}&dateTo={Uri.EscapeDataString(to)}", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AuditSearchResponse>>(JsonOptions);
        envelope!.Data!.Items.Should().OnlyContain(x => x.Timestamp >= now.AddDays(-2));
    }

    [Fact]
    public async Task Search_Should_SupportCursorPagination()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        // Use a dedicated actor so auto-audit events (from the filter) don't shift offsets
        var paginationActor = Guid.NewGuid();
        await SeedAuditEventsAsync(paginationActor, 5);

        var client = _factory.CreateClient();

        // First page (pageSize=2), filtered by our specific actor
        using var req1 = BuildAuthorizedRequest(HttpMethod.Get, $"/api/v1/admin/audit?pageSize=2&actorUserId={paginationActor}", userId, tenantId, "SanzuAdmin");
        var res1 = await client.SendAsync(req1);
        var page1 = await res1.Content.ReadFromJsonAsync<ApiEnvelope<AuditSearchResponse>>(JsonOptions);
        page1!.Data!.Items.Should().HaveCount(2);
        page1.Data.NextCursor.Should().NotBeNullOrEmpty();

        // Second page using cursor
        using var req2 = BuildAuthorizedRequest(HttpMethod.Get, $"/api/v1/admin/audit?pageSize=2&actorUserId={paginationActor}&cursor={Uri.EscapeDataString(page1.Data.NextCursor!)}", userId, tenantId, "SanzuAdmin");
        var res2 = await client.SendAsync(req2);
        var page2 = await res2.Content.ReadFromJsonAsync<ApiEnvelope<AuditSearchResponse>>(JsonOptions);
        page2!.Data!.Items.Should().HaveCount(2);

        // Pages should not overlap
        var page1Ids = page1.Data.Items.Select(x => x.Id).ToHashSet();
        page2.Data.Items.Should().OnlyContain(x => !page1Ids.Contains(x.Id));
    }

    [Fact]
    public async Task Search_Should_ResolveActorNames()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        await SeedAuditEventsAsync(userId, 1);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, $"/api/v1/admin/audit?actorUserId={userId}", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AuditSearchResponse>>(JsonOptions);
        envelope!.Data!.Items.Should().Contain(x => x.ActorName == "Audit Admin");
    }

    // ── RBAC ──

    [Fact]
    public async Task Search_Should_Return403_ForSanzuViewer()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuViewer");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/audit", userId, tenantId, "SanzuViewer");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Search_Should_Return403_ForSanzuSupport()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuSupport");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/audit", userId, tenantId, "SanzuSupport");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Search_Should_Return200_ForSanzuFinance()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuFinance");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/audit", userId, tenantId, "SanzuFinance");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Export Endpoint ──

    [Fact]
    public async Task Export_Csv_Should_ReturnCsvFile()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        await SeedAuditEventsAsync(userId, 2);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/audit/export?format=csv", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Id,ActorUserId,ActorName,EventType,CaseId,Metadata,Timestamp");
    }

    [Fact]
    public async Task Export_Json_Should_ReturnJsonFile()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin");
        await SeedAuditEventsAsync(userId, 2);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/audit/export?format=json", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        var items = JsonSerializer.Deserialize<List<AuditEventResponse>>(content, JsonOptions);
        items.Should().NotBeNull();
        items!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Export_Should_Return403_ForSanzuOps()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuOps");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/audit/export?format=csv", userId, tenantId, "SanzuOps");

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
            Id = tenantId, Name = $"AuditCtrl-{Guid.NewGuid():N}", Location = "Test",
            Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, OrgId = tenantId, Email = $"audit.{userId:N}@sanzu.pt", FullName = "Audit Admin", CreatedAt = DateTime.UtcNow });
        db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), UserId = userId, RoleType = Enum.Parse<PlatformRole>(role), TenantId = null, GrantedBy = userId, GrantedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();
        return (tenantId, userId);
    }

    private async Task SeedAuditEventsAsync(Guid actorUserId, int count)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        for (var i = 0; i < count; i++)
        {
            db.AuditEvents.Add(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = $"Admin.Test.Event{i}",
                Metadata = $"{{\"index\":{i}}}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task SeedSpecificAuditEventAsync(Guid actorUserId, string eventType, Guid? caseId, DateTime? createdAt = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            EventType = eventType,
            CaseId = caseId,
            Metadata = "{}",
            CreatedAt = createdAt ?? DateTime.UtcNow
        });

        await db.SaveChangesAsync();
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
