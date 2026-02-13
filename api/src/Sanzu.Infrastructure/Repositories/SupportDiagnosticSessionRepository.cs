using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class SupportDiagnosticSessionRepository : ISupportDiagnosticSessionRepository
{
    private readonly SanzuDbContext _dbContext;

    public SupportDiagnosticSessionRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateAsync(SupportDiagnosticSession session, CancellationToken cancellationToken)
    {
        _dbContext.SupportDiagnosticSessions.Add(session);
        return Task.CompletedTask;
    }

    public Task<SupportDiagnosticSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return _dbContext.SupportDiagnosticSessions.FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
    }

    public Task<int> CountStartedSinceAsync(Guid tenantId, DateTime sinceUtc, CancellationToken cancellationToken)
    {
        return _dbContext.SupportDiagnosticSessions.CountAsync(
            x => x.TenantId == tenantId && x.StartedAt >= sinceUtc,
            cancellationToken);
    }
}
