namespace Sanzu.Core.Models.Responses;

public sealed class InviteCaseParticipantResponse
{
    public CaseParticipantResponse Participant { get; init; } = new();
    public string InvitationToken { get; init; } = string.Empty;
}
