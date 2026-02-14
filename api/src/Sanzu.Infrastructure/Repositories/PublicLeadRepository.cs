using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class PublicLeadRepository : IPublicLeadRepository
{
    private readonly SanzuDbContext _dbContext;

    public PublicLeadRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateAsync(PublicLead lead, CancellationToken cancellationToken)
    {
        _dbContext.PublicLeads.Add(lead);
        return Task.CompletedTask;
    }
}
