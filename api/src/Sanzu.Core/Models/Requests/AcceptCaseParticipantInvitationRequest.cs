namespace Sanzu.Core.Models.Requests;

public sealed class AcceptCaseParticipantInvitationRequest
{
    public string InvitationToken { get; init; } = string.Empty;
}
