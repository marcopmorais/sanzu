using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class CaseParticipantRepository : ICaseParticipantRepository
{
    private readonly SanzuDbContext _dbContext;

    public CaseParticipantRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateAsync(CaseParticipant participant, CancellationToken cancellationToken)
    {
        _dbContext.CaseParticipants.Add(participant);
        return Task.CompletedTask;
    }

    public Task<CaseParticipant?> GetByIdAsync(Guid participantId, CancellationToken cancellationToken)
    {
        return _dbContext.CaseParticipants.FirstOrDefaultAsync(x => x.Id == participantId, cancellationToken);
    }

    public Task<bool> HasActivePendingInviteAsync(Guid caseId, string email, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _dbContext.CaseParticipants.AnyAsync(
            x => x.CaseId == caseId
                && x.Email == normalizedEmail
                && x.Status == CaseParticipantStatus.Pending
                && x.ExpiresAt > nowUtc,
            cancellationToken);
    }

    public Task<CaseParticipant?> GetAcceptedParticipantAsync(Guid caseId, Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.CaseParticipants.FirstOrDefaultAsync(
            x => x.CaseId == caseId
                && x.ParticipantUserId == userId
                && x.Status == CaseParticipantStatus.Accepted,
            cancellationToken);
    }

    public async Task<IReadOnlyList<CaseParticipant>> GetAcceptedByCaseIdAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return await _dbContext.CaseParticipants
            .Where(x => x.CaseId == caseId && x.Status == CaseParticipantStatus.Accepted)
            .ToListAsync(cancellationToken);
    }

    public async Task ExpirePendingInvitesAsync(Guid caseId, string email, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var staleInvites = await _dbContext.CaseParticipants
            .Where(
                x => x.CaseId == caseId
                    && x.Email == normalizedEmail
                    && x.Status == CaseParticipantStatus.Pending
                    && x.ExpiresAt <= nowUtc)
            .ToListAsync(cancellationToken);

        foreach (var invite in staleInvites)
        {
            invite.Status = CaseParticipantStatus.Expired;
        }
    }
}
