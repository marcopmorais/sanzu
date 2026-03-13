using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Infrastructure.Data;
using Sanzu.Tests.Integration;

namespace Sanzu.Tests.Integration.Controllers;

public sealed class AdminRevenueExportTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminRevenueExportTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Revenue Export ──

    [Fact]
    public async Task ExportRevenue_Should_Return200WithCsvContentType()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuFinance", "Inicial");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/revenue/export", userId, tenantId, "SanzuFinance");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }

    [Fact]
    public async Task ExportRevenue_Should_ContainExpectedHeaders()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin", "Profissional");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/revenue/export", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        content.Should().StartWith("TenantName,PlanTier,MrrContribution,BillingStatus,LastPaymentDate,NextRenewal");
    }

    [Fact]
    public async Task ExportRevenue_Should_IncludeSeededTenantData()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin", "Inicial");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/revenue/export", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Length.Should().BeGreaterThan(1, "should have header + at least one data row");
    }

    // ── Billing Health Export ──

    [Fact]
    public async Task ExportBillingHealth_Should_Return200WithCsvContentType()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin", "Inicial");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/revenue/billing-health/export", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }

    [Fact]
    public async Task ExportBillingHealth_Should_ContainExpectedHeaders()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin", "Inicial");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/revenue/billing-health/export", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        content.Should().StartWith("TenantName,IssueType,FailedAmount,LastFailedAt,GracePeriodRetryAt,NextRenewalDate");
    }

    // ── RBAC tests ──

    [Fact]
    public async Task ExportRevenue_Should_Return401_WhenUnauthenticated()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admin/revenue/export");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExportRevenue_Should_Return403_ForNonFinanceRole()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuSupport", "Inicial");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/revenue/export", userId, tenantId, "SanzuSupport");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExportBillingHealth_Should_Return403_ForNonFinanceRole()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuViewer", "Inicial");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/revenue/billing-health/export", userId, tenantId, "SanzuViewer");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Audit test ──

    [Fact]
    public async Task ExportRevenue_Should_LogAuditEvent()
    {
        var (tenantId, userId) = await SeedTenantAndAdminAsync("SanzuAdmin", "Inicial");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var beforeCount = dbContext.AuditEvents
            .Count(e => e.EventType.Contains("Revenue"));

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Get, "/api/v1/admin/revenue/export", userId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterCount = dbContext.AuditEvents
            .Count(e => e.EventType.Contains("Revenue"));
        afterCount.Should().BeGreaterThan(beforeCount);
    }

    // ── Helpers ──

    private async Task<(Guid tenantId, Guid userId)> SeedTenantAndAdminAsync(string role, string plan)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var tenantId = Guid.NewGuid();
        dbContext.Organizations.Add(new Organization
        {
            Id = tenantId,
            Name = $"Export-{Guid.NewGuid():N}",
            Location = "Test",
            Status = TenantStatus.Active,
            SubscriptionPlan = plan,
            OnboardingCompletedAt = DateTime.UtcNow.AddDays(-30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var userId = Guid.NewGuid();
        dbContext.Users.Add(new User
        {
            Id = userId,
            OrgId = tenantId,
            Email = $"export.{userId:N}@sanzu.pt",
            FullName = "Export Admin",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleType = Enum.Parse<PlatformRole>(role),
            TenantId = null,
            GrantedBy = userId,
            GrantedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return (tenantId, userId);
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
