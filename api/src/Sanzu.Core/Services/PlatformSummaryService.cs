using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class PlatformSummaryService : IPlatformSummaryService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly IWorkflowStepRepository _workflowStepRepository;
    private readonly ICaseDocumentRepository _caseDocumentRepository;

    public PlatformSummaryService(
        IOrganizationRepository organizationRepository,
        ICaseRepository caseRepository,
        IWorkflowStepRepository workflowStepRepository,
        ICaseDocumentRepository caseDocumentRepository)
    {
        _organizationRepository = organizationRepository;
        _caseRepository = caseRepository;
        _workflowStepRepository = workflowStepRepository;
        _caseDocumentRepository = caseDocumentRepository;
    }

    public async Task<PlatformOperationsSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var tenants = await _organizationRepository.GetAllAsync(cancellationToken);
        var activeTenants = tenants.Count(t => t.Status == TenantStatus.Active);

        var totalActiveCases = 0;
        var stepsCompleted = 0;
        var stepsActive = 0;
        var stepsBlocked = 0;
        var totalDocuments = 0;

        foreach (var tenant in tenants)
        {
            var cases = await _caseRepository.GetByTenantIdForPlatformAsync(tenant.Id, cancellationToken);

            var activeCases = cases.Where(c =>
                c.Status != CaseStatus.Closed
                && c.Status != CaseStatus.Archived
                && c.Status != CaseStatus.Cancelled).ToList();

            totalActiveCases += activeCases.Count;

            foreach (var caseEntity in activeCases)
            {
                var steps = await _workflowStepRepository.GetByCaseIdAsync(caseEntity.Id, cancellationToken);

                stepsCompleted += steps.Count(s => s.Status == WorkflowStepStatus.Complete);
                stepsActive += steps.Count(s =>
                    s.Status == WorkflowStepStatus.InProgress
                    || s.Status == WorkflowStepStatus.Ready
                    || s.Status == WorkflowStepStatus.AwaitingEvidence);
                stepsBlocked += steps.Count(s =>
                    s.Status == WorkflowStepStatus.Blocked
                    || s.Status == WorkflowStepStatus.Overdue);

                var documents = await _caseDocumentRepository.GetByCaseIdForPlatformAsync(caseEntity.Id, cancellationToken);
                totalDocuments += documents.Count;
            }
        }

        return new PlatformOperationsSummaryResponse
        {
            TotalActiveTenants = activeTenants,
            TotalActiveCases = totalActiveCases,
            WorkflowStepsCompleted = stepsCompleted,
            WorkflowStepsActive = stepsActive,
            WorkflowStepsBlocked = stepsBlocked,
            TotalDocuments = totalDocuments
        };
    }
}
