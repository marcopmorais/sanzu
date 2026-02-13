using FluentValidation;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class UpsertKpiThresholdRequestValidator : AbstractValidator<UpsertKpiThresholdRequest>
{
    public UpsertKpiThresholdRequestValidator()
    {
        RuleFor(x => x.MetricKey)
            .NotEmpty()
            .Must(BeKnownMetricKey)
            .WithMessage("MetricKey must be a valid KPI metric.");

        RuleFor(x => x.ThresholdValue)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Severity)
            .NotEmpty()
            .Must(BeKnownSeverity)
            .WithMessage("Severity must be a valid KPI alert severity.");

        RuleFor(x => x.RouteTarget)
            .NotEmpty()
            .MaximumLength(256);
    }

    private static bool BeKnownMetricKey(string value)
    {
        return Enum.TryParse<KpiMetricKey>(value, ignoreCase: true, out _);
    }

    private static bool BeKnownSeverity(string value)
    {
        return Enum.TryParse<KpiAlertSeverity>(value, ignoreCase: true, out _);
    }
}
