using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface ICaseParticipantRepository
{
    Task CreateAsync(CaseParticipant participant, CancellationToken cancellationToken);
    Task<CaseParticipant?> GetByIdAsync(Guid participantId, CancellationToken cancellationToken);
    Task<bool> HasActivePendingInviteAsync(Guid caseId, string email, DateTime nowUtc, CancellationToken cancellationToken);
    Task ExpirePendingInvitesAsync(Guid caseId, string email, DateTime nowUtc, CancellationToken cancellationToken);
    Task<CaseParticipant?> GetAcceptedParticipantAsync(Guid caseId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CaseParticipant>> GetAcceptedByCaseIdAsync(Guid caseId, CancellationToken cancellationToken);
}
