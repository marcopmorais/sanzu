using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;
using Sanzu.Core.Services;
using Sanzu.Core.Validators;
using Sanzu.Infrastructure.Data;
using Sanzu.Infrastructure.Repositories;

namespace Sanzu.Tests.Unit.Services;

public sealed class CaseServiceTests
{
    [Fact]
    public async Task CreateCase_ShouldCreateDraftCaseAndAudit_WhenRequestIsValid()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);

        var result = await service.CreateCaseAsync(
            tenantId,
            actorUserId,
            new CreateCaseRequest
            {
                DeceasedFullName = "Maria Fernanda Silva",
                DateOfDeath = DateTime.UtcNow.AddDays(-2),
                CaseType = "Estate",
                Urgency = "High",
                Notes = "Family contacted agency via phone."
            },
            CancellationToken.None);

        result.TenantId.Should().Be(tenantId);
        result.ManagerUserId.Should().Be(actorUserId);
        result.Status.Should().Be(CaseStatus.Draft);
        result.CaseNumber.Should().Be("CASE-00001");
        result.CaseType.Should().Be("ESTATE");
        result.Urgency.Should().Be("HIGH");

        var persistedCase = await dbContext.Cases.SingleAsync(x => x.Id == result.CaseId);
        persistedCase.DeceasedFullName.Should().Be("Maria Fernanda Silva");
        persistedCase.Status.Should().Be(CaseStatus.Draft);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "CaseCreated");
    }

    [Fact]
    public async Task CreateCase_ShouldThrowAccessDenied_WhenActorHasNoTenantAdminRole()
    {
        var dbContext = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId, TenantStatus.Active);
        await SeedUserAsync(dbContext, actorUserId, tenantId);
        var service = CreateService(dbContext);

        var act = () => service.CreateCaseAsync(
            tenantId,
            actorUserId,
            new CreateCaseRequest
            {
                DeceasedFullName = "Jose Matos",
                DateOfDeath = DateTime.UtcNow.AddDays(-1)
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantAccessDeniedException>();
    }

    [Fact]
    public async Task CreateCase_ShouldThrowCaseStateException_WhenTenantIsNotActive()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Pending);
        var service = CreateService(dbContext);

        var act = () => service.CreateCaseAsync(
            tenantId,
            actorUserId,
            new CreateCaseRequest
            {
                DeceasedFullName = "Ana Gomes",
                DateOfDeath = DateTime.UtcNow.AddDays(-3)
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseStateException>()
            .WithMessage("*active subscription*");
    }

    [Fact]
    public async Task CreateCase_ShouldIncrementCaseSequence_ForSameTenant()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);

        var firstCase = await CreateCaseAsync(service, tenantId, actorUserId, "First Person");
        var secondCase = await CreateCaseAsync(service, tenantId, actorUserId, "Second Person");

        firstCase.CaseNumber.Should().Be("CASE-00001");
        secondCase.CaseNumber.Should().Be("CASE-00002");
    }

    [Fact]
    public async Task UpdateCaseDetails_ShouldPersistChangesAndWriteAudit_WhenCaseIsMutable()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Maria Original");

        var result = await service.UpdateCaseDetailsAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UpdateCaseDetailsRequest
            {
                DeceasedFullName = "Maria Atualizada",
                CaseType = "Insurance",
                Urgency = "Urgent",
                Notes = "Updated notes"
            },
            CancellationToken.None);

        result.CaseId.Should().Be(createdCase.CaseId);
        result.DeceasedFullName.Should().Be("Maria Atualizada");
        result.CaseType.Should().Be("INSURANCE");
        result.Urgency.Should().Be("URGENT");
        result.Notes.Should().Be("Updated notes");

        var persistedCase = await dbContext.Cases.SingleAsync(x => x.Id == createdCase.CaseId);
        persistedCase.DeceasedFullName.Should().Be("Maria Atualizada");
        persistedCase.CaseType.Should().Be("INSURANCE");
        persistedCase.Urgency.Should().Be("URGENT");
        persistedCase.Notes.Should().Be("Updated notes");
        persistedCase.UpdatedAt.Should().BeAfter(persistedCase.CreatedAt);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "CaseDetailsUpdated" && x.CaseId == createdCase.CaseId);
    }

    [Fact]
    public async Task UpdateCaseLifecycle_ShouldSetClosedAndArchivedTimestamps_WhenTransitionsAreValid()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Lifecycle Subject");

        await service.UpdateCaseLifecycleAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UpdateCaseLifecycleRequest { TargetStatus = "Intake" },
            CancellationToken.None);
        await service.UpdateCaseLifecycleAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UpdateCaseLifecycleRequest { TargetStatus = "Active" },
            CancellationToken.None);
        await service.UpdateCaseLifecycleAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UpdateCaseLifecycleRequest { TargetStatus = "Review" },
            CancellationToken.None);
        var closed = await service.UpdateCaseLifecycleAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UpdateCaseLifecycleRequest { TargetStatus = "Closed" },
            CancellationToken.None);
        var archived = await service.UpdateCaseLifecycleAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UpdateCaseLifecycleRequest { TargetStatus = "Archived" },
            CancellationToken.None);

        closed.Status.Should().Be(CaseStatus.Closed);
        closed.ClosedAt.Should().NotBeNull();
        archived.Status.Should().Be(CaseStatus.Archived);
        archived.ArchivedAt.Should().NotBeNull();

        var persistedCase = await dbContext.Cases.SingleAsync(x => x.Id == createdCase.CaseId);
        persistedCase.Status.Should().Be(CaseStatus.Archived);
        persistedCase.ClosedAt.Should().NotBeNull();
        persistedCase.ArchivedAt.Should().NotBeNull();
        dbContext.AuditEvents.Count(x => x.EventType == "CaseStatusChanged" && x.CaseId == createdCase.CaseId).Should().Be(5);
    }

    [Fact]
    public async Task UpdateCaseLifecycle_ShouldThrowCaseStateException_WhenTransitionIsInvalid()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Invalid Transition");

        var act = () => service.UpdateCaseLifecycleAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UpdateCaseLifecycleRequest { TargetStatus = "Closed" },
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseStateException>()
            .WithMessage("*Invalid lifecycle transition*");
    }

    [Fact]
    public async Task GetCaseMilestones_ShouldReturnCreatedAndLifecycleEventsOrdered()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Timeline Case");

        await service.UpdateCaseLifecycleAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UpdateCaseLifecycleRequest { TargetStatus = "Intake" },
            CancellationToken.None);
        await service.UpdateCaseLifecycleAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UpdateCaseLifecycleRequest { TargetStatus = "Active" },
            CancellationToken.None);

        var result = await service.GetCaseMilestonesAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        result.CaseId.Should().Be(createdCase.CaseId);
        result.Milestones.Should().HaveCount(3);
        result.Milestones[0].EventType.Should().Be("CaseCreated");
        result.Milestones[0].Status.Should().Be(CaseStatus.Draft);
        result.Milestones[1].Status.Should().Be(CaseStatus.Intake);
        result.Milestones[2].Status.Should().Be(CaseStatus.Active);
        result.Milestones.Should().BeInAscendingOrder(x => x.OccurredAt);
    }

    [Fact]
    public async Task InviteCaseParticipant_ShouldCreatePendingInvitationAndAudit_WhenCaseIsActive()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Participant Invite");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);

        var result = await service.InviteCaseParticipantAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new InviteCaseParticipantRequest
            {
                Email = "family.editor@agency.pt",
                Role = "Editor",
                ExpirationDays = 7
            },
            CancellationToken.None);

        result.InvitationToken.Should().NotBeNullOrWhiteSpace();
        result.Participant.Email.Should().Be("family.editor@agency.pt");
        result.Participant.Role.Should().Be(CaseRole.Editor);
        result.Participant.Status.Should().Be(CaseParticipantStatus.Pending);

        dbContext.CaseParticipants.Should().Contain(
            x => x.Id == result.Participant.ParticipantId
                 && x.CaseId == createdCase.CaseId
                 && x.Status == CaseParticipantStatus.Pending);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "CaseParticipantInvited" && x.CaseId == createdCase.CaseId);
    }

    [Fact]
    public async Task InviteCaseParticipant_ShouldThrowCaseStateException_WhenCaseIsNotActive()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Draft Case");

        var act = () => service.InviteCaseParticipantAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new InviteCaseParticipantRequest
            {
                Email = "family.reader@agency.pt",
                Role = "Reader"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseStateException>()
            .WithMessage("*only allowed for active cases*");
    }

    [Fact]
    public async Task AcceptCaseParticipantInvitation_ShouldProvisionRole_WhenTokenAndActorEmailMatch()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var invitedUserId = Guid.NewGuid();
        await SeedUserAsync(dbContext, invitedUserId, tenantId, "family.accept@agency.pt", "Family User");

        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Acceptance Case");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);
        var invitation = await service.InviteCaseParticipantAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new InviteCaseParticipantRequest
            {
                Email = "family.accept@agency.pt",
                Role = "Editor"
            },
            CancellationToken.None);

        var accepted = await service.AcceptCaseParticipantInvitationAsync(
            tenantId,
            invitedUserId,
            createdCase.CaseId,
            invitation.Participant.ParticipantId,
            new AcceptCaseParticipantInvitationRequest
            {
                InvitationToken = invitation.InvitationToken
            },
            CancellationToken.None);

        accepted.Status.Should().Be(CaseParticipantStatus.Accepted);
        accepted.ParticipantUserId.Should().Be(invitedUserId);
        accepted.AcceptedAt.Should().NotBeNull();
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "CaseParticipantAccepted" && x.CaseId == createdCase.CaseId);
    }

    [Fact]
    public async Task UpdateCaseParticipantRole_ShouldPersistRoleAndWriteAudit_WhenParticipantExists()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Role Update Case");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);
        var invitation = await service.InviteCaseParticipantAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new InviteCaseParticipantRequest
            {
                Email = "family.role@agency.pt",
                Role = "Editor"
            },
            CancellationToken.None);

        var updated = await service.UpdateCaseParticipantRoleAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            invitation.Participant.ParticipantId,
            new UpdateCaseParticipantRoleRequest
            {
                Role = "Reader"
            },
            CancellationToken.None);

        updated.Role.Should().Be(CaseRole.Reader);
        dbContext.CaseParticipants.Should().Contain(x => x.Id == invitation.Participant.ParticipantId && x.Role == CaseRole.Reader);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "CaseParticipantRoleUpdated" && x.CaseId == createdCase.CaseId);
    }

    // ===== Story 2.4: Role-Based Access Control Tests =====

    [Fact]
    public async Task GetCaseDetails_ShouldReturnDetails_WhenUserIsReader()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Reader Details Case");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);
        var readerUserId = await SeedAcceptedParticipantAsync(
            dbContext, service, tenantId, actorUserId, createdCase.CaseId, "reader.details@agency.pt", "Reader");

        var result = await service.GetCaseDetailsAsync(tenantId, readerUserId, createdCase.CaseId, CancellationToken.None);

        result.CaseId.Should().Be(createdCase.CaseId);
        result.DeceasedFullName.Should().Be("Reader Details Case");
        result.Status.Should().Be(CaseStatus.Active);
    }

    [Fact]
    public async Task GetCaseDetails_ShouldReturnDetails_WhenUserIsTenantAdmin()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Admin Details Case");

        var result = await service.GetCaseDetailsAsync(tenantId, actorUserId, createdCase.CaseId, CancellationToken.None);

        result.CaseId.Should().Be(createdCase.CaseId);
        result.DeceasedFullName.Should().Be("Admin Details Case");
    }

    [Fact]
    public async Task GetCaseDetails_ShouldThrowCaseAccessDeniedAndAudit_WhenUserHasNoCaseRole()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "No Access Case");
        var noRoleUserId = Guid.NewGuid();
        await SeedUserAsync(dbContext, noRoleUserId, tenantId);

        var act = () => service.GetCaseDetailsAsync(tenantId, noRoleUserId, createdCase.CaseId, CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "NO_CASE_ACCESS" && e.AttemptedAction == "GetCaseDetails");
        AssertAccessDeniedAudit(dbContext, noRoleUserId, createdCase.CaseId, "GetCaseDetails", "Reader", null, "NO_CASE_ACCESS");
    }

    [Fact]
    public async Task UpdateCaseDetails_ShouldSucceed_WhenUserIsEditor()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Editor Update Case");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);
        var editorUserId = await SeedAcceptedParticipantAsync(
            dbContext, service, tenantId, actorUserId, createdCase.CaseId, "editor.update@agency.pt", "Editor");

        var result = await service.UpdateCaseDetailsAsync(
            tenantId,
            editorUserId,
            createdCase.CaseId,
            new UpdateCaseDetailsRequest { Notes = "Updated by editor" },
            CancellationToken.None);

        result.Notes.Should().Be("Updated by editor");
    }

    [Fact]
    public async Task UpdateCaseDetails_ShouldThrowCaseAccessDeniedAndAudit_WhenUserIsReader()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Reader Update Case");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);
        var readerUserId = await SeedAcceptedParticipantAsync(
            dbContext, service, tenantId, actorUserId, createdCase.CaseId, "reader.update@agency.pt", "Reader");

        var act = () => service.UpdateCaseDetailsAsync(
            tenantId,
            readerUserId,
            createdCase.CaseId,
            new UpdateCaseDetailsRequest { Notes = "Attempted by reader" },
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "UpdateCaseDetails");
        AssertAccessDeniedAudit(dbContext, readerUserId, createdCase.CaseId, "UpdateCaseDetails", "Editor", "Reader", "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task UpdateCaseLifecycle_ShouldThrowCaseAccessDeniedAndAudit_WhenUserIsEditor()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Editor Lifecycle Case");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);
        var editorUserId = await SeedAcceptedParticipantAsync(
            dbContext, service, tenantId, actorUserId, createdCase.CaseId, "editor.lifecycle@agency.pt", "Editor");

        var act = () => service.UpdateCaseLifecycleAsync(
            tenantId,
            editorUserId,
            createdCase.CaseId,
            new UpdateCaseLifecycleRequest { TargetStatus = "Review" },
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "UpdateCaseLifecycle");
        AssertAccessDeniedAudit(dbContext, editorUserId, createdCase.CaseId, "UpdateCaseLifecycle", "Manager", "Editor", "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task GetCaseMilestones_ShouldSucceed_WhenUserIsReader()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Reader Milestones Case");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);
        var readerUserId = await SeedAcceptedParticipantAsync(
            dbContext, service, tenantId, actorUserId, createdCase.CaseId, "reader.milestones@agency.pt", "Reader");

        var result = await service.GetCaseMilestonesAsync(tenantId, readerUserId, createdCase.CaseId, CancellationToken.None);

        result.CaseId.Should().Be(createdCase.CaseId);
        result.Milestones.Should().NotBeEmpty();
    }

    [Fact]
    public async Task InviteCaseParticipant_ShouldThrowCaseAccessDeniedAndAudit_WhenUserIsEditor()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Editor Invite Case");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);
        var editorUserId = await SeedAcceptedParticipantAsync(
            dbContext, service, tenantId, actorUserId, createdCase.CaseId, "editor.invite@agency.pt", "Editor");

        var act = () => service.InviteCaseParticipantAsync(
            tenantId,
            editorUserId,
            createdCase.CaseId,
            new InviteCaseParticipantRequest { Email = "new.invite@agency.pt", Role = "Reader", ExpirationDays = 7 },
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "InviteCaseParticipant");
        AssertAccessDeniedAudit(dbContext, editorUserId, createdCase.CaseId, "InviteCaseParticipant", "Manager", "Editor", "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task UpdateCaseParticipantRole_ShouldThrowCaseAccessDeniedAndAudit_WhenUserIsEditor()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Editor Role Update Case");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);
        var editorUserId = await SeedAcceptedParticipantAsync(
            dbContext, service, tenantId, actorUserId, createdCase.CaseId, "editor.role@agency.pt", "Editor");

        var invitation = await service.InviteCaseParticipantAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new InviteCaseParticipantRequest { Email = "target.role@agency.pt", Role = "Reader", ExpirationDays = 7 },
            CancellationToken.None);

        var act = () => service.UpdateCaseParticipantRoleAsync(
            tenantId,
            editorUserId,
            createdCase.CaseId,
            invitation.Participant.ParticipantId,
            new UpdateCaseParticipantRoleRequest { Role = "Editor" },
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "UpdateCaseParticipantRole");
        AssertAccessDeniedAudit(dbContext, editorUserId, createdCase.CaseId, "UpdateCaseParticipantRole", "Manager", "Editor", "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task SubmitCaseIntake_ShouldPersistStructuredDataAndTransitionToIntake_WhenUserIsEditor()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Intake Case");
        var editorUserId = Guid.NewGuid();
        const string editorEmail = "family.intake.editor@agency.pt";
        await SeedUserAsync(dbContext, editorUserId, tenantId, editorEmail, "Family Editor");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, editorUserId, editorEmail, CaseRole.Editor);

        var result = await service.SubmitCaseIntakeAsync(
            tenantId,
            editorUserId,
            createdCase.CaseId,
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = "Ana Pereira",
                PrimaryContactPhone = "+351919999999",
                RelationshipToDeceased = "Daughter",
                HasWill = true,
                RequiresLegalSupport = true,
                RequiresFinancialSupport = false,
                ConfirmAccuracy = true,
                Notes = "Family has all civil documents available."
            },
            CancellationToken.None);

        result.Status.Should().Be(CaseStatus.Intake);

        var persistedCase = await dbContext.Cases.SingleAsync(x => x.Id == createdCase.CaseId);
        persistedCase.Status.Should().Be(CaseStatus.Intake);
        persistedCase.IntakeCompletedByUserId.Should().Be(editorUserId);
        persistedCase.IntakeCompletedAt.Should().NotBeNull();
        persistedCase.IntakeData.Should().NotBeNullOrWhiteSpace();

        using var intakeDoc = JsonDocument.Parse(persistedCase.IntakeData!);
        intakeDoc.RootElement.GetProperty("PrimaryContactName").GetString().Should().Be("Ana Pereira");
        intakeDoc.RootElement.GetProperty("RelationshipToDeceased").GetString().Should().Be("Daughter");

        dbContext.AuditEvents.Should().Contain(x => x.EventType == "CaseIntakeSubmitted" && x.CaseId == createdCase.CaseId && x.ActorUserId == editorUserId);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "CaseStatusChanged" && x.CaseId == createdCase.CaseId);
    }

    [Fact]
    public async Task SubmitCaseIntake_ShouldThrowCaseAccessDeniedAndAudit_WhenUserIsReader()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Reader Intake Case");
        var readerUserId = Guid.NewGuid();
        const string readerEmail = "family.intake.reader@agency.pt";
        await SeedUserAsync(dbContext, readerUserId, tenantId, readerEmail, "Family Reader");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, readerUserId, readerEmail, CaseRole.Reader);

        var act = () => service.SubmitCaseIntakeAsync(
            tenantId,
            readerUserId,
            createdCase.CaseId,
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = "Reader Attempt",
                PrimaryContactPhone = "+351910000000",
                RelationshipToDeceased = "Sibling",
                ConfirmAccuracy = true
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "SubmitCaseIntake");
        AssertAccessDeniedAudit(dbContext, readerUserId, createdCase.CaseId, "SubmitCaseIntake", "Editor", "Reader", "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task SubmitCaseIntake_ShouldThrowValidationException_WhenRequestIsInvalid()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Invalid Intake Case");

        var act = () => service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = string.Empty,
                PrimaryContactPhone = string.Empty,
                RelationshipToDeceased = string.Empty,
                ConfirmAccuracy = false
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        dbContext.AuditEvents.Should().NotContain(x => x.EventType == "CaseIntakeSubmitted" && x.CaseId == createdCase.CaseId);
    }

    [Fact]
    public async Task GenerateCasePlan_ShouldCreateStepsDependenciesAndSetInitialReadiness_WhenIntakeIsCompleted()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Plan Generation Case");

        await service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = "Ana Pereira",
                PrimaryContactPhone = "+351919999999",
                RelationshipToDeceased = "Daughter",
                HasWill = true,
                RequiresLegalSupport = true,
                RequiresFinancialSupport = false,
                ConfirmAccuracy = true
            },
            CancellationToken.None);

        var response = await service.GenerateCasePlanAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        response.CaseId.Should().Be(createdCase.CaseId);
        response.Steps.Should().HaveCount(5);
        response.Steps.Select(x => x.StepKey).Should().ContainInOrder(
            "collect-civil-records",
            "gather-estate-inventory",
            "submit-succession-notification",
            "validate-will",
            "engage-legal-support");
        response.Steps.Should().Contain(x => x.StepKey == "collect-civil-records" && x.Status == WorkflowStepStatus.Ready);
        response.Steps.Should().Contain(x => x.StepKey == "gather-estate-inventory" && x.Status == WorkflowStepStatus.Ready);
        response.Steps.Should().Contain(x => x.StepKey == "submit-succession-notification" && x.Status == WorkflowStepStatus.Blocked && x.DependsOnStepIds.Count == 2);
        response.Steps.Should().Contain(x => x.StepKey == "validate-will" && x.Status == WorkflowStepStatus.Blocked && x.DependsOnStepIds.Count == 1);
        response.Steps.Should().Contain(x => x.StepKey == "engage-legal-support" && x.Status == WorkflowStepStatus.Blocked && x.DependsOnStepIds.Count == 1);

        dbContext.WorkflowStepInstances.Count(x => x.CaseId == createdCase.CaseId).Should().Be(5);
        dbContext.WorkflowStepDependencies.Count(x => x.CaseId == createdCase.CaseId).Should().Be(4);
        dbContext.Cases.Single(x => x.Id == createdCase.CaseId).Status.Should().Be(CaseStatus.Active);
        dbContext.AuditEvents.Should().Contain(x => x.EventType == "CasePlanGenerated" && x.CaseId == createdCase.CaseId);
    }

    [Fact]
    public async Task GenerateCasePlan_ShouldThrowCaseStateException_WhenIntakeIsMissing()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Plan Without Intake Case");

        var act = () => service.GenerateCasePlanAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseStateException>()
            .WithMessage("*Structured intake must be completed*");
    }

    [Fact]
    public async Task GenerateCasePlan_ShouldThrowCaseAccessDeniedAndAudit_WhenUserIsEditor()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Editor Plan Case");

        await service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = "Ana Pereira",
                PrimaryContactPhone = "+351919999999",
                RelationshipToDeceased = "Daughter",
                ConfirmAccuracy = true
            },
            CancellationToken.None);

        var editorUserId = Guid.NewGuid();
        const string editorEmail = "family.plan.editor@agency.pt";
        await SeedUserAsync(dbContext, editorUserId, tenantId, editorEmail, "Family Editor");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, editorUserId, editorEmail, CaseRole.Editor);

        var act = () => service.GenerateCasePlanAsync(
            tenantId,
            editorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "GenerateCasePlan");
        AssertAccessDeniedAudit(dbContext, editorUserId, createdCase.CaseId, "GenerateCasePlan", "Manager", "Editor", "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task RecalculateCasePlanReadiness_ShouldSetDependentStepsToReady_WhenPrerequisitesAreCompleted()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Recalculate Readiness Case");

        await service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = "Ana Pereira",
                PrimaryContactPhone = "+351919999999",
                RelationshipToDeceased = "Daughter",
                HasWill = true,
                RequiresLegalSupport = true,
                ConfirmAccuracy = true
            },
            CancellationToken.None);

        await service.GenerateCasePlanAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        var recordsStep = await dbContext.WorkflowStepInstances
            .SingleAsync(x => x.CaseId == createdCase.CaseId && x.StepKey == "collect-civil-records");
        var inventoryStep = await dbContext.WorkflowStepInstances
            .SingleAsync(x => x.CaseId == createdCase.CaseId && x.StepKey == "gather-estate-inventory");
        var nowUtc = DateTime.UtcNow;
        recordsStep.Status = WorkflowStepStatus.Complete;
        recordsStep.UpdatedAt = nowUtc;
        inventoryStep.Status = WorkflowStepStatus.Complete;
        inventoryStep.UpdatedAt = nowUtc;
        await dbContext.SaveChangesAsync();

        var response = await service.RecalculateCasePlanReadinessAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        response.Steps.Should().Contain(
            x => x.StepKey == "submit-succession-notification" && x.Status == WorkflowStepStatus.Ready);
        response.Steps.Should().Contain(
            x => x.StepKey == "validate-will" && x.Status == WorkflowStepStatus.Ready);
        response.Steps.Should().Contain(
            x => x.StepKey == "engage-legal-support" && x.Status == WorkflowStepStatus.Blocked);
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CasePlanReadinessRecalculated" && x.CaseId == createdCase.CaseId);
    }

    [Fact]
    public async Task RecalculateCasePlanReadiness_ShouldThrowCaseStateException_WhenCasePlanWasNotGenerated()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "No Plan Recalculate Case");

        var act = () => service.RecalculateCasePlanReadinessAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseStateException>()
            .WithMessage("*generated case plan is required*");
    }

    [Fact]
    public async Task OverrideWorkflowStepReadiness_ShouldPersistOverrideMetadataAndAudit_WhenManagerOverrides()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Override Readiness Case");

        await service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = "Ana Pereira",
                PrimaryContactPhone = "+351919999999",
                RelationshipToDeceased = "Daughter",
                ConfirmAccuracy = true
            },
            CancellationToken.None);

        await service.GenerateCasePlanAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        var targetStep = await dbContext.WorkflowStepInstances
            .SingleAsync(x => x.CaseId == createdCase.CaseId && x.StepKey == "submit-succession-notification");

        var response = await service.OverrideWorkflowStepReadinessAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            targetStep.Id,
            new OverrideWorkflowStepReadinessRequest
            {
                TargetStatus = "Ready",
                Rationale = "Manual unblock approved after external verification."
            },
            CancellationToken.None);

        response.Steps.Should().Contain(x => x.StepId == targetStep.Id && x.Status == WorkflowStepStatus.Ready);

        var persistedStep = await dbContext.WorkflowStepInstances.SingleAsync(x => x.Id == targetStep.Id);
        persistedStep.IsReadinessOverridden.Should().BeTrue();
        persistedStep.ReadinessOverrideRationale.Should().Be("Manual unblock approved after external verification.");
        persistedStep.ReadinessOverrideByUserId.Should().Be(actorUserId);
        persistedStep.ReadinessOverriddenAt.Should().NotBeNull();
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CasePlanReadinessOverridden" && x.CaseId == createdCase.CaseId);
    }

    [Fact]
    public async Task OverrideWorkflowStepReadiness_ShouldThrowCaseAccessDeniedAndAudit_WhenUserIsEditor()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Editor Override Case");

        await service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = "Ana Pereira",
                PrimaryContactPhone = "+351919999999",
                RelationshipToDeceased = "Daughter",
                ConfirmAccuracy = true
            },
            CancellationToken.None);

        await service.GenerateCasePlanAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        var editorUserId = Guid.NewGuid();
        const string editorEmail = "family.override.editor@agency.pt";
        await SeedUserAsync(dbContext, editorUserId, tenantId, editorEmail, "Family Editor");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, editorUserId, editorEmail, CaseRole.Editor);
        var targetStep = await dbContext.WorkflowStepInstances
            .SingleAsync(x => x.CaseId == createdCase.CaseId && x.StepKey == "submit-succession-notification");

        var act = () => service.OverrideWorkflowStepReadinessAsync(
            tenantId,
            editorUserId,
            createdCase.CaseId,
            targetStep.Id,
            new OverrideWorkflowStepReadinessRequest
            {
                TargetStatus = "Ready",
                Rationale = "Attempted by editor."
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "OverrideWorkflowStepReadiness");
        AssertAccessDeniedAudit(
            dbContext,
            editorUserId,
            createdCase.CaseId,
            "OverrideWorkflowStepReadiness",
            "Manager",
            "Editor",
            "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task GetCaseTaskWorkspace_ShouldReturnTasksSortedByPriorityAndDeadlineUrgency()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Task Workspace Case");

        await service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = "Ana Pereira",
                PrimaryContactPhone = "+351919999999",
                RelationshipToDeceased = "Daughter",
                HasWill = true,
                RequiresLegalSupport = true,
                ConfirmAccuracy = true
            },
            CancellationToken.None);

        await service.GenerateCasePlanAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        var inProgressStep = await dbContext.WorkflowStepInstances
            .SingleAsync(x => x.CaseId == createdCase.CaseId && x.StepKey == "collect-civil-records");
        var readyOverdueStep = await dbContext.WorkflowStepInstances
            .SingleAsync(x => x.CaseId == createdCase.CaseId && x.StepKey == "gather-estate-inventory");
        inProgressStep.Status = WorkflowStepStatus.InProgress;
        inProgressStep.DueDate = DateTime.UtcNow.AddDays(2);
        inProgressStep.UpdatedAt = DateTime.UtcNow;
        readyOverdueStep.Status = WorkflowStepStatus.Ready;
        readyOverdueStep.DueDate = DateTime.UtcNow.AddDays(-1);
        readyOverdueStep.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var workspace = await service.GetCaseTaskWorkspaceAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        workspace.Tasks.Should().NotBeEmpty();
        workspace.Tasks[0].StepKey.Should().Be("collect-civil-records");
        workspace.Tasks[0].PriorityRank.Should().Be(1);
        workspace.Tasks[0].AssignedUserId.Should().Be(actorUserId);
        workspace.Tasks[1].StepKey.Should().Be("gather-estate-inventory");
        workspace.Tasks[1].UrgencyIndicator.Should().Be("overdue");
    }

    [Fact]
    public async Task UpdateWorkflowTaskStatus_ShouldPersistStatusAndAudit_WhenEditorMarksTaskAsStarted()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Task Status Update Case");

        await service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = "Ana Pereira",
                PrimaryContactPhone = "+351919999999",
                RelationshipToDeceased = "Daughter",
                ConfirmAccuracy = true
            },
            CancellationToken.None);

        await service.GenerateCasePlanAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        var editorUserId = Guid.NewGuid();
        const string editorEmail = "family.task.editor@agency.pt";
        await SeedUserAsync(dbContext, editorUserId, tenantId, editorEmail, "Family Editor");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, editorUserId, editorEmail, CaseRole.Editor);

        var step = await dbContext.WorkflowStepInstances
            .SingleAsync(x => x.CaseId == createdCase.CaseId && x.StepKey == "collect-civil-records");

        var response = await service.UpdateWorkflowTaskStatusAsync(
            tenantId,
            editorUserId,
            createdCase.CaseId,
            step.Id,
            new UpdateWorkflowTaskStatusRequest
            {
                TargetStatus = "Started",
                Notes = "Started by assigned editor."
            },
            CancellationToken.None);

        response.Tasks.Should().Contain(x => x.StepId == step.Id && x.Status == WorkflowStepStatus.InProgress);
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "WorkflowTaskStatusUpdated" && x.CaseId == createdCase.CaseId && x.ActorUserId == editorUserId);
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseNotificationQueued" && x.CaseId == createdCase.CaseId && x.ActorUserId == editorUserId);
    }

    [Fact]
    public async Task UpdateWorkflowTaskStatus_ShouldRecalculateReadiness_WhenTaskIsCompleted()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Task Completion Case");

        await service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = "Ana Pereira",
                PrimaryContactPhone = "+351919999999",
                RelationshipToDeceased = "Daughter",
                ConfirmAccuracy = true
            },
            CancellationToken.None);

        await service.GenerateCasePlanAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        var recordsStep = await dbContext.WorkflowStepInstances
            .SingleAsync(x => x.CaseId == createdCase.CaseId && x.StepKey == "collect-civil-records");
        var inventoryStep = await dbContext.WorkflowStepInstances
            .SingleAsync(x => x.CaseId == createdCase.CaseId && x.StepKey == "gather-estate-inventory");

        await service.UpdateWorkflowTaskStatusAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            recordsStep.Id,
            new UpdateWorkflowTaskStatusRequest { TargetStatus = "Completed" },
            CancellationToken.None);

        var response = await service.UpdateWorkflowTaskStatusAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            inventoryStep.Id,
            new UpdateWorkflowTaskStatusRequest { TargetStatus = "Completed" },
            CancellationToken.None);

        response.Tasks.Should().Contain(
            x => x.StepKey == "submit-succession-notification" && x.Status == WorkflowStepStatus.Ready);
    }

    [Fact]
    public async Task UpdateWorkflowTaskStatus_ShouldQueueMissingInputNotification_WhenTaskNeedsReview()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Missing Input Notification Case");

        await service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = "Ana Pereira",
                PrimaryContactPhone = "+351919999999",
                RelationshipToDeceased = "Daughter",
                ConfirmAccuracy = true
            },
            CancellationToken.None);

        await service.GenerateCasePlanAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        var step = await dbContext.WorkflowStepInstances
            .SingleAsync(x => x.CaseId == createdCase.CaseId && x.StepKey == "collect-civil-records");

        await service.UpdateWorkflowTaskStatusAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            step.Id,
            new UpdateWorkflowTaskStatusRequest
            {
                TargetStatus = "NeedsReview",
                Notes = "Awaiting signed declaration from family."
            },
            CancellationToken.None);

        var notificationEvents = dbContext.AuditEvents
            .Where(x => x.EventType == "CaseNotificationQueued" && x.CaseId == createdCase.CaseId)
            .ToList();
        notificationEvents.Count.Should().BeGreaterThanOrEqualTo(2);
        notificationEvents.Should().Contain(x => x.Metadata.Contains("MissingInputRequired"));
    }

    [Fact]
    public async Task UpdateWorkflowTaskStatus_ShouldThrowCaseAccessDeniedAndAudit_WhenUserIsReader()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Reader Task Update Case");

        await service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = "Ana Pereira",
                PrimaryContactPhone = "+351919999999",
                RelationshipToDeceased = "Daughter",
                ConfirmAccuracy = true
            },
            CancellationToken.None);

        await service.GenerateCasePlanAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        var readerUserId = Guid.NewGuid();
        const string readerEmail = "family.task.reader@agency.pt";
        await SeedUserAsync(dbContext, readerUserId, tenantId, readerEmail, "Family Reader");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, readerUserId, readerEmail, CaseRole.Reader);
        var step = await dbContext.WorkflowStepInstances
            .SingleAsync(x => x.CaseId == createdCase.CaseId && x.StepKey == "collect-civil-records");

        var act = () => service.UpdateWorkflowTaskStatusAsync(
            tenantId,
            readerUserId,
            createdCase.CaseId,
            step.Id,
            new UpdateWorkflowTaskStatusRequest { TargetStatus = "Started" },
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "UpdateWorkflowTaskStatus");
        AssertAccessDeniedAudit(
            dbContext,
            readerUserId,
            createdCase.CaseId,
            "UpdateWorkflowTaskStatus",
            "Editor",
            "Reader",
            "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task GetCaseAuditTrail_ShouldReturnEntriesAndRemainImmutable_WhenUserIsManager()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Audit Trail Case");

        await service.UpdateCaseDetailsAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UpdateCaseDetailsRequest
            {
                DeceasedFullName = "Audit Trail Case Updated",
                CaseType = "General",
                Urgency = "Normal"
            },
            CancellationToken.None);

        var beforeCount = dbContext.AuditEvents.Count(x => x.CaseId == createdCase.CaseId);
        var auditTrail = await service.GetCaseAuditTrailAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);
        var afterCount = dbContext.AuditEvents.Count(x => x.CaseId == createdCase.CaseId);

        auditTrail.CaseId.Should().Be(createdCase.CaseId);
        auditTrail.Entries.Should().NotBeEmpty();
        auditTrail.Entries[0].Action.Should().NotBeNullOrWhiteSpace();
        auditTrail.Entries[0].OccurredAt.Should().NotBe(default);
        auditTrail.Entries[0].ContextJson.Should().NotBeNullOrWhiteSpace();
        afterCount.Should().Be(beforeCount);
    }

    [Fact]
    public async Task GetCaseAuditTrail_ShouldThrowCaseAccessDeniedAndAudit_WhenUserIsReader()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Audit Trail Access Case");

        var readerUserId = Guid.NewGuid();
        const string readerEmail = "family.audit.reader@agency.pt";
        await SeedUserAsync(dbContext, readerUserId, tenantId, readerEmail, "Audit Reader");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, readerUserId, readerEmail, CaseRole.Reader);

        var act = () => service.GetCaseAuditTrailAsync(
            tenantId,
            readerUserId,
            createdCase.CaseId,
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "GetCaseAuditTrail");
        AssertAccessDeniedAudit(
            dbContext,
            readerUserId,
            createdCase.CaseId,
            "GetCaseAuditTrail",
            "Manager",
            "Reader",
            "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task GetCaseTimeline_ShouldReturnOrderedEventsAndCurrentOwners()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Timeline Ownership Case");

        await service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = "Ana Pereira",
                PrimaryContactPhone = "+351919999999",
                RelationshipToDeceased = "Daughter",
                ConfirmAccuracy = true
            },
            CancellationToken.None);

        await service.GenerateCasePlanAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        var step = await dbContext.WorkflowStepInstances
            .SingleAsync(x => x.CaseId == createdCase.CaseId && x.StepKey == "collect-civil-records");
        await service.UpdateWorkflowTaskStatusAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            step.Id,
            new UpdateWorkflowTaskStatusRequest
            {
                TargetStatus = "Started"
            },
            CancellationToken.None);

        var timeline = await service.GetCaseTimelineAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        timeline.CurrentOwners.Should().NotBeEmpty();
        timeline.CurrentOwners.Should().Contain(x => x.AssignedUserId == actorUserId);
        timeline.Events.Should().NotBeEmpty();
        timeline.Events.Should().BeInAscendingOrder(x => x.OccurredAt);
        timeline.Events.Should().Contain(x => x.EventType == "WorkflowTaskStatusUpdated");
        timeline.Events.Should().Contain(x => x.EventType == "CaseNotificationQueued");
    }

    [Fact]
    public async Task UploadCaseDocument_ShouldPersistDocumentAndAudit_WhenUserIsEditor()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Document Upload Case");
        var editorUserId = Guid.NewGuid();
        const string editorEmail = "family.docs.editor@agency.pt";
        await SeedUserAsync(dbContext, editorUserId, tenantId, editorEmail, "Family Editor");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, editorUserId, editorEmail, CaseRole.Editor);

        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("document-content"));
        var response = await service.UploadCaseDocumentAsync(
            tenantId,
            editorUserId,
            createdCase.CaseId,
            new UploadCaseDocumentRequest
            {
                FileName = "certificate.pdf",
                ContentType = "application/pdf",
                ContentBase64 = payload
            },
            CancellationToken.None);

        response.CaseId.Should().Be(createdCase.CaseId);
        response.FileName.Should().Be("certificate.pdf");
        response.SizeBytes.Should().Be(16);
        dbContext.CaseDocuments.Should().Contain(x => x.Id == response.DocumentId && x.CaseId == createdCase.CaseId);
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseDocumentUploaded" && x.CaseId == createdCase.CaseId && x.ActorUserId == editorUserId);
    }

    [Fact]
    public async Task UploadCaseDocument_ShouldThrowCaseAccessDeniedAndAudit_WhenUserIsReader()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Reader Upload Case");
        var readerUserId = Guid.NewGuid();
        const string readerEmail = "family.docs.reader@agency.pt";
        await SeedUserAsync(dbContext, readerUserId, tenantId, readerEmail, "Family Reader");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, readerUserId, readerEmail, CaseRole.Reader);

        var act = () => service.UploadCaseDocumentAsync(
            tenantId,
            readerUserId,
            createdCase.CaseId,
            new UploadCaseDocumentRequest
            {
                FileName = "certificate.pdf",
                ContentType = "application/pdf",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("content"))
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "UploadCaseDocument");
        AssertAccessDeniedAudit(
            dbContext,
            readerUserId,
            createdCase.CaseId,
            "UploadCaseDocument",
            "Editor",
            "Reader",
            "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task DownloadCaseDocument_ShouldReturnContentAndAudit_WhenUserIsReader()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Download Document Case");
        var readerUserId = Guid.NewGuid();
        const string readerEmail = "family.docs.download.reader@agency.pt";
        await SeedUserAsync(dbContext, readerUserId, tenantId, readerEmail, "Family Reader");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, readerUserId, readerEmail, CaseRole.Reader);

        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("download-content"));
        var uploaded = await service.UploadCaseDocumentAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UploadCaseDocumentRequest
            {
                FileName = "declaration.txt",
                ContentType = "text/plain",
                ContentBase64 = contentBase64
            },
            CancellationToken.None);

        var downloaded = await service.DownloadCaseDocumentAsync(
            tenantId,
            readerUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            CancellationToken.None);

        downloaded.DocumentId.Should().Be(uploaded.DocumentId);
        downloaded.ContentType.Should().Be("text/plain");
        downloaded.ContentBase64.Should().Be(contentBase64);
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseDocumentDownloaded" && x.CaseId == createdCase.CaseId && x.ActorUserId == readerUserId);
    }

    [Fact]
    public async Task UploadCaseDocumentVersion_ShouldIncrementVersionAndPersistLineage_WhenUserIsEditor()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Versioned Document Case");
        var editorUserId = Guid.NewGuid();
        const string editorEmail = "family.docs.version.editor@agency.pt";
        await SeedUserAsync(dbContext, editorUserId, tenantId, editorEmail, "Family Editor");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, editorUserId, editorEmail, CaseRole.Editor);

        var initialPayload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("v1-content"));
        var uploaded = await service.UploadCaseDocumentAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UploadCaseDocumentRequest
            {
                FileName = "statement.txt",
                ContentType = "text/plain",
                ContentBase64 = initialPayload
            },
            CancellationToken.None);

        var v2Payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("v2-content"));
        var versioned = await service.UploadCaseDocumentVersionAsync(
            tenantId,
            editorUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            new UploadCaseDocumentRequest
            {
                FileName = "statement-v2.txt",
                ContentType = "text/plain",
                ContentBase64 = v2Payload
            },
            CancellationToken.None);

        versioned.DocumentId.Should().Be(uploaded.DocumentId);
        versioned.VersionNumber.Should().Be(2);
        var storedDocument = dbContext.CaseDocuments.Single(x => x.Id == uploaded.DocumentId);
        storedDocument.CurrentVersionNumber.Should().Be(2);
        storedDocument.FileName.Should().Be("statement-v2.txt");
        dbContext.CaseDocumentVersions.Count(x => x.DocumentId == uploaded.DocumentId).Should().Be(2);
        dbContext.CaseDocumentVersions
            .Where(x => x.DocumentId == uploaded.DocumentId)
            .OrderBy(x => x.VersionNumber)
            .Select(x => x.VersionNumber)
            .Should()
            .Equal(1, 2);
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseDocumentVersionUploaded" && x.CaseId == createdCase.CaseId && x.ActorUserId == editorUserId);
    }

    [Fact]
    public async Task DownloadCaseDocument_ShouldDenyReader_WhenDocumentIsRestricted()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Restricted Download Case");
        var readerUserId = Guid.NewGuid();
        const string readerEmail = "family.docs.restricted.reader@agency.pt";
        await SeedUserAsync(dbContext, readerUserId, tenantId, readerEmail, "Family Reader");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, readerUserId, readerEmail, CaseRole.Reader);

        var uploaded = await service.UploadCaseDocumentAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UploadCaseDocumentRequest
            {
                FileName = "restricted.pdf",
                ContentType = "application/pdf",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("restricted-content"))
            },
            CancellationToken.None);

        await service.UpdateCaseDocumentClassificationAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            new UpdateCaseDocumentClassificationRequest { Classification = "Restricted" },
            CancellationToken.None);

        var act = () => service.DownloadCaseDocumentAsync(
            tenantId,
            readerUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "DownloadCaseDocument");
        AssertAccessDeniedAudit(
            dbContext,
            readerUserId,
            createdCase.CaseId,
            "DownloadCaseDocument",
            "Manager",
            "Reader",
            "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task UploadCaseDocumentVersion_ShouldDenyEditor_WhenDocumentIsRestricted()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Restricted Version Case");
        var editorUserId = Guid.NewGuid();
        const string editorEmail = "family.docs.restricted.editor@agency.pt";
        await SeedUserAsync(dbContext, editorUserId, tenantId, editorEmail, "Family Editor");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, editorUserId, editorEmail, CaseRole.Editor);

        var uploaded = await service.UploadCaseDocumentAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UploadCaseDocumentRequest
            {
                FileName = "protected.txt",
                ContentType = "text/plain",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("v1"))
            },
            CancellationToken.None);

        var classification = await service.UpdateCaseDocumentClassificationAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            new UpdateCaseDocumentClassificationRequest { Classification = "Restricted" },
            CancellationToken.None);

        classification.Classification.Should().Be("Restricted");

        var act = () => service.UploadCaseDocumentVersionAsync(
            tenantId,
            editorUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            new UploadCaseDocumentRequest
            {
                FileName = "protected-v2.txt",
                ContentType = "text/plain",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("v2"))
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "UploadCaseDocumentVersion");
        AssertAccessDeniedAudit(
            dbContext,
            editorUserId,
            createdCase.CaseId,
            "UploadCaseDocumentVersion",
            "Manager",
            "Editor",
            "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task GetCaseDocumentVersions_ShouldReturnOrderedVersionHistory()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Version History Case");

        var uploaded = await service.UploadCaseDocumentAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UploadCaseDocumentRequest
            {
                FileName = "timeline.txt",
                ContentType = "text/plain",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("v1"))
            },
            CancellationToken.None);

        await service.UploadCaseDocumentVersionAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            new UploadCaseDocumentRequest
            {
                FileName = "timeline-v2.txt",
                ContentType = "text/plain",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("v2"))
            },
            CancellationToken.None);

        await service.UploadCaseDocumentVersionAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            new UploadCaseDocumentRequest
            {
                FileName = "timeline-v3.txt",
                ContentType = "text/plain",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("v3"))
            },
            CancellationToken.None);

        var history = await service.GetCaseDocumentVersionsAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            CancellationToken.None);

        history.DocumentId.Should().Be(uploaded.DocumentId);
        history.LatestVersionNumber.Should().Be(3);
        history.Classification.Should().Be("Optional");
        history.Versions.Select(x => x.VersionNumber).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task GenerateOutboundTemplate_ShouldReturnMappedContentAndAudit_WhenIntakeIsCompleted()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Template Case");

        await service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new SubmitCaseIntakeRequest
            {
                PrimaryContactName = "Ana Pereira",
                PrimaryContactPhone = "+351919999999",
                RelationshipToDeceased = "Daughter",
                HasWill = true,
                RequiresLegalSupport = false,
                RequiresFinancialSupport = true,
                ConfirmAccuracy = true
            },
            CancellationToken.None);

        var generated = await service.GenerateOutboundTemplateAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new GenerateOutboundTemplateRequest { TemplateKey = "CaseSummaryLetter" },
            CancellationToken.None);

        generated.CaseId.Should().Be(createdCase.CaseId);
        generated.TemplateKey.Should().Be("CaseSummaryLetter");
        generated.ContentType.Should().Be("text/plain");
        var content = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(generated.ContentBase64));
        content.Should().Contain("CaseNumber: ");
        content.Should().Contain("DeceasedFullName: Template Case");
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseOutboundTemplateGenerated" && x.CaseId == createdCase.CaseId && x.ActorUserId == actorUserId);
    }

    [Fact]
    public async Task GenerateOutboundTemplate_ShouldThrowCaseStateException_WhenIntakeIsMissing()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Template Without Intake");

        var act = () => service.GenerateOutboundTemplateAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new GenerateOutboundTemplateRequest { TemplateKey = "CaseSummaryLetter" },
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseStateException>()
            .Where(e => e.Message.Contains("Structured intake must be completed"));
    }

    [Fact]
    public async Task GenerateOutboundTemplate_ShouldThrowCaseAccessDeniedAndAudit_WhenUserIsEditor()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Template Access Case");
        var editorUserId = Guid.NewGuid();
        const string editorEmail = "family.template.editor@agency.pt";
        await SeedUserAsync(dbContext, editorUserId, tenantId, editorEmail, "Template Editor");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, editorUserId, editorEmail, CaseRole.Editor);

        var act = () => service.GenerateOutboundTemplateAsync(
            tenantId,
            editorUserId,
            createdCase.CaseId,
            new GenerateOutboundTemplateRequest { TemplateKey = "CaseSummaryLetter" },
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "GenerateOutboundTemplate");
        AssertAccessDeniedAudit(
            dbContext,
            editorUserId,
            createdCase.CaseId,
            "GenerateOutboundTemplate",
            "Manager",
            "Editor",
            "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task ExtractDocumentCandidates_ShouldReturnPendingCandidatesAndAudit_WhenDocumentIsSupported()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Extraction Case");

        var content = string.Join(
            Environment.NewLine,
            "PrimaryContactName: Ana Pereira",
            "PrimaryContactPhone: +351910000000",
            "RelationshipToDeceased: Daughter",
            "DeceasedFullName: Maria Silva",
            "DateOfDeath: 2026-01-10");
        var uploaded = await service.UploadCaseDocumentAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UploadCaseDocumentRequest
            {
                FileName = "intake.txt",
                ContentType = "text/plain",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content))
            },
            CancellationToken.None);

        var extracted = await service.ExtractDocumentCandidatesAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            CancellationToken.None);

        extracted.DocumentId.Should().Be(uploaded.DocumentId);
        extracted.Candidates.Should().NotBeEmpty();
        extracted.Candidates.Should().OnlyContain(x => x.Status == "Pending");
        extracted.Candidates.Should().OnlyContain(x => x.ConfidenceScore > 0m && x.ConfidenceScore <= 1m);
        dbContext.ExtractionCandidates.Should().Contain(x => x.DocumentId == uploaded.DocumentId && x.Status == ExtractionCandidateStatus.Pending);
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseDocumentExtractionCompleted" && x.CaseId == createdCase.CaseId && x.ActorUserId == actorUserId);
    }

    [Fact]
    public async Task ExtractDocumentCandidates_ShouldThrowCaseStateException_WhenContentTypeIsUnsupported()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Unsupported Extraction Case");

        var uploaded = await service.UploadCaseDocumentAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UploadCaseDocumentRequest
            {
                FileName = "scan.pdf",
                ContentType = "application/pdf",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("pdf-binary-simulated"))
            },
            CancellationToken.None);

        var act = () => service.ExtractDocumentCandidatesAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseStateException>()
            .Where(e => e.Message.Contains("not supported for extraction"));
    }

    [Fact]
    public async Task ExtractDocumentCandidates_ShouldThrowCaseAccessDeniedAndAudit_WhenRestrictedDocumentAndUserIsEditor()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Restricted Extraction Case");
        var editorUserId = Guid.NewGuid();
        const string editorEmail = "family.extraction.editor@agency.pt";
        await SeedUserAsync(dbContext, editorUserId, tenantId, editorEmail, "Extraction Editor");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, editorUserId, editorEmail, CaseRole.Editor);

        var uploaded = await service.UploadCaseDocumentAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UploadCaseDocumentRequest
            {
                FileName = "restricted.txt",
                ContentType = "text/plain",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("PrimaryContactName: Ana"))
            },
            CancellationToken.None);

        await service.UpdateCaseDocumentClassificationAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            new UpdateCaseDocumentClassificationRequest { Classification = "Restricted" },
            CancellationToken.None);

        var act = () => service.ExtractDocumentCandidatesAsync(
            tenantId,
            editorUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "ExtractDocumentCandidates");
        AssertAccessDeniedAudit(
            dbContext,
            editorUserId,
            createdCase.CaseId,
            "ExtractDocumentCandidates",
            "Manager",
            "Editor",
            "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task ApplyExtractionDecisions_ShouldApplyOnlyApprovedValuesAndAudit()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Extraction Review Case");

        var content = string.Join(
            Environment.NewLine,
            "PrimaryContactName: Ana Pereira",
            "PrimaryContactPhone: +351910000000",
            "RelationshipToDeceased: Daughter");
        var uploaded = await service.UploadCaseDocumentAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UploadCaseDocumentRequest
            {
                FileName = "review.txt",
                ContentType = "text/plain",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content))
            },
            CancellationToken.None);

        var extracted = await service.ExtractDocumentCandidatesAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            CancellationToken.None);

        var nameCandidate = extracted.Candidates.Single(x => x.FieldKey == "PrimaryContactName");
        var phoneCandidate = extracted.Candidates.Single(x => x.FieldKey == "PrimaryContactPhone");
        var relationshipCandidate = extracted.Candidates.Single(x => x.FieldKey == "RelationshipToDeceased");

        var reviewed = await service.ApplyExtractionDecisionsAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            new ApplyExtractionDecisionsRequest
            {
                Decisions =
                [
                    new ExtractionDecisionRequest { CandidateId = nameCandidate.CandidateId, Action = "Approve" },
                    new ExtractionDecisionRequest { CandidateId = phoneCandidate.CandidateId, Action = "Edit", EditedValue = "+351911111111" },
                    new ExtractionDecisionRequest { CandidateId = relationshipCandidate.CandidateId, Action = "Reject" }
                ]
            },
            CancellationToken.None);

        reviewed.TotalDecisions.Should().Be(3);
        reviewed.AppliedCount.Should().Be(2);
        reviewed.RejectedCount.Should().Be(1);
        reviewed.Candidates.Should().Contain(x => x.CandidateId == nameCandidate.CandidateId && x.Status == "Approved");
        reviewed.Candidates.Should().Contain(x => x.CandidateId == phoneCandidate.CandidateId && x.Status == "Approved" && x.CandidateValue == "+351911111111");
        reviewed.Candidates.Should().Contain(x => x.CandidateId == relationshipCandidate.CandidateId && x.Status == "Rejected");

        var persistedCase = dbContext.Cases.Single(x => x.Id == createdCase.CaseId);
        persistedCase.IntakeData.Should().NotBeNullOrWhiteSpace();
        using var intakeDoc = JsonDocument.Parse(persistedCase.IntakeData!);
        intakeDoc.RootElement.GetProperty("PrimaryContactName").GetString().Should().Be("Ana Pereira");
        intakeDoc.RootElement.GetProperty("PrimaryContactPhone").GetString().Should().Be("+351911111111");
        intakeDoc.RootElement.TryGetProperty("RelationshipToDeceased", out _).Should().BeFalse();

        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseExtractionDecisionsReviewed" && x.CaseId == createdCase.CaseId && x.ActorUserId == actorUserId);
    }

    [Fact]
    public async Task ApplyExtractionDecisions_ShouldThrowCaseStateException_WhenNoPendingCandidatesExist()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "No Pending Candidates Case");

        var uploaded = await service.UploadCaseDocumentAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UploadCaseDocumentRequest
            {
                FileName = "raw.txt",
                ContentType = "text/plain",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("PrimaryContactName: Ana"))
            },
            CancellationToken.None);

        var act = () => service.ApplyExtractionDecisionsAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            new ApplyExtractionDecisionsRequest
            {
                Decisions =
                [
                    new ExtractionDecisionRequest { CandidateId = Guid.NewGuid(), Action = "Approve" }
                ]
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseStateException>()
            .Where(e => e.Message.Contains("No pending extraction candidates"));
    }

    [Fact]
    public async Task ApplyExtractionDecisions_ShouldThrowCaseAccessDeniedAndAudit_WhenUserIsEditor()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Extraction Review Access Case");
        var editorUserId = Guid.NewGuid();
        const string editorEmail = "family.extraction.review.editor@agency.pt";
        await SeedUserAsync(dbContext, editorUserId, tenantId, editorEmail, "Extraction Review Editor");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, editorUserId, editorEmail, CaseRole.Editor);

        var uploaded = await service.UploadCaseDocumentAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UploadCaseDocumentRequest
            {
                FileName = "review-access.txt",
                ContentType = "text/plain",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("PrimaryContactName: Ana"))
            },
            CancellationToken.None);

        var extracted = await service.ExtractDocumentCandidatesAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            CancellationToken.None);

        var act = () => service.ApplyExtractionDecisionsAsync(
            tenantId,
            editorUserId,
            createdCase.CaseId,
            uploaded.DocumentId,
            new ApplyExtractionDecisionsRequest
            {
                Decisions =
                [
                    new ExtractionDecisionRequest
                    {
                        CandidateId = extracted.Candidates.First().CandidateId,
                        Action = "Approve"
                    }
                ]
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "ApplyExtractionDecisions");
        AssertAccessDeniedAudit(
            dbContext,
            editorUserId,
            createdCase.CaseId,
            "ApplyExtractionDecisions",
            "Manager",
            "Editor",
            "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task GenerateCaseHandoffPacket_ShouldReturnRequiredActionsAndEvidenceAndAudit_WhenPrerequisitesAreMet()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Handoff Ready Case");

        await service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
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
            CancellationToken.None);

        await service.UpdateCaseLifecycleAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UpdateCaseLifecycleRequest { TargetStatus = "Active" },
            CancellationToken.None);

        await service.GenerateCasePlanAsync(tenantId, actorUserId, createdCase.CaseId, CancellationToken.None);

        await service.UploadCaseDocumentAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UploadCaseDocumentRequest
            {
                FileName = "evidence.txt",
                ContentType = "text/plain",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("evidence-data"))
            },
            CancellationToken.None);

        var packet = await service.GenerateCaseHandoffPacketAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        packet.CaseId.Should().Be(createdCase.CaseId);
        packet.RequiredActions.Should().NotBeEmpty();
        packet.EvidenceContext.Should().NotBeEmpty();
        var content = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(packet.ContentBase64));
        content.Should().Contain("Required Actions:");
        content.Should().Contain("Evidence Context:");
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseHandoffPacketGenerated" && x.CaseId == createdCase.CaseId && x.ActorUserId == actorUserId);
    }

    [Fact]
    public async Task GenerateCaseHandoffPacket_ShouldThrowCaseStateException_WhenCaseIsNotActive()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Handoff Inactive Case");

        var act = () => service.GenerateCaseHandoffPacketAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseStateException>()
            .Where(e => e.Message.Contains("only be generated for active cases"));
    }

    [Fact]
    public async Task GenerateCaseHandoffPacket_ShouldThrowCaseStateException_WhenEvidenceIsMissing()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Handoff Missing Evidence Case");

        await service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
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
            CancellationToken.None);

        await service.UpdateCaseLifecycleAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UpdateCaseLifecycleRequest { TargetStatus = "Active" },
            CancellationToken.None);

        await service.GenerateCasePlanAsync(tenantId, actorUserId, createdCase.CaseId, CancellationToken.None);

        var act = () => service.GenerateCaseHandoffPacketAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseStateException>()
            .Where(e => e.Message.Contains("At least one case document is required"));
    }

    [Fact]
    public async Task GenerateCaseHandoffPacket_ShouldThrowCaseAccessDeniedAndAudit_WhenUserIsEditor()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Handoff Access Case");
        var editorUserId = Guid.NewGuid();
        const string editorEmail = "family.handoff.editor@agency.pt";
        await SeedUserAsync(dbContext, editorUserId, tenantId, editorEmail, "Handoff Editor");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, editorUserId, editorEmail, CaseRole.Editor);

        var act = () => service.GenerateCaseHandoffPacketAsync(
            tenantId,
            editorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "GenerateCaseHandoffPacket");
        AssertAccessDeniedAudit(
            dbContext,
            editorUserId,
            createdCase.CaseId,
            "GenerateCaseHandoffPacket",
            "Manager",
            "Editor",
            "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task GetCaseHandoffState_ShouldReturnLatestState_WhenHandoffExists()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Handoff State Case");

        await service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
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
            CancellationToken.None);

        await service.UpdateCaseLifecycleAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UpdateCaseLifecycleRequest { TargetStatus = "Active" },
            CancellationToken.None);

        await service.GenerateCasePlanAsync(tenantId, actorUserId, createdCase.CaseId, CancellationToken.None);

        await service.UploadCaseDocumentAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UploadCaseDocumentRequest
            {
                FileName = "handoff-state-evidence.txt",
                ContentType = "text/plain",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("evidence-data"))
            },
            CancellationToken.None);

        var packet = await service.GenerateCaseHandoffPacketAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        var state = await service.GetCaseHandoffStateAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        state.CaseId.Should().Be(createdCase.CaseId);
        state.HandoffId.Should().Be(packet.HandoffId);
        state.Status.Should().Be(CaseHandoffStatus.PendingAdvisor.ToString());
        state.FollowUpRequired.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCaseHandoffState_ShouldPersistChangesAndAudit_WhenRequestIsValid()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Handoff Update Case");

        await service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
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
            CancellationToken.None);

        await service.UpdateCaseLifecycleAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UpdateCaseLifecycleRequest { TargetStatus = "Active" },
            CancellationToken.None);

        await service.GenerateCasePlanAsync(tenantId, actorUserId, createdCase.CaseId, CancellationToken.None);

        await service.UploadCaseDocumentAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UploadCaseDocumentRequest
            {
                FileName = "handoff-update-evidence.txt",
                ContentType = "text/plain",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("evidence-data"))
            },
            CancellationToken.None);

        var packet = await service.GenerateCaseHandoffPacketAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        var result = await service.UpdateCaseHandoffStateAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            packet.HandoffId,
            new UpdateCaseHandoffStateRequest
            {
                Status = "Completed",
                Notes = "Advisor confirmed completion."
            },
            CancellationToken.None);

        result.HandoffId.Should().Be(packet.HandoffId);
        result.Status.Should().Be(CaseHandoffStatus.Completed.ToString());
        result.FollowUpRequired.Should().BeFalse();
        result.StatusNotes.Should().Be("Advisor confirmed completion.");

        var persistedHandoff = await dbContext.CaseHandoffs.SingleAsync(x => x.Id == packet.HandoffId);
        persistedHandoff.Status.Should().Be(CaseHandoffStatus.Completed);
        persistedHandoff.FollowUpRequired.Should().BeFalse();
        persistedHandoff.StatusNotes.Should().Be("Advisor confirmed completion.");
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "CaseHandoffStateUpdated" && x.CaseId == createdCase.CaseId && x.ActorUserId == actorUserId);
    }

    [Fact]
    public async Task UpdateCaseHandoffState_ShouldThrowCaseAccessDeniedAndAudit_WhenUserIsReader()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Handoff Reader Access Case");
        var readerUserId = Guid.NewGuid();
        const string readerEmail = "family.handoff.reader@agency.pt";
        await SeedUserAsync(dbContext, readerUserId, tenantId, readerEmail, "Handoff Reader");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, readerUserId, readerEmail, CaseRole.Reader);

        await service.SubmitCaseIntakeAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
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
            CancellationToken.None);

        await service.UpdateCaseLifecycleAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UpdateCaseLifecycleRequest { TargetStatus = "Active" },
            CancellationToken.None);

        await service.GenerateCasePlanAsync(tenantId, actorUserId, createdCase.CaseId, CancellationToken.None);

        await service.UploadCaseDocumentAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            new UploadCaseDocumentRequest
            {
                FileName = "handoff-reader-evidence.txt",
                ContentType = "text/plain",
                ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("evidence-data"))
            },
            CancellationToken.None);

        var packet = await service.GenerateCaseHandoffPacketAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        var act = () => service.UpdateCaseHandoffStateAsync(
            tenantId,
            readerUserId,
            createdCase.CaseId,
            packet.HandoffId,
            new UpdateCaseHandoffStateRequest
            {
                Status = "Completed"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "UpdateCaseHandoffState");
        AssertAccessDeniedAudit(
            dbContext,
            readerUserId,
            createdCase.CaseId,
            "UpdateCaseHandoffState",
            "Manager",
            "Reader",
            "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task ProvisionProcessAlias_ShouldCreateUniqueAliasAndAudit_WhenCaseIsEligible()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Process Alias Provision Case");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);

        var alias = await service.ProvisionProcessAliasAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        alias.CaseId.Should().Be(createdCase.CaseId);
        alias.Status.Should().Be(ProcessAliasStatus.Active.ToString());
        alias.AliasEmail.Should().StartWith("process-");
        alias.AliasEmail.Should().EndWith("@sanzy.ai");

        var persisted = await dbContext.ProcessAliases.SingleAsync(x => x.Id == alias.AliasId);
        persisted.Status.Should().Be(ProcessAliasStatus.Active);
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "ProcessAliasCreated" && x.CaseId == createdCase.CaseId && x.ActorUserId == actorUserId);
    }

    [Fact]
    public async Task RotateProcessAlias_ShouldMarkPreviousAsRotatedAndCreateNewActiveAlias()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Process Alias Rotate Case");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);

        var initialAlias = await service.ProvisionProcessAliasAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        var rotatedAlias = await service.RotateProcessAliasAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        rotatedAlias.Status.Should().Be(ProcessAliasStatus.Active.ToString());
        rotatedAlias.AliasId.Should().NotBe(initialAlias.AliasId);
        rotatedAlias.RotatedFromAliasId.Should().Be(initialAlias.AliasId);
        rotatedAlias.AliasEmail.Should().NotBe(initialAlias.AliasEmail);

        var previous = await dbContext.ProcessAliases.SingleAsync(x => x.Id == initialAlias.AliasId);
        previous.Status.Should().Be(ProcessAliasStatus.Rotated);
        var current = await dbContext.ProcessAliases.SingleAsync(x => x.Id == rotatedAlias.AliasId);
        current.Status.Should().Be(ProcessAliasStatus.Active);
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "ProcessAliasRotated" && x.CaseId == createdCase.CaseId && x.ActorUserId == actorUserId);
    }

    [Fact]
    public async Task DeactivateAndArchiveProcessAlias_ShouldPersistLifecycleAndAudit()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Process Alias Lifecycle Case");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);

        var provisioned = await service.ProvisionProcessAliasAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        var deactivated = await service.DeactivateProcessAliasAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        deactivated.AliasId.Should().Be(provisioned.AliasId);
        deactivated.Status.Should().Be(ProcessAliasStatus.Deactivated.ToString());

        var archived = await service.ArchiveProcessAliasAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        archived.AliasId.Should().Be(provisioned.AliasId);
        archived.Status.Should().Be(ProcessAliasStatus.Archived.ToString());

        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "ProcessAliasDeactivated" && x.CaseId == createdCase.CaseId && x.ActorUserId == actorUserId);
        dbContext.AuditEvents.Should().Contain(
            x => x.EventType == "ProcessAliasArchived" && x.CaseId == createdCase.CaseId && x.ActorUserId == actorUserId);
    }

    [Fact]
    public async Task ProvisionProcessAlias_ShouldThrowCaseAccessDeniedAndAudit_WhenUserIsReader()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Process Alias Access Case");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);

        var readerUserId = Guid.NewGuid();
        const string readerEmail = "family.process.alias.reader@agency.pt";
        await SeedUserAsync(dbContext, readerUserId, tenantId, readerEmail, "Alias Reader");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, readerUserId, readerEmail, CaseRole.Reader);

        var act = () => service.ProvisionProcessAliasAsync(
            tenantId,
            readerUserId,
            createdCase.CaseId,
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "ROLE_INSUFFICIENT" && e.AttemptedAction == "ProvisionProcessAlias");
        AssertAccessDeniedAudit(
            dbContext,
            readerUserId,
            createdCase.CaseId,
            "ProvisionProcessAlias",
            "Manager",
            "Reader",
            "ROLE_INSUFFICIENT");
    }

    [Fact]
    public async Task GetProcessInbox_ShouldReturnThreadHistoryAndMetadata_WhenMessagesExist()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Process Inbox Case");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);
        var alias = await service.ProvisionProcessAliasAsync(tenantId, actorUserId, createdCase.CaseId, CancellationToken.None);
        var now = DateTime.UtcNow;

        dbContext.ProcessEmails.AddRange(
            new ProcessEmail
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CaseId = createdCase.CaseId,
                ProcessAliasId = alias.AliasId,
                ThreadId = "thread-a",
                Direction = ProcessEmailDirection.Outbound,
                Subject = "Document request",
                SenderEmail = alias.AliasEmail,
                RecipientEmails = "advisor.one@external.pt",
                BodyPreview = "Please share pending documentation.",
                ExternalMessageId = "msg-001",
                SentAt = now.AddMinutes(-15),
                CreatedAt = now.AddMinutes(-15),
                UpdatedAt = now.AddMinutes(-15)
            },
            new ProcessEmail
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CaseId = createdCase.CaseId,
                ProcessAliasId = alias.AliasId,
                ThreadId = "thread-a",
                Direction = ProcessEmailDirection.Inbound,
                Subject = "Re: Document request",
                SenderEmail = "advisor.one@external.pt",
                RecipientEmails = alias.AliasEmail,
                BodyPreview = "Attached for your review.",
                ExternalMessageId = "msg-002",
                SentAt = now.AddMinutes(-5),
                CreatedAt = now.AddMinutes(-5),
                UpdatedAt = now.AddMinutes(-5)
            },
            new ProcessEmail
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CaseId = createdCase.CaseId,
                ProcessAliasId = alias.AliasId,
                ThreadId = "thread-b",
                Direction = ProcessEmailDirection.Outbound,
                Subject = "Tax authority follow-up",
                SenderEmail = alias.AliasEmail,
                RecipientEmails = "tax.office@external.pt",
                BodyPreview = "Following up on the previous request.",
                ExternalMessageId = "msg-003",
                SentAt = now.AddMinutes(-10),
                CreatedAt = now.AddMinutes(-10),
                UpdatedAt = now.AddMinutes(-10)
            });
        await dbContext.SaveChangesAsync();

        var inbox = await service.GetProcessInboxAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        inbox.CaseId.Should().Be(createdCase.CaseId);
        inbox.Threads.Should().HaveCount(2);
        inbox.Threads[0].ThreadId.Should().Be("thread-a");
        inbox.Threads[0].MessageCount.Should().Be(2);
        inbox.Threads[0].LatestDirection.Should().Be(ProcessEmailDirection.Inbound.ToString());
        inbox.Threads[0].CaseContextUrl.Should().Contain(createdCase.CaseId.ToString());
        inbox.Threads[0].Participants.Should().Contain(alias.AliasEmail);
        inbox.Threads[0].Participants.Should().Contain("advisor.one@external.pt");
        inbox.Threads[0].Messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProcessInbox_ShouldThrowCaseStateException_WhenAliasIsNotProvisioned()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Process Inbox Without Alias Case");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);

        var act = () => service.GetProcessInboxAsync(
            tenantId,
            actorUserId,
            createdCase.CaseId,
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseStateException>()
            .Where(e => e.Message.Contains("Process alias must be provisioned"));
    }

    [Fact]
    public async Task GetProcessInbox_ShouldAllowReader_WhenCaseScopedAccessIsGranted()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Process Inbox Reader Case");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);
        var alias = await service.ProvisionProcessAliasAsync(tenantId, actorUserId, createdCase.CaseId, CancellationToken.None);

        var readerUserId = Guid.NewGuid();
        const string readerEmail = "family.process.inbox.reader@agency.pt";
        await SeedUserAsync(dbContext, readerUserId, tenantId, readerEmail, "Inbox Reader");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, readerUserId, readerEmail, CaseRole.Reader);

        dbContext.ProcessEmails.Add(
            new ProcessEmail
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CaseId = createdCase.CaseId,
                ProcessAliasId = alias.AliasId,
                ThreadId = "thread-reader",
                Direction = ProcessEmailDirection.Outbound,
                Subject = "Reader-visible thread",
                SenderEmail = alias.AliasEmail,
                RecipientEmails = readerEmail,
                BodyPreview = "Shared update.",
                ExternalMessageId = "msg-r1",
                SentAt = DateTime.UtcNow.AddMinutes(-2),
                CreatedAt = DateTime.UtcNow.AddMinutes(-2),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-2)
            });
        await dbContext.SaveChangesAsync();

        var inbox = await service.GetProcessInboxAsync(
            tenantId,
            readerUserId,
            createdCase.CaseId,
            CancellationToken.None);

        inbox.Threads.Should().HaveCount(1);
        inbox.Threads[0].ThreadId.Should().Be("thread-reader");
        inbox.Threads[0].Messages.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetProcessInbox_ShouldThrowCaseAccessDeniedAndAudit_WhenReaderHasNoVisibleThreads()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Process Inbox Reader Denied Case");
        await MoveCaseToActiveAsync(service, tenantId, actorUserId, createdCase.CaseId);
        var alias = await service.ProvisionProcessAliasAsync(tenantId, actorUserId, createdCase.CaseId, CancellationToken.None);

        var readerUserId = Guid.NewGuid();
        const string readerEmail = "family.process.inbox.denied@agency.pt";
        await SeedUserAsync(dbContext, readerUserId, tenantId, readerEmail, "Inbox Reader Denied");
        await SeedAcceptedParticipantDirectAsync(dbContext, tenantId, createdCase.CaseId, readerUserId, readerEmail, CaseRole.Reader);

        dbContext.ProcessEmails.Add(
            new ProcessEmail
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CaseId = createdCase.CaseId,
                ProcessAliasId = alias.AliasId,
                ThreadId = "thread-hidden",
                Direction = ProcessEmailDirection.Outbound,
                Subject = "Hidden thread",
                SenderEmail = alias.AliasEmail,
                RecipientEmails = "advisor.hidden@external.pt",
                BodyPreview = "Not visible to reader.",
                ExternalMessageId = "msg-hidden",
                SentAt = DateTime.UtcNow.AddMinutes(-2),
                CreatedAt = DateTime.UtcNow.AddMinutes(-2),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-2)
            });
        await dbContext.SaveChangesAsync();

        var act = () => service.GetProcessInboxAsync(
            tenantId,
            readerUserId,
            createdCase.CaseId,
            CancellationToken.None);

        await act.Should().ThrowAsync<CaseAccessDeniedException>()
            .Where(e => e.ReasonCode == "NO_INBOX_THREAD_ACCESS" && e.AttemptedAction == "GetProcessInbox");
        AssertAccessDeniedAudit(
            dbContext,
            readerUserId,
            createdCase.CaseId,
            "GetProcessInbox",
            "InboxParticipant",
            "Reader",
            "NO_INBOX_THREAD_ACCESS");
    }

    [Fact]
    public async Task GetCaseDetails_ShouldReturnDetails_WhenUserIsCaseManager()
    {
        var dbContext = CreateContext();
        var (tenantId, actorUserId) = await SeedTenantWithAdminAsync(dbContext, TenantStatus.Active);
        var service = CreateService(dbContext);
        var createdCase = await CreateCaseAsync(service, tenantId, actorUserId, "Manager Details Case");

        // actorUserId is both tenant admin and case manager — test via case manager path
        var result = await service.GetCaseDetailsAsync(tenantId, actorUserId, createdCase.CaseId, CancellationToken.None);

        result.CaseId.Should().Be(createdCase.CaseId);
        result.ManagerUserId.Should().Be(actorUserId);
    }

    private static SanzuDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SanzuDbContext>()
            .UseInMemoryDatabase($"sanzu-case-service-tests-{Guid.NewGuid()}")
            .Options;

        return new SanzuDbContext(options);
    }

    private static async Task<(Guid TenantId, Guid UserId)> SeedTenantWithAdminAsync(
        SanzuDbContext dbContext,
        TenantStatus status)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await SeedTenantAsync(dbContext, tenantId, status);
        await SeedUserAsync(dbContext, userId, tenantId);
        dbContext.UserRoles.Add(
            new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RoleType = PlatformRole.AgencyAdmin,
                TenantId = tenantId,
                GrantedBy = userId,
                GrantedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
        return (tenantId, userId);
    }

    private static async Task SeedTenantAsync(SanzuDbContext dbContext, Guid tenantId, TenantStatus status)
    {
        dbContext.Organizations.Add(
            new Organization
            {
                Id = tenantId,
                Name = $"Tenant-{tenantId:N}",
                Location = "Lisbon",
                Status = status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedUserAsync(
        SanzuDbContext dbContext,
        Guid userId,
        Guid tenantId,
        string? email = null,
        string? fullName = null)
    {
        dbContext.Users.Add(
            new User
            {
                Id = userId,
                Email = email ?? $"user-{userId:N}@agency.pt",
                FullName = fullName ?? "Manager User",
                OrgId = tenantId,
                CreatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedAcceptedParticipantDirectAsync(
        SanzuDbContext dbContext,
        Guid tenantId,
        Guid caseId,
        Guid participantUserId,
        string email,
        CaseRole role)
    {
        dbContext.CaseParticipants.Add(
            new CaseParticipant
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CaseId = caseId,
                Email = email.ToLowerInvariant(),
                Role = role,
                Status = CaseParticipantStatus.Accepted,
                InvitedByUserId = participantUserId,
                ParticipantUserId = participantUserId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                AcceptedAt = DateTime.UtcNow,
                TokenHash = "DIRECT_SEED",
                CreatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }

    private static Task<CreateCaseResponse> CreateCaseAsync(
        CaseService service,
        Guid tenantId,
        Guid actorUserId,
        string deceasedFullName)
    {
        return service.CreateCaseAsync(
            tenantId,
            actorUserId,
            new CreateCaseRequest
            {
                DeceasedFullName = deceasedFullName,
                DateOfDeath = DateTime.UtcNow.AddDays(-3),
                CaseType = "General",
                Urgency = "Normal",
                Notes = "Seed case"
            },
            CancellationToken.None);
    }

    private static async Task MoveCaseToActiveAsync(
        CaseService service,
        Guid tenantId,
        Guid actorUserId,
        Guid caseId)
    {
        await service.UpdateCaseLifecycleAsync(
            tenantId,
            actorUserId,
            caseId,
            new UpdateCaseLifecycleRequest { TargetStatus = "Intake" },
            CancellationToken.None);

        await service.UpdateCaseLifecycleAsync(
            tenantId,
            actorUserId,
            caseId,
            new UpdateCaseLifecycleRequest { TargetStatus = "Active" },
            CancellationToken.None);
    }

    private static CaseService CreateService(SanzuDbContext dbContext)
    {
        return new CaseService(
            new OrganizationRepository(dbContext),
            new UserRoleRepository(dbContext),
            new UserRepository(dbContext),
            new CaseRepository(dbContext),
            new CaseDocumentRepository(dbContext),
            new CaseHandoffRepository(dbContext),
            new ProcessAliasRepository(dbContext),
            new ProcessEmailRepository(dbContext),
            new ExtractionCandidateRepository(dbContext),
            new CaseParticipantRepository(dbContext),
            new WorkflowStepRepository(dbContext),
            new AuditRepository(dbContext),
            new EfUnitOfWork(dbContext),
            new CreateCaseRequestValidator(),
            new SubmitCaseIntakeRequestValidator(),
            new ApplyExtractionDecisionsRequestValidator(),
            new GenerateOutboundTemplateRequestValidator(),
            new UpdateCaseHandoffStateRequestValidator(),
            new UploadCaseDocumentRequestValidator(),
            new UpdateCaseDocumentClassificationRequestValidator(),
            new OverrideWorkflowStepReadinessRequestValidator(),
            new UpdateWorkflowTaskStatusRequestValidator(),
            new UpdateCaseDetailsRequestValidator(),
            new UpdateCaseLifecycleRequestValidator(),
            new InviteCaseParticipantRequestValidator(),
            new AcceptCaseParticipantInvitationRequestValidator(),
            new UpdateCaseParticipantRoleRequestValidator());
    }

    private static async Task<Guid> SeedAcceptedParticipantAsync(
        SanzuDbContext dbContext,
        CaseService service,
        Guid tenantId,
        Guid adminUserId,
        Guid caseId,
        string email,
        string role)
    {
        var participantUserId = Guid.NewGuid();
        await SeedUserAsync(dbContext, participantUserId, tenantId, email, "Participant User");

        var invitation = await service.InviteCaseParticipantAsync(
            tenantId,
            adminUserId,
            caseId,
            new InviteCaseParticipantRequest
            {
                Email = email,
                Role = role,
                ExpirationDays = 7
            },
            CancellationToken.None);

        await service.AcceptCaseParticipantInvitationAsync(
            tenantId,
            participantUserId,
            caseId,
            invitation.Participant.ParticipantId,
            new AcceptCaseParticipantInvitationRequest
            {
                InvitationToken = invitation.InvitationToken
            },
            CancellationToken.None);

        return participantUserId;
    }

    private static void AssertAccessDeniedAudit(
        SanzuDbContext dbContext,
        Guid actorUserId,
        Guid caseId,
        string attemptedAction,
        string requiredRole,
        string? actualRole,
        string reasonCode)
    {
        var auditEvent = dbContext.AuditEvents
            .Where(x => x.EventType == "CaseAccessDenied" && x.CaseId == caseId && x.ActorUserId == actorUserId)
            .OrderByDescending(x => x.CreatedAt)
            .First();

        auditEvent.Should().NotBeNull();
        using var doc = JsonDocument.Parse(auditEvent.Metadata);
        var root = doc.RootElement;
        root.GetProperty("AttemptedAction").GetString().Should().Be(attemptedAction);
        root.GetProperty("RequiredRole").GetString().Should().Be(requiredRole);
        root.GetProperty("ReasonCode").GetString().Should().Be(reasonCode);

        if (actualRole is not null)
        {
            root.GetProperty("ActualRole").GetString().Should().Be(actualRole);
        }
        else
        {
            root.GetProperty("ActualRole").ValueKind.Should().Be(JsonValueKind.Null);
        }
    }
}
