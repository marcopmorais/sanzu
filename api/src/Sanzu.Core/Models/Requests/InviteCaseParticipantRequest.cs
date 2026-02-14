namespace Sanzu.Core.Models.Requests;

public sealed class InviteCaseParticipantRequest
{
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public int ExpirationDays { get; init; } = 7;
}
