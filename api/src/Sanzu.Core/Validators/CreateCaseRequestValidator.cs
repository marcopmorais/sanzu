using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class CreateCaseRequestValidator : AbstractValidator<CreateCaseRequest>
{
    private static readonly HashSet<string> SupportedCaseTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "GENERAL",
        "ESTATE",
        "INSURANCE",
        "BENEFITS"
    };

    private static readonly HashSet<string> SupportedUrgency = new(StringComparer.OrdinalIgnoreCase)
    {
        "LOW",
        "NORMAL",
        "HIGH",
        "URGENT"
    };

    public CreateCaseRequestValidator()
    {
        RuleFor(x => x.DeceasedFullName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("DeceasedFullName is required.")
            .MaximumLength(255);

        RuleFor(x => x.DateOfDeath)
            .Must(date => date != default)
            .WithMessage("DateOfDeath is required.")
            .Must(date => date.Date <= DateTime.UtcNow.Date)
            .WithMessage("DateOfDeath cannot be in the future.");

        RuleFor(x => x.CaseType)
            .MaximumLength(32)
            .Must(type => string.IsNullOrWhiteSpace(type) || SupportedCaseTypes.Contains(type.Trim()))
            .WithMessage("CaseType is not supported.");

        RuleFor(x => x.Urgency)
            .MaximumLength(16)
            .Must(urgency => string.IsNullOrWhiteSpace(urgency) || SupportedUrgency.Contains(urgency.Trim()))
            .WithMessage("Urgency is not supported.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000);
    }
}
