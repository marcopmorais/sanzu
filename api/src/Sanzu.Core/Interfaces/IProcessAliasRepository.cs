using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IProcessAliasRepository
{
    Task CreateAsync(ProcessAlias alias, CancellationToken cancellationToken);
    Task<ProcessAlias?> GetLatestByCaseIdAsync(Guid caseId, CancellationToken cancellationToken);
    Task<ProcessAlias?> GetCurrentByCaseIdAsync(Guid caseId, CancellationToken cancellationToken);
    Task<bool> ExistsByAliasEmailAsync(string aliasEmail, CancellationToken cancellationToken);
}
