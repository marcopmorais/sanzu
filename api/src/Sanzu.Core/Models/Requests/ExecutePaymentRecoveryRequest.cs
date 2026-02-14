namespace Sanzu.Core.Models.Requests;

public sealed class ExecutePaymentRecoveryRequest
{
    public bool RetrySucceeded { get; init; }
    public bool ReminderSent { get; init; } = true;
    public string? FailureReason { get; init; }
}
