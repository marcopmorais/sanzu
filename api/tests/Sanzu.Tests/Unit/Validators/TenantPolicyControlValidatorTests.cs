using FluentAssertions;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Validators;

namespace Sanzu.Tests.Unit.Validators;

public sealed class TenantPolicyControlValidatorTests
{
    [Fact]
    public async Task Validator_ShouldFail_WhenControlTypeOrReasonCodeIsInvalid()
    {
        var validator = new ApplyTenantPolicyControlRequestValidator();

        var result = await validator.ValidateAsync(
            new ApplyTenantPolicyControlRequest
            {
                ControlType = "UnknownControl",
                IsEnabled = true,
                ReasonCode = "bad reason"
            });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(ApplyTenantPolicyControlRequest.ControlType));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(ApplyTenantPolicyControlRequest.ReasonCode));
    }

    [Fact]
    public async Task Validator_ShouldPass_WhenPayloadIsValid()
    {
        var validator = new ApplyTenantPolicyControlRequestValidator();

        var result = await validator.ValidateAsync(
            new ApplyTenantPolicyControlRequest
            {
                ControlType = "RiskHold",
                IsEnabled = true,
                ReasonCode = "RISK_ESCALATION"
            });

        result.IsValid.Should().BeTrue();
    }
}
