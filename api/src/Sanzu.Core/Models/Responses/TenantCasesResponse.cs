namespace Sanzu.Core.Models.Responses;

public sealed class TenantCasesResponse
{
    public IReadOnlyList<TenantCaseItem> Cases { get; init; } = [];
}

public sealed class TenantCaseItem
{
    public Guid CaseId { get; init; }
    public string CaseNumber { get; init; } = string.Empty;
    public string DeceasedFullName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string? WorkflowKey { get; init; }
    public TenantCaseWorkflowProgress WorkflowProgress { get; init; } = new();
    public IReadOnlyList<TenantCaseBlockedStep> BlockedSteps { get; init; } = [];
}

public sealed class TenantCaseWorkflowProgress
{
    public int TotalSteps { get; init; }
    public int CompletedSteps { get; init; }
    public int InProgressSteps { get; init; }
    public int BlockedSteps { get; init; }
}

public sealed class TenantCaseBlockedStep
{
    public string StepKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? BlockedReasonCode { get; init; }
    public string? BlockedReasonDetail { get; init; }
}
