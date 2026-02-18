namespace Sanzu.Core.Models.Requests;

public sealed class RequestCopilotDraftRequest
{
    public string DraftType { get; set; } = string.Empty;
    public Guid CaseId { get; set; }
    public Guid? WorkflowStepId { get; set; }
    public Guid? HandoffId { get; set; }
}

public sealed class AcceptCopilotDraftRequest
{
    public Guid DraftId { get; set; }
    public string? EditedContent { get; set; }
}
