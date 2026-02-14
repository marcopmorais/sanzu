using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class InviteCaseParticipantRequestValidator : AbstractValidator<InviteCaseParticipantRequest>
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "MANAGER",
        "EDITOR",
        "READER"
    };

    public InviteCaseParticipantRequestValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Email is required.")
            .MaximumLength(255)
            .EmailAddress();

        RuleFor(x => x.Role)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Role is required.")
            .MaximumLength(32)
            .Must(role => AllowedRoles.Contains(role.Trim()))
            .WithMessage("Role is not supported.");

        RuleFor(x => x.ExpirationDays)
            .InclusiveBetween(1, 30)
            .WithMessage("ExpirationDays must be between 1 and 30.");
    }
}
