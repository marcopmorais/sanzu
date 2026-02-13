using System.Net;
using System.Net.Http.Json;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
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
