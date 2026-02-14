using FluentAssertions;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Validators;

namespace Sanzu.Tests.Unit.Validators;

public sealed class TenantSubscriptionValidatorsTests
{
    [Fact]
    public async Task PreviewValidator_ShouldPass_WhenPayloadIsValid()
    {
        var validator = new PreviewPlanChangeRequestValidator();
        var request = new PreviewPlanChangeRequest
        {
            PlanCode = "Growth",
            BillingCycle = "Monthly"
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PreviewValidator_ShouldFail_WhenPlanCodeIsUnsupported()
    {
        var validator = new PreviewPlanChangeRequestValidator();
        var request = new PreviewPlanChangeRequest
        {
            PlanCode = "BASIC",
            BillingCycle = "Monthly"
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(PreviewPlanChangeRequest.PlanCode));
    }

    [Fact]
    public async Task PreviewValidator_ShouldFail_WhenBillingCycleIsUnsupported()
    {
        var validator = new PreviewPlanChangeRequestValidator();
        var request = new PreviewPlanChangeRequest
        {
            PlanCode = "Starter",
            BillingCycle = "WEEKLY"
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(PreviewPlanChangeRequest.BillingCycle));
    }

    [Fact]
    public async Task PreviewValidator_ShouldFail_WhenPlanCodeIsEmpty()
    {
        var validator = new PreviewPlanChangeRequestValidator();
        var request = new PreviewPlanChangeRequest
        {
            PlanCode = "",
            BillingCycle = "Monthly"
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(PreviewPlanChangeRequest.PlanCode));
    }

    [Fact]
    public async Task ChangePlanValidator_ShouldPass_WhenPayloadIsValid()
    {
        var validator = new ChangePlanRequestValidator();
        var request = new ChangePlanRequest
        {
            PlanCode = "Enterprise",
            BillingCycle = "Annual",
            ConfirmedProrationAmount = 125.50m
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePlanValidator_ShouldFail_WhenPlanCodeIsUnsupported()
    {
        var validator = new ChangePlanRequestValidator();
        var request = new ChangePlanRequest
        {
            PlanCode = "PREMIUM",
            BillingCycle = "Monthly",
            ConfirmedProrationAmount = 0m
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(ChangePlanRequest.PlanCode));
    }

    [Fact]
    public async Task ChangePlanValidator_ShouldAcceptNegativeProration_ForDowngrades()
    {
        var validator = new ChangePlanRequestValidator();
        var request = new ChangePlanRequest
        {
            PlanCode = "Starter",
            BillingCycle = "Monthly",
            ConfirmedProrationAmount = -125.00m
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CancelValidator_ShouldPass_WhenPayloadIsValid()
    {
        var validator = new CancelSubscriptionRequestValidator();
        var request = new CancelSubscriptionRequest
        {
            Reason = "We no longer need this service for our agency operations.",
            Confirmed = true
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CancelValidator_ShouldFail_WhenReasonIsTooShort()
    {
        var validator = new CancelSubscriptionRequestValidator();
        var request = new CancelSubscriptionRequest
        {
            Reason = "Too expensive",
            Confirmed = true
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CancelSubscriptionRequest.Reason));
    }

    [Fact]
    public async Task CancelValidator_ShouldFail_WhenReasonIsEmpty()
    {
        var validator = new CancelSubscriptionRequestValidator();
        var request = new CancelSubscriptionRequest
        {
            Reason = "",
            Confirmed = true
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CancelSubscriptionRequest.Reason));
    }

    [Fact]
    public async Task CancelValidator_ShouldFail_WhenConfirmationIsFalse()
    {
        var validator = new CancelSubscriptionRequestValidator();
        var request = new CancelSubscriptionRequest
        {
            Reason = "We no longer need this service for our agency operations.",
            Confirmed = false
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CancelSubscriptionRequest.Confirmed));
    }
}
