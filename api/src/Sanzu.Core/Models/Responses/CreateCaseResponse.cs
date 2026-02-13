using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class CreateCaseResponse
{
    public Guid CaseId { get; init; }
    public Guid TenantId { get; init; }
    public string CaseNumber { get; init; } = string.Empty;
    public string DeceasedFullName { get; init; } = string.Empty;
    public DateTime DateOfDeath { get; init; }
    public string CaseType { get; init; } = string.Empty;
    public string Urgency { get; init; } = string.Empty;
    public string? WorkflowKey { get; init; }
    public string? TemplateKey { get; init; }
    public CaseStatus Status { get; init; }
    public Guid ManagerUserId { get; init; }
    public DateTime CreatedAt { get; init; }
}
