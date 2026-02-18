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

public sealed class CopilotControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CopilotControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RequestDraft_EvidenceRequest_ShouldReturnDraftWithExplainability()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "copilot-evidence@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Copilot Evidence Case");
        await SeedBlockedStepAsync(signup.OrganizationId, createdCase.CaseId, "Gather ID Documents", BlockedReasonCode.EvidenceMissing, "Missing passport copy");

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/copilot/draft",
            new RequestCopilotDraftRequest
            {
                DraftType = "evidence_request",
                CaseId = createdCase.CaseId
            },
            signup.UserId, signup.OrganizationId, "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CopilotDraftResponse>>();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.DraftType.Should().Be("evidence_request");
        envelope.Data.Content.Should().Contain("Gather ID Documents");
        envelope.Data.Checklist.Should().NotBeEmpty();
        envelope.Data.Explainability.Should().NotBeNull();
        envelope.Data.Explainability.BasedOn.Should().NotBeNullOrEmpty();
        envelope.Data.Explainability.ReasonCategory.Should().Be("EvidenceMissing");
        envelope.Data.Explainability.ConfidenceBand.Should().Be("high");
        envelope.Data.Explainability.SafeFallback.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RequestDraft_HandoffChecklist_ShouldReturnChecklistDraft()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "copilot-handoff@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Copilot Handoff Case");

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/copilot/draft",
            new RequestCopilotDraftRequest
            {
                DraftType = "handoff_checklist",
                CaseId = createdCase.CaseId
            },
            signup.UserId, signup.OrganizationId, "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CopilotDraftResponse>>();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.DraftType.Should().Be("handoff_checklist");
        envelope.Data.Checklist.Should().NotBeEmpty();
        envelope.Data.Explainability.Should().NotBeNull();
    }

    [Fact]
    public async Task AcceptDraft_ShouldRecordAuditEvent()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "copilot-accept@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Copilot Accept Case");

        // Generate a draft first
        using var draftRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/copilot/draft",
            new RequestCopilotDraftRequest
            {
                DraftType = "evidence_request",
                CaseId = createdCase.CaseId
            },
            signup.UserId, signup.OrganizationId, "AgencyAdmin");

        var draftResponse = await client.SendAsync(draftRequest);
        draftResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var draftEnvelope = await draftResponse.Content.ReadFromJsonAsync<ApiEnvelope<CopilotDraftResponse>>();
        var draftId = draftEnvelope!.Data!.Id;

        // Accept the draft
        using var acceptRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/copilot/draft/accept",
            new AcceptCopilotDraftRequest
            {
                DraftId = draftId,
                EditedContent = "Slightly edited content"
            },
            signup.UserId, signup.OrganizationId, "AgencyAdmin");

        var acceptResponse = await client.SendAsync(acceptRequest);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var acceptEnvelope = await acceptResponse.Content.ReadFromJsonAsync<ApiEnvelope<CopilotDraftAcceptedResponse>>();
        acceptEnvelope!.Data.Should().NotBeNull();
        acceptEnvelope.Data!.Status.Should().Be("Accepted");
        acceptEnvelope.Data.DraftId.Should().Be(draftId);

        // Verify audit events
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().Contain(e => e.EventType == "CopilotDraftRequested" && e.ActorUserId == signup.UserId);
        dbContext.AuditEvents.Should().Contain(e => e.EventType == "CopilotDraftAccepted" && e.ActorUserId == signup.UserId);
    }

    [Fact]
    public async Task RequestDraft_InvalidType_ShouldReturn409()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "copilot-invalid@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Copilot Invalid Case");

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/copilot/draft",
            new RequestCopilotDraftRequest
            {
                DraftType = "nonexistent_type",
                CaseId = createdCase.CaseId
            },
            signup.UserId, signup.OrganizationId, "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RequestDraft_RecoveryPlan_ShouldReturnPlanWithSteps()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "copilot-recovery@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Copilot Recovery Case");
        await SeedBlockedStepAsync(signup.OrganizationId, createdCase.CaseId, "Payment Verification", BlockedReasonCode.PaymentOrBilling, "Outstanding invoice");

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/copilot/draft",
            new RequestCopilotDraftRequest
            {
                DraftType = "recovery_plan",
                CaseId = createdCase.CaseId
            },
            signup.UserId, signup.OrganizationId, "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CopilotDraftResponse>>();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.DraftType.Should().Be("recovery_plan");
        envelope.Data.Content.Should().Contain("Payment Verification");
        envelope.Data.Checklist.Should().NotBeEmpty();
        envelope.Data.Explainability.ReasonCategory.Should().Be("PaymentOrBilling");
    }

    [Fact]
    public async Task RequestDraft_ExplainWhy_ShouldReturnExplanation()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "copilot-explain@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Copilot Explain Case");
        await SeedBlockedStepAsync(signup.OrganizationId, createdCase.CaseId, "External Registry Check", BlockedReasonCode.ExternalDependency, "Awaiting registry response");

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/copilot/draft",
            new RequestCopilotDraftRequest
            {
                DraftType = "explain_why",
                CaseId = createdCase.CaseId
            },
            signup.UserId, signup.OrganizationId, "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CopilotDraftResponse>>();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.DraftType.Should().Be("explain_why");
        envelope.Data.Content.Should().Contain("External Registry Check");
        envelope.Data.Explainability.ReasonCategory.Should().Be("ExternalDependency");
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
                PaymentMethodReference = "pm_copilot_tests",
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
