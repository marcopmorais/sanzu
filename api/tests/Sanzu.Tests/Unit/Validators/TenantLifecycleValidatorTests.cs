using FluentAssertions;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Validators;

namespace Sanzu.Tests.Unit.Validators;

public sealed class TenantLifecycleValidatorTests
{
    [Fact]
    public async Task Validator_ShouldFail_WhenTargetStatusOrReasonIsMissing()
    {
        var validator = new UpdateTenantLifecycleStateRequestValidator();

        var result = await validator.ValidateAsync(
            new UpdateTenantLifecycleStateRequest
            {
                TargetStatus = string.Empty,
                Reason = string.Empty
            });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateTenantLifecycleStateRequest.TargetStatus));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateTenantLifecycleStateRequest.Reason));
    }

    [Fact]
    public async Task Validator_ShouldPass_WhenPayloadIsValid()
    {
        var validator = new UpdateTenantLifecycleStateRequestValidator();

        var result = await validator.ValidateAsync(
            new UpdateTenantLifecycleStateRequest
            {
                TargetStatus = "Suspended",
                Reason = "Policy escalation required."
            });

        result.IsValid.Should().BeTrue();
    }
}
