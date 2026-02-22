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

public sealed class AdminDashboardControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminDashboardControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── AC #1: GET /admin/dashboard/summary returns correct envelope ──

    [Fact]
    public async Task GetSummary_Should_Return200WithDashboardResponse_WhenSnapshotExists()
    {
        var tenantId = await SeedTenantAsync();
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuAdmin);
        await SeedSnapshotAsync(DateTime.UtcNow.AddMinutes(-2));

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/dashboard/summary",
            adminUserId,
            tenantId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<DashboardResponse<AdminDashboardSummary>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Data.Should().NotBeNull();
        envelope.Data.ComputedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
    }

    // ── AC #2: isStale = false when fresh ──

    [Fact]
    public async Task GetSummary_Should_ReturnIsStale_False_WhenSnapshotIsFresh()
    {
        var tenantId = await SeedTenantAsync();
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuAdmin);
        // Fresh: 3 min ago, which is < 2× default 5 min interval = 10 min
        await SeedSnapshotAsync(DateTime.UtcNow.AddMinutes(-3));

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/dashboard/summary",
            adminUserId,
            tenantId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<DashboardResponse<AdminDashboardSummary>>>(JsonOptions);
        envelope!.Data!.IsStale.Should().BeFalse("snapshot is less than 2× the 5-minute interval");
    }

    // ── AC #3: isStale = true when old ──

    [Fact]
    public async Task GetSummary_Should_ReturnIsStale_True_WhenSnapshotIsOld()
    {
        var tenantId = await SeedTenantAsync();
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuAdmin);
        // Old: 15 min ago, which is > 2× default 5 min interval = 10 min
        await SeedSnapshotAsync(DateTime.UtcNow.AddMinutes(-15));

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/dashboard/summary",
            adminUserId,
            tenantId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<DashboardResponse<AdminDashboardSummary>>>(JsonOptions);
        envelope!.Data!.IsStale.Should().BeTrue("snapshot is more than 2× the 5-minute interval");
    }

    // ── AC #4: default empty summary when no snapshot exists ──

    [Fact]
    public async Task GetSummary_Should_ReturnEmptySummaryWithIsStaleTrue_WhenNoSnapshotExists()
    {
        var tenantId = await SeedTenantAsync();
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuAdmin);

        // Clear any snapshots left by other tests in the shared database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
            var existing = db.AdminDashboardSnapshots
                .Where(s => s.SnapshotType == "DashboardSummary")
                .ToList();
            db.AdminDashboardSnapshots.RemoveRange(existing);
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/dashboard/summary",
            adminUserId,
            tenantId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<DashboardResponse<AdminDashboardSummary>>>(JsonOptions);
        envelope!.Data!.IsStale.Should().BeTrue("no snapshot exists");
        envelope.Data.Data.Tenants.Total.Should().Be(0);
        envelope.Data.Data.Revenue.Mrr.Should().Be(0m);
    }

    // ── Auth tests ──

    [Fact]
    public async Task GetSummary_Should_Return401_WhenUnauthenticated()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admin/dashboard/summary");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSummary_Should_Return403_WhenNonAdminRole()
    {
        var tenantId = await SeedTenantAsync();
        var userId = Guid.NewGuid();

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/dashboard/summary",
            userId,
            tenantId,
            "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("SanzuAdmin")]
    [InlineData("SanzuOps")]
    [InlineData("SanzuFinance")]
    [InlineData("SanzuSupport")]
    [InlineData("SanzuViewer")]
    public async Task GetSummary_Should_Return200_ForAllAdminRoles(string role)
    {
        var tenantId = await SeedTenantAsync();
        var adminUserId = await SeedAdminUserAsync(tenantId, Enum.Parse<PlatformRole>(role));

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/dashboard/summary",
            adminUserId,
            tenantId,
            role);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Audit test ──

    [Fact]
    public async Task GetSummary_Should_LogAuditEvent_ViaAdminAuditActionFilter()
    {
        var tenantId = await SeedTenantAsync();
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuAdmin);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var beforeCount = dbContext.AuditEvents
            .Count(e => e.EventType.Contains("Dashboard"));

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/dashboard/summary",
            adminUserId,
            tenantId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterCount = dbContext.AuditEvents
            .Count(e => e.EventType.Contains("Dashboard"));
        afterCount.Should().BeGreaterThan(beforeCount, "AdminAuditActionFilter should log an audit event");
    }

    // ── Helpers ──

    private async Task<Guid> SeedTenantAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var tenantId = Guid.NewGuid();
        dbContext.Organizations.Add(new Organization
        {
            Id = tenantId,
            Name = $"DashCtrl-{tenantId:N}",
            Location = "Test",
            Status = TenantStatus.Active,
            OnboardingCompletedAt = DateTime.UtcNow.AddDays(-30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return tenantId;
    }

    private async Task<Guid> SeedAdminUserAsync(Guid tenantId, PlatformRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var userId = Guid.NewGuid();

        dbContext.Users.Add(new User
        {
            Id = userId,
            OrgId = tenantId,
            Email = $"admin.{userId:N}@sanzu.pt",
            FullName = "Admin User",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleType = role,
            TenantId = null,
            GrantedBy = userId,
            GrantedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return userId;
    }

    private async Task SeedSnapshotAsync(DateTime computedAt)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var summary = new AdminDashboardSummary(
            computedAt,
            new TenantCounts(10, 7, 2, 1, 0),
            new RevenuePulse(5000m, 60000m, 2.1m, 5.3m),
            new HealthDistribution(6, 3, 1, new List<AtRiskTenant>
            {
                new(Guid.NewGuid(), "At-Risk Agency", 25, "BillingFailed")
            }),
            new AlertCounts(1, 3, 5, 2),
            new OnboardingStatus(85.5m, 1));

        var payload = JsonSerializer.Serialize(summary, JsonOptions);

        // Remove any existing snapshot to avoid upsert complexity in tests
        var existing = dbContext.AdminDashboardSnapshots
            .Where(s => s.SnapshotType == "DashboardSummary")
            .ToList();
        dbContext.AdminDashboardSnapshots.RemoveRange(existing);

        dbContext.AdminDashboardSnapshots.Add(new AdminDashboardSnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotType = "DashboardSummary",
            JsonPayload = payload,
            ComputedAt = computedAt,
            ExpiresAt = computedAt.AddMinutes(10)
        });

        await dbContext.SaveChangesAsync();
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
