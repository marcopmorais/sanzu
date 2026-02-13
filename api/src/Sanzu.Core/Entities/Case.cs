using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class Case
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string DeceasedFullName { get; set; } = string.Empty;
    public DateTime DateOfDeath { get; set; }
    public string CaseType { get; set; } = "GENERAL";
    public string Urgency { get; set; } = "NORMAL";
    public CaseStatus Status { get; set; } = CaseStatus.Draft;
    public string? Notes { get; set; }
    public Guid ManagerUserId { get; set; }
    public string? IntakeData { get; set; }
    public DateTime? IntakeCompletedAt { get; set; }
    public Guid? IntakeCompletedByUserId { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Organization? Tenant { get; set; }
    public User? ManagerUser { get; set; }
    public ICollection<CaseParticipant> Participants { get; set; } = new List<CaseParticipant>();
    public ICollection<WorkflowStepInstance> WorkflowSteps { get; set; } = new List<WorkflowStepInstance>();
    public ICollection<CaseDocument> Documents { get; set; } = new List<CaseDocument>();
    public ICollection<CaseHandoff> Handoffs { get; set; } = new List<CaseHandoff>();
}
