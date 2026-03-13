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

public sealed class AdminHealthHistoryControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminHealthHistoryControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealthHistory_Should_Return200()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuOps");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, $"/api/v1/admin/tenants/{tenantId}/health-history", userId, tenantId, "SanzuOps");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<HealthHistoryDto>>(JsonOptions);
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.TenantId.Should().Be(tenantId);
        envelope.Data!.Trend.Should().BeOneOf("Improving", "Degrading", "Stable");
    }

    [Fact]
    public async Task GetHealthHistory_Should_ReturnDataPoints()
    {
        var (tenantId, userId) = await SeedWithHealthScoresAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, $"/api/v1/admin/tenants/{tenantId}/health-history?period=90d", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<HealthHistoryDto>>(JsonOptions);
        envelope!.Data!.DataPoints.Should().NotBeEmpty();
        envelope.Data!.Period.Should().Be("90d");
    }

    [Fact]
    public async Task GetHealthHistory_Should_ComputeImprovingTrend()
    {
        var (tenantId, userId) = await SeedWithTrendDataAsync(olderScore: 30, recentScore: 80);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, $"/api/v1/admin/tenants/{tenantId}/health-history", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<HealthHistoryDto>>(JsonOptions);
        envelope!.Data!.Trend.Should().Be("Improving");
    }

    [Fact]
    public async Task GetHealthHistory_Should_ComputeDegradingTrend()
    {
        var (tenantId, userId) = await SeedWithTrendDataAsync(olderScore: 80, recentScore: 30);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, $"/api/v1/admin/tenants/{tenantId}/health-history", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<HealthHistoryDto>>(JsonOptions);
        envelope!.Data!.Trend.Should().Be("Degrading");
    }

    [Fact]
    public async Task GetHealthHistory_Should_Return403_ForViewer()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuViewer");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, $"/api/v1/admin/tenants/{tenantId}/health-history", userId, tenantId, "SanzuViewer");

        var response = await client.SendAsync(request);
        // AdminSupport allows SanzuAdmin, SanzuOps, SanzuSupport — not SanzuViewer
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetHealthHistory_Should_FilterByPeriod()
    {
        var (tenantId, userId) = await SeedWithHealthScoresAsync("SanzuAdmin", includedOldData: true);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, $"/api/v1/admin/tenants/{tenantId}/health-history?period=30d", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<HealthHistoryDto>>(JsonOptions);
        envelope!.Data!.Period.Should().Be("30d");
    }

    // ── Helpers ──

    private sealed class HealthHistoryDto
    {
        public Guid TenantId { get; set; }
        public string Period { get; set; } = string.Empty;
        public string Trend { get; set; } = string.Empty;
        public List<HealthHistoryDataPointDto> DataPoints { get; set; } = [];
    }

    private sealed class HealthHistoryDataPointDto
    {
        public DateTime Date { get; set; }
        public int Score { get; set; }
        public string HealthBand { get; set; } = string.Empty;
    }

    private async Task<(Guid tenantId, Guid userId)> SeedAdminAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var tenantId = Guid.NewGuid();
        db.Organizations.Add(new Organization
        {
            Id = tenantId, Name = $"HealthHistory-{Guid.NewGuid():N}", Location = "Test",
            Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, OrgId = tenantId, Email = $"hh.{userId:N}@sanzu.pt", FullName = "Health Admin", CreatedAt = DateTime.UtcNow });
        db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), UserId = userId, RoleType = Enum.Parse<PlatformRole>(role), TenantId = null, GrantedBy = userId, GrantedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();
        return (tenantId, userId);
    }

    private async Task<(Guid tenantId, Guid userId)> SeedWithHealthScoresAsync(string role, bool includedOldData = false)
    {
        var (tenantId, userId) = await SeedAdminAsync(role);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        for (var i = 0; i < 10; i++)
        {
            db.TenantHealthScores.Add(new TenantHealthScore
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OverallScore = 50 + i * 3,
                BillingScore = 60,
                CaseCompletionScore = 50,
                OnboardingScore = 40,
                HealthBand = HealthBand.Yellow,
                ComputedAt = DateTime.UtcNow.AddDays(-i * 5)
            });
        }

        if (includedOldData)
        {
            db.TenantHealthScores.Add(new TenantHealthScore
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OverallScore = 20,
                BillingScore = 20,
                CaseCompletionScore = 20,
                OnboardingScore = 20,
                HealthBand = HealthBand.Red,
                ComputedAt = DateTime.UtcNow.AddDays(-60)
            });
        }

        await db.SaveChangesAsync();
        return (tenantId, userId);
    }

    private async Task<(Guid tenantId, Guid userId)> SeedWithTrendDataAsync(int olderScore, int recentScore)
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        // Older data (30+ days ago)
        for (var i = 0; i < 5; i++)
        {
            db.TenantHealthScores.Add(new TenantHealthScore
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OverallScore = olderScore,
                BillingScore = olderScore,
                CaseCompletionScore = olderScore,
                OnboardingScore = olderScore,
                HealthBand = olderScore >= 70 ? HealthBand.Green : olderScore >= 40 ? HealthBand.Yellow : HealthBand.Red,
                ComputedAt = DateTime.UtcNow.AddDays(-30 - i)
            });
        }

        // Recent data (last 14 days)
        for (var i = 0; i < 5; i++)
        {
            db.TenantHealthScores.Add(new TenantHealthScore
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OverallScore = recentScore,
                BillingScore = recentScore,
                CaseCompletionScore = recentScore,
                OnboardingScore = recentScore,
                HealthBand = recentScore >= 70 ? HealthBand.Green : recentScore >= 40 ? HealthBand.Yellow : HealthBand.Red,
                ComputedAt = DateTime.UtcNow.AddDays(-i)
            });
        }

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
