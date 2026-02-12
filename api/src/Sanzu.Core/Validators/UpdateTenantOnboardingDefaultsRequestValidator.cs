using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class UpdateTenantOnboardingDefaultsRequestValidator : AbstractValidator<UpdateTenantOnboardingDefaultsRequest>
{
    private static readonly HashSet<string> SupportedLocales = new(StringComparer.OrdinalIgnoreCase)
    {
        "pt-PT",
        "en-US"
    };

    private static readonly HashSet<string> SupportedTimeZones = new(StringComparer.OrdinalIgnoreCase)
    {
        "Europe/Lisbon",
        "UTC"
    };

    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "EUR",
        "USD"
    };

    public UpdateTenantOnboardingDefaultsRequestValidator()
    {
        RuleFor(x => x.DefaultLocale)
            .NotEmpty()
            .Must(locale => SupportedLocales.Contains(locale))
            .WithMessage("DefaultLocale is not supported.");

        RuleFor(x => x.DefaultTimeZone)
            .NotEmpty()
            .Must(timeZone => SupportedTimeZones.Contains(timeZone))
            .WithMessage("DefaultTimeZone is not supported.");

        RuleFor(x => x.DefaultCurrency)
            .NotEmpty()
            .Must(currency => SupportedCurrencies.Contains(currency))
            .WithMessage("DefaultCurrency is not supported.");

        RuleFor(x => x.DefaultWorkflowKey)
            .MaximumLength(128);

        RuleFor(x => x.DefaultTemplateKey)
            .MaximumLength(128);
    }
}
