using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class ExecutePaymentRecoveryRequestValidator : AbstractValidator<ExecutePaymentRecoveryRequest>
{
    public ExecutePaymentRecoveryRequestValidator()
    {
        RuleFor(x => x.FailureReason)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("FailureReason is required when retry does not succeed.")
            .MinimumLength(10)
            .WithMessage("FailureReason must be at least 10 characters.")
            .MaximumLength(2000)
            .When(x => !x.RetrySucceeded);
    }
}
