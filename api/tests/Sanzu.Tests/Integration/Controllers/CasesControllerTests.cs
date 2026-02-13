using System.Net;
using System.Net.Http.Json;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Tests.Integration.Controllers;

public sealed class CasesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CasesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateCase_ShouldReturn201AndAssignManagerOwnership_WhenMetadataIsValid()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-admin-1@agency.pt");
        await ActivateTenantAsync(client, signup);

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Joao Manuel Pereira",
                DateOfDeath = DateTime.UtcNow.AddDays(-2),
                CaseType = "General",
                Urgency = "High",
                Notes = "Initial contact completed."
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CreateCaseResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Status.Should().Be(CaseStatus.Draft);
        envelope.Data.ManagerUserId.Should().Be(signup.UserId);
        envelope.Data.CaseNumber.Should().Be("CASE-00001");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.Cases.Should().Contain(x => x.Id == envelope.Data.CaseId && x.TenantId == signup.OrganizationId);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "CaseCreated");
    }

    [Fact]
    public async Task CreateCase_ShouldReturn400_WhenPayloadIsInvalid()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-admin-2@agency.pt");
        await ActivateTenantAsync(client, signup);

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = string.Empty,
                DateOfDeath = DateTime.UtcNow.AddDays(1)
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var validation = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        validation.Should().NotBeNull();
        validation!.Errors.Keys.Should().Contain(nameof(CreateCaseRequest.DeceasedFullName));
    }

    [Fact]
    public async Task CreateCase_ShouldReturn403_WhenActorBelongsToDifferentTenant()
    {
        var client = _factory.CreateClient();
        var tenantA = await CreateTenantAsync(client, "cases-tenant-a@agency.pt");
        var tenantB = await CreateTenantAsync(client, "cases-tenant-b@agency.pt");
        await ActivateTenantAsync(client, tenantA);
        await ActivateTenantAsync(client, tenantB);

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{tenantA.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Cross Tenant Attempt",
                DateOfDeath = DateTime.UtcNow.AddDays(-1)
            },
            tenantB.UserId,
            tenantB.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCase_ShouldReturn409_WhenTenantIsNotActive()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-pending@agency.pt");

        using var request = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases",
            new CreateCaseRequest
            {
                DeceasedFullName = "Pending Tenant Case",
                DateOfDeath = DateTime.UtcNow.AddDays(-3)
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateCaseDetails_ShouldReturn200AndPersistChanges_WhenRequestIsValid()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-admin-update@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Original Name");

        using var updateRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}",
            new UpdateCaseDetailsRequest
            {
                DeceasedFullName = "Updated Name",
                CaseType = "Insurance",
                Urgency = "Urgent",
                Notes = "Updated from integration test"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CaseDetailsResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.DeceasedFullName.Should().Be("Updated Name");
        envelope.Data.CaseType.Should().Be("INSURANCE");
        envelope.Data.Urgency.Should().Be("URGENT");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.Cases.Should().Contain(x => x.Id == createdCase.CaseId && x.DeceasedFullName == "Updated Name");
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "CaseDetailsUpdated" && x.CaseId == createdCase.CaseId);
    }

    [Fact]
    public async Task UpdateCaseLifecycle_ShouldReturn409_WhenTransitionIsInvalid()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-admin-invalid-transition@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Transition Case");

        using var lifecycleRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/lifecycle",
            new UpdateCaseLifecycleRequest
            {
                TargetStatus = "Closed",
                Reason = "Attempting invalid jump"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(lifecycleRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateCaseLifecycle_ShouldSetClosedAndArchived_WhenTransitionsAreValid()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-admin-lifecycle@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Lifecycle Case");

        async Task<ApiEnvelope<CaseDetailsResponse>?> TransitionAsync(string targetStatus)
        {
            using var lifecycleRequest = BuildAuthorizedJsonRequest(
                HttpMethod.Patch,
                $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/lifecycle",
                new UpdateCaseLifecycleRequest
                {
                    TargetStatus = targetStatus
                },
                signup.UserId,
                signup.OrganizationId,
                "AgencyAdmin");

            var lifecycleResponse = await client.SendAsync(lifecycleRequest);
            lifecycleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            return await lifecycleResponse.Content.ReadFromJsonAsync<ApiEnvelope<CaseDetailsResponse>>();
        }

        await TransitionAsync("Intake");
        await TransitionAsync("Active");
        await TransitionAsync("Review");
        var closedEnvelope = await TransitionAsync("Closed");
        var archivedEnvelope = await TransitionAsync("Archived");

        closedEnvelope.Should().NotBeNull();
        closedEnvelope!.Data.Should().NotBeNull();
        closedEnvelope.Data!.Status.Should().Be(CaseStatus.Closed);
        closedEnvelope.Data.ClosedAt.Should().NotBeNull();

        archivedEnvelope.Should().NotBeNull();
        archivedEnvelope!.Data.Should().NotBeNull();
        archivedEnvelope.Data!.Status.Should().Be(CaseStatus.Archived);
        archivedEnvelope.Data.ArchivedAt.Should().NotBeNull();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.Cases.Should().Contain(x => x.Id == createdCase.CaseId && x.Status == CaseStatus.Archived && x.ClosedAt.HasValue && x.ArchivedAt.HasValue);
        dbContext.AuditEvents.Count(x => x.EventType == "CaseStatusChanged" && x.CaseId == createdCase.CaseId).Should().Be(5);
    }

    [Fact]
    public async Task GetCaseMilestones_ShouldReturnOrderedCaseHistory_WhenLifecycleEventsExist()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-admin-milestones@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Milestone Case");

        foreach (var status in new[] { "Intake", "Active" })
        {
            using var lifecycleRequest = BuildAuthorizedJsonRequest(
                HttpMethod.Patch,
                $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/lifecycle",
                new UpdateCaseLifecycleRequest { TargetStatus = status },
                signup.UserId,
                signup.OrganizationId,
                "AgencyAdmin");

            var lifecycleResponse = await client.SendAsync(lifecycleRequest);
            lifecycleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var milestonesRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/milestones",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(milestonesRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CaseMilestonesResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.CaseId.Should().Be(createdCase.CaseId);
        envelope.Data.Milestones.Should().HaveCount(3);
        envelope.Data.Milestones.Should().BeInAscendingOrder(x => x.OccurredAt);
        envelope.Data.Milestones.Select(x => x.Status).Should().ContainInOrder(CaseStatus.Draft, CaseStatus.Intake, CaseStatus.Active);
    }

    [Fact]
    public async Task GetTenantComplianceStatus_ShouldReturn200AndCasePolicyStates_WhenActorIsTenantAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-admin-compliance@agency.pt");
        await ActivateTenantAsync(client, signup);

        var activeCase = await CreateCaseAsync(client, signup, "Compliance Active Integration");
        await MoveCaseToActiveAsync(client, signup, activeCase.CaseId);

        var closedCase = await CreateCaseAsync(client, signup, "Compliance Closed Integration");
        await MoveCaseToActiveAsync(client, signup, closedCase.CaseId);
        foreach (var status in new[] { "Review", "Closed" })
        {
            using var lifecycleRequest = BuildAuthorizedJsonRequest(
                HttpMethod.Patch,
                $"/api/v1/tenants/{signup.OrganizationId}/cases/{closedCase.CaseId}/lifecycle",
                new UpdateCaseLifecycleRequest { TargetStatus = status },
                signup.UserId,
                signup.OrganizationId,
                "AgencyAdmin");
            var lifecycleResponse = await client.SendAsync(lifecycleRequest);
            lifecycleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
            var persistedClosedCase = await dbContext.Cases.SingleAsync(x => x.Id == closedCase.CaseId);
            persistedClosedCase.ClosedAt = DateTime.UtcNow.AddDays(-120);
            await dbContext.SaveChangesAsync();
        }

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/compliance",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<TenantComplianceStatusResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.TenantId.Should().Be(signup.OrganizationId);
        envelope.Data.Cases.Should().Contain(x => x.CaseId == activeCase.CaseId && x.CaseStatus == CaseStatus.Active);
        envelope.Data.Cases.Should().Contain(
            x => x.CaseId == closedCase.CaseId
                 && x.CaseStatus == CaseStatus.Closed
                 && x.Exceptions.Contains("RETENTION_REVIEW_REQUIRED"));
    }

    [Fact]
    public async Task GetTenantComplianceStatus_ShouldReturn403_WhenActorIsNotTenantAdmin()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-compliance-reader@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Compliance Reader Integration");
        await MoveCaseToActiveAsync(client, signup, createdCase.CaseId);
        var readerUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.compliance.reader@agency.pt",
            CaseRole.Reader);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/compliance",
            readerUserId,
            signup.OrganizationId,
            "Reader");
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCaseAuditTrail_ShouldReturn200AndEntries_WhenUserIsManager()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-admin-audit@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Audit Integration Case");

        using var preScope = _factory.Services.CreateScope();
        var preDbContext = preScope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var beforeCount = preDbContext.AuditEvents.Count(x => x.CaseId == createdCase.CaseId);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/audit",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CaseAuditTrailResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.CaseId.Should().Be(createdCase.CaseId);
        envelope.Data.Entries.Should().NotBeEmpty();
        envelope.Data.Entries[0].Action.Should().NotBeNullOrWhiteSpace();
        envelope.Data.Entries[0].ContextJson.Should().NotBeNullOrWhiteSpace();

        using var postScope = _factory.Services.CreateScope();
        var postDbContext = postScope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var afterCount = postDbContext.AuditEvents.Count(x => x.CaseId == createdCase.CaseId);
        afterCount.Should().Be(beforeCount);
    }

    [Fact]
    public async Task GetCaseAuditTrail_ShouldReturn403_WhenUserIsReader()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-audit-reader@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Audit Reader Integration Case");
        var readerUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.audit.reader@agency.pt",
            CaseRole.Reader);

        using var request = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/audit",
            readerUserId,
            signup.OrganizationId,
            "Reader");
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SubmitCaseIntake_ShouldReturn200AndPersistIntake_WhenUserIsEditor()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-intake-editor@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Intake Editor Case");
        var editorUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.intake.editor@agency.pt",
            CaseRole.Editor);

        using var intakeRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Put,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/intake",
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = "Ana Pereira",
                PrimaryContactPhone = "+351919999999",
                RelationshipToDeceased = "Daughter",
                HasWill = true,
                RequiresLegalSupport = true,
                ConfirmAccuracy = true,
                Notes = "Structured intake completed by editor."
            },
            editorUserId,
            signup.OrganizationId,
            "Editor");

        var response = await client.SendAsync(intakeRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CaseDetailsResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Status.Should().Be(CaseStatus.Intake);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.Cases.Should().Contain(
            x => x.Id == createdCase.CaseId
                 && x.Status == CaseStatus.Intake
                 && x.IntakeCompletedByUserId == editorUserId
                 && x.IntakeCompletedAt.HasValue
                 && !string.IsNullOrWhiteSpace(x.IntakeData));
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "CaseIntakeSubmitted" && x.CaseId == createdCase.CaseId && x.ActorUserId == editorUserId);
    }

    [Fact]
    public async Task SubmitCaseIntake_ShouldReturn403_WhenUserIsReader()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-intake-reader@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Intake Reader Case");
        var readerUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.intake.reader@agency.pt",
            CaseRole.Reader);

        using var intakeRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Put,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/intake",
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = "Reader Attempt",
                PrimaryContactPhone = "+351918888888",
                RelationshipToDeceased = "Sibling",
                ConfirmAccuracy = true
            },
            readerUserId,
            signup.OrganizationId,
            "Reader");

        var response = await client.SendAsync(intakeRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "CaseAccessDenied" && x.CaseId == createdCase.CaseId && x.ActorUserId == readerUserId);
    }

    [Fact]
    public async Task SubmitCaseIntake_ShouldReturn400_WhenRequestIsInvalid()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-intake-invalid@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Invalid Intake Case");

        using var intakeRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Put,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/intake",
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = string.Empty,
                PrimaryContactPhone = string.Empty,
                RelationshipToDeceased = string.Empty,
                ConfirmAccuracy = false
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(intakeRequest);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var validation = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        validation.Should().NotBeNull();
        validation!.Errors.Keys.Should().Contain(nameof(SubmitCaseIntakeRequest.PrimaryContactName));
        validation.Errors.Keys.Should().Contain(nameof(SubmitCaseIntakeRequest.ConfirmAccuracy));
    }

    [Fact]
    public async Task GenerateCasePlan_ShouldReturn200AndPersistPlan_WhenManagerRunsAfterIntake()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-plan-generate@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Generate Plan Case");

        using (var intakeRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Put,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/intake",
                   new SubmitCaseIntakeRequest
                   {
                       PrimaryContactName = "Ana Pereira",
                       PrimaryContactPhone = "+351910000000",
                       RelationshipToDeceased = "Daughter",
                       HasWill = true,
                       RequiresLegalSupport = true,
                       ConfirmAccuracy = true
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var intakeResponse = await client.SendAsync(intakeRequest);
            intakeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var generateRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/plan/generate",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(generateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<GenerateCasePlanResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.CaseId.Should().Be(createdCase.CaseId);
        envelope.Data.Steps.Should().HaveCount(5);
        envelope.Data.Steps.Should().Contain(x => x.StepKey == "collect-civil-records" && x.Status == WorkflowStepStatus.Ready);
        envelope.Data.Steps.Should().Contain(x => x.StepKey == "submit-succession-notification" && x.Status == WorkflowStepStatus.Blocked);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.WorkflowStepInstances.Count(x => x.CaseId == createdCase.CaseId).Should().Be(5);
        dbContext.WorkflowStepDependencies.Count(x => x.CaseId == createdCase.CaseId).Should().Be(4);
        dbContext.Cases.Should().Contain(x => x.Id == createdCase.CaseId && x.Status == CaseStatus.Active);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "CasePlanGenerated" && x.CaseId == createdCase.CaseId);
    }

    [Fact]
    public async Task GenerateCasePlan_ShouldReturn409_WhenIntakeIsNotCompleted()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-plan-no-intake@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Generate Plan Without Intake Case");

        using var generateRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/plan/generate",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(generateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GenerateCasePlan_ShouldReturn403_WhenUserIsEditor()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-plan-editor@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Generate Plan Editor Case");
        var editorUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.plan.editor@agency.pt",
            CaseRole.Editor);

        using (var intakeRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Put,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/intake",
                   new SubmitCaseIntakeRequest
                   {
                       PrimaryContactName = "Ana Pereira",
                       PrimaryContactPhone = "+351910000000",
                       RelationshipToDeceased = "Daughter",
                       ConfirmAccuracy = true
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var intakeResponse = await client.SendAsync(intakeRequest);
            intakeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var generateRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/plan/generate",
            editorUserId,
            signup.OrganizationId,
            "Editor");

        var response = await client.SendAsync(generateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RecalculateCasePlanReadiness_ShouldReturn200AndUpdateReadiness_WhenPrerequisitesAreCompleted()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-plan-recalculate@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Recalculate Readiness Case");

        using (var intakeRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Put,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/intake",
                   new SubmitCaseIntakeRequest
                   {
                       PrimaryContactName = "Ana Pereira",
                       PrimaryContactPhone = "+351910000000",
                       RelationshipToDeceased = "Daughter",
                       HasWill = true,
                       RequiresLegalSupport = true,
                       ConfirmAccuracy = true
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var intakeResponse = await client.SendAsync(intakeRequest);
            intakeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var generateRequest = BuildAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/plan/generate",
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var generateResponse = await client.SendAsync(generateRequest);
            generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
            var recordsStep = dbContext.WorkflowStepInstances
                .Single(x => x.CaseId == createdCase.CaseId && x.StepKey == "collect-civil-records");
            var inventoryStep = dbContext.WorkflowStepInstances
                .Single(x => x.CaseId == createdCase.CaseId && x.StepKey == "gather-estate-inventory");
            var nowUtc = DateTime.UtcNow;
            recordsStep.Status = WorkflowStepStatus.Complete;
            recordsStep.UpdatedAt = nowUtc;
            inventoryStep.Status = WorkflowStepStatus.Complete;
            inventoryStep.UpdatedAt = nowUtc;
            await dbContext.SaveChangesAsync();
        }

        using var recalculateRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/plan/readiness/recalculate",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(recalculateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<GenerateCasePlanResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Steps.Should().Contain(
            x => x.StepKey == "submit-succession-notification" && x.Status == WorkflowStepStatus.Ready);
        envelope.Data.Steps.Should().Contain(
            x => x.StepKey == "validate-will" && x.Status == WorkflowStepStatus.Ready);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        verificationContext.AuditEvents.Should().Contain(
            x => x.EventType == "CasePlanReadinessRecalculated" && x.CaseId == createdCase.CaseId);
    }

    [Fact]
    public async Task OverrideWorkflowStepReadiness_ShouldReturn200AndPersistOverride_WhenManagerRequests()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-plan-override-manager@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Override Readiness Case");

        using (var intakeRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Put,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/intake",
                   new SubmitCaseIntakeRequest
                   {
                       PrimaryContactName = "Ana Pereira",
                       PrimaryContactPhone = "+351910000000",
                       RelationshipToDeceased = "Daughter",
                       ConfirmAccuracy = true
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var intakeResponse = await client.SendAsync(intakeRequest);
            intakeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var generateRequest = BuildAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/plan/generate",
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var generateResponse = await client.SendAsync(generateRequest);
            generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        Guid targetStepId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
            targetStepId = dbContext.WorkflowStepInstances
                .Single(x => x.CaseId == createdCase.CaseId && x.StepKey == "submit-succession-notification")
                .Id;
        }

        using var overrideRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/plan/steps/{targetStepId}/readiness-override",
            new OverrideWorkflowStepReadinessRequest
            {
                TargetStatus = "Ready",
                Rationale = "Manual unblock approved by manager."
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(overrideRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<GenerateCasePlanResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Steps.Should().Contain(
            x => x.StepId == targetStepId && x.Status == WorkflowStepStatus.Ready);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var persistedStep = verificationContext.WorkflowStepInstances.Single(x => x.Id == targetStepId);
        persistedStep.IsReadinessOverridden.Should().BeTrue();
        persistedStep.ReadinessOverrideRationale.Should().Be("Manual unblock approved by manager.");
        persistedStep.ReadinessOverrideByUserId.Should().Be(signup.UserId);
        persistedStep.ReadinessOverriddenAt.Should().NotBeNull();
        verificationContext.AuditEvents.Should().Contain(
            x => x.EventType == "CasePlanReadinessOverridden" && x.CaseId == createdCase.CaseId);
    }

    [Fact]
    public async Task OverrideWorkflowStepReadiness_ShouldReturn403_WhenUserIsEditor()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-plan-override-editor@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Override Readiness Editor Case");
        var editorUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.override.editor@agency.pt",
            CaseRole.Editor);

        using (var intakeRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Put,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/intake",
                   new SubmitCaseIntakeRequest
                   {
                       PrimaryContactName = "Ana Pereira",
                       PrimaryContactPhone = "+351910000000",
                       RelationshipToDeceased = "Daughter",
                       ConfirmAccuracy = true
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var intakeResponse = await client.SendAsync(intakeRequest);
            intakeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var generateRequest = BuildAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/plan/generate",
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var generateResponse = await client.SendAsync(generateRequest);
            generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        Guid targetStepId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
            targetStepId = dbContext.WorkflowStepInstances
                .Single(x => x.CaseId == createdCase.CaseId && x.StepKey == "submit-succession-notification")
                .Id;
        }

        using var overrideRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/plan/steps/{targetStepId}/readiness-override",
            new OverrideWorkflowStepReadinessRequest
            {
                TargetStatus = "Ready",
                Rationale = "Attempted by editor."
            },
            editorUserId,
            signup.OrganizationId,
            "Editor");

        var response = await client.SendAsync(overrideRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCaseTaskWorkspace_ShouldReturn200AndPrioritySortedTasks_WhenUserIsEditor()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-tasks-workspace@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Task Workspace Integration Case");
        var editorUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.tasks.editor@agency.pt",
            CaseRole.Editor);

        using (var intakeRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Put,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/intake",
                   new SubmitCaseIntakeRequest
                   {
                       PrimaryContactName = "Ana Pereira",
                       PrimaryContactPhone = "+351910000000",
                       RelationshipToDeceased = "Daughter",
                       ConfirmAccuracy = true
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var intakeResponse = await client.SendAsync(intakeRequest);
            intakeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var generateRequest = BuildAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/plan/generate",
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var generateResponse = await client.SendAsync(generateRequest);
            generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
            var step = dbContext.WorkflowStepInstances
                .Single(x => x.CaseId == createdCase.CaseId && x.StepKey == "collect-civil-records");
            step.Status = WorkflowStepStatus.InProgress;
            step.DueDate = DateTime.UtcNow.AddDays(2);
            step.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        using var workspaceRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/tasks",
            editorUserId,
            signup.OrganizationId,
            "Editor");

        var response = await client.SendAsync(workspaceRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CaseTaskWorkspaceResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Tasks.Should().NotBeEmpty();
        envelope.Data.Tasks[0].StepKey.Should().Be("collect-civil-records");
        envelope.Data.Tasks[0].PriorityRank.Should().Be(1);
    }

    [Fact]
    public async Task UpdateWorkflowTaskStatus_ShouldReturn200AndPersistStatus_WhenEditorStartsTask()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-tasks-update@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Task Update Integration Case");
        var editorUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.tasks.update.editor@agency.pt",
            CaseRole.Editor);

        using (var intakeRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Put,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/intake",
                   new SubmitCaseIntakeRequest
                   {
                       PrimaryContactName = "Ana Pereira",
                       PrimaryContactPhone = "+351910000000",
                       RelationshipToDeceased = "Daughter",
                       ConfirmAccuracy = true
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var intakeResponse = await client.SendAsync(intakeRequest);
            intakeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var generateRequest = BuildAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/plan/generate",
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var generateResponse = await client.SendAsync(generateRequest);
            generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        Guid targetStepId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
            targetStepId = dbContext.WorkflowStepInstances
                .Single(x => x.CaseId == createdCase.CaseId && x.StepKey == "collect-civil-records")
                .Id;
        }

        using var updateRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/tasks/{targetStepId}/status",
            new UpdateWorkflowTaskStatusRequest
            {
                TargetStatus = "Started",
                Notes = "Started by editor"
            },
            editorUserId,
            signup.OrganizationId,
            "Editor");

        var response = await client.SendAsync(updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        verificationContext.WorkflowStepInstances.Should().Contain(
            x => x.Id == targetStepId && x.Status == WorkflowStepStatus.InProgress);
        verificationContext.AuditEvents.Should().Contain(
            x => x.EventType == "WorkflowTaskStatusUpdated" && x.CaseId == createdCase.CaseId && x.ActorUserId == editorUserId);
        verificationContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseNotificationQueued" && x.CaseId == createdCase.CaseId && x.ActorUserId == editorUserId);
    }

    [Fact]
    public async Task UpdateWorkflowTaskStatus_ShouldReturn403_WhenUserIsReader()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-tasks-reader-forbidden@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Task Update Reader Case");
        var readerUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.tasks.reader@agency.pt",
            CaseRole.Reader);

        using (var intakeRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Put,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/intake",
                   new SubmitCaseIntakeRequest
                   {
                       PrimaryContactName = "Ana Pereira",
                       PrimaryContactPhone = "+351910000000",
                       RelationshipToDeceased = "Daughter",
                       ConfirmAccuracy = true
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var intakeResponse = await client.SendAsync(intakeRequest);
            intakeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var generateRequest = BuildAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/plan/generate",
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var generateResponse = await client.SendAsync(generateRequest);
            generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        Guid targetStepId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
            targetStepId = dbContext.WorkflowStepInstances
                .Single(x => x.CaseId == createdCase.CaseId && x.StepKey == "collect-civil-records")
                .Id;
        }

        using var updateRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/tasks/{targetStepId}/status",
            new UpdateWorkflowTaskStatusRequest
            {
                TargetStatus = "Started"
            },
            readerUserId,
            signup.OrganizationId,
            "Reader");

        var response = await client.SendAsync(updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCaseTimeline_ShouldReturn200WithOrderedEventsAndCurrentOwners()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-timeline@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Timeline Integration Case");

        using (var intakeRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Put,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/intake",
                   new SubmitCaseIntakeRequest
                   {
                       PrimaryContactName = "Ana Pereira",
                       PrimaryContactPhone = "+351910000000",
                       RelationshipToDeceased = "Daughter",
                       ConfirmAccuracy = true
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var intakeResponse = await client.SendAsync(intakeRequest);
            intakeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var generateRequest = BuildAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/plan/generate",
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var generateResponse = await client.SendAsync(generateRequest);
            generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        Guid targetStepId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
            targetStepId = dbContext.WorkflowStepInstances
                .Single(x => x.CaseId == createdCase.CaseId && x.StepKey == "collect-civil-records")
                .Id;
        }

        using (var updateRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Patch,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/tasks/{targetStepId}/status",
                   new UpdateWorkflowTaskStatusRequest { TargetStatus = "Started" },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var updateResponse = await client.SendAsync(updateRequest);
            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var timelineRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/timeline",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(timelineRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CaseTimelineResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.CurrentOwners.Should().NotBeEmpty();
        envelope.Data.CurrentOwners.Should().Contain(x => x.AssignedUserId == signup.UserId);
        envelope.Data.Events.Should().NotBeEmpty();
        envelope.Data.Events.Should().BeInAscendingOrder(x => x.OccurredAt);
        envelope.Data.Events.Should().Contain(x => x.EventType == "WorkflowTaskStatusUpdated");
        envelope.Data.Events.Should().Contain(x => x.EventType == "CaseNotificationQueued");
    }

    [Fact]
    public async Task UploadCaseDocument_ShouldReturn201AndPersistDocument_WhenUserIsEditor()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-doc-upload-editor@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Document Upload Integration Case");
        var editorUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.docs.editor@agency.pt",
            CaseRole.Editor);

        using var uploadRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents",
            new UploadCaseDocumentRequest
            {
                FileName = "will.pdf",
                ContentType = "application/pdf",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("mock-pdf-content"))
            },
            editorUserId,
            signup.OrganizationId,
            "Editor");

        var response = await client.SendAsync(uploadRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CaseDocumentUploadResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.CaseId.Should().Be(createdCase.CaseId);
        envelope.Data.FileName.Should().Be("will.pdf");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.CaseDocuments.Should().Contain(x => x.Id == envelope.Data.DocumentId && x.CaseId == createdCase.CaseId);
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseDocumentUploaded" && x.CaseId == createdCase.CaseId && x.ActorUserId == editorUserId);
    }

    [Fact]
    public async Task DownloadCaseDocument_ShouldReturn200AndDocumentContent_WhenUserIsReader()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-doc-download-reader@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Document Download Integration Case");
        var readerUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.docs.reader@agency.pt",
            CaseRole.Reader);

        Guid documentId;
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("downloadable-content"));
        using (var uploadRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents",
                   new UploadCaseDocumentRequest
                   {
                       FileName = "statement.txt",
                       ContentType = "text/plain",
                       ContentBase64 = payload
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var uploadResponse = await client.SendAsync(uploadRequest);
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var uploadEnvelope = await uploadResponse.Content.ReadFromJsonAsync<ApiEnvelope<CaseDocumentUploadResponse>>();
            uploadEnvelope.Should().NotBeNull();
            uploadEnvelope!.Data.Should().NotBeNull();
            documentId = uploadEnvelope.Data!.DocumentId;
        }

        using var downloadRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/{documentId}",
            readerUserId,
            signup.OrganizationId,
            "Reader");

        var response = await client.SendAsync(downloadRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CaseDocumentDownloadResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.DocumentId.Should().Be(documentId);
        envelope.Data.ContentType.Should().Be("text/plain");
        envelope.Data.ContentBase64.Should().Be(payload);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseDocumentDownloaded" && x.CaseId == createdCase.CaseId && x.ActorUserId == readerUserId);
    }

    [Fact]
    public async Task UploadCaseDocument_ShouldReturn403_WhenUserIsReader()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-doc-upload-reader@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Document Upload Forbidden Case");
        var readerUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.docs.upload.reader@agency.pt",
            CaseRole.Reader);

        using var uploadRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents",
            new UploadCaseDocumentRequest
            {
                FileName = "will.pdf",
                ContentType = "application/pdf",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("forbidden"))
            },
            readerUserId,
            signup.OrganizationId,
            "Reader");

        var response = await client.SendAsync(uploadRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UploadCaseDocumentVersion_ShouldReturn201AndIncrementVersion_WhenUserIsEditor()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-doc-version-editor@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Document Version Integration Case");
        var editorUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.docs.version.editor@agency.pt",
            CaseRole.Editor);

        Guid documentId;
        using (var uploadRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents",
                   new UploadCaseDocumentRequest
                   {
                       FileName = "statement.txt",
                       ContentType = "text/plain",
                       ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("v1"))
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var uploadResponse = await client.SendAsync(uploadRequest);
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var uploadEnvelope = await uploadResponse.Content.ReadFromJsonAsync<ApiEnvelope<CaseDocumentUploadResponse>>();
            uploadEnvelope.Should().NotBeNull();
            uploadEnvelope!.Data.Should().NotBeNull();
            documentId = uploadEnvelope.Data!.DocumentId;
            uploadEnvelope.Data.VersionNumber.Should().Be(1);
        }

        using var versionRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/{documentId}/versions",
            new UploadCaseDocumentRequest
            {
                FileName = "statement-v2.txt",
                ContentType = "text/plain",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("v2"))
            },
            editorUserId,
            signup.OrganizationId,
            "Editor");

        var response = await client.SendAsync(versionRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CaseDocumentUploadResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.DocumentId.Should().Be(documentId);
        envelope.Data.VersionNumber.Should().Be(2);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.CaseDocumentVersions.Count(x => x.DocumentId == documentId).Should().Be(2);
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseDocumentVersionUploaded" && x.CaseId == createdCase.CaseId && x.ActorUserId == editorUserId);
    }

    [Fact]
    public async Task GetCaseDocumentVersions_ShouldReturn200AndVersionHistory()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-doc-version-history@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Document Version History Integration Case");

        Guid documentId;
        using (var uploadRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents",
                   new UploadCaseDocumentRequest
                   {
                       FileName = "declaration.txt",
                       ContentType = "text/plain",
                       ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("v1"))
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var uploadResponse = await client.SendAsync(uploadRequest);
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var uploadEnvelope = await uploadResponse.Content.ReadFromJsonAsync<ApiEnvelope<CaseDocumentUploadResponse>>();
            uploadEnvelope.Should().NotBeNull();
            uploadEnvelope!.Data.Should().NotBeNull();
            documentId = uploadEnvelope.Data!.DocumentId;
        }

        using (var v2Request = BuildAuthorizedJsonRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/{documentId}/versions",
                   new UploadCaseDocumentRequest
                   {
                       FileName = "declaration-v2.txt",
                       ContentType = "text/plain",
                       ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("v2"))
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var v2Response = await client.SendAsync(v2Request);
            v2Response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using var historyRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/{documentId}/versions",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(historyRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CaseDocumentVersionHistoryResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.DocumentId.Should().Be(documentId);
        envelope.Data.LatestVersionNumber.Should().Be(2);
        envelope.Data.Versions.Select(x => x.VersionNumber).Should().Equal(1, 2);
    }

    [Fact]
    public async Task UpdateCaseDocumentClassification_ShouldReturn200_WhenUserIsManager()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-doc-classification-manager@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Document Classification Integration Case");

        Guid documentId;
        using (var uploadRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents",
                   new UploadCaseDocumentRequest
                   {
                       FileName = "classification.pdf",
                       ContentType = "application/pdf",
                       ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("content"))
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var uploadResponse = await client.SendAsync(uploadRequest);
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var uploadEnvelope = await uploadResponse.Content.ReadFromJsonAsync<ApiEnvelope<CaseDocumentUploadResponse>>();
            uploadEnvelope.Should().NotBeNull();
            uploadEnvelope!.Data.Should().NotBeNull();
            documentId = uploadEnvelope.Data!.DocumentId;
        }

        using var classificationRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/{documentId}/classification",
            new UpdateCaseDocumentClassificationRequest { Classification = "Restricted" },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(classificationRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CaseDocumentClassificationResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.DocumentId.Should().Be(documentId);
        envelope.Data.Classification.Should().Be("Restricted");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseDocumentClassificationUpdated" && x.CaseId == createdCase.CaseId && x.ActorUserId == signup.UserId);
    }

    [Fact]
    public async Task DownloadCaseDocument_ShouldReturn403_WhenDocumentIsRestrictedAndUserIsReader()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-doc-restricted-reader@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Document Restricted Download Integration Case");
        var readerUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.docs.restricted.reader@agency.pt",
            CaseRole.Reader);

        Guid documentId;
        using (var uploadRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents",
                   new UploadCaseDocumentRequest
                   {
                       FileName = "restricted.txt",
                       ContentType = "text/plain",
                       ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("restricted"))
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var uploadResponse = await client.SendAsync(uploadRequest);
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var uploadEnvelope = await uploadResponse.Content.ReadFromJsonAsync<ApiEnvelope<CaseDocumentUploadResponse>>();
            uploadEnvelope.Should().NotBeNull();
            uploadEnvelope!.Data.Should().NotBeNull();
            documentId = uploadEnvelope.Data!.DocumentId;
        }

        using (var classificationRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Patch,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/{documentId}/classification",
                   new UpdateCaseDocumentClassificationRequest { Classification = "Restricted" },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var classificationResponse = await client.SendAsync(classificationRequest);
            classificationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var downloadRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/{documentId}",
            readerUserId,
            signup.OrganizationId,
            "Reader");

        var response = await client.SendAsync(downloadRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GenerateOutboundTemplate_ShouldReturn200AndMappedContent_WhenIntakeIsCompleted()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-template-success@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Template Integration Case");

        using (var intakeRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Put,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/intake",
                   new SubmitCaseIntakeRequest
                   {
                       PrimaryContactName = "Ana Pereira",
                       PrimaryContactPhone = "+351910000000",
                       RelationshipToDeceased = "Daughter",
                       HasWill = true,
                       RequiresLegalSupport = false,
                       RequiresFinancialSupport = true,
                       ConfirmAccuracy = true
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var intakeResponse = await client.SendAsync(intakeRequest);
            intakeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var templateRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/templates/generate",
            new GenerateOutboundTemplateRequest { TemplateKey = "CaseSummaryLetter" },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(templateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<GenerateOutboundTemplateResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.CaseId.Should().Be(createdCase.CaseId);
        envelope.Data.TemplateKey.Should().Be("CaseSummaryLetter");
        var content = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(envelope.Data.ContentBase64));
        content.Should().Contain("Template: CaseSummaryLetter");
        content.Should().Contain("DeceasedFullName: Template Integration Case");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseOutboundTemplateGenerated" && x.CaseId == createdCase.CaseId && x.ActorUserId == signup.UserId);
    }

    [Fact]
    public async Task GenerateOutboundTemplate_ShouldReturn409_WhenIntakeIsMissing()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-template-no-intake@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Template Without Intake Integration Case");

        using var templateRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/templates/generate",
            new GenerateOutboundTemplateRequest { TemplateKey = "CaseSummaryLetter" },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(templateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GenerateOutboundTemplate_ShouldReturn403_WhenUserIsEditor()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-template-editor-forbidden@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Template Editor Forbidden Integration Case");
        var editorUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.template.editor@agency.pt",
            CaseRole.Editor);

        using var templateRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/templates/generate",
            new GenerateOutboundTemplateRequest { TemplateKey = "CaseSummaryLetter" },
            editorUserId,
            signup.OrganizationId,
            "Editor");

        var response = await client.SendAsync(templateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExtractDocumentCandidates_ShouldReturn200AndPendingCandidates_WhenDocumentIsSupported()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-extraction-success@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Extraction Integration Case");

        Guid documentId;
        var content = string.Join(
            Environment.NewLine,
            "PrimaryContactName: Ana Pereira",
            "PrimaryContactPhone: +351910000000",
            "RelationshipToDeceased: Daughter");
        using (var uploadRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents",
                   new UploadCaseDocumentRequest
                   {
                       FileName = "intake.txt",
                       ContentType = "text/plain",
                       ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content))
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var uploadResponse = await client.SendAsync(uploadRequest);
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var uploadEnvelope = await uploadResponse.Content.ReadFromJsonAsync<ApiEnvelope<CaseDocumentUploadResponse>>();
            uploadEnvelope.Should().NotBeNull();
            uploadEnvelope!.Data.Should().NotBeNull();
            documentId = uploadEnvelope.Data!.DocumentId;
        }

        using var extractionRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/{documentId}/extraction/candidates",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(extractionRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<ExtractDocumentCandidatesResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.DocumentId.Should().Be(documentId);
        envelope.Data.Candidates.Should().NotBeEmpty();
        envelope.Data.Candidates.Should().OnlyContain(x => x.Status == "Pending");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseDocumentExtractionCompleted" && x.CaseId == createdCase.CaseId && x.ActorUserId == signup.UserId);
    }

    [Fact]
    public async Task ExtractDocumentCandidates_ShouldReturn409_WhenContentTypeIsUnsupported()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-extraction-unsupported@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Extraction Unsupported Integration Case");

        Guid documentId;
        using (var uploadRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents",
                   new UploadCaseDocumentRequest
                   {
                       FileName = "scan.pdf",
                       ContentType = "application/pdf",
                       ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("binary"))
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var uploadResponse = await client.SendAsync(uploadRequest);
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var uploadEnvelope = await uploadResponse.Content.ReadFromJsonAsync<ApiEnvelope<CaseDocumentUploadResponse>>();
            uploadEnvelope.Should().NotBeNull();
            uploadEnvelope!.Data.Should().NotBeNull();
            documentId = uploadEnvelope.Data!.DocumentId;
        }

        using var extractionRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/{documentId}/extraction/candidates",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(extractionRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ExtractDocumentCandidates_ShouldReturn403_WhenDocumentIsRestrictedAndUserIsEditor()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-extraction-restricted-editor@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Extraction Restricted Integration Case");
        var editorUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.extraction.editor@agency.pt",
            CaseRole.Editor);

        Guid documentId;
        using (var uploadRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents",
                   new UploadCaseDocumentRequest
                   {
                       FileName = "restricted.txt",
                       ContentType = "text/plain",
                       ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("PrimaryContactName: Ana"))
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var uploadResponse = await client.SendAsync(uploadRequest);
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var uploadEnvelope = await uploadResponse.Content.ReadFromJsonAsync<ApiEnvelope<CaseDocumentUploadResponse>>();
            uploadEnvelope.Should().NotBeNull();
            uploadEnvelope!.Data.Should().NotBeNull();
            documentId = uploadEnvelope.Data!.DocumentId;
        }

        using (var classificationRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Patch,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/{documentId}/classification",
                   new UpdateCaseDocumentClassificationRequest { Classification = "Restricted" },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var classificationResponse = await client.SendAsync(classificationRequest);
            classificationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var extractionRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/{documentId}/extraction/candidates",
            editorUserId,
            signup.OrganizationId,
            "Editor");

        var response = await client.SendAsync(extractionRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ApplyExtractionDecisions_ShouldReturn200AndApplyOnlyApprovedValues()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-extraction-review-success@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Extraction Review Integration Case");

        Guid documentId;
        using (var uploadRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents",
                   new UploadCaseDocumentRequest
                   {
                       FileName = "review.txt",
                       ContentType = "text/plain",
                       ContentBase64 = Convert.ToBase64String(
                           System.Text.Encoding.UTF8.GetBytes(
                               "PrimaryContactName: Ana Pereira\nPrimaryContactPhone: +351910000000\nRelationshipToDeceased: Daughter"))
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var uploadResponse = await client.SendAsync(uploadRequest);
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var uploadEnvelope = await uploadResponse.Content.ReadFromJsonAsync<ApiEnvelope<CaseDocumentUploadResponse>>();
            uploadEnvelope.Should().NotBeNull();
            uploadEnvelope!.Data.Should().NotBeNull();
            documentId = uploadEnvelope.Data!.DocumentId;
        }

        IReadOnlyList<ExtractionCandidateResponse> extractedCandidates;
        using (var extractionRequest = BuildAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/{documentId}/extraction/candidates",
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var extractionResponse = await client.SendAsync(extractionRequest);
            extractionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var extractionEnvelope = await extractionResponse.Content.ReadFromJsonAsync<ApiEnvelope<ExtractDocumentCandidatesResponse>>();
            extractionEnvelope.Should().NotBeNull();
            extractionEnvelope!.Data.Should().NotBeNull();
            extractedCandidates = extractionEnvelope.Data!.Candidates;
        }

        var nameCandidate = extractedCandidates.Single(x => x.FieldKey == "PrimaryContactName");
        var phoneCandidate = extractedCandidates.Single(x => x.FieldKey == "PrimaryContactPhone");
        var relationshipCandidate = extractedCandidates.Single(x => x.FieldKey == "RelationshipToDeceased");

        using var reviewRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/{documentId}/extraction/review",
            new ApplyExtractionDecisionsRequest
            {
                Decisions =
                [
                    new ExtractionDecisionRequest { CandidateId = nameCandidate.CandidateId, Action = "Approve" },
                    new ExtractionDecisionRequest { CandidateId = phoneCandidate.CandidateId, Action = "Edit", EditedValue = "+351911111111" },
                    new ExtractionDecisionRequest { CandidateId = relationshipCandidate.CandidateId, Action = "Reject" }
                ]
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(reviewRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<ApplyExtractionDecisionsResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.AppliedCount.Should().Be(2);
        envelope.Data.RejectedCount.Should().Be(1);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.ExtractionCandidates.Should().Contain(x => x.Id == nameCandidate.CandidateId && x.Status == ExtractionCandidateStatus.Approved);
        dbContext.ExtractionCandidates.Should().Contain(x => x.Id == phoneCandidate.CandidateId && x.Status == ExtractionCandidateStatus.Approved && x.CandidateValue == "+351911111111");
        dbContext.ExtractionCandidates.Should().Contain(x => x.Id == relationshipCandidate.CandidateId && x.Status == ExtractionCandidateStatus.Rejected);
        var persistedCase = dbContext.Cases.Single(x => x.Id == createdCase.CaseId);
        using var intakeDoc = System.Text.Json.JsonDocument.Parse(persistedCase.IntakeData!);
        intakeDoc.RootElement.GetProperty("PrimaryContactName").GetString().Should().Be("Ana Pereira");
        intakeDoc.RootElement.GetProperty("PrimaryContactPhone").GetString().Should().Be("+351911111111");
        intakeDoc.RootElement.TryGetProperty("RelationshipToDeceased", out _).Should().BeFalse();
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseExtractionDecisionsReviewed" && x.CaseId == createdCase.CaseId && x.ActorUserId == signup.UserId);
    }

    [Fact]
    public async Task ApplyExtractionDecisions_ShouldReturn409_WhenNoPendingCandidatesExist()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-extraction-review-no-pending@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Extraction Review No Pending Integration Case");

        Guid documentId;
        using (var uploadRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents",
                   new UploadCaseDocumentRequest
                   {
                       FileName = "raw.txt",
                       ContentType = "text/plain",
                       ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("PrimaryContactName: Ana"))
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var uploadResponse = await client.SendAsync(uploadRequest);
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var uploadEnvelope = await uploadResponse.Content.ReadFromJsonAsync<ApiEnvelope<CaseDocumentUploadResponse>>();
            uploadEnvelope.Should().NotBeNull();
            uploadEnvelope!.Data.Should().NotBeNull();
            documentId = uploadEnvelope.Data!.DocumentId;
        }

        using var reviewRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/{documentId}/extraction/review",
            new ApplyExtractionDecisionsRequest
            {
                Decisions =
                [
                    new ExtractionDecisionRequest { CandidateId = Guid.NewGuid(), Action = "Approve" }
                ]
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(reviewRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ApplyExtractionDecisions_ShouldReturn403_WhenUserIsEditor()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-extraction-review-editor@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Extraction Review Editor Integration Case");
        var editorUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.extraction.review.editor@agency.pt",
            CaseRole.Editor);

        Guid documentId;
        using (var uploadRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents",
                   new UploadCaseDocumentRequest
                   {
                       FileName = "review-editor.txt",
                       ContentType = "text/plain",
                       ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("PrimaryContactName: Ana"))
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var uploadResponse = await client.SendAsync(uploadRequest);
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var uploadEnvelope = await uploadResponse.Content.ReadFromJsonAsync<ApiEnvelope<CaseDocumentUploadResponse>>();
            uploadEnvelope.Should().NotBeNull();
            uploadEnvelope!.Data.Should().NotBeNull();
            documentId = uploadEnvelope.Data!.DocumentId;
        }

        Guid candidateId;
        using (var extractionRequest = BuildAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/{documentId}/extraction/candidates",
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var extractionResponse = await client.SendAsync(extractionRequest);
            extractionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var extractionEnvelope = await extractionResponse.Content.ReadFromJsonAsync<ApiEnvelope<ExtractDocumentCandidatesResponse>>();
            extractionEnvelope.Should().NotBeNull();
            extractionEnvelope!.Data.Should().NotBeNull();
            candidateId = extractionEnvelope.Data!.Candidates.First().CandidateId;
        }

        using var reviewRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents/{documentId}/extraction/review",
            new ApplyExtractionDecisionsRequest
            {
                Decisions =
                [
                    new ExtractionDecisionRequest { CandidateId = candidateId, Action = "Approve" }
                ]
            },
            editorUserId,
            signup.OrganizationId,
            "Editor");

        var response = await client.SendAsync(reviewRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GenerateCaseHandoffPacket_ShouldReturn200AndIncludeActionsAndEvidence_WhenPrerequisitesAreMet()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-handoff-success@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Handoff Integration Case");

        using (var intakeRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Put,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/intake",
                   new SubmitCaseIntakeRequest
                   {
                       PrimaryContactName = "Ana Pereira",
                       PrimaryContactPhone = "+351910000000",
                       RelationshipToDeceased = "Daughter",
                       HasWill = true,
                       RequiresLegalSupport = false,
                       RequiresFinancialSupport = true,
                       ConfirmAccuracy = true
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var intakeResponse = await client.SendAsync(intakeRequest);
            intakeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var activateRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Patch,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/lifecycle",
                   new UpdateCaseLifecycleRequest { TargetStatus = "Active" },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var activateResponse = await client.SendAsync(activateRequest);
            activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var generatePlanRequest = BuildAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/plan/generate",
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var planResponse = await client.SendAsync(generatePlanRequest);
            planResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var uploadRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents",
                   new UploadCaseDocumentRequest
                   {
                       FileName = "evidence.txt",
                       ContentType = "text/plain",
                       ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("evidence-content"))
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var uploadResponse = await client.SendAsync(uploadRequest);
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using var handoffRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/handoffs/packet",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(handoffRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<GenerateCaseHandoffPacketResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.CaseId.Should().Be(createdCase.CaseId);
        envelope.Data.RequiredActions.Should().NotBeEmpty();
        envelope.Data.EvidenceContext.Should().NotBeEmpty();
        var content = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(envelope.Data.ContentBase64));
        content.Should().Contain("Required Actions:");
        content.Should().Contain("Evidence Context:");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseHandoffPacketGenerated" && x.CaseId == createdCase.CaseId && x.ActorUserId == signup.UserId);
    }

    [Fact]
    public async Task GenerateCaseHandoffPacket_ShouldReturn409_WhenEvidenceIsMissing()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-handoff-no-evidence@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Handoff No Evidence Integration Case");

        using (var intakeRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Put,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/intake",
                   new SubmitCaseIntakeRequest
                   {
                       PrimaryContactName = "Ana Pereira",
                       PrimaryContactPhone = "+351910000000",
                       RelationshipToDeceased = "Daughter",
                       HasWill = true,
                       RequiresLegalSupport = false,
                       RequiresFinancialSupport = true,
                       ConfirmAccuracy = true
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var intakeResponse = await client.SendAsync(intakeRequest);
            intakeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var activateRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Patch,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/lifecycle",
                   new UpdateCaseLifecycleRequest { TargetStatus = "Active" },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var activateResponse = await client.SendAsync(activateRequest);
            activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var generatePlanRequest = BuildAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/plan/generate",
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var planResponse = await client.SendAsync(generatePlanRequest);
            planResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var handoffRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/handoffs/packet",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(handoffRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GenerateCaseHandoffPacket_ShouldReturn403_WhenUserIsEditor()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-handoff-editor@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Handoff Editor Integration Case");
        var editorUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.handoff.editor@agency.pt",
            CaseRole.Editor);

        using var handoffRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/handoffs/packet",
            editorUserId,
            signup.OrganizationId,
            "Editor");

        var response = await client.SendAsync(handoffRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCaseHandoffState_ShouldReturn200AndLatestState_WhenHandoffExists()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-handoff-state-get@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Handoff State Get Case");
        var packet = await GenerateHandoffPacketForCaseAsync(client, signup, createdCase);

        using var stateRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/handoffs/state",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(stateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CaseHandoffStateResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.CaseId.Should().Be(createdCase.CaseId);
        envelope.Data.HandoffId.Should().Be(packet.HandoffId);
        envelope.Data.Status.Should().Be(CaseHandoffStatus.PendingAdvisor.ToString());
        envelope.Data.FollowUpRequired.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCaseHandoffState_ShouldReturn200AndPersist_WhenRequestIsValid()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-handoff-state-update@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Handoff State Update Case");
        var packet = await GenerateHandoffPacketForCaseAsync(client, signup, createdCase);

        using var updateRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/handoffs/{packet.HandoffId}/state",
            new UpdateCaseHandoffStateRequest
            {
                Status = "Completed",
                Notes = "Advisor confirmed completion."
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CaseHandoffStateResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.HandoffId.Should().Be(packet.HandoffId);
        envelope.Data.Status.Should().Be(CaseHandoffStatus.Completed.ToString());
        envelope.Data.FollowUpRequired.Should().BeFalse();
        envelope.Data.StatusNotes.Should().Be("Advisor confirmed completion.");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.CaseHandoffs.Should().Contain(
            x => x.Id == packet.HandoffId
                 && x.Status == CaseHandoffStatus.Completed
                 && x.FollowUpRequired == false
                 && x.StatusNotes == "Advisor confirmed completion.");
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseHandoffStateUpdated" && x.CaseId == createdCase.CaseId && x.ActorUserId == signup.UserId);
    }

    [Fact]
    public async Task UpdateCaseHandoffState_ShouldReturn403_WhenUserIsReader()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-handoff-state-reader@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Handoff State Reader Case");
        var packet = await GenerateHandoffPacketForCaseAsync(client, signup, createdCase);
        var readerUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.handoff.reader@agency.pt",
            CaseRole.Reader);

        using var updateRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/handoffs/{packet.HandoffId}/state",
            new UpdateCaseHandoffStateRequest
            {
                Status = "Completed"
            },
            readerUserId,
            signup.OrganizationId,
            "Reader");

        var response = await client.SendAsync(updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ProvisionProcessAlias_ShouldReturn200AndCreateAlias_WhenCaseIsEligible()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-process-alias-provision@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Process Alias Provision Integration");
        await MoveCaseToActiveAsync(client, signup, createdCase.CaseId);

        using var provisionRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/process-alias/provision",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(provisionRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<ProcessAliasResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.CaseId.Should().Be(createdCase.CaseId);
        envelope.Data.Status.Should().Be(ProcessAliasStatus.Active.ToString());
        envelope.Data.AliasEmail.Should().StartWith("process-");
        envelope.Data.AliasEmail.Should().EndWith("@sanzy.ai");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.ProcessAliases.Should().Contain(x => x.Id == envelope.Data.AliasId && x.CaseId == createdCase.CaseId);
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "ProcessAliasCreated" && x.CaseId == createdCase.CaseId && x.ActorUserId == signup.UserId);
    }

    [Fact]
    public async Task RotateProcessAlias_ShouldReturn200AndRotateAlias_WhenActiveAliasExists()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-process-alias-rotate@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Process Alias Rotate Integration");
        await MoveCaseToActiveAsync(client, signup, createdCase.CaseId);

        using (var provisionRequest = BuildAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/process-alias/provision",
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var provisionResponse = await client.SendAsync(provisionRequest);
            provisionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var rotateRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/process-alias/rotate",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(rotateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<ProcessAliasResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Status.Should().Be(ProcessAliasStatus.Active.ToString());
        envelope.Data.RotatedFromAliasId.Should().NotBeNull();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.ProcessAliases.Should().Contain(x => x.Id == envelope.Data.AliasId && x.Status == ProcessAliasStatus.Active);
        dbContext.ProcessAliases.Should().Contain(x => x.Id == envelope.Data.RotatedFromAliasId && x.Status == ProcessAliasStatus.Rotated);
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "ProcessAliasRotated" && x.CaseId == createdCase.CaseId && x.ActorUserId == signup.UserId);
    }

    [Fact]
    public async Task DeactivateAndArchiveProcessAlias_ShouldReturn200AndPersistLifecycle()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-process-alias-lifecycle@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Process Alias Lifecycle Integration");
        await MoveCaseToActiveAsync(client, signup, createdCase.CaseId);

        using (var provisionRequest = BuildAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/process-alias/provision",
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var provisionResponse = await client.SendAsync(provisionRequest);
            provisionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var deactivateRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/process-alias/deactivate",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var deactivateResponse = await client.SendAsync(deactivateRequest);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deactivateEnvelope = await deactivateResponse.Content.ReadFromJsonAsync<ApiEnvelope<ProcessAliasResponse>>();
        deactivateEnvelope.Should().NotBeNull();
        deactivateEnvelope!.Data.Should().NotBeNull();
        deactivateEnvelope.Data!.Status.Should().Be(ProcessAliasStatus.Deactivated.ToString());

        using var archiveRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/process-alias/archive",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var archiveResponse = await client.SendAsync(archiveRequest);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var archiveEnvelope = await archiveResponse.Content.ReadFromJsonAsync<ApiEnvelope<ProcessAliasResponse>>();
        archiveEnvelope.Should().NotBeNull();
        archiveEnvelope!.Data.Should().NotBeNull();
        archiveEnvelope.Data!.Status.Should().Be(ProcessAliasStatus.Archived.ToString());

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "ProcessAliasDeactivated" && x.CaseId == createdCase.CaseId && x.ActorUserId == signup.UserId);
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "ProcessAliasArchived" && x.CaseId == createdCase.CaseId && x.ActorUserId == signup.UserId);
    }

    [Fact]
    public async Task ProvisionProcessAlias_ShouldReturn403_WhenUserIsReader()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-process-alias-reader@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Process Alias Reader Integration");
        await MoveCaseToActiveAsync(client, signup, createdCase.CaseId);
        var readerUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.process.alias.reader@agency.pt",
            CaseRole.Reader);

        using var provisionRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/process-alias/provision",
            readerUserId,
            signup.OrganizationId,
            "Reader");

        var response = await client.SendAsync(provisionRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetProcessInbox_ShouldReturn200AndThreadMetadata_WhenMessagesExist()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-process-inbox-success@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Process Inbox Integration");
        await MoveCaseToActiveAsync(client, signup, createdCase.CaseId);

        Guid aliasId;
        string aliasEmail;
        using (var provisionRequest = BuildAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/process-alias/provision",
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var provisionResponse = await client.SendAsync(provisionRequest);
            provisionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var provisionEnvelope = await provisionResponse.Content.ReadFromJsonAsync<ApiEnvelope<ProcessAliasResponse>>();
            provisionEnvelope.Should().NotBeNull();
            provisionEnvelope!.Data.Should().NotBeNull();
            aliasId = provisionEnvelope.Data!.AliasId;
            aliasEmail = provisionEnvelope.Data.AliasEmail;
        }

        var now = DateTime.UtcNow;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
            dbContext.ProcessEmails.AddRange(
                new ProcessEmail
                {
                    Id = Guid.NewGuid(),
                    TenantId = signup.OrganizationId,
                    CaseId = createdCase.CaseId,
                    ProcessAliasId = aliasId,
                    ThreadId = "thread-a",
                    Direction = ProcessEmailDirection.Outbound,
                    Subject = "Document request",
                    SenderEmail = aliasEmail,
                    RecipientEmails = "advisor.one@external.pt",
                    BodyPreview = "Please send files.",
                    ExternalMessageId = "ext-1",
                    SentAt = now.AddMinutes(-12),
                    CreatedAt = now.AddMinutes(-12),
                    UpdatedAt = now.AddMinutes(-12)
                },
                new ProcessEmail
                {
                    Id = Guid.NewGuid(),
                    TenantId = signup.OrganizationId,
                    CaseId = createdCase.CaseId,
                    ProcessAliasId = aliasId,
                    ThreadId = "thread-a",
                    Direction = ProcessEmailDirection.Inbound,
                    Subject = "Re: Document request",
                    SenderEmail = "advisor.one@external.pt",
                    RecipientEmails = aliasEmail,
                    BodyPreview = "Attached.",
                    ExternalMessageId = "ext-2",
                    SentAt = now.AddMinutes(-3),
                    CreatedAt = now.AddMinutes(-3),
                    UpdatedAt = now.AddMinutes(-3)
                },
                new ProcessEmail
                {
                    Id = Guid.NewGuid(),
                    TenantId = signup.OrganizationId,
                    CaseId = createdCase.CaseId,
                    ProcessAliasId = aliasId,
                    ThreadId = "thread-b",
                    Direction = ProcessEmailDirection.Outbound,
                    Subject = "Tax follow-up",
                    SenderEmail = aliasEmail,
                    RecipientEmails = "tax.office@external.pt",
                    BodyPreview = "Following up.",
                    ExternalMessageId = "ext-3",
                    SentAt = now.AddMinutes(-8),
                    CreatedAt = now.AddMinutes(-8),
                    UpdatedAt = now.AddMinutes(-8)
                });
            await dbContext.SaveChangesAsync();
        }

        using var inboxRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/process-inbox",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var response = await client.SendAsync(inboxRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<ProcessInboxResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.CaseId.Should().Be(createdCase.CaseId);
        envelope.Data.Threads.Should().HaveCount(2);
        envelope.Data.Threads[0].ThreadId.Should().Be("thread-a");
        envelope.Data.Threads[0].MessageCount.Should().Be(2);
        envelope.Data.Threads[0].CaseContextUrl.Should().Contain(createdCase.CaseId.ToString());
    }

    [Fact]
    public async Task GetProcessInbox_ShouldReturn409_WhenProcessAliasIsMissing()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-process-inbox-no-alias@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Process Inbox Missing Alias");
        await MoveCaseToActiveAsync(client, signup, createdCase.CaseId);

        using var inboxRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/process-inbox",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var response = await client.SendAsync(inboxRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetProcessInbox_ShouldReturn200_WhenUserIsReader()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-process-inbox-reader@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Process Inbox Reader");
        await MoveCaseToActiveAsync(client, signup, createdCase.CaseId);
        var readerUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.process.inbox.reader@agency.pt",
            CaseRole.Reader);

        Guid aliasId;
        string aliasEmail;
        using (var provisionRequest = BuildAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/process-alias/provision",
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var provisionResponse = await client.SendAsync(provisionRequest);
            provisionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var provisionEnvelope = await provisionResponse.Content.ReadFromJsonAsync<ApiEnvelope<ProcessAliasResponse>>();
            provisionEnvelope.Should().NotBeNull();
            provisionEnvelope!.Data.Should().NotBeNull();
            aliasId = provisionEnvelope.Data!.AliasId;
            aliasEmail = provisionEnvelope.Data.AliasEmail;
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
            dbContext.ProcessEmails.Add(
                new ProcessEmail
                {
                    Id = Guid.NewGuid(),
                    TenantId = signup.OrganizationId,
                    CaseId = createdCase.CaseId,
                    ProcessAliasId = aliasId,
                    ThreadId = "thread-reader",
                    Direction = ProcessEmailDirection.Outbound,
                    Subject = "Reader visible",
                    SenderEmail = aliasEmail,
                    RecipientEmails = "family.process.inbox.reader@agency.pt",
                    BodyPreview = "Shared for transparency.",
                    ExternalMessageId = "ext-r1",
                    SentAt = DateTime.UtcNow.AddMinutes(-1),
                    CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                    UpdatedAt = DateTime.UtcNow.AddMinutes(-1)
                });
            await dbContext.SaveChangesAsync();
        }

        using var inboxRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/process-inbox",
            readerUserId,
            signup.OrganizationId,
            "Reader");
        var response = await client.SendAsync(inboxRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProcessInbox_ShouldReturn403_WhenReaderHasNoVisibleThreads()
    {
        var client = _factory.CreateClient();
        var signup = await CreateTenantAsync(client, "cases-process-inbox-reader-denied@agency.pt");
        await ActivateTenantAsync(client, signup);
        var createdCase = await CreateCaseAsync(client, signup, "Process Inbox Reader Denied");
        await MoveCaseToActiveAsync(client, signup, createdCase.CaseId);
        var readerUserId = await SeedAcceptedParticipantAsync(
            signup.OrganizationId,
            createdCase.CaseId,
            "family.process.inbox.denied@agency.pt",
            CaseRole.Reader);

        Guid aliasId;
        string aliasEmail;
        using (var provisionRequest = BuildAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/process-alias/provision",
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var provisionResponse = await client.SendAsync(provisionRequest);
            provisionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var provisionEnvelope = await provisionResponse.Content.ReadFromJsonAsync<ApiEnvelope<ProcessAliasResponse>>();
            provisionEnvelope.Should().NotBeNull();
            provisionEnvelope!.Data.Should().NotBeNull();
            aliasId = provisionEnvelope.Data!.AliasId;
            aliasEmail = provisionEnvelope.Data.AliasEmail;
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
            dbContext.ProcessEmails.Add(
                new ProcessEmail
                {
                    Id = Guid.NewGuid(),
                    TenantId = signup.OrganizationId,
                    CaseId = createdCase.CaseId,
                    ProcessAliasId = aliasId,
                    ThreadId = "thread-hidden",
                    Direction = ProcessEmailDirection.Outbound,
                    Subject = "Hidden from reader",
                    SenderEmail = aliasEmail,
                    RecipientEmails = "advisor.hidden@external.pt",
                    BodyPreview = "Not visible to reader.",
                    ExternalMessageId = "ext-hidden",
                    SentAt = DateTime.UtcNow.AddMinutes(-1),
                    CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                    UpdatedAt = DateTime.UtcNow.AddMinutes(-1)
                });
            await dbContext.SaveChangesAsync();
        }

        using var inboxRequest = BuildAuthorizedRequest(
            HttpMethod.Get,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/process-inbox",
            readerUserId,
            signup.OrganizationId,
            "Reader");
        var response = await client.SendAsync(inboxRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
                PaymentMethodReference = "pm_case_tests",
                InvoiceProfileLegalName = "Agency Lda",
                InvoiceProfileVatNumber = "PT123456789",
                InvoiceProfileBillingEmail = "billing@agency.pt",
                InvoiceProfileCountryCode = "PT"
            },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var activationResponse = await client.SendAsync(activationRequest);
        activationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<CreateCaseResponse> CreateCaseAsync(
        HttpClient client,
        CreateAgencyAccountResponse signup,
        string deceasedFullName)
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
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<CreateCaseResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        return envelope.Data!;
    }

    private static async Task MoveCaseToActiveAsync(
        HttpClient client,
        CreateAgencyAccountResponse signup,
        Guid caseId)
    {
        using (var intakeRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Put,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{caseId}/intake",
                   new SubmitCaseIntakeRequest
                   {
                       PrimaryContactName = "Ana Pereira",
                       PrimaryContactPhone = "+351910000000",
                       RelationshipToDeceased = "Daughter",
                       HasWill = true,
                       RequiresLegalSupport = false,
                       RequiresFinancialSupport = true,
                       ConfirmAccuracy = true
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var intakeResponse = await client.SendAsync(intakeRequest);
            intakeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var activateRequest = BuildAuthorizedJsonRequest(
            HttpMethod.Patch,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{caseId}/lifecycle",
            new UpdateCaseLifecycleRequest { TargetStatus = "Active" },
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");
        var activateResponse = await client.SendAsync(activateRequest);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<GenerateCaseHandoffPacketResponse> GenerateHandoffPacketForCaseAsync(
        HttpClient client,
        CreateAgencyAccountResponse signup,
        CreateCaseResponse createdCase)
    {
        using (var intakeRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Put,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/intake",
                   new SubmitCaseIntakeRequest
                   {
                       PrimaryContactName = "Ana Pereira",
                       PrimaryContactPhone = "+351910000000",
                       RelationshipToDeceased = "Daughter",
                       HasWill = true,
                       RequiresLegalSupport = false,
                       RequiresFinancialSupport = true,
                       ConfirmAccuracy = true
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var intakeResponse = await client.SendAsync(intakeRequest);
            intakeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var activateRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Patch,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/lifecycle",
                   new UpdateCaseLifecycleRequest { TargetStatus = "Active" },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var activateResponse = await client.SendAsync(activateRequest);
            activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var generatePlanRequest = BuildAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/plan/generate",
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var planResponse = await client.SendAsync(generatePlanRequest);
            planResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var uploadRequest = BuildAuthorizedJsonRequest(
                   HttpMethod.Post,
                   $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/documents",
                   new UploadCaseDocumentRequest
                   {
                       FileName = "evidence.txt",
                       ContentType = "text/plain",
                       ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("evidence-content"))
                   },
                   signup.UserId,
                   signup.OrganizationId,
                   "AgencyAdmin"))
        {
            var uploadResponse = await client.SendAsync(uploadRequest);
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using var handoffRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            $"/api/v1/tenants/{signup.OrganizationId}/cases/{createdCase.CaseId}/handoffs/packet",
            signup.UserId,
            signup.OrganizationId,
            "AgencyAdmin");

        var handoffResponse = await client.SendAsync(handoffRequest);
        handoffResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await handoffResponse.Content.ReadFromJsonAsync<ApiEnvelope<GenerateCaseHandoffPacketResponse>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        return envelope.Data!;
    }

    private async Task<Guid> SeedAcceptedParticipantAsync(
        Guid tenantId,
        Guid caseId,
        string email,
        CaseRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SanzuDbContext>();
        var userId = Guid.NewGuid();

        dbContext.Users.Add(
            new User
            {
                Id = userId,
                OrgId = tenantId,
                Email = email,
                FullName = "Family Participant",
                CreatedAt = DateTime.UtcNow
            });

        dbContext.CaseParticipants.Add(
            new CaseParticipant
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CaseId = caseId,
                Email = email.ToLowerInvariant(),
                Role = role,
                Status = CaseParticipantStatus.Accepted,
                TokenHash = "DIRECT_SEED",
                InvitedByUserId = userId,
                ParticipantUserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                AcceptedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
        return userId;
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

    private static async Task<CreateAgencyAccountResponse> CreateTenantAsync(HttpClient client, string email)
    {
        var request = new CreateAgencyAccountRequest
        {
            Email = email,
            FullName = "Case Manager",
            AgencyName = "Case Agency",
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
