using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class UpdateCaseParticipantRoleRequestValidator : AbstractValidator<UpdateCaseParticipantRoleRequest>
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "MANAGER",
        "EDITOR",
        "READER"
    };

    public UpdateCaseParticipantRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Role is required.")
            .MaximumLength(32)
            .Must(role => AllowedRoles.Contains(role.Trim()))
            .WithMessage("Role is not supported.");
    }
}
