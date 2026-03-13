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

public sealed class AdminReportsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminReportsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BusinessSummary_Should_Return200()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, "/api/v1/admin/reports/business-summary", userId, tenantId, "SanzuAdmin");
        request.Content = JsonContent(new { month = 2, year = 2026 });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<BusinessSummaryDto>>(JsonOptions);
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.TotalActiveTenants.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task BusinessSummary_Should_Return200_ForFinance()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuFinance");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, "/api/v1/admin/reports/business-summary", userId, tenantId, "SanzuFinance");
        request.Content = JsonContent(new { month = 1, year = 2026 });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BusinessSummary_Should_Return403_ForOps()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuOps");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, "/api/v1/admin/reports/business-summary", userId, tenantId, "SanzuOps");
        request.Content = JsonContent(new { month = 1, year = 2026 });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ComplianceAudit_Should_Return200()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuAdmin");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, "/api/v1/admin/reports/compliance-audit", userId, tenantId, "SanzuAdmin");
        request.Content = JsonContent(new
        {
            dateFrom = "2026-01-01T00:00:00Z",
            dateTo = "2026-12-31T23:59:59Z"
        });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<ComplianceAuditDto>>(JsonOptions);
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.TotalEvents.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ComplianceAudit_Should_Return403_ForFinance()
    {
        var (tenantId, userId) = await SeedAdminAsync("SanzuFinance");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, "/api/v1/admin/reports/compliance-audit", userId, tenantId, "SanzuFinance");
        request.Content = JsonContent(new
        {
            dateFrom = "2026-01-01T00:00:00Z",
            dateTo = "2026-12-31T23:59:59Z"
        });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BusinessSummary_Should_ReturnRevenueMetrics()
    {
        var (tenantId, userId) = await SeedWithBillingAsync();

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(HttpMethod.Post, "/api/v1/admin/reports/business-summary", userId, tenantId, "SanzuAdmin");
        request.Content = JsonContent(new { month = DateTime.UtcNow.Month, year = DateTime.UtcNow.Year });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<BusinessSummaryDto>>(JsonOptions);
        envelope!.Data!.Mrr.Should().BeGreaterThanOrEqualTo(0);
        envelope.Data!.Arr.Should().BeGreaterThanOrEqualTo(0);
    }

    // ── Helpers ──

    private sealed class BusinessSummaryDto
    {
        public int TotalActiveTenants { get; set; }
        public int NewSignups { get; set; }
        public int ChurnedTenants { get; set; }
        public double Mrr { get; set; }
        public double Arr { get; set; }
        public double GrowthRate { get; set; }
        public double ChurnRate { get; set; }
        public int BillingFailedCount { get; set; }
        public int BillingOverdueCount { get; set; }
        public int AlertsFired { get; set; }
    }

    private sealed class ComplianceAuditDto
    {
        public int TotalEvents { get; set; }
        public int UniqueActors { get; set; }
    }

    private async Task<(Guid tenantId, Guid userId)> SeedAdminAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var tenantId = Guid.NewGuid();
        db.Organizations.Add(new Organization
        {
            Id = tenantId, Name = $"Reports-{Guid.NewGuid():N}", Location = "Test",
            Status = TenantStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, OrgId = tenantId, Email = $"rpt.{userId:N}@sanzu.pt", FullName = "Report Admin", CreatedAt = DateTime.UtcNow });
        db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), UserId = userId, RoleType = Enum.Parse<PlatformRole>(role), TenantId = null, GrantedBy = userId, GrantedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();
        return (tenantId, userId);
    }

    private async Task<(Guid tenantId, Guid userId)> SeedWithBillingAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var tenantId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        db.Organizations.Add(new Organization
        {
            Id = tenantId, Name = $"BillingReport-{Guid.NewGuid():N}", Location = "Test",
            Status = TenantStatus.Active, CreatedAt = now, UpdatedAt = now
        });

        db.BillingRecords.Add(new BillingRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceNumber = $"INV-RPT-{Guid.NewGuid():N}",
            BillingCycleStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            BillingCycleEnd = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1),
            TotalAmount = 99.99m,
            Currency = "EUR",
            Status = "Paid",
            CreatedAt = now
        });

        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, OrgId = tenantId, Email = $"rptbill.{userId:N}@sanzu.pt", FullName = "Billing Reporter", CreatedAt = now });
        db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), UserId = userId, RoleType = PlatformRole.SanzuAdmin, TenantId = null, GrantedBy = userId, GrantedAt = now });

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
