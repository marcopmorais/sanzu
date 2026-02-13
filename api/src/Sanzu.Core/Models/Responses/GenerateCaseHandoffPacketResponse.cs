namespace Sanzu.Core.Models.Responses;

public sealed class GenerateCaseHandoffPacketResponse
{
    public Guid CaseId { get; init; }
    public Guid HandoffId { get; init; }
    public string HandoffStatus { get; init; } = string.Empty;
    public bool FollowUpRequired { get; init; }
    public string CaseNumber { get; init; } = string.Empty;
    public string PacketTitle { get; init; } = string.Empty;
    public string ContentType { get; init; } = "text/plain";
    public string ContentBase64 { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
    public IReadOnlyList<CaseHandoffActionItemResponse> RequiredActions { get; init; } = [];
    public IReadOnlyList<CaseHandoffEvidenceItemResponse> EvidenceContext { get; init; } = [];
}
