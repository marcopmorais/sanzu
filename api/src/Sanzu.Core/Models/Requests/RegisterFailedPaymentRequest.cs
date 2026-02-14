namespace Sanzu.Core.Models.Requests;

public sealed class RegisterFailedPaymentRequest
{
    public string Reason { get; init; } = string.Empty;
    public string? PaymentReference { get; init; }
}
