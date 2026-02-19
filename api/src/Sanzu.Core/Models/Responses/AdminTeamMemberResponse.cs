namespace Sanzu.Core.Models.Responses;

public sealed class AdminTeamMemberResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime GrantedAt { get; init; }
}
