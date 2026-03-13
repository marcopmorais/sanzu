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

public sealed class AdminFunnelControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminFunnelControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetFunnel_Should_Return200WithSixStages()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/analytics/funnel", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<FunnelDto>>(JsonOptions);
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Stages.Should().HaveCount(6);
        envelope.Data!.Stages.Select(s => s.StageName).Should().ContainInOrder(
            "Signup", "OnboardingDefaults", "OnboardingComplete", "BillingActive", "FirstCaseCreated", "ActiveUsage");
    }

    [Fact]
    public async Task GetFunnel_Should_Return200_ForViewer()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuViewer");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/analytics/funnel", userId, tenantId, "SanzuViewer");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetFunnel_Should_SupportCohortFilter()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");
        var now = DateTime.UtcNow;
        var cohortValue = $"{now.Year}-{now.Month:D2}";

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get,
            $"/api/v1/admin/analytics/funnel?cohort=month&cohortValue={cohortValue}",
            userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<FunnelDto>>(JsonOptions);
        envelope!.Data!.Cohort.Should().Be("month");
        envelope.Data!.CohortValue.Should().Be(cohortValue);
    }

    [Fact]
    public async Task GetStageTenants_Should_Return200()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get,
            "/api/v1/admin/analytics/funnel/stages/signup/tenants",
            userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<List<FunnelTenantDto>>>(JsonOptions);
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetStageTenants_Should_ReturnTenantDetails()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get,
            "/api/v1/admin/analytics/funnel/stages/signup/tenants",
            userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<List<FunnelTenantDto>>>(JsonOptions);
        var tenant = envelope!.Data!.FirstOrDefault(t => t.TenantId == tenantId);
        tenant.Should().NotBeNull();
        tenant!.DaysAtStage.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetFunnel_Should_ShowDropOff()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/analytics/funnel", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<FunnelDto>>(JsonOptions);

        // Signup stage should have 0 drop-off (it's the first stage)
        var signupStage = envelope!.Data!.Stages.First(s => s.StageName == "Signup");
        signupStage.DropOffCount.Should().Be(0);
    }

    // ── Helpers ──

    private sealed class FunnelDto
    {
        public List<FunnelStageDto> Stages { get; set; } = [];
        public string? Cohort { get; set; }
        public string? CohortValue { get; set; }
    }

    private sealed class FunnelStageDto
    {
        public string StageName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int DropOffCount { get; set; }
        public double DropOffPercentage { get; set; }
    }

    private sealed class FunnelTenantDto
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime SignupDate { get; set; }
        public int DaysAtStage { get; set; }
    }

    private async Task<(Guid tenantId, Guid userId)> SeedAdminAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var tenantId = Guid.NewGuid();
        db.Organizations.Add(new Organization
        {
            Id = tenantId, Name = $"Funnel-{Guid.NewGuid():N}", Location = "Test",
            Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, OrgId = tenantId, Email = $"funnel.{userId:N}@sanzu.pt", FullName = "Funnel Admin", CreatedAt = DateTime.UtcNow });
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
