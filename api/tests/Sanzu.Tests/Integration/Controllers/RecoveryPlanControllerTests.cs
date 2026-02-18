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

public sealed class RecoveryPlanControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RecoveryPlanControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RecoveryPlan_ShouldReturnPlanWithStepsAndExplainability()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "recovery-plan@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Recovery Plan Case");
        await SeedBlockedStepAsync(signup.OrganizationId, createdCase.CaseId, "Gather Tax Certificate", BlockedReasonCode.ExternalDependency, "Awaiting tax office response");

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/copilot/recovery-plan",
            new RequestRecoveryPlanRequest
            {
                CaseId = createdCase.CaseId
            },
            signup.UserId, signup.OrganizationId, "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<RecoveryPlanResponse>>();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.ReasonCategory.Should().Be("ExternalDependency");
        envelope.Data.ReasonLabel.Should().Be("Waiting on an external institution");
        envelope.Data.Steps.Should().NotBeEmpty();
        envelope.Data.Steps.Should().AllSatisfy(s =>
        {
            s.Order.Should().BeGreaterThan(0);
            s.Description.Should().NotBeNullOrEmpty();
            s.Owner.Should().NotBeNullOrEmpty();
        });
        envelope.Data.EvidenceChecklist.Should().NotBeEmpty();
        envelope.Data.Escalation.Should().NotBeNull();
        envelope.Data.Escalation.TargetRole.Should().NotBeNullOrEmpty();
        envelope.Data.Explainability.Should().NotBeNull();
        envelope.Data.Explainability.ConfidenceBand.Should().Be("high");
        envelope.Data.BoundaryMessage.Should().Contain("cannot perform autonomous changes");
    }

    [Fact]
    public async Task RecoveryPlan_ShouldRecordAuditEvent()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "recovery-audit@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Recovery Audit Case");

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/copilot/recovery-plan",
            new RequestRecoveryPlanRequest { CaseId = createdCase.CaseId },
            signup.UserId, signup.OrganizationId, "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().Contain(e => e.EventType == "RecoveryPlanGenerated" && e.ActorUserId == signup.UserId);
    }

    [Fact]
    public async Task AdminRecoveryPlan_ShouldWorkForSanzuAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "recovery-admin@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Admin Recovery Case");
        await GrantSanzuAdminAsync(signup.UserId);
        await SeedBlockedStepAsync(signup.OrganizationId, createdCase.CaseId, "Policy Review", BlockedReasonCode.PolicyRestriction, "Requires admin override");

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/admin/recovery/{signup.OrganizationId}/cases/{createdCase.CaseId}/plan",
            signup.UserId, Guid.Empty, "SanzuAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<RecoveryPlanResponse>>();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.ReasonCategory.Should().NotBeNullOrEmpty();
        envelope.Data.Steps.Should().NotBeEmpty();
        envelope.Data.BoundaryMessage.Should().Contain("cannot perform autonomous changes");
    }

    [Fact]
    public async Task RecoveryPlan_ShouldReturnLowConfidence_WhenNoBlockedSteps()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "recovery-noblock@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "No Blocked Steps Case");

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/copilot/recovery-plan",
            new RequestRecoveryPlanRequest { CaseId = createdCase.CaseId },
            signup.UserId, signup.OrganizationId, "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<RecoveryPlanResponse>>();
        envelope!.Data!.Explainability.ConfidenceBand.Should().Be("low");
        envelope.Data.Explainability.MissingOrUnknown.Should().NotBeEmpty();
    }

    private async Task SeedBlockedStepAsync(Guid tenantId, Guid caseId, string title, BlockedReasonCode reasonCode, string detail)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.WorkflowStepInstances.Add(new WorkflowStepInstance
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CaseId = caseId,
            StepKey = title.Replace(" ", "_").ToLowerInvariant(),
            Title = title,
            Sequence = 1,
            Status = WorkflowStepStatus.Blocked,
            BlockedReasonCode = reasonCode,
            BlockedReasonDetail = detail,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    private async Task GrantSanzuAdminAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = null,
            RoleType = PlatformRole.SanzuAdmin,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = userId
        });
        await dbContext.SaveChangesAsync();
    }

    private async Task ActivateTenantAsync(HttpClient client, CreateAgencyAccountResponse signup)
    {
        using var defaultsRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/defaults",
            new UpdateTenantOnboardingDefaultsRequest
            {
                DefaultLocale = "pt-PT",
                DefaultTimeZone = "Europe/Lisbon",
                DefaultCurrency = "EUR"
            },
            signup.UserId, signup.OrganizationId, "AgencyAdmin");
        var defaultsResponse = await client.SendAsync(defaultsRequest);
        defaultsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var completionRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/complete",
            new CompleteTenantOnboardingRequest { ConfirmCompletion = true },
            signup.UserId, signup.OrganizationId, "AgencyAdmin");
        var completionResponse = await client.SendAsync(completionRequest);
        completionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var activationRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/billing/activate",
            new ActivateTenantBillingRequest
            {
                PlanCode = "Growth",
                BillingCycle = "Monthly",
                PaymentMethodType = "Card",
                PaymentMethodReference = "pm_recovery_tests",
                InvoiceProfileLegalName = "Agency Lda",
                InvoiceProfileVatNumber = "PT123456789",
                InvoiceProfileBillingEmail = "billing@agency.pt",
                InvoiceProfileCountryCode = "PT"
            },
            signup.UserId, signup.OrganizationId, "AgencyAdmin");
        var activationResponse = await client.SendAsync(activationRequest);
        activationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<CreateCaseResponse> CreateCaseAsync(
        HttpClient client, CreateAgencyAccountResponse signup, string deceasedFullName)
    {
        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = deceasedFullName,
                DateOfDeath = DateTime.UtcNow.AddDays(-2),
                CaseType = "General",
                Urgency = "Normal"
            },
            signup.UserId, signup.OrganizationId, "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CreateCaseResponse>>();
        return envelope!.Data!;
    }

    private static HttpRequestMessage BuildAuthorizedJsonRequest(
        HttpMethod method, string uri, object payload, Guid userId, Guid tenantId, string role)
    {
        var message = new HttpRequestMessage(method, uri) { Content = JsonContent.Create(payload) };
        message.Headers.Add("X-User-Id", userId.ToString());
        message.Headers.Add("X-Tenant-Id", tenantId.ToString());
        message.Headers.Add("X-User-Role", role);
        return message;
    }

    private static HttpRequestMessage BuildAuthorizedRequest(
        HttpMethod method, string uri, Guid userId, Guid tenantId, string role)
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
            Email = email, FullName = "Agency Admin", AgencyName = "Agency", Location = "Lisbon"
        };
        var signupResponse = await client.PostAsJsonAsync("/api/v1/tenants/signup", request);
        signupResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await signupResponse.Content.ReadFromJsonAsync<ApiEnvelope<CreateAgencyAccountResponse>>();
        return envelope!.Data!;
    }
}
