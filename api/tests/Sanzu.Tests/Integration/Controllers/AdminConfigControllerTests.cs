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

public sealed class AdminConfigControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminConfigControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAlertThresholds_Should_ReturnDefaults()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/config/alerts", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AlertThresholdsDto>>(JsonOptions);
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.HealthScoreRedThreshold.Should().Be(30);
        envelope.Data!.CaseStalledDaysThreshold.Should().Be(14);
        envelope.Data!.OnboardingStalledDaysThreshold.Should().Be(21);
        envelope.Data!.BillingFailedAlertEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAlertThresholds_Should_PersistAndReturn()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var putRequest = BuildAuthorizedRequest(HttpMethod.Put, "/api/v1/admin/config/alerts", userId, tenantId, "SanzuAdmin");
        putRequest.Content = JsonContent(new
        {
            healthScoreRedThreshold = 25,
            caseStalledDaysThreshold = 10,
            onboardingStalledDaysThreshold = 30,
            billingFailedAlertEnabled = false
        });

        var putResponse = await client.SendAsync(putRequest);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await putResponse.Content.ReadFromJsonAsync<ApiEnvelope<AlertThresholdsDto>>(JsonOptions);
        envelope!.Data!.HealthScoreRedThreshold.Should().Be(25);
        envelope.Data!.CaseStalledDaysThreshold.Should().Be(10);
        envelope.Data!.OnboardingStalledDaysThreshold.Should().Be(30);
        envelope.Data!.BillingFailedAlertEnabled.Should().BeFalse();

        // Verify GET returns updated values
        using var getRequest = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/config/alerts", userId, tenantId, "SanzuAdmin");
        var getResponse = await client.SendAsync(getRequest);
        var getEnvelope = await getResponse.Content.ReadFromJsonAsync<ApiEnvelope<AlertThresholdsDto>>(JsonOptions);
        getEnvelope!.Data!.HealthScoreRedThreshold.Should().Be(25);
    }

    [Fact]
    public async Task UpdateAlertThresholds_Should_Return403_ForNonAdmin()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuOps");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Put, "/api/v1/admin/config/alerts", userId, tenantId, "SanzuOps");
        request.Content = JsonContent(new
        {
            healthScoreRedThreshold = 25,
            caseStalledDaysThreshold = 10,
            onboardingStalledDaysThreshold = 30,
            billingFailedAlertEnabled = false
        });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetHealthWeights_Should_Return200WithValidWeights()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/config/health-weights", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<HealthWeightsDto>>(JsonOptions);
        envelope!.Data.Should().NotBeNull();
        // Weights should sum to 100 (either defaults or previously updated values)
        var sum = envelope.Data!.BillingWeight + envelope.Data!.CaseCompletionWeight + envelope.Data!.OnboardingWeight;
        sum.Should().Be(100);
    }

    [Fact]
    public async Task UpdateHealthWeights_Should_PersistAndReturn()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Put, "/api/v1/admin/config/health-weights", userId, tenantId, "SanzuAdmin");
        request.Content = JsonContent(new
        {
            billingWeight = 50,
            caseCompletionWeight = 30,
            onboardingWeight = 20
        });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<HealthWeightsDto>>(JsonOptions);
        envelope!.Data!.BillingWeight.Should().Be(50);
        envelope.Data!.CaseCompletionWeight.Should().Be(30);
        envelope.Data!.OnboardingWeight.Should().Be(20);
    }

    [Fact]
    public async Task UpdateHealthWeights_Should_Return400_WhenNotSumTo100()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Put, "/api/v1/admin/config/health-weights", userId, tenantId, "SanzuAdmin");
        request.Content = JsonContent(new
        {
            billingWeight = 50,
            caseCompletionWeight = 30,
            onboardingWeight = 30
        });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateHealthWeights_Should_Return403_ForNonAdmin()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuFinance");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Put, "/api/v1/admin/config/health-weights", userId, tenantId, "SanzuFinance");
        request.Content = JsonContent(new
        {
            billingWeight = 40,
            caseCompletionWeight = 35,
            onboardingWeight = 25
        });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAlertThresholds_Should_Return403_ForViewer()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuViewer");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/config/alerts", userId, tenantId, "SanzuViewer");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Helpers ──

    private sealed class AlertThresholdsDto
    {
        public int HealthScoreRedThreshold { get; set; }
        public int CaseStalledDaysThreshold { get; set; }
        public int OnboardingStalledDaysThreshold { get; set; }
        public bool BillingFailedAlertEnabled { get; set; }
    }

    private sealed class HealthWeightsDto
    {
        public int BillingWeight { get; set; }
        public int CaseCompletionWeight { get; set; }
        public int OnboardingWeight { get; set; }
    }

    private async Task<(Guid tenantId, Guid userId)> SeedAdminAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var tenantId = Guid.NewGuid();
        db.Organizations.Add(new Organization
        {
            Id = tenantId, Name = $"Config-{Guid.NewGuid():N}", Location = "Test",
            Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, OrgId = tenantId, Email = $"config.{userId:N}@sanzu.pt", FullName = "Config Admin", CreatedAt = DateTime.UtcNow });
        db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), UserId = userId, RoleType = Enum.Parse<PlatformRole>(role), TenantId = null, GrantedBy = userId, GrantedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();
        return (tenantId, userId);
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
