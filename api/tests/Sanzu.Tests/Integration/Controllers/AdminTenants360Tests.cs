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

public sealed class AdminTenants360Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminTenants360Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ═══════════════════════════════════════════════════════════
    // Summary endpoint
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task Summary_Should_Return200WithFields_ForSanzuAdmin()
    {
        var tenantId = await SeedTenantAsync("Summary-Admin");
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuAdmin);
        await SeedHealthScoreAsync(tenantId, 75, HealthBand.Green);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/tenants/{tenantId}/summary",
            adminUserId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<TenantSummaryResponse>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Id.Should().Be(tenantId);
        envelope.Data.Name.Should().Contain("Summary-Admin");
        envelope.Data.Status.Should().Be("Active");
        envelope.Data.HealthScore.Should().Be(75);
        envelope.Data.HealthBand.Should().Be("Green");
    }

    [Theory]
    [InlineData("SanzuAdmin")]
    [InlineData("SanzuOps")]
    [InlineData("SanzuFinance")]
    [InlineData("SanzuSupport")]
    [InlineData("SanzuViewer")]
    public async Task Summary_Should_Return200_ForAllAdminRoles(string role)
    {
        var tenantId = await SeedTenantAsync($"Summary-{role}");
        var adminUserId = await SeedAdminUserAsync(tenantId, Enum.Parse<PlatformRole>(role));

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/tenants/{tenantId}/summary",
            adminUserId, tenantId, role);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Summary_Should_Return404_ForNonExistentTenant()
    {
        var tenantId = await SeedTenantAsync("Summary-404");
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuAdmin);
        var bogusId = Guid.NewGuid();

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/tenants/{bogusId}/summary",
            adminUserId, tenantId, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═══════════════════════════════════════════════════════════
    // Billing endpoint
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task Billing_Should_Return200WithInvoices_ForSanzuFinance()
    {
        var tenantId = await SeedTenantAsync("Billing-Finance");
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuFinance);
        await SeedBillingRecordAsync(tenantId, "INV-00001");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/tenants/{tenantId}/billing",
            adminUserId, tenantId, "SanzuFinance");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<TenantBillingResponse>>(JsonOptions);
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.SubscriptionPlan.Should().Be("Profissional");
        envelope.Data.RecentInvoices.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Billing_Should_Return403_ForSanzuSupport()
    {
        var tenantId = await SeedTenantAsync("Billing-Support");
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuSupport);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/tenants/{tenantId}/billing",
            adminUserId, tenantId, "SanzuSupport");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Billing_Should_Return403_ForSanzuViewer()
    {
        var tenantId = await SeedTenantAsync("Billing-Viewer");
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuViewer);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/tenants/{tenantId}/billing",
            adminUserId, tenantId, "SanzuViewer");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ═══════════════════════════════════════════════════════════
    // Cases endpoint
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task Cases_Should_Return200WithWorkflowProgress_ForSanzuOps()
    {
        var tenantId = await SeedTenantAsync("Cases-Ops");
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuOps);
        await SeedCaseWithStepsAsync(tenantId, adminUserId);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/tenants/{tenantId}/cases",
            adminUserId, tenantId, "SanzuOps");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<TenantCasesResponse>>(JsonOptions);
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Cases.Should().NotBeEmpty();
        var caseItem = envelope.Data.Cases[0];
        caseItem.WorkflowProgress.TotalSteps.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Cases_Should_Return403_ForSanzuFinance()
    {
        var tenantId = await SeedTenantAsync("Cases-Finance");
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuFinance);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/tenants/{tenantId}/cases",
            adminUserId, tenantId, "SanzuFinance");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ═══════════════════════════════════════════════════════════
    // Activity endpoint
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task Activity_Should_Return200WithTimeline_ForSanzuSupport()
    {
        var tenantId = await SeedTenantAsync("Activity-Support");
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuSupport);
        var caseId = await SeedCaseAsync(tenantId, adminUserId);
        await SeedAuditEventAsync(caseId, adminUserId, "Case.Created");

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/tenants/{tenantId}/activity",
            adminUserId, tenantId, "SanzuSupport");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<TenantActivityResponse>>(JsonOptions);
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Events.Should().NotBeEmpty();
        envelope.Data.Events[0].EventType.Should().Be("Case.Created");
    }

    [Fact]
    public async Task Activity_Should_Return403_ForSanzuFinance()
    {
        var tenantId = await SeedTenantAsync("Activity-Finance");
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuFinance);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/tenants/{tenantId}/activity",
            adminUserId, tenantId, "SanzuFinance");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Activity_Should_Return403_ForSanzuViewer()
    {
        var tenantId = await SeedTenantAsync("Activity-Viewer");
        var adminUserId = await SeedAdminUserAsync(tenantId, PlatformRole.SanzuViewer);

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/tenants/{tenantId}/activity",
            adminUserId, tenantId, "SanzuViewer");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ═══════════════════════════════════════════════════════════
    // Auth guards (shared across all 360 endpoints)
    // ═══════════════════════════════════════════════════════════

    [Theory]
    [InlineData("summary")]
    [InlineData("billing")]
    [InlineData("cases")]
    [InlineData("activity")]
    public async Task AllEndpoints_Should_Return401_WhenUnauthenticated(string endpoint)
    {
        var tenantId = Guid.NewGuid();
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/admin/tenants/{tenantId}/{endpoint}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("summary")]
    [InlineData("billing")]
    [InlineData("cases")]
    [InlineData("activity")]
    public async Task AllEndpoints_Should_Return403_ForAgencyAdmin(string endpoint)
    {
        var tenantId = await SeedTenantAsync($"AgencyGuard-{endpoint}");
        var userId = Guid.NewGuid();

        var client = _factory.CreateClient();
        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/tenants/{tenantId}/{endpoint}",
            userId, tenantId, "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════

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
            SubscriptionBillingCycle = "Monthly",
            SubscriptionActivatedAt = DateTime.UtcNow.AddMonths(-3),
            InvoiceProfileBillingEmail = $"{nameSuffix.ToLower()}@test.com",
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
            Email = $"admin-360.{userId:N}@sanzu.pt",
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

    private async Task SeedBillingRecordAsync(Guid tenantId, string invoiceNumber)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        dbContext.BillingRecords.Add(new BillingRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceNumber = invoiceNumber,
            BillingCycleStart = DateTime.UtcNow.AddMonths(-1),
            BillingCycleEnd = DateTime.UtcNow,
            PlanCode = "Profissional",
            BillingCycle = "Monthly",
            BaseAmount = 99.00m,
            TotalAmount = 99.00m,
            Currency = "EUR",
            Status = "FINALIZED",
            InvoiceSnapshot = "{}",
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid> SeedCaseAsync(Guid tenantId, Guid managerUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var caseId = Guid.NewGuid();

        dbContext.Cases.Add(new Case
        {
            Id = caseId,
            TenantId = tenantId,
            CaseNumber = $"CASE-{Guid.NewGuid():N}".Substring(0, 10).ToUpper(),
            DeceasedFullName = "Test Person",
            DateOfDeath = DateTime.UtcNow.AddDays(-30),
            Status = CaseStatus.Active,
            ManagerUserId = managerUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return caseId;
    }

    private async Task SeedCaseWithStepsAsync(Guid tenantId, Guid managerUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var caseId = Guid.NewGuid();

        dbContext.Cases.Add(new Case
        {
            Id = caseId,
            TenantId = tenantId,
            CaseNumber = $"CASE-{Guid.NewGuid():N}".Substring(0, 10).ToUpper(),
            DeceasedFullName = "Steps Person",
            DateOfDeath = DateTime.UtcNow.AddDays(-15),
            Status = CaseStatus.Active,
            WorkflowKey = "standard",
            ManagerUserId = managerUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        dbContext.WorkflowStepInstances.Add(new WorkflowStepInstance
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            TenantId = tenantId,
            StepKey = "intake",
            Title = "Complete Intake",
            Sequence = 1,
            Status = WorkflowStepStatus.Complete,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        dbContext.WorkflowStepInstances.Add(new WorkflowStepInstance
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            TenantId = tenantId,
            StepKey = "docs",
            Title = "Upload Documents",
            Sequence = 2,
            Status = WorkflowStepStatus.InProgress,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    private async Task SeedAuditEventAsync(Guid caseId, Guid actorUserId, string eventType)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();

        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            ActorUserId = actorUserId,
            EventType = eventType,
            Metadata = "{}",
            CreatedAt = DateTime.UtcNow
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
