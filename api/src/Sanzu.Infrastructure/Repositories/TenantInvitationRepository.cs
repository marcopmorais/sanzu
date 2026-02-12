using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class TenantInvitationRepository : ITenantInvitationRepository
{
    private readonly SanzuDbContext _dbContext;

    public TenantInvitationRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateAsync(TenantInvitation invitation, CancellationToken cancellationToken)
    {
        _dbContext.TenantInvitations.Add(invitation);
        return Task.CompletedTask;
    }

    public async Task ExpirePendingInvitesAsync(Guid tenantId, string email, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var staleInvites = await _dbContext.TenantInvitations
            .Where(
                x =>
                    x.TenantId == tenantId
                    && x.Email == normalizedEmail
                    && x.Status == TenantInvitationStatus.Pending
                    && x.ExpiresAt <= nowUtc)
            .ToListAsync(cancellationToken);

        foreach (var invite in staleInvites)
        {
            invite.Status = TenantInvitationStatus.Expired;
        }
    }

    public Task<bool> HasActivePendingInviteAsync(Guid tenantId, string email, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _dbContext.TenantInvitations.AnyAsync(
            x =>
                x.TenantId == tenantId
                && x.Email == normalizedEmail
                && x.Status == TenantInvitationStatus.Pending
                && x.ExpiresAt > nowUtc,
            cancellationToken);
    }
}
