using FluentAssertions;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Validators;

namespace Sanzu.Tests.Unit.Validators;

public sealed class CaseValidatorsTests
{
    [Fact]
    public void CreateCaseValidator_ShouldPass_WhenRequestIsValid()
    {
        var validator = new CreateCaseRequestValidator();
        var request = new CreateCaseRequest
        {
            DeceasedFullName = "Antonio Costa",
            DateOfDeath = DateTime.UtcNow.AddDays(-1),
            CaseType = "General",
            Urgency = "Normal"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateCaseValidator_ShouldFail_WhenDeceasedNameIsMissing()
    {
        var validator = new CreateCaseRequestValidator();
        var request = new CreateCaseRequest
        {
            DeceasedFullName = string.Empty,
            DateOfDeath = DateTime.UtcNow.AddDays(-1)
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateCaseRequest.DeceasedFullName));
    }

    [Fact]
    public void CreateCaseValidator_ShouldFail_WhenDateOfDeathIsInFuture()
    {
        var validator = new CreateCaseRequestValidator();
        var request = new CreateCaseRequest
        {
            DeceasedFullName = "Future Case",
            DateOfDeath = DateTime.UtcNow.AddDays(1)
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateCaseRequest.DateOfDeath));
    }

    [Fact]
    public void CreateCaseValidator_ShouldFail_WhenUrgencyIsUnsupported()
    {
        var validator = new CreateCaseRequestValidator();
        var request = new CreateCaseRequest
        {
            DeceasedFullName = "Unsupported Urgency",
            DateOfDeath = DateTime.UtcNow.AddDays(-2),
            Urgency = "Critical"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateCaseRequest.Urgency));
    }

    [Fact]
    public void UpdateCaseDetailsValidator_ShouldPass_WhenAtLeastOneFieldIsProvided()
    {
        var validator = new UpdateCaseDetailsRequestValidator();
        var request = new UpdateCaseDetailsRequest
        {
            Notes = "Updated details"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateCaseDetailsValidator_ShouldFail_WhenNoFieldsAreProvided()
    {
        var validator = new UpdateCaseDetailsRequestValidator();
        var request = new UpdateCaseDetailsRequest();

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("At least one case field"));
    }

    [Fact]
    public void UpdateCaseLifecycleValidator_ShouldPass_WhenTargetStatusIsAllowed()
    {
        var validator = new UpdateCaseLifecycleRequestValidator();
        var request = new UpdateCaseLifecycleRequest
        {
            TargetStatus = "Closed"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateCaseLifecycleValidator_ShouldFail_WhenTargetStatusIsUnsupported()
    {
        var validator = new UpdateCaseLifecycleRequestValidator();
        var request = new UpdateCaseLifecycleRequest
        {
            TargetStatus = "Cancelled"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateCaseLifecycleRequest.TargetStatus));
    }

    [Fact]
    public void SubmitCaseIntakeValidator_ShouldPass_WhenRequestIsValid()
    {
        var validator = new SubmitCaseIntakeRequestValidator();
        var request = new SubmitCaseIntakeRequest
        {
            PrimaryContactName = "Ana Pereira",
            PrimaryContactPhone = "+351910000000",
            RelationshipToDeceased = "Daughter",
            HasWill = true,
            ConfirmAccuracy = true
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SubmitCaseIntakeValidator_ShouldFail_WhenConfirmationIsMissing()
    {
        var validator = new SubmitCaseIntakeRequestValidator();
        var request = new SubmitCaseIntakeRequest
        {
            PrimaryContactName = "Ana Pereira",
            PrimaryContactPhone = "+351910000000",
            RelationshipToDeceased = "Daughter",
            ConfirmAccuracy = false
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(SubmitCaseIntakeRequest.ConfirmAccuracy));
    }

    [Fact]
    public void InviteCaseParticipantValidator_ShouldPass_WhenPayloadIsValid()
    {
        var validator = new InviteCaseParticipantRequestValidator();
        var request = new InviteCaseParticipantRequest
        {
            Email = "family.editor@agency.pt",
            Role = "Editor",
            ExpirationDays = 7
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void InviteCaseParticipantValidator_ShouldFail_WhenRoleIsUnsupported()
    {
        var validator = new InviteCaseParticipantRequestValidator();
        var request = new InviteCaseParticipantRequest
        {
            Email = "family.editor@agency.pt",
            Role = "AgencyAdmin"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(InviteCaseParticipantRequest.Role));
    }

    [Fact]
    public void AcceptCaseParticipantInvitationValidator_ShouldFail_WhenTokenIsMissing()
    {
        var validator = new AcceptCaseParticipantInvitationRequestValidator();
        var request = new AcceptCaseParticipantInvitationRequest();

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(AcceptCaseParticipantInvitationRequest.InvitationToken));
    }

    [Fact]
    public void UpdateCaseParticipantRoleValidator_ShouldFail_WhenRoleIsMissing()
    {
        var validator = new UpdateCaseParticipantRoleRequestValidator();
        var request = new UpdateCaseParticipantRoleRequest();

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateCaseParticipantRoleRequest.Role));
    }

    [Fact]
    public void OverrideWorkflowStepReadinessValidator_ShouldPass_WhenTargetStatusAndRationaleAreValid()
    {
        var validator = new OverrideWorkflowStepReadinessRequestValidator();
        var request = new OverrideWorkflowStepReadinessRequest
        {
            TargetStatus = "Ready",
            Rationale = "Manual unblock approved by manager."
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void OverrideWorkflowStepReadinessValidator_ShouldFail_WhenTargetStatusIsUnsupported()
    {
        var validator = new OverrideWorkflowStepReadinessRequestValidator();
        var request = new OverrideWorkflowStepReadinessRequest
        {
            TargetStatus = "Complete",
            Rationale = "Unsupported target."
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(OverrideWorkflowStepReadinessRequest.TargetStatus));
    }

    [Fact]
    public void OverrideWorkflowStepReadinessValidator_ShouldFail_WhenRationaleIsMissing()
    {
        var validator = new OverrideWorkflowStepReadinessRequestValidator();
        var request = new OverrideWorkflowStepReadinessRequest
        {
            TargetStatus = "Blocked",
            Rationale = string.Empty
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(OverrideWorkflowStepReadinessRequest.Rationale));
    }

    [Fact]
    public void UpdateWorkflowTaskStatusValidator_ShouldPass_WhenStatusIsSupported()
    {
        var validator = new UpdateWorkflowTaskStatusRequestValidator();
        var request = new UpdateWorkflowTaskStatusRequest
        {
            TargetStatus = "Started",
            Notes = "Work started."
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateWorkflowTaskStatusValidator_ShouldFail_WhenStatusIsUnsupported()
    {
        var validator = new UpdateWorkflowTaskStatusRequestValidator();
        var request = new UpdateWorkflowTaskStatusRequest
        {
            TargetStatus = "Blocked"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateWorkflowTaskStatusRequest.TargetStatus));
    }

    [Fact]
    public void UploadCaseDocumentValidator_ShouldPass_WhenPayloadIsValid()
    {
        var validator = new UploadCaseDocumentRequestValidator();
        var request = new UploadCaseDocumentRequest
        {
            FileName = "certificate.pdf",
            ContentType = "application/pdf",
            ContentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("valid-content"))
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UploadCaseDocumentValidator_ShouldFail_WhenBase64IsInvalid()
    {
        var validator = new UploadCaseDocumentRequestValidator();
        var request = new UploadCaseDocumentRequest
        {
            FileName = "certificate.pdf",
            ContentType = "application/pdf",
            ContentBase64 = "***invalid***"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UploadCaseDocumentRequest.ContentBase64));
    }

    [Fact]
    public void UpdateCaseDocumentClassificationValidator_ShouldPass_WhenClassificationIsSupported()
    {
        var validator = new UpdateCaseDocumentClassificationRequestValidator();
        var request = new UpdateCaseDocumentClassificationRequest
        {
            Classification = "Restricted"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateCaseDocumentClassificationValidator_ShouldFail_WhenClassificationIsUnsupported()
    {
        var validator = new UpdateCaseDocumentClassificationRequestValidator();
        var request = new UpdateCaseDocumentClassificationRequest
        {
            Classification = "Confidential"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateCaseDocumentClassificationRequest.Classification));
    }
}
