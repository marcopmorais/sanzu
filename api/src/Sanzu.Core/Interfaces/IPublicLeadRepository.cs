using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IPublicLeadRepository
{
    Task CreateAsync(PublicLead lead, CancellationToken cancellationToken);
}
