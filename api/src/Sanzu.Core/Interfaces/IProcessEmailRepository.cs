using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IProcessEmailRepository
{
    Task CreateAsync(ProcessEmail email, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProcessEmail>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken);
}
