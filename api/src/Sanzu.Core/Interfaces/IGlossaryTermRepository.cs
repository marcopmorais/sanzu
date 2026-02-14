using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IGlossaryTermRepository
{
    Task<GlossaryTerm?> GetByKeyAsync(Guid tenantId, string key, string locale, CancellationToken cancellationToken);
    Task<IReadOnlyList<GlossaryTerm>> SearchAsync(Guid tenantId, string query, string locale, CancellationToken cancellationToken);
    Task CreateAsync(GlossaryTerm term, CancellationToken cancellationToken);
    Task UpdateAsync(GlossaryTerm term, CancellationToken cancellationToken);
}

