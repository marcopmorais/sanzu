using FluentAssertions;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Validators;

namespace Sanzu.Tests.Unit.Validators;

public sealed class TenantBillingRecoveryValidatorsTests
{
    [Fact]
    public void RegisterFailedPaymentValidator_ShouldPass_WhenRequestIsValid()
    {
        var validator = new RegisterFailedPaymentRequestValidator();
        var request = new RegisterFailedPaymentRequest
        {
            Reason = "Card processor declined the monthly subscription charge.",
            PaymentReference = "evt_12345"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterFailedPaymentValidator_ShouldFail_WhenReasonMissing()
    {
        var validator = new RegisterFailedPaymentRequestValidator();
        var request = new RegisterFailedPaymentRequest
        {
            Reason = string.Empty
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(RegisterFailedPaymentRequest.Reason));
    }

    [Fact]
    public void ExecutePaymentRecoveryValidator_ShouldPass_WhenRetrySucceeded()
    {
        var validator = new ExecutePaymentRecoveryRequestValidator();
        var request = new ExecutePaymentRecoveryRequest
        {
            RetrySucceeded = true,
            ReminderSent = true
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ExecutePaymentRecoveryValidator_ShouldFail_WhenRetryFailedWithoutReason()
    {
        var validator = new ExecutePaymentRecoveryRequestValidator();
        var request = new ExecutePaymentRecoveryRequest
        {
            RetrySucceeded = false,
            ReminderSent = true
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(ExecutePaymentRecoveryRequest.FailureReason));
    }
}
