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

public sealed class AdminTenantsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminTenantsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── AC #1: GET /admin/tenants returns 200 with paginated tenant list ──

    [Fact]
    public async Task ListTenants_Should_Return200WithPaginatedList_ForSanzuAdmin()
    {
        var tenantId = await SeedTenantAsync("ListAll-Admin");
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuAdmin);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/tenants",
            adminUserId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<PaginatedResponse<TenantListItemResponse>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Items.Should().NotBeEmpty();
        envelope.Data.Page.Should().BeGreaterThanOrEqualTo(1);
        envelope.Data.PageSize.Should().BeGreaterThan(0);
        envelope.Data.TotalCount.Should().BeGreaterThan(0);
    }

    // ── AC #10: 401 when unauthenticated ──

    [Fact]
    public async Task ListTenants_Should_Return401_WhenUnauthenticated()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admin/tenants");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── AC #10: 403 for non-admin role ──

    [Fact]
    public async Task ListTenants_Should_Return403_ForAgencyAdmin()
    {
        var tenantId = await SeedTenantAsync("ListTenants-Agency");
        var userId = Guid.NewGuid();

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/tenants",
            userId, tenantId, "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── AC #10: All 5 admin roles return 200 ──

    [Theory]
    [InlineData("SanzuAdmin")]
    [InlineData("SanzuOps")]
    [InlineData("SanzuFinance")]
    [InlineData("SanzuSupport")]
    [InlineData("SanzuViewer")]
    public async Task ListTenants_Should_Return200_ForAllAdminRoles(string role)
    {
        var tenantId = await SeedTenantAsync($"AllRoles-{role}");
        var adminUserId = await SeedAdminUserAsync(tenantId, Enum.Parse<PlatformRole>(role));

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/tenants",
            adminUserId, tenantId, role);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── AC #2: Name filter ──

    [Fact]
    public async Task ListTenants_Should_FilterByName()
    {
        var suffix = $"UniqueSearchTarget-{Guid.NewGuid():N}";
        var tenantId = await SeedTenantAsync(suffix);
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuAdmin);
        var expectedName = $"Tenant-{suffix}";

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/tenants?name=UniqueSearchTarget",
            adminUserId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<PaginatedResponse<TenantListItemResponse>>>(JsonOptions);
        envelope!.Data!.Items.Should().Contain(t => t.Name == expectedName);
    }

    // ── AC #3: Status filter ──

    [Fact]
    public async Task ListTenants_Should_FilterByStatus()
    {
        var tenantId = await SeedTenantAsync("StatusFilter-Suspended", TenantStatus.Suspended);
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuAdmin);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/tenants?status=Suspended",
            adminUserId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<PaginatedResponse<TenantListItemResponse>>>(JsonOptions);
        envelope!.Data!.Items.Should().OnlyContain(t => t.Status == "Suspended");
    }

    // ── AC #4: HealthBand filter ──

    [Fact]
    public async Task ListTenants_Should_FilterByHealthBand()
    {
        var tenantId = await SeedTenantAsync("HealthFilter-Red");
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuAdmin);
        await SeedHealthScoreAsync(tenantId, 20, HealthBand.Red);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/tenants?healthBand=Red",
            adminUserId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<PaginatedResponse<TenantListItemResponse>>>(JsonOptions);
        envelope!.Data!.Items.Should().OnlyContain(t => t.HealthBand == "Red");
        envelope.Data.Items.Should().Contain(t => t.Id == tenantId);
    }

    // ── AC #7: Sort by healthScore ──

    [Fact]
    public async Task ListTenants_Should_SortByHealthScoreAsc()
    {
        var tenantA = await SeedTenantAsync("SortHealth-A");
        var tenantB = await SeedTenantAsync("SortHealth-B");
        var adminUserId = await SeedAdminUserAsync(tenantA, PlatformRole.SanzuAdmin);
        await SeedHealthScoreAsync(tenantA, 80, HealthBand.Green);
        await SeedHealthScoreAsync(tenantB, 20, HealthBand.Red);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/tenants?sort=healthScore&order=asc",
            adminUserId, tenantA, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<PaginatedResponse<TenantListItemResponse>>>(JsonOptions);
        var scores = envelope!.Data!.Items
            .Where(t => t.HealthScore.HasValue)
            .Select(t => t.HealthScore!.Value)
            .ToList();

        scores.Should().BeInAscendingOrder();
    }

    // ── AC #8: Pagination ──

    [Fact]
    public async Task ListTenants_Should_PaginateCorrectly()
    {
        var tenantId = await SeedTenantAsync("Paginate-Test");
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuAdmin);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/tenants?page=1&pageSize=2",
            adminUserId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<PaginatedResponse<TenantListItemResponse>>>(JsonOptions);
        envelope!.Data!.Page.Should().Be(1);
        envelope.Data.PageSize.Should().Be(2);
        envelope.Data.Items.Count.Should().BeLessThanOrEqualTo(2);
        envelope.Data.TotalPages.Should().BeGreaterThanOrEqualTo(1);
    }

    // ── AC #11: Audit event logged ──

    [Fact]
    public async Task ListTenants_Should_LogAuditEvent()
    {
        var tenantId = await SeedTenantAsync("AuditTest-List");
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuAdmin);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var beforeCount = dbContext.AuditEvents
            .Count(e => e.EventType.Contains("Tenants"));

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/tenants",
            adminUserId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterCount = dbContext.AuditEvents
            .Count(e => e.EventType.Contains("Tenants"));
        afterCount.Should().BeGreaterThan(beforeCount, "AdminAuditActionFilter should log an audit event");
    }

    // ── AC #9: Combined filters use AND logic ──

    [Fact]
    public async Task ListTenants_Should_CombineFiltersWithAndLogic()
    {
        var suffix = $"CombinedFilter-{Guid.NewGuid():N}";
        var tenantId = await SeedTenantAsync(suffix, TenantStatus.Active);
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuAdmin);
        var expectedName = $"Tenant-{suffix}";

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/tenants?name=CombinedFilter&status=Suspended",
            adminUserId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<PaginatedResponse<TenantListItemResponse>>>(JsonOptions);
        // Name matches but status doesn't → 0 results for this tenant
        envelope!.Data!.Items.Should().NotContain(t => t.Name == expectedName);
    }

    // ── AC #7: Default sort is CreatedAt descending ──

    [Fact]
    public async Task ListTenants_Should_DefaultSortByCreatedAtDescending()
    {
        var tenantId = await SeedTenantAsync("DefaultSort-Test");
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuAdmin);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/tenants",
            adminUserId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<PaginatedResponse<TenantListItemResponse>>>(JsonOptions);
        var dates = envelope!.Data!.Items.Select(t => t.SignupDate).ToList();
        dates.Should().BeInDescendingOrder();
    }

    // ── Helpers ──

    private async Task<Guid> SeedTenantAsync(string nameSuffix, TenantStatus status = TenantStatus.Active)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var tenantId = Guid.NewGuid();
        dbContext.Organizations.Add(new Organization
        {
            Id = tenantId,
            Name = $"Tenant-{nameSuffix}",
            Location = "EU-West",
            Status = status,
            SubscriptionPlan = "Profissional",
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
            Email = $"tenant-admin.{userId:N}@sanzu.pt",
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

    private async Task SeedHealthScoreAsync(Guid tenantId, int score, HealthBand band)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        dbContext.TenantHealthScores.Add(new TenantHealthScore
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OverallScore = score,
            BillingScore = score,
            CaseCompletionScore = score,
            OnboardingScore = score,
            HealthBand = band,
            PrimaryIssue = band == HealthBand.Red ? "BillingFailed" : null,
            ComputedAt = DateTime.UtcNow
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
