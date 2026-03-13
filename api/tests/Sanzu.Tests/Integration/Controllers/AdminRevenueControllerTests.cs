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

public sealed class AdminRevenueControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminRevenueControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── GET /admin/revenue — Overview ──

    [Fact]
    public async Task GetOverview_Should_Return200WithRevenueOverview()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin", "Inicial");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/revenue", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<RevenueOverviewResponse>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Mrr.Should().BeGreaterThanOrEqualTo(0m);
        envelope.Data.Arr.Should().Be(envelope.Data.Mrr * 12);
        envelope.Data.PlanBreakdown.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOverview_Should_ComputeCorrectMrrFromPlanPricing()
    {
        var tenantId1 = await SeedTenantWithPlanAsync("Inicial", "Rev-MRR-1");
        var tenantId2 = await SeedTenantWithPlanAsync("Profissional", "Rev-MRR-2");
        var userId = await SeedAdminUserAsync(tenantId1, PlatformRole.SanzuFinance);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/revenue", userId, tenantId1, "SanzuFinance");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<RevenueOverviewResponse>>(JsonOptions);
        // At minimum should include our seeded tenants' MRR (49 + 99 = 148)
        envelope!.Data!.Mrr.Should().BeGreaterThanOrEqualTo(148m);
    }

    // ── GET /admin/revenue/trends ──

    [Fact]
    public async Task GetTrends_Should_Return200WithDataPoints()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin", "Profissional");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/revenue/trends?period=monthly", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<RevenueTrendsResponse>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.DataPoints.Should().NotBeNull();
    }

    [Theory]
    [InlineData("daily")]
    [InlineData("weekly")]
    [InlineData("monthly")]
    public async Task GetTrends_Should_AcceptAllPeriodValues(string period)
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin", "Inicial");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, $"/api/v1/admin/revenue/trends?period={period}", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /admin/revenue/billing-health ──

    [Fact]
    public async Task GetBillingHealth_Should_Return200WithHealthData()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin", "Inicial");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/revenue/billing-health", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<BillingHealthResponse>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.FailedPayments.Should().NotBeNull();
        envelope.Data.GracePeriodTenants.Should().NotBeNull();
        envelope.Data.UpcomingRenewals.Should().NotBeNull();
    }

    // ── Auth / RBAC tests ──

    [Fact]
    public async Task GetOverview_Should_Return401_WhenUnauthenticated()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admin/revenue");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOverview_Should_Return403_WhenNonFinanceRole()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuSupport", "Inicial");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/revenue", userId, tenantId, "SanzuSupport");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("SanzuAdmin")]
    [InlineData("SanzuFinance")]
    public async Task GetOverview_Should_Return200_ForAllowedRoles(string role)
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync(role, "Inicial");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/revenue", userId, tenantId, role);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOverview_Should_Return403_ForAgencyAdmin()
    {
        var tenantId = await SeedTenantWithPlanAsync("Inicial", "Rev-Agency");
        var userId = Guid.NewGuid();

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/revenue", userId, tenantId, "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Audit test ──

    [Fact]
    public async Task GetOverview_Should_LogAuditEvent_ViaAdminAuditActionFilter()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin", "Inicial");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var beforeCount = dbContext.AuditEvents
            .Count(e => e.EventType.Contains("Revenue"));

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/revenue", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterCount = dbContext.AuditEvents
            .Count(e => e.EventType.Contains("Revenue"));
        afterCount.Should().BeGreaterThan(beforeCount, "AdminAuditActionFilter should log an audit event");
    }

    // ── Helpers ──

    private async Task<(Guid tenantId, Guid userId)> SeedTenantAndAdminAsync(string role, string plan)
    {
        var tenantId = await SeedTenantWithPlanAsync(plan, $"Rev-{Guid.NewGuid():N}");
        var userId = await SeedAdminUserAsync(tenantId, Enum.Parse<PlatformRole>(role));
        return (tenantId, userId);
    }

    private async Task<Guid> SeedTenantWithPlanAsync(string plan, string nameSuffix)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var tenantId = Guid.NewGuid();
        dbContext.Organizations.Add(new Organization
        {
            Id = tenantId,
            Name = $"RevTenant-{nameSuffix}",
            Location = "Test",
            Status = TenantStatus.Active,
            SubscriptionPlan = plan,
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
            Email = $"rev.{userId:N}@sanzu.pt",
            FullName = "Revenue Admin",
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
