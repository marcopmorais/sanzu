using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class CompleteTenantOnboardingRequestValidator : AbstractValidator<CompleteTenantOnboardingRequest>
{
    public CompleteTenantOnboardingRequestValidator()
    {
        RuleFor(x => x.ConfirmCompletion)
            .Equal(true)
            .WithMessage("ConfirmCompletion must be true.");
    }
}
