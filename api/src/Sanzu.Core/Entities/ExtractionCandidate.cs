using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class ExtractionCandidate
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    public Guid DocumentId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string CandidateValue { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public int SourceVersionNumber { get; set; }
    public ExtractionCandidateStatus Status { get; set; } = ExtractionCandidateStatus.Pending;
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public CaseDocument? Document { get; set; }
}
