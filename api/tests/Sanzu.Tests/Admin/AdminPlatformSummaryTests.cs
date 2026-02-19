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

public sealed class AdminPlatformSummaryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminPlatformSummaryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── AC #1: SanzuAdmin gets platform summary with correct shape ──

    [Fact]
    public async Task GetSummary_Should_ReturnOk_When_SanzuAdmin()
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync("SanzuAdmin");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/platform/summary",
            userId,
            tenantId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<PlatformOperationsSummaryResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.TotalActiveTenants.Should().BeGreaterThanOrEqualTo(0);
        envelope.Data.TotalActiveCases.Should().BeGreaterThanOrEqualTo(0);
        envelope.Data.WorkflowStepsCompleted.Should().BeGreaterThanOrEqualTo(0);
        envelope.Data.WorkflowStepsActive.Should().BeGreaterThanOrEqualTo(0);
        envelope.Data.WorkflowStepsBlocked.Should().BeGreaterThanOrEqualTo(0);
        envelope.Data.TotalDocuments.Should().BeGreaterThanOrEqualTo(0);
    }

    // ── AC #3: Non-SanzuAdmin roles get 403 ──

    [Theory]
    [InlineData("SanzuOps")]
    [InlineData("SanzuFinance")]
    [InlineData("SanzuSupport")]
    [InlineData("SanzuViewer")]
    public async Task GetSummary_Should_Return403_When_NonSanzuAdmin(string role)
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync(role);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/platform/summary",
            userId,
            tenantId,
            role);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSummary_Should_Return403_When_AgencyAdmin()
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync("AgencyAdmin");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/platform/summary",
            userId,
            tenantId,
            "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSummary_Should_Return401_When_Unauthenticated()
    {
        var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/platform/summary");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── AC #1: Summary counts match seeded data ──

    [Fact]
    public async Task GetSummary_Should_ReturnCorrectCounts_When_DataSeeded()
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync("SanzuAdmin");
        await SeedPlatformDataAsync(tenantId, userId);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/platform/summary",
            userId,
            tenantId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<PlatformOperationsSummaryResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();

        // We seeded 1 active tenant, 1 active case, 3 workflow steps, 1 document
        envelope.Data!.TotalActiveTenants.Should().BeGreaterThanOrEqualTo(1);
        envelope.Data.TotalActiveCases.Should().BeGreaterThanOrEqualTo(1);
        envelope.Data.WorkflowStepsCompleted.Should().BeGreaterThanOrEqualTo(1);
        envelope.Data.WorkflowStepsActive.Should().BeGreaterThanOrEqualTo(1);
        envelope.Data.WorkflowStepsBlocked.Should().BeGreaterThanOrEqualTo(1);
        envelope.Data.TotalDocuments.Should().BeGreaterThanOrEqualTo(1);
    }

    // ── AC #2: Audit event logged ──

    [Fact]
    public async Task GetSummary_Should_LogAuditEvent_When_CalledByAdmin()
    {
        var client = _factory.CreateClient();
        var (userId, tenantId) = await SeedUserWithRoleAsync("SanzuAdmin");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            "/api/v1/admin/platform/summary",
            userId,
            tenantId,
            "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "Admin.Platform.GetSummary"
                 && x.ActorUserId == userId);
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
                Status = TenantStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        dbContext.Users.Add(
            new User
            {
                Id = userId,
                OrgId = tenantId,
                Email = $"platform.{roleName.ToLowerInvariant()}.{userId:N}@sanzu.pt",
                FullName = $"Platform Test {roleName}",
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

    private async Task SeedPlatformDataAsync(Guid tenantId, Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        var caseId = Guid.NewGuid();
        dbContext.Cases.Add(
            new Case
            {
                Id = caseId,
                TenantId = tenantId,
                CaseNumber = "PS-001",
                DeceasedFullName = "Test Deceased",
                DateOfDeath = DateTime.UtcNow.AddDays(-30),
                Status = CaseStatus.Active,
                ManagerUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        dbContext.WorkflowStepInstances.Add(
            new WorkflowStepInstance
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CaseId = caseId,
                StepKey = "step-1",
                Title = "Completed Step",
                Sequence = 1,
                Status = WorkflowStepStatus.Complete,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        dbContext.WorkflowStepInstances.Add(
            new WorkflowStepInstance
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CaseId = caseId,
                StepKey = "step-2",
                Title = "Active Step",
                Sequence = 2,
                Status = WorkflowStepStatus.InProgress,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        dbContext.WorkflowStepInstances.Add(
            new WorkflowStepInstance
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CaseId = caseId,
                StepKey = "step-3",
                Title = "Blocked Step",
                Sequence = 3,
                Status = WorkflowStepStatus.Blocked,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        dbContext.CaseDocuments.Add(
            new CaseDocument
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CaseId = caseId,
                FileName = "test-doc.pdf",
                ContentType = "application/pdf",
                SizeBytes = 1024,
                Content = new byte[] { 0x00 },
                UploadedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
