using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface ITenantInvitationRepository
{
    Task CreateAsync(TenantInvitation invitation, CancellationToken cancellationToken);
    Task ExpirePendingInvitesAsync(Guid tenantId, string email, DateTime nowUtc, CancellationToken cancellationToken);
    Task<bool> HasActivePendingInviteAsync(Guid tenantId, string email, DateTime nowUtc, CancellationToken cancellationToken);
}
