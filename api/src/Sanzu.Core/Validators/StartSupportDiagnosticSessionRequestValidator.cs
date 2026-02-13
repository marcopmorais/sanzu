using FluentValidation;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class StartSupportDiagnosticSessionRequestValidator : AbstractValidator<StartSupportDiagnosticSessionRequest>
{
    public StartSupportDiagnosticSessionRequestValidator()
    {
        RuleFor(x => x.Scope)
            .NotEmpty()
            .Must(BeKnownScope)
            .WithMessage("Scope must be a valid diagnostic scope.");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(5, 240);

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(2000);
    }

    private static bool BeKnownScope(string value)
    {
        return Enum.TryParse<SupportDiagnosticScope>(value, ignoreCase: true, out _);
    }
}
