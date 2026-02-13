using FluentAssertions;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Validators;

namespace Sanzu.Tests.Unit.Validators;

public sealed class KpiAlertValidatorsTests
{
    [Fact]
    public async Task UpsertValidator_ShouldFail_WhenMetricSeverityOrRouteIsInvalid()
    {
        var validator = new UpsertKpiThresholdRequestValidator();

        var result = await validator.ValidateAsync(
            new UpsertKpiThresholdRequest
            {
                MetricKey = "UnknownMetric",
                ThresholdValue = -1,
                Severity = "UnknownSeverity",
                RouteTarget = string.Empty,
                IsEnabled = true
            });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateValidator_ShouldFail_WhenPeriodOrLimitsAreInvalid()
    {
        var validator = new EvaluateKpiAlertsRequestValidator();

        var result = await validator.ValidateAsync(
            new EvaluateKpiAlertsRequest
            {
                PeriodDays = 2,
                TenantLimit = 0,
                CaseLimit = 101
            });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validators_ShouldPass_WhenPayloadsAreValid()
    {
        var upsertValidator = new UpsertKpiThresholdRequestValidator();
        var evaluateValidator = new EvaluateKpiAlertsRequestValidator();

        var upsert = await upsertValidator.ValidateAsync(
            new UpsertKpiThresholdRequest
            {
                MetricKey = "CasesCreated",
                ThresholdValue = 5,
                Severity = "High",
                RouteTarget = "ops@agency.pt",
                IsEnabled = true
            });

        var evaluate = await evaluateValidator.ValidateAsync(
            new EvaluateKpiAlertsRequest
            {
                PeriodDays = 30,
                TenantLimit = 10,
                CaseLimit = 10
            });

        upsert.IsValid.Should().BeTrue();
        evaluate.IsValid.Should().BeTrue();
    }
}
