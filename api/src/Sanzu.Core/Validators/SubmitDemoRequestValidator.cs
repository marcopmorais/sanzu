using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class SubmitDemoRequestValidator : AbstractValidator<SubmitDemoRequest>
{
    public SubmitDemoRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.OrganizationName)
            .NotEmpty()
            .MaximumLength(160);

        RuleFor(x => x.TeamSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(200000);

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
