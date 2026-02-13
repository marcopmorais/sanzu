using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Tests.Integration.Controllers;

public sealed class KpiControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public KpiControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDashboard_ShouldReturn200WithCurrentBaselineAndDrilldown_WhenActorIsSanzuAdmin()
    {
        var client = _factory.CreateClient();
        var signupA = await CreateTenantAsync(client, "kpi-dashboard-alpha@agency.pt");
        var signupB = await CreateTenantAsync(client, "kpi-dashboard-beta@agency.pt");
        await SetTenantStatusAsync(signupA.OrganizationId, TenantStatus.Active);
        await SetTenantStatusAsync(signupB.OrganizationId, TenantStatus.Active);
        var sanzuAdminUserId = await SeedSanzuAdminAsync(signupA.OrganizationId);
        await SeedKpiDataAsync(signupA.OrganizationId, signupB.OrganizationId, signupA.UserId, signupB.UserId);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/kpi/dashboard?periodDays=30&tenantLimit=10&caseLimit=10",
            sanzuAdminUserId,
            signupA.OrganizationId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<PlatformKpiDashboardResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Current.CasesCreated.Should().BeGreaterThanOrEqualTo(2);
        envelope.Data.Current.DocumentsUploaded.Should().BeGreaterThanOrEqualTo(2);
        envelope.Data.TenantContributions.Should().Contain(x => x.TenantId == signupA.OrganizationId);
        envelope.Data.TenantContributions.Should().Contain(x => x.TenantId == signupB.OrganizationId);
        envelope.Data.CaseContributions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDashboard_ShouldReturn403_WhenActorIsNotSanzuAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "kpi-dashboard-forbidden@agency.pt");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/kpi/dashboard?periodDays=30",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<Guid> SeedSanzuAdminAsync(Guid tenantId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var userId = Guid.NewGuid();

        dbContext.Users.Add(
            new User
            {
                Id = userId,
                Email = $"sanzu-admin-{userId:N}@sanzu.pt",
                FullName = "Sanzu Platform Admin",
                OrgId = tenantId,
                CreatedAt = DateTime.UtcNow
            });

        dbContext.UserRoles.Add(
            new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RoleType = PlatformRole.SanzuAdmin,
                TenantId = null,
                GrantedBy = userId,
                GrantedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
        return userId;
    }

    private async Task SetTenantStatusAsync(Guid tenantId, TenantStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var tenant = await dbContext.Organizations.FindAsync(tenantId);
        tenant.Should().NotBeNull();
        tenant!.Status = status;
        tenant.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedKpiDataAsync(Guid tenantAId, Guid tenantBId, Guid tenantAUserId, Guid tenantBUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var now = DateTime.UtcNow;

        var caseAId = Guid.NewGuid();
        var caseBId = Guid.NewGuid();
        dbContext.Cases.AddRange(
            new Case
            {
                Id = caseAId,
                TenantId = tenantAId,
                CaseNumber = "CASE-90001",
                DeceasedFullName = "KPI Alpha",
                DateOfDeath = now.Date.AddDays(-3),
                CaseType = "GENERAL",
                Urgency = "NORMAL",
                Status = CaseStatus.Active,
                ManagerUserId = tenantAUserId,
                CreatedAt = now.AddDays(-4),
                UpdatedAt = now.AddDays(-2)
            },
            new Case
            {
                Id = caseBId,
                TenantId = tenantBId,
                CaseNumber = "CASE-90002",
                DeceasedFullName = "KPI Beta",
                DateOfDeath = now.Date.AddDays(-8),
                CaseType = "GENERAL",
                Urgency = "NORMAL",
                Status = CaseStatus.Closed,
                ManagerUserId = tenantBUserId,
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now.AddDays(-1),
                ClosedAt = now.AddDays(-1)
            });

        dbContext.CaseDocuments.AddRange(
            new CaseDocument
            {
                Id = Guid.NewGuid(),
                TenantId = tenantAId,
                CaseId = caseAId,
                FileName = "alpha-kpi-doc.txt",
                ContentType = "text/plain",
                Content = System.Text.Encoding.UTF8.GetBytes("alpha"),
                SizeBytes = 5,
                UploadedByUserId = tenantAUserId,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2)
            },
            new CaseDocument
            {
                Id = Guid.NewGuid(),
                TenantId = tenantBId,
                CaseId = caseBId,
                FileName = "beta-kpi-doc.txt",
                ContentType = "text/plain",
                Content = System.Text.Encoding.UTF8.GetBytes("beta"),
                SizeBytes = 4,
                UploadedByUserId = tenantBUserId,
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1)
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

    private static async Task<CreateAgencyAccountResponse> CreateTenantAsync(HttpClient client, string email)
    {
        var request = new CreateAgencyAccountRequest
        {
            Email = email,
            FullName = "Agency Admin",
            AgencyName = $"Agency-{Guid.NewGuid():N}",
            Location = "Lisbon"
        };

        using var response = await client.PostAsJsonAsync("/api/v1/tenants/signup", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CreateAgencyAccountResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        return envelope.Data!;
    }
}
