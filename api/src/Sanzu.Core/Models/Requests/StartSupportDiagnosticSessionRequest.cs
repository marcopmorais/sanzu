namespace Sanzu.Core.Models.Requests;

public sealed class StartSupportDiagnosticSessionRequest
{
    public string Scope { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public string Reason { get; init; } = string.Empty;
}
