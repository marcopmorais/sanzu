using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class CancelSubscriptionRequestValidator : AbstractValidator<CancelSubscriptionRequest>
{
    public CancelSubscriptionRequestValidator()
    {
        RuleFor(x => x.Reason)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Reason is required.")
            .MinimumLength(20)
            .WithMessage("Reason must be at least 20 characters.")
            .MaximumLength(2000);

        RuleFor(x => x.Confirmed)
            .Equal(true)
            .WithMessage("Cancellation must be explicitly confirmed.");
    }
}
