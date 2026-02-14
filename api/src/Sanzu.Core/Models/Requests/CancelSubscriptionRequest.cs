namespace Sanzu.Core.Models.Requests;

public sealed class CancelSubscriptionRequest
{
    public string Reason { get; set; } = string.Empty;
    public bool Confirmed { get; set; }
}
