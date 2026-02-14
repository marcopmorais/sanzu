using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class PaymentRecoveryStatusResponse
{
    public Guid TenantId { get; init; }
    public TenantStatus TenantStatus { get; init; }
    public int FailedPaymentAttempts { get; init; }
    public DateTime? LastPaymentFailedAt { get; init; }
    public string? LastPaymentFailureReason { get; init; }
    public DateTime? NextPaymentRetryAt { get; init; }
    public DateTime? NextPaymentReminderAt { get; init; }
    public DateTime? LastPaymentReminderSentAt { get; init; }
    public bool RecoveryComplete { get; init; }
    public string Message { get; init; } = string.Empty;
}
