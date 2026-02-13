using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class WorkflowStepRepository : IWorkflowStepRepository
{
    private readonly SanzuDbContext _dbContext;

    public WorkflowStepRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateStepsAsync(IEnumerable<WorkflowStepInstance> steps, CancellationToken cancellationToken)
    {
        _dbContext.WorkflowStepInstances.AddRange(steps);
        return Task.CompletedTask;
    }

    public Task CreateDependenciesAsync(IEnumerable<WorkflowStepDependency> dependencies, CancellationToken cancellationToken)
    {
        _dbContext.WorkflowStepDependencies.AddRange(dependencies);
        return Task.CompletedTask;
    }

    public async Task DeletePlanByCaseIdAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var dependencies = await _dbContext.WorkflowStepDependencies
            .Where(x => x.CaseId == caseId)
            .ToListAsync(cancellationToken);
        var steps = await _dbContext.WorkflowStepInstances
            .Where(x => x.CaseId == caseId)
            .ToListAsync(cancellationToken);

        _dbContext.WorkflowStepDependencies.RemoveRange(dependencies);
        _dbContext.WorkflowStepInstances.RemoveRange(steps);
    }

    public async Task<IReadOnlyList<WorkflowStepInstance>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return await _dbContext.WorkflowStepInstances
            .Where(x => x.CaseId == caseId)
            .OrderBy(x => x.Sequence)
            .ThenBy(x => x.StepKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowStepDependency>> GetDependenciesByCaseIdAsync(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WorkflowStepDependencies
            .Where(x => x.CaseId == caseId)
            .ToListAsync(cancellationToken);
    }
}
