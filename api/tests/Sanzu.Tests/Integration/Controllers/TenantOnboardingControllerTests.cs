using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Tests.Integration.Controllers;

public sealed class TenantOnboardingControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TenantOnboardingControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateOnboardingProfile_ShouldReturn200AndMoveTenantToOnboarding_WhenAuthorizedAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "onboarding-admin-1@agency.pt");

        var request = new UpdateTenantOnboardingProfileRequest
        {
            AgencyName = "Agency Updated",
            Location = "Porto"
        };

        using var message = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/profile",
            request,
            signup.UserId,
            signup.OrganizationId);

        var response = await client.SendAsync(message);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<TenantOnboardingProfileResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.TenantStatus.Should().Be(Core.Enums.TenantStatus.Onboarding);
    }

    [Fact]
    public async Task CreateOnboardingInvitation_ShouldReturnConflict_WhenDuplicatePendingInviteExists()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "onboarding-admin-2@agency.pt");
        var invitationRequest = new CreateTenantInvitationRequest
        {
            Email = "invitee@agency.pt",
            RoleType = Core.Enums.PlatformRole.AgencyAdmin,
            ExpirationDays = 7
        };

        using var first = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/invitations",
            invitationRequest,
            signup.UserId,
            signup.OrganizationId);
        var firstResponse = await client.SendAsync(first);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var second = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/invitations",
            invitationRequest,
            signup.UserId,
            signup.OrganizationId);
        var secondResponse = await client.SendAsync(second);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CompleteOnboarding_ShouldReturn401_WhenRequestIsUnauthenticated()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "onboarding-admin-3@agency.pt");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/complete",
            new CompleteTenantOnboardingRequest { ConfirmCompletion = true });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateOnboardingDefaults_ShouldReturn403_WhenActorBelongsToDifferentTenant()
    {
        var client = _factory.CreateClient();
        var tenantA = await CreateTenantAsync(client, "tenant-a-admin@agency.pt");
        var tenantB = await CreateTenantAsync(client, "tenant-b-admin@agency.pt");

        var request = new UpdateTenantOnboardingDefaultsRequest
        {
            DefaultLocale = "pt-PT",
            DefaultTimeZone = "Europe/Lisbon",
            DefaultCurrency = "EUR"
        };

        using var message = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{tenantA.OrganizationId}/onboarding/defaults",
            request,
            tenantB.UserId,
            tenantB.OrganizationId);

        var response = await client.SendAsync(message);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CaseDefaultsEndpoints_ShouldReturnVersionedConfigAndApplyDefaultsToNewCases_WhenTenantAdminAuthorized()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "tenant-case-defaults-admin@agency.pt");
        await SetDefaultsAndCompleteOnboardingAsync(client, signup);
        await ActivateBillingAsync(client, signup, "Starter", "Monthly");

        using var updateRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/case-defaults",
            new UpdateTenantCaseDefaultsRequest
            {
                DefaultWorkflowKey = "workflow.v2",
                DefaultTemplateKey = "template.v5"
            },
            signup.UserId,
            signup.OrganizationId);
        var updateResponse = await client.SendAsync(updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateEnvelope = await updateResponse.Content.ReadFromJsonAsync<ApiEnvelope<TenantCaseDefaultsResponse>>();
        updateEnvelope.Should().NotBeNull();
        updateEnvelope!.Data.Should().NotBeNull();
        updateEnvelope.Data!.DefaultWorkflowKey.Should().Be("workflow.v2");
        updateEnvelope.Data.DefaultTemplateKey.Should().Be("template.v5");
        updateEnvelope.Data.Version.Should().BeGreaterThan(0);

        using var getRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/settings/case-defaults",
            signup.UserId,
            signup.OrganizationId);
        var getResponse = await client.SendAsync(getRequest);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getEnvelope = await getResponse.Content.ReadFromJsonAsync<ApiEnvelope<TenantCaseDefaultsResponse>>();
        getEnvelope.Should().NotBeNull();
        getEnvelope!.Data.Should().NotBeNull();
        getEnvelope.Data!.Version.Should().Be(updateEnvelope.Data.Version);
        getEnvelope.Data.DefaultWorkflowKey.Should().Be("workflow.v2");
        getEnvelope.Data.DefaultTemplateKey.Should().Be("template.v5");

        using var createCaseRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Case Defaults Integration",
                DateOfDeath = DateTime.UtcNow.AddDays(-2),
                CaseType = "General",
                Urgency = "Normal"
            },
            signup.UserId,
            signup.OrganizationId);
        var createCaseResponse = await client.SendAsync(createCaseRequest);
        createCaseResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createCaseEnvelope = await createCaseResponse.Content.ReadFromJsonAsync<ApiEnvelope<CreateCaseResponse>>();
        createCaseEnvelope.Should().NotBeNull();
        createCaseEnvelope!.Data.Should().NotBeNull();
        createCaseEnvelope.Data!.WorkflowKey.Should().Be("workflow.v2");
        createCaseEnvelope.Data.TemplateKey.Should().Be("template.v5");
    }

    [Fact]
    public async Task UpdateCaseDefaults_ShouldReturn403_WhenActorBelongsToDifferentTenant()
    {
        var client = _factory.CreateClient();
        var tenantA = await CreateTenantAsync(client, "tenant-case-defaults-a@agency.pt");
        var tenantB = await CreateTenantAsync(client, "tenant-case-defaults-b@agency.pt");

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{tenantA.OrganizationId}/settings/case-defaults",
            new UpdateTenantCaseDefaultsRequest
            {
                DefaultWorkflowKey = "workflow.denied.v1"
            },
            tenantB.UserId,
            tenantB.OrganizationId);
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CompleteOnboarding_ShouldPersistCompletionMarkerAndAudit_WhenDefaultsExist()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "onboarding-admin-4@agency.pt");

        using var defaultsRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/defaults",
            new UpdateTenantOnboardingDefaultsRequest
            {
                DefaultLocale = "pt-PT",
                DefaultTimeZone = "Europe/Lisbon",
                DefaultCurrency = "EUR"
            },
            signup.UserId,
            signup.OrganizationId);
        var defaultsResponse = await client.SendAsync(defaultsRequest);
        defaultsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var completionRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/complete",
            new CompleteTenantOnboardingRequest { ConfirmCompletion = true },
            signup.UserId,
            signup.OrganizationId);
        var completionResponse = await client.SendAsync(completionRequest);
        completionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var organization = await dbContext.Organizations.FindAsync(signup.OrganizationId);
        organization.Should().NotBeNull();
        organization!.OnboardingCompletedAt.Should().NotBeNull();
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantOnboardingDefaultsUpdated");
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantOnboardingCompleted");
    }

    [Fact]
    public async Task ActivateBilling_ShouldSetTenantActiveAndWriteAudit_WhenOnboardingIsComplete()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "onboarding-admin-5@agency.pt");

        using var defaultsRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/defaults",
            new UpdateTenantOnboardingDefaultsRequest
            {
                DefaultLocale = "pt-PT",
                DefaultTimeZone = "Europe/Lisbon",
                DefaultCurrency = "EUR"
            },
            signup.UserId,
            signup.OrganizationId);
        var defaultsResponse = await client.SendAsync(defaultsRequest);
        defaultsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var completionRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/complete",
            new CompleteTenantOnboardingRequest { ConfirmCompletion = true },
            signup.UserId,
            signup.OrganizationId);
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
                PaymentMethodReference = "pm_123",
                InvoiceProfileLegalName = "Agency Lda",
                InvoiceProfileVatNumber = "PT123456789",
                InvoiceProfileBillingEmail = "billing@agency.pt",
                InvoiceProfileCountryCode = "PT"
            },
            signup.UserId,
            signup.OrganizationId);
        var activationResponse = await client.SendAsync(activationRequest);
        activationResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await activationResponse.Content.ReadFromJsonAsync<ApiEnvelope<TenantBillingActivationResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.TenantStatus.Should().Be(Core.Enums.TenantStatus.Active);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var organization = await dbContext.Organizations.FindAsync(signup.OrganizationId);
        organization.Should().NotBeNull();
        organization!.Status.Should().Be(Core.Enums.TenantStatus.Active);
        organization.SubscriptionActivatedAt.Should().NotBeNull();
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantBillingActivated");
    }

    [Fact]
    public async Task FullFlow_ShouldSupportSignupOnboardActivateChangePlanAndCancel()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "flow-admin-1@agency.pt");

        await SetDefaultsAndCompleteOnboardingAsync(client, signup);
        await ActivateBillingAsync(client, signup, "Starter", "Monthly");

        // Preview plan change
        using var previewRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/subscription/preview-change",
            new PreviewPlanChangeRequest { PlanCode = "Growth", BillingCycle = "Monthly" },
            signup.UserId,
            signup.OrganizationId);
        var previewResponse = await client.SendAsync(previewRequest);
        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var previewEnvelope = await previewResponse.Content.ReadFromJsonAsync<ApiEnvelope<PlanChangePreviewResponse>>();
        previewEnvelope.Should().NotBeNull();
        previewEnvelope!.Data.Should().NotBeNull();
        previewEnvelope.Data!.CurrentPlan.Should().Be("STARTER");
        previewEnvelope.Data.NewPlan.Should().Be("GROWTH");

        // Change plan
        using var changeRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/subscription/plan",
            new ChangePlanRequest
            {
                PlanCode = "Growth",
                BillingCycle = "Monthly",
                ConfirmedProrationAmount = previewEnvelope.Data.ProrationAmount
            },
            signup.UserId,
            signup.OrganizationId);
        var changeResponse = await client.SendAsync(changeRequest);
        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var changeEnvelope = await changeResponse.Content.ReadFromJsonAsync<ApiEnvelope<ChangePlanResponse>>();
        changeEnvelope.Should().NotBeNull();
        changeEnvelope!.Data.Should().NotBeNull();
        changeEnvelope.Data!.PlanCode.Should().Be("GROWTH");
        changeEnvelope.Data.PreviousPlan.Should().Be("STARTER");

        // Cancel subscription
        using var cancelRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/subscription/cancel",
            new CancelSubscriptionRequest
            {
                Reason = "We are closing down our agency and no longer need subscription services.",
                Confirmed = true
            },
            signup.UserId,
            signup.OrganizationId);
        var cancelResponse = await client.SendAsync(cancelRequest);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelEnvelope = await cancelResponse.Content.ReadFromJsonAsync<ApiEnvelope<CancelSubscriptionResponse>>();
        cancelEnvelope.Should().NotBeNull();
        cancelEnvelope!.Data.Should().NotBeNull();
        cancelEnvelope.Data!.TenantStatus.Should().Be(Core.Enums.TenantStatus.Suspended);

        // Verify final DB state
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var organization = await dbContext.Organizations.FindAsync(signup.OrganizationId);
        organization.Should().NotBeNull();
        organization!.Status.Should().Be(Core.Enums.TenantStatus.Suspended);
        organization.SubscriptionCancelledAt.Should().NotBeNull();
        organization.PreviousSubscriptionPlan.Should().Be("STARTER");
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantSubscriptionPlanChanged");
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantSubscriptionCancelled");
    }

    [Fact]
    public async Task ChangePlan_ShouldReturn403_WhenActorBelongsToDifferentTenant()
    {
        var client = _factory.CreateClient();
        var tenantA = await CreateTenantAsync(client, "sub-tenant-a@agency.pt");
        var tenantB = await CreateTenantAsync(client, "sub-tenant-b@agency.pt");

        await SetDefaultsAndCompleteOnboardingAsync(client, tenantA);
        await ActivateBillingAsync(client, tenantA, "Starter", "Monthly");

        using var changeRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{tenantA.OrganizationId}/subscription/plan",
            new ChangePlanRequest
            {
                PlanCode = "Growth",
                BillingCycle = "Monthly",
                ConfirmedProrationAmount = 0m
            },
            tenantB.UserId,
            tenantB.OrganizationId);
        var changeResponse = await client.SendAsync(changeRequest);
        changeResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CancelSubscription_ShouldReturn409_WhenTenantIsNotActive()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cancel-pending@agency.pt");

        using var cancelRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/subscription/cancel",
            new CancelSubscriptionRequest
            {
                Reason = "We are closing down our agency and no longer need subscription services.",
                Confirmed = true
            },
            signup.UserId,
            signup.OrganizationId);
        var cancelResponse = await client.SendAsync(cancelRequest);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task BillingEndpoints_ShouldGenerateInvoiceAndReturnHistoryUsageAndSnapshot_WhenTenantIsActive()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "billing-admin-1@agency.pt");

        await SetDefaultsAndCompleteOnboardingAsync(client, signup);
        await ActivateBillingAsync(client, signup, "Starter", "Monthly");

        using var generateInvoiceRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/billing/invoices/generate",
            signup.UserId,
            signup.OrganizationId);
        var generateInvoiceResponse = await client.SendAsync(generateInvoiceRequest);
        generateInvoiceResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var generatedInvoiceEnvelope = await generateInvoiceResponse
            .Content
            .ReadFromJsonAsync<ApiEnvelope<BillingRecordResponse>>();
        generatedInvoiceEnvelope.Should().NotBeNull();
        generatedInvoiceEnvelope!.Data.Should().NotBeNull();
        var generatedInvoice = generatedInvoiceEnvelope.Data!;

        using var historyRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/billing/history",
            signup.UserId,
            signup.OrganizationId);
        var historyResponse = await client.SendAsync(historyRequest);
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var historyEnvelope = await historyResponse.Content.ReadFromJsonAsync<ApiEnvelope<BillingHistoryResponse>>();
        historyEnvelope.Should().NotBeNull();
        historyEnvelope!.Data.Should().NotBeNull();
        historyEnvelope.Data!.TenantId.Should().Be(signup.OrganizationId);
        historyEnvelope.Data.Records.Should().Contain(x => x.Id == generatedInvoice.Id);

        using var usageRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/billing/usage",
            signup.UserId,
            signup.OrganizationId);
        var usageResponse = await client.SendAsync(usageRequest);
        usageResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var usageEnvelope = await usageResponse.Content.ReadFromJsonAsync<ApiEnvelope<BillingUsageSummaryResponse>>();
        usageEnvelope.Should().NotBeNull();
        usageEnvelope!.Data.Should().NotBeNull();
        usageEnvelope.Data!.TenantId.Should().Be(signup.OrganizationId);
        usageEnvelope.Data.PlanCode.Should().Be("STARTER");
        usageEnvelope.Data.BillingCycle.Should().Be("MONTHLY");

        using var invoiceRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/billing/invoices/{generatedInvoice.Id}",
            signup.UserId,
            signup.OrganizationId);
        var invoiceResponse = await client.SendAsync(invoiceRequest);
        invoiceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoiceEnvelope = await invoiceResponse.Content.ReadFromJsonAsync<ApiEnvelope<InvoiceDownloadResponse>>();
        invoiceEnvelope.Should().NotBeNull();
        invoiceEnvelope!.Data.Should().NotBeNull();
        invoiceEnvelope.Data!.TenantId.Should().Be(signup.OrganizationId);
        invoiceEnvelope.Data.InvoiceNumber.Should().Be(generatedInvoice.InvoiceNumber);
        invoiceEnvelope.Data.InvoiceSnapshot.Should().Contain(generatedInvoice.InvoiceNumber);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantBillingRecordCreated");
    }

    [Fact]
    public async Task BillingEndpoints_ShouldReturn403_WhenActorBelongsToDifferentTenant()
    {
        var client = _factory.CreateClient();
        var tenantA = await CreateTenantAsync(client, "billing-tenant-a@agency.pt");
        var tenantB = await CreateTenantAsync(client, "billing-tenant-b@agency.pt");

        await SetDefaultsAndCompleteOnboardingAsync(client, tenantA);
        await ActivateBillingAsync(client, tenantA, "Starter", "Monthly");

        using var generateInvoiceRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{tenantA.OrganizationId}/billing/invoices/generate",
            tenantA.UserId,
            tenantA.OrganizationId);
        var generateInvoiceResponse = await client.SendAsync(generateInvoiceRequest);
        generateInvoiceResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var crossTenantHistoryRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{tenantA.OrganizationId}/billing/history",
            tenantB.UserId,
            tenantB.OrganizationId);
        var crossTenantHistoryResponse = await client.SendAsync(crossTenantHistoryRequest);
        crossTenantHistoryResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GenerateInvoice_ShouldReturn409_WhenTenantIsNotActive()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "billing-pending@agency.pt");

        using var generateInvoiceRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/billing/invoices/generate",
            signup.UserId,
            signup.OrganizationId);
        var generateInvoiceResponse = await client.SendAsync(generateInvoiceRequest);
        generateInvoiceResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PaymentRecoveryEndpoints_ShouldApplyRetryAndReminderSchedule_AndRecoverTenant()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "billing-recovery-1@agency.pt");

        await SetDefaultsAndCompleteOnboardingAsync(client, signup);
        await ActivateBillingAsync(client, signup, "Growth", "Monthly");

        using var failedPaymentRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/billing/payment-failures",
            new RegisterFailedPaymentRequest
            {
                Reason = "Payment provider rejected the automatic monthly collection.",
                PaymentReference = "evt_recovery_001"
            },
            signup.UserId,
            signup.OrganizationId);
        var failedPaymentResponse = await client.SendAsync(failedPaymentRequest);
        failedPaymentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var failedPaymentEnvelope = await failedPaymentResponse
            .Content
            .ReadFromJsonAsync<ApiEnvelope<PaymentRecoveryStatusResponse>>();
        failedPaymentEnvelope.Should().NotBeNull();
        failedPaymentEnvelope!.Data.Should().NotBeNull();
        failedPaymentEnvelope.Data!.TenantStatus.Should().Be(Core.Enums.TenantStatus.PaymentIssue);
        failedPaymentEnvelope.Data.FailedPaymentAttempts.Should().Be(1);
        failedPaymentEnvelope.Data.NextPaymentRetryAt.Should().NotBeNull();
        failedPaymentEnvelope.Data.NextPaymentReminderAt.Should().NotBeNull();

        using var executeRecoveryRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/billing/recovery/execute",
            new ExecutePaymentRecoveryRequest
            {
                RetrySucceeded = true,
                ReminderSent = true
            },
            signup.UserId,
            signup.OrganizationId);
        var executeRecoveryResponse = await client.SendAsync(executeRecoveryRequest);
        executeRecoveryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var executeRecoveryEnvelope = await executeRecoveryResponse
            .Content
            .ReadFromJsonAsync<ApiEnvelope<PaymentRecoveryStatusResponse>>();
        executeRecoveryEnvelope.Should().NotBeNull();
        executeRecoveryEnvelope!.Data.Should().NotBeNull();
        executeRecoveryEnvelope.Data!.TenantStatus.Should().Be(Core.Enums.TenantStatus.Active);
        executeRecoveryEnvelope.Data.RecoveryComplete.Should().BeTrue();
        executeRecoveryEnvelope.Data.FailedPaymentAttempts.Should().Be(0);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantPaymentFailed");
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "TenantPaymentRecovered");
    }

    [Fact]
    public async Task RegisterFailedPayment_ShouldReturn403_WhenActorBelongsToDifferentTenant()
    {
        var client = _factory.CreateClient();
        var tenantA = await CreateTenantAsync(client, "billing-recovery-tenant-a@agency.pt");
        var tenantB = await CreateTenantAsync(client, "billing-recovery-tenant-b@agency.pt");

        await SetDefaultsAndCompleteOnboardingAsync(client, tenantA);
        await ActivateBillingAsync(client, tenantA, "Starter", "Monthly");

        using var failedPaymentRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{tenantA.OrganizationId}/billing/payment-failures",
            new RegisterFailedPaymentRequest
            {
                Reason = "Payment provider declined the subscription charge."
            },
            tenantB.UserId,
            tenantB.OrganizationId);
        var failedPaymentResponse = await client.SendAsync(failedPaymentRequest);
        failedPaymentResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExecutePaymentRecovery_ShouldReturn409_WhenTenantHasNoPaymentIssue()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "billing-recovery-no-issue@agency.pt");

        await SetDefaultsAndCompleteOnboardingAsync(client, signup);
        await ActivateBillingAsync(client, signup, "Starter", "Monthly");

        using var executeRecoveryRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/billing/recovery/execute",
            new ExecutePaymentRecoveryRequest
            {
                RetrySucceeded = true,
                ReminderSent = true
            },
            signup.UserId,
            signup.OrganizationId);
        var executeRecoveryResponse = await client.SendAsync(executeRecoveryRequest);
        executeRecoveryResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task SetDefaultsAndCompleteOnboardingAsync(HttpClient client, CreateAgencyAccountResponse signup)
    {
        using var defaultsMsg = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/defaults",
            new UpdateTenantOnboardingDefaultsRequest
            {
                DefaultLocale = "pt-PT",
                DefaultTimeZone = "Europe/Lisbon",
                DefaultCurrency = "EUR"
            },
            signup.UserId,
            signup.OrganizationId);
        var defaultsResp = await client.SendAsync(defaultsMsg);
        defaultsResp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var completeMsg = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/complete",
            new CompleteTenantOnboardingRequest { ConfirmCompletion = true },
            signup.UserId,
            signup.OrganizationId);
        var completeResp = await client.SendAsync(completeMsg);
        completeResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task ActivateBillingAsync(
        HttpClient client,
        CreateAgencyAccountResponse signup,
        string planCode,
        string billingCycle)
    {
        using var activateMsg = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/billing/activate",
            new ActivateTenantBillingRequest
            {
                PlanCode = planCode,
                BillingCycle = billingCycle,
                PaymentMethodType = "Card",
                PaymentMethodReference = "pm_integration",
                InvoiceProfileLegalName = "Agency Lda",
                InvoiceProfileVatNumber = "PT123456789",
                InvoiceProfileBillingEmail = "billing@agency.pt",
                InvoiceProfileCountryCode = "PT"
            },
            signup.UserId,
            signup.OrganizationId);
        var activateResp = await client.SendAsync(activateMsg);
        activateResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static HttpRequestMessage BuildAuthorizedRequest(
        HttpMethod method,
        string uri,
        Guid userId,
        Guid tenantId)
    {
        var message = new HttpRequestMessage(method, uri);
        message.Headers.Add("X-User-Id", userId.ToString());
        message.Headers.Add("X-Tenant-Id", tenantId.ToString());
        message.Headers.Add("X-User-Role", "AgencyAdmin");
        return message;
    }

    private static HttpRequestMessage BuildAuthorizedJsonRequest(
        HttpMethod method,
        string uri,
        object payload,
        Guid userId,
        Guid tenantId)
    {
        var message = new HttpRequestMessage(method, uri)
        {
            Content = JsonContent.Create(payload)
        };

        message.Headers.Add("X-User-Id", userId.ToString());
        message.Headers.Add("X-Tenant-Id", tenantId.ToString());
        message.Headers.Add("X-User-Role", "AgencyAdmin");
        return message;
    }

    private static async Task<CreateAgencyAccountResponse> CreateTenantAsync(HttpClient client, string email)
    {
        var request = new CreateAgencyAccountRequest
        {
            Email = email,
            FullName = "Agency Admin",
            AgencyName = "Agency",
            Location = "Lisbon"
        };

        var signupResponse = await client.PostAsJsonAsync("/api/v1/tenants/signup", request);
        signupResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await signupResponse.Content.ReadFromJsonAsync<ApiEnvelope<CreateAgencyAccountResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        return envelope.Data!;
    }
}
