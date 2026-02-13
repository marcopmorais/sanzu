using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IWorkflowStepRepository
{
    Task CreateStepsAsync(IEnumerable<WorkflowStepInstance> steps, CancellationToken cancellationToken);
    Task CreateDependenciesAsync(IEnumerable<WorkflowStepDependency> dependencies, CancellationToken cancellationToken);
    Task DeletePlanByCaseIdAsync(Guid caseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkflowStepInstance>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkflowStepDependency>> GetDependenciesByCaseIdAsync(Guid caseId, CancellationToken cancellationToken);
}
