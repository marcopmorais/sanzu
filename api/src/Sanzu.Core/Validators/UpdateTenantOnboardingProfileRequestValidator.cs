using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class UpdateTenantOnboardingProfileRequestValidator : AbstractValidator<UpdateTenantOnboardingProfileRequest>
{
    public UpdateTenantOnboardingProfileRequestValidator()
    {
        RuleFor(x => x.AgencyName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Location)
            .NotEmpty()
            .MaximumLength(255);
    }
}
