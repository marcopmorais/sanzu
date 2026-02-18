using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Tests.Integration.Controllers;

public sealed class CaseExportControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CaseExportControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExportCaseAudit_ShouldReturnExport_WhenCalledByAgencyAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "export-admin@agency.pt");
        await ActivateTenantAsync(client, signup);

        // Create a case
        using var createCaseRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Export Test",
                DateOfDeath = DateTime.UtcNow.AddDays(-5),
                CaseType = "General",
                Urgency = "Normal"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var createResponse = await client.SendAsync(createCaseRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<ApiEnvelope<CreateCaseResponse>>();
        var caseId = createEnvelope!.Data!.CaseId;

        // Export the case
        using var exportRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{caseId}/export",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(exportRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CaseAuditExportResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.CaseId.Should().Be(caseId);
        envelope.Data.TenantId.Should().Be(signup.OrganizationId);
        envelope.Data.ExportId.Should().NotBeNullOrEmpty();
        envelope.Data.CaseSummary.Should().NotBeNull();
        envelope.Data.CaseSummary.DeceasedFullName.Should().Be("Export Test");
        envelope.Data.AuditEvents.Should().NotBeNull();
        envelope.Data.AuditEvents.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportCaseAudit_ShouldReturn403_WhenCalledByNonAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "export-nonadmin@agency.pt");
        await ActivateTenantAsync(client, signup);

        // Create a case as admin
        using var createCaseRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "No Admin Export",
                DateOfDeath = DateTime.UtcNow.AddDays(-3),
                CaseType = "General",
                Urgency = "Normal"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var createResponse = await client.SendAsync(createCaseRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<ApiEnvelope<CreateCaseResponse>>();
        var caseId = createEnvelope!.Data!.CaseId;

        // Try to export as CaseWorker
        using var exportRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{caseId}/export",
            signup.UserId,
            signup.OrganizationId,
            "CaseWorker");

        var response = await client.SendAsync(exportRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExportCaseAudit_ShouldRecordExportGeneratedEvent()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "export-audit@agency.pt");
        await ActivateTenantAsync(client, signup);

        // Create a case
        using var createCaseRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Audit Export Test",
                DateOfDeath = DateTime.UtcNow.AddDays(-2),
                CaseType = "General",
                Urgency = "Normal"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var createResponse = await client.SendAsync(createCaseRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<ApiEnvelope<CreateCaseResponse>>();
        var caseId = createEnvelope!.Data!.CaseId;

        // Export the case
        using var exportRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{caseId}/export",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(exportRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CaseAuditExportResponse>>();
        var exportId = envelope!.Data!.ExportId;

        // The ExportGenerated event was written after fetching events,
        // so we verify it by exporting again and checking it appears in the new export
        using var exportRequest2 = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{caseId}/export",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response2 = await client.SendAsync(exportRequest2);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope2 = await response2.Content.ReadFromJsonAsync<ApiEnvelope<CaseAuditExportResponse>>();
        envelope2!.Data!.AuditEvents.Should().Contain(
            e => e.EventType == "ExportGenerated" && e.Metadata.Contains(exportId));
    }

    [Fact]
    public async Task ExportCaseAudit_ShouldReturn409_WhenCaseNotFound()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "export-notfound@agency.pt");
        await ActivateTenantAsync(client, signup);

        var fakeCaseId = Guid.NewGuid();

        using var exportRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{fakeCaseId}/export",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(exportRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ExportCaseAudit_ShouldIncludeEvidenceReferences_WhenDocumentsExist()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "export-docs@agency.pt");
        await ActivateTenantAsync(client, signup);

        // Create a case
        using var createCaseRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Evidence Export Test",
                DateOfDeath = DateTime.UtcNow.AddDays(-1),
                CaseType = "General",
                Urgency = "Normal"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var createResponse = await client.SendAsync(createCaseRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<ApiEnvelope<CreateCaseResponse>>();
        var caseId = createEnvelope!.Data!.CaseId;

        // Upload a document
        using var uploadRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{caseId}/documents",
            new UploadCaseDocumentRequest
            {
                FileName = "test.pdf",
                ContentType = "application/pdf",
                ContentBase64 = Convert.ToBase64String(new byte[] { 0x25, 0x50, 0x44, 0x46 })
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var uploadResponse = await client.SendAsync(uploadRequest);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Export the case
        using var exportRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{caseId}/export",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(exportRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CaseAuditExportResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.EvidenceReferences.Should().NotBeEmpty();
        envelope.Data.EvidenceReferences.Should().Contain(e => e.FileName == "test.pdf");
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
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var defaultsResponse = await client.SendAsync(defaultsRequest);
        defaultsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var completionRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/onboarding/complete",
            new CompleteTenantOnboardingRequest { ConfirmCompletion = true },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
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
                PaymentMethodReference = "pm_export_tests",
                InvoiceProfileLegalName = "Export Agency Lda",
                InvoiceProfileVatNumber = "PT123456789",
                InvoiceProfileBillingEmail = "billing@export.pt",
                InvoiceProfileCountryCode = "PT"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var activationResponse = await client.SendAsync(activationRequest);
        activationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static HttpRequestMessage BuildAuthorizedJsonRequest(
        HttpMethod method,
        string uri,
        object payload,
        Guid userId,
        Guid tenantId,
        string role)
    {
        var message = new HttpRequestMessage(method, uri)
        {
            Content = JsonContent.Create(payload)
        };

        message.Headers.Add("X-User-Id", userId.ToString());
        message.Headers.Add("X-Tenant-Id", tenantId.ToString());
        message.Headers.Add("X-User-Role", role);
        return message;
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
