using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class UpdateCaseDetailsRequestValidator : AbstractValidator<UpdateCaseDetailsRequest>
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

    public UpdateCaseDetailsRequestValidator()
    {
        RuleFor(x => x)
            .Must(HasAtLeastOneChange)
            .WithMessage("At least one case field must be provided for update.");

        RuleFor(x => x.DeceasedFullName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("DeceasedFullName cannot be empty.")
            .MaximumLength(255)
            .When(x => x.DeceasedFullName is not null);

        RuleFor(x => x.DateOfDeath)
            .Must(date => date!.Value.Date <= DateTime.UtcNow.Date)
            .WithMessage("DateOfDeath cannot be in the future.")
            .When(x => x.DateOfDeath.HasValue);

        RuleFor(x => x.CaseType)
            .MaximumLength(32)
            .Must(type => type is null || SupportedCaseTypes.Contains(type.Trim()))
            .WithMessage("CaseType is not supported.")
            .When(x => x.CaseType is not null);

        RuleFor(x => x.Urgency)
            .MaximumLength(16)
            .Must(urgency => urgency is null || SupportedUrgency.Contains(urgency.Trim()))
            .WithMessage("Urgency is not supported.")
            .When(x => x.Urgency is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .When(x => x.Notes is not null);
    }

    private static bool HasAtLeastOneChange(UpdateCaseDetailsRequest request)
    {
        return request.DeceasedFullName is not null
               || request.DateOfDeath.HasValue
               || request.CaseType is not null
               || request.Urgency is not null
               || request.Notes is not null;
    }
}
