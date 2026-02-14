using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class ActivateTenantBillingRequestValidator : AbstractValidator<ActivateTenantBillingRequest>
{
    private static readonly HashSet<string> SupportedPlans = new(StringComparer.OrdinalIgnoreCase)
    {
        "STARTER",
        "GROWTH",
        "ENTERPRISE"
    };

    private static readonly HashSet<string> SupportedBillingCycles = new(StringComparer.OrdinalIgnoreCase)
    {
        "MONTHLY",
        "ANNUAL"
    };

    private static readonly HashSet<string> SupportedPaymentMethodTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "CARD",
        "SEPA_DIRECT_DEBIT",
        "BANK_TRANSFER"
    };

    public ActivateTenantBillingRequestValidator()
    {
        RuleFor(x => x.PlanCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("PlanCode is required.")
            .MaximumLength(32)
            .Must(value => SupportedPlans.Contains(value.Trim()))
            .WithMessage("PlanCode is not supported.");

        RuleFor(x => x.BillingCycle)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("BillingCycle is required.")
            .MaximumLength(16)
            .Must(value => SupportedBillingCycles.Contains(value.Trim()))
            .WithMessage("BillingCycle is not supported.");

        RuleFor(x => x.PaymentMethodType)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("PaymentMethodType is required.")
            .MaximumLength(32)
            .Must(value => SupportedPaymentMethodTypes.Contains(value.Trim()))
            .WithMessage("PaymentMethodType is not supported.");

        RuleFor(x => x.PaymentMethodReference)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("PaymentMethodReference is required.")
            .MaximumLength(128);

        RuleFor(x => x.InvoiceProfileLegalName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("InvoiceProfileLegalName is required.")
            .MaximumLength(255);

        RuleFor(x => x.InvoiceProfileVatNumber)
            .MaximumLength(64);

        RuleFor(x => x.InvoiceProfileBillingEmail)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("InvoiceProfileBillingEmail is required.")
            .MaximumLength(255)
            .EmailAddress();

        RuleFor(x => x.InvoiceProfileCountryCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("InvoiceProfileCountryCode is required.")
            .Matches("^[A-Za-z]{2}$")
            .WithMessage("InvoiceProfileCountryCode must be a 2-letter country code.");
    }
}
