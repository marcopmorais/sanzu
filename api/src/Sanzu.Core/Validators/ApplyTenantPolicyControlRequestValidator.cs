using FluentValidation;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class ApplyTenantPolicyControlRequestValidator : AbstractValidator<ApplyTenantPolicyControlRequest>
{
    public ApplyTenantPolicyControlRequestValidator()
    {
        RuleFor(x => x.ControlType)
            .NotEmpty()
            .Must(BeKnownControlType)
            .WithMessage("ControlType must be a valid tenant policy control.");

        RuleFor(x => x.ReasonCode)
            .NotEmpty()
            .MaximumLength(128)
            .Matches("^[A-Z0-9_-]{3,128}$")
            .WithMessage("ReasonCode must contain only uppercase letters, digits, '_' or '-'.");
    }

    private static bool BeKnownControlType(string value)
    {
        return Enum.TryParse<TenantPolicyControlType>(value, ignoreCase: true, out _);
    }
}
