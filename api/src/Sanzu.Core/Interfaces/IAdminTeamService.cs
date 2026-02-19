using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface IAdminTeamService
{
    Task<IReadOnlyList<AdminTeamMemberResponse>> ListTeamMembersAsync(CancellationToken cancellationToken);
    Task GrantRoleAsync(Guid targetUserId, string role, Guid grantedByUserId, CancellationToken cancellationToken);
    Task RevokeRoleAsync(Guid targetUserId, string role, Guid revokedByUserId, CancellationToken cancellationToken);
}
