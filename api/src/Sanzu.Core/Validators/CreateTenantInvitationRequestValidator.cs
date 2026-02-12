using FluentValidation;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class CreateTenantInvitationRequestValidator : AbstractValidator<CreateTenantInvitationRequest>
{
    public CreateTenantInvitationRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.RoleType)
            .Equal(PlatformRole.AgencyAdmin)
            .WithMessage("Only AgencyAdmin invitations are supported in onboarding.");

        RuleFor(x => x.ExpirationDays)
            .InclusiveBetween(1, 30);
    }
}
