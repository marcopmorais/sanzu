namespace Sanzu.Core.Models.Responses;

public sealed class CaseAuditExportResponse
{
    public string ExportId { get; init; } = string.Empty;
    public Guid CaseId { get; init; }
    public Guid TenantId { get; init; }
    public DateTime GeneratedAt { get; init; }
    public string GeneratedByUserId { get; init; } = string.Empty;
    public CaseAuditExportSummary CaseSummary { get; init; } = new();
    public IReadOnlyList<CaseAuditExportEvent> AuditEvents { get; init; } = [];
    public IReadOnlyList<CaseAuditExportEvidenceReference> EvidenceReferences { get; init; } = [];
}

public sealed class CaseAuditExportSummary
{
    public string CaseNumber { get; init; } = string.Empty;
    public string DeceasedFullName { get; init; } = string.Empty;
    public DateTime DateOfDeath { get; init; }
    public string CaseType { get; init; } = string.Empty;
    public string Urgency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid? PlaybookId { get; init; }
    public int? PlaybookVersion { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
}

public sealed class CaseAuditExportEvent
{
    public Guid EventId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string ActorUserId { get; init; } = string.Empty;
    public string Metadata { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed class CaseAuditExportEvidenceReference
{
    public Guid DocumentId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string Classification { get; init; } = string.Empty;
    public int CurrentVersionNumber { get; init; }
    public DateTime UploadedAt { get; init; }
}
