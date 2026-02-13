using FluentValidation;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class UpdateTenantLifecycleStateRequestValidator : AbstractValidator<UpdateTenantLifecycleStateRequest>
{
    public UpdateTenantLifecycleStateRequestValidator()
    {
        RuleFor(x => x.TargetStatus)
            .NotEmpty()
            .Must(BeKnownTenantStatus)
            .WithMessage("TargetStatus must be a valid tenant lifecycle status.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(2000);
    }

    private static bool BeKnownTenantStatus(string value)
    {
        return Enum.TryParse<TenantStatus>(value, ignoreCase: true, out _);
    }
}
