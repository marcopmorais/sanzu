using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class StartAccountIntentRequestValidator : AbstractValidator<StartAccountIntentRequest>
{
    public StartAccountIntentRequestValidator()
    {
        RuleFor(x => x.AgencyName)
            .NotEmpty()
            .MaximumLength(160);

        RuleFor(x => x.AdminFullName)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.AdminEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.Location)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.TermsAccepted)
            .Equal(true)
            .WithMessage("TermsAccepted must be true.");

        RuleFor(x => x.UtmSource)
            .MaximumLength(120);

        RuleFor(x => x.UtmMedium)
            .MaximumLength(120);

        RuleFor(x => x.UtmCampaign)
            .MaximumLength(160);

        RuleFor(x => x.ReferrerPath)
            .MaximumLength(512);

        RuleFor(x => x.LandingPath)
            .MaximumLength(512);
    }
}
