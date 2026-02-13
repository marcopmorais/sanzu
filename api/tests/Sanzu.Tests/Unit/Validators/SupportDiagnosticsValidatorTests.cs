using FluentAssertions;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Validators;

namespace Sanzu.Tests.Unit.Validators;

public sealed class SupportDiagnosticsValidatorTests
{
    [Fact]
    public async Task Validator_ShouldFail_WhenScopeDurationOrReasonIsInvalid()
    {
        var validator = new StartSupportDiagnosticSessionRequestValidator();

        var result = await validator.ValidateAsync(
            new StartSupportDiagnosticSessionRequest
            {
                Scope = "UnknownScope",
                DurationMinutes = 1,
                Reason = string.Empty
            });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(StartSupportDiagnosticSessionRequest.Scope));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(StartSupportDiagnosticSessionRequest.DurationMinutes));
        result.Errors.Should().Contain(x => x.PropertyName == nameof(StartSupportDiagnosticSessionRequest.Reason));
    }

    [Fact]
    public async Task Validator_ShouldPass_WhenRequestIsWithinPolicyBounds()
    {
        var validator = new StartSupportDiagnosticSessionRequestValidator();

        var result = await validator.ValidateAsync(
            new StartSupportDiagnosticSessionRequest
            {
                Scope = "TenantOperationalRead",
                DurationMinutes = 30,
                Reason = "Escalated support incident."
            });

        result.IsValid.Should().BeTrue();
    }
}
