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
            new CaseParticipantRepository(dbContext),
            new WorkflowStepRepository(dbContext),
            new AuditRepository(dbContext),
            new EfUnitOfWork(dbContext),
            new CreateCaseRequestValidator(),
            new SubmitCaseIntakeRequestValidator(),
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
