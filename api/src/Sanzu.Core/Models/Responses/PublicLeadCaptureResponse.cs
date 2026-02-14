namespace Sanzu.Core.Models.Responses;

public sealed class PublicLeadCaptureResponse
{
    public Guid LeadId { get; set; }
    public string IntentType { get; set; } = string.Empty;
    public bool Qualified { get; set; }
    public string RouteTarget { get; set; } = string.Empty;
    public string RouteStatus { get; set; } = string.Empty;
    public string? RouteFailureReason { get; set; }
    public DateTime CapturedAt { get; set; }
}
