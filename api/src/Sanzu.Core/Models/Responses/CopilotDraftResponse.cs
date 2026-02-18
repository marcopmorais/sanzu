namespace Sanzu.Core.Models.Responses;

public sealed class CopilotDraftResponse
{
    public Guid Id { get; set; }
    public string DraftType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Checklist { get; set; } = new();
    public CopilotExplainabilityBlock Explainability { get; set; } = new();
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
}

public sealed class CopilotExplainabilityBlock
{
    public string BasedOn { get; set; } = string.Empty;
    public string ReasonCategory { get; set; } = string.Empty;
    public string ConfidenceBand { get; set; } = string.Empty;
    public List<string> MissingOrUnknown { get; set; } = new();
    public string SafeFallback { get; set; } = string.Empty;
}

public sealed class CopilotDraftAcceptedResponse
{
    public Guid DraftId { get; set; }
    public string Status { get; set; } = "Accepted";
    public DateTime AcceptedAt { get; set; }
}
