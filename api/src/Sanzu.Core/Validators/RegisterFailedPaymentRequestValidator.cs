using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class RegisterFailedPaymentRequestValidator : AbstractValidator<RegisterFailedPaymentRequest>
{
    public RegisterFailedPaymentRequestValidator()
    {
        RuleFor(x => x.Reason)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Reason is required.")
            .MinimumLength(10)
            .WithMessage("Reason must be at least 10 characters.")
            .MaximumLength(2000);

        RuleFor(x => x.PaymentReference)
            .MaximumLength(128)
            .When(x => !string.IsNullOrWhiteSpace(x.PaymentReference));
    }
}
