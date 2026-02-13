using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class SubmitCaseIntakeRequestValidator : AbstractValidator<SubmitCaseIntakeRequest>
{
    public SubmitCaseIntakeRequestValidator()
    {
        RuleFor(x => x.PrimaryContactName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("PrimaryContactName is required.")
            .MaximumLength(255);

        RuleFor(x => x.PrimaryContactPhone)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("PrimaryContactPhone is required.")
            .MaximumLength(32);

        RuleFor(x => x.RelationshipToDeceased)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("RelationshipToDeceased is required.")
            .MaximumLength(64);

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .When(x => x.Notes is not null);

        RuleFor(x => x.ConfirmAccuracy)
            .Equal(true)
            .WithMessage("ConfirmAccuracy must be true to submit intake.");
    }
}
