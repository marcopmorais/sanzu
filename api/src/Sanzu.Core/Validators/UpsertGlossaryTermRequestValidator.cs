using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class UpsertGlossaryTermRequestValidator : AbstractValidator<UpsertGlossaryTermRequest>
{
    public UpsertGlossaryTermRequestValidator()
    {
        RuleFor(x => x.Term)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Definition)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.WhyThisMatters)
            .MaximumLength(400)
            .When(x => x.WhyThisMatters is not null);

        RuleFor(x => x.Locale)
            .MaximumLength(16)
            .When(x => x.Locale is not null);
    }
}

