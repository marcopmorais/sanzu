using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class GrantAdminRoleRequestValidator : AbstractValidator<GrantAdminRoleRequest>
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "SanzuOps",
        "SanzuFinance",
        "SanzuSupport",
        "SanzuViewer"
    };

    public GrantAdminRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => AllowedRoles.Contains(role))
            .WithMessage("Role must be one of: SanzuOps, SanzuFinance, SanzuSupport, SanzuViewer.");
    }
}
