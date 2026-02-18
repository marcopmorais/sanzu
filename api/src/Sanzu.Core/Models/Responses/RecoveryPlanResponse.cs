namespace Sanzu.Core.Models.Responses;

public sealed class RecoveryPlanResponse
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public string ReasonCategory { get; set; } = string.Empty;
    public string ReasonLabel { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public List<RecoveryStep> Steps { get; set; } = new();
    public List<string> EvidenceChecklist { get; set; } = new();
    public RecoveryEscalation Escalation { get; set; } = new();
    public CopilotExplainabilityBlock Explainability { get; set; } = new();
    public string BoundaryMessage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class RecoveryStep
{
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
}

public sealed class RecoveryEscalation
{
    public string TargetRole { get; set; } = string.Empty;
    public string Instruction { get; set; } = string.Empty;
}
