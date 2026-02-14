using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class AcceptCaseParticipantInvitationRequestValidator : AbstractValidator<AcceptCaseParticipantInvitationRequest>
{
    public AcceptCaseParticipantInvitationRequestValidator()
    {
        RuleFor(x => x.InvitationToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("InvitationToken is required.")
            .MaximumLength(128);
    }
}
