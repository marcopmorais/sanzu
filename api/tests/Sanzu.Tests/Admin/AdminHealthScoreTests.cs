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

public sealed class AdminHealthScoreTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminHealthScoreTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── AC #4: SanzuAdmin can list health scores ──

    [Fact]
    public async Task ListHealthScores_Should_ReturnOk_When_SanzuAdmin()
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync("SanzuAdmin");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/health-scores",
            userId,
            tenantId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── AC #4: SanzuOps can list health scores ──

    [Fact]
    public async Task ListHealthScores_Should_ReturnOk_When_SanzuOps()
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync("SanzuOps");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/health-scores",
            userId,
            tenantId,
            "SanzuOps");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── AC #5: Non-admin roles get 403 ──

    [Theory]
    [InlineData("SanzuFinance")]
    [InlineData("SanzuSupport")]
    [InlineData("SanzuViewer")]
    public async Task ListHealthScores_Should_Return403_When_NonOpsRole(string role)
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync(role);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/health-scores",
            userId,
            tenantId,
            role);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── AC #5: Unauthenticated gets 401 ──

    [Fact]
    public async Task ListHealthScores_Should_Return401_When_Unauthenticated()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/health-scores");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Compute: SanzuAdmin can trigger ──

    [Fact]
    public async Task Compute_Should_Return204_When_SanzuAdmin()
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync("SanzuAdmin");

        // Seed an active tenant
        await SeedActiveTenantAsync();

        using var request = BuildAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/admin/health-scores/compute",
            userId,
            tenantId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Compute: Creates TenantHealthScore rows ──

    [Fact]
    public async Task Compute_Should_CreateHealthScoreRows()
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync("SanzuAdmin");
        var activeTenantId = await SeedActiveTenantAsync();

        using var request = BuildAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/admin/health-scores/compute",
            userId,
            tenantId,
            "SanzuAdmin");

        await client.SendAsync(request);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.TenantHealthScores.Should().Contain(s => s.TenantId == activeTenantId);
    }

    // ── FloorCap: Failed billing caps overall score ──

    [Fact]
    public async Task Compute_Should_ApplyFloorCap_When_BillingFailed()
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync("SanzuAdmin");
        var failedTenantId = await SeedActiveTenantWithFailedBillingAsync();

        using var request = BuildAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/admin/health-scores/compute",
            userId,
            tenantId,
            "SanzuAdmin");

        await client.SendAsync(request);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var score = dbContext.TenantHealthScores
            .Where(s => s.TenantId == failedTenantId)
            .OrderByDescending(s => s.ComputedAt)
            .FirstOrDefault();

        score.Should().NotBeNull();
        score!.OverallScore.Should().BeLessThanOrEqualTo(30);
        score.PrimaryIssue.Should().Be("BillingFailed");
        score.HealthBand.Should().Be(HealthBand.Red);
    }

    // ── HealthBand: Green >= 70 ──

    [Fact]
    public async Task Compute_Should_ClassifyGreen_When_HealthyTenant()
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync("SanzuAdmin");
        var healthyTenantId = await SeedHealthyTenantAsync();

        using var request = BuildAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/admin/health-scores/compute",
            userId,
            tenantId,
            "SanzuAdmin");

        await client.SendAsync(request);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var score = dbContext.TenantHealthScores
            .Where(s => s.TenantId == healthyTenantId)
            .OrderByDescending(s => s.ComputedAt)
            .FirstOrDefault();

        score.Should().NotBeNull();
        score!.OverallScore.Should().BeGreaterThanOrEqualTo(70);
        score.HealthBand.Should().Be(HealthBand.Green);
    }

    // ── Compute: SanzuOps gets 403 on compute ──

    [Fact]
    public async Task Compute_Should_Return403_When_SanzuOps()
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync("SanzuOps");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Post,
            "/api/v1/admin/health-scores/compute",
            userId,
            tenantId,
            "SanzuOps");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
                Email = $"health.{roleName.ToLowerInvariant()}.{userId:N}@sanzu.pt",
                FullName = $"Health Test {roleName}",
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

    private async Task<Guid> SeedActiveTenantAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var tenantId = Guid.NewGuid();

        dbContext.Organizations.Add(
            new Organization
            {
                Id = tenantId,
                Name = $"ActiveTenant-{tenantId:N}",
                Location = "Test",
                Status = TenantStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
        return tenantId;
    }

    private async Task<Guid> SeedActiveTenantWithFailedBillingAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var tenantId = Guid.NewGuid();

        dbContext.Organizations.Add(
            new Organization
            {
                Id = tenantId,
                Name = $"FailedBilling-{tenantId:N}",
                Location = "Test",
                Status = TenantStatus.Active,
                FailedPaymentAttempts = 3,
                LastPaymentFailedAt = DateTime.UtcNow.AddDays(-1),
                OnboardingCompletedAt = DateTime.UtcNow.AddDays(-30),
                SubscriptionActivatedAt = DateTime.UtcNow.AddDays(-30),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
        return tenantId;
    }

    private async Task<Guid> SeedHealthyTenantAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var tenantId = Guid.NewGuid();

        dbContext.Organizations.Add(
            new Organization
            {
                Id = tenantId,
                Name = $"HealthyTenant-{tenantId:N}",
                Location = "Test",
                Status = TenantStatus.Active,
                SubscriptionPlan = "Pro",
                PaymentMethodType = "CreditCard",
                OnboardingCompletedAt = DateTime.UtcNow.AddDays(-60),
                SubscriptionActivatedAt = DateTime.UtcNow.AddDays(-60),
                DefaultLocale = "pt-PT",
                DefaultTimeZone = "Europe/Lisbon",
                DefaultCurrency = "EUR",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
        return tenantId;
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
