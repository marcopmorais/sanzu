using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class FleetPostureService : IFleetPostureService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly IWorkflowStepRepository _workflowStepRepository;
    private readonly ICaseDocumentRepository _caseDocumentRepository;

    public FleetPostureService(
        IOrganizationRepository organizationRepository,
        IUserRoleRepository userRoleRepository,
        ICaseRepository caseRepository,
        IWorkflowStepRepository workflowStepRepository,
        ICaseDocumentRepository caseDocumentRepository)
    {
        _organizationRepository = organizationRepository;
        _userRoleRepository = userRoleRepository;
        _caseRepository = caseRepository;
        _workflowStepRepository = workflowStepRepository;
        _caseDocumentRepository = caseDocumentRepository;
    }

    public async Task<FleetPostureResponse> GetFleetPostureAsync(
        Guid actorUserId,
        string? search,
        string? statusFilter,
        CancellationToken cancellationToken)
    {
        await EnsureSanzuAdminAccessAsync(actorUserId, cancellationToken);

        var allTenants = await _organizationRepository.GetAllAsync(cancellationToken);

        var filteredTenants = allTenants.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filteredTenants = filteredTenants.Where(
                t => t.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                     || t.Location.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<TenantStatus>(statusFilter, true, out var status))
        {
            filteredTenants = filteredTenants.Where(t => t.Status == status);
        }

        var tenantList = filteredTenants.ToList();
        var postures = new List<TenantPostureResponse>(tenantList.Count);

        foreach (var tenant in tenantList)
        {
            var cases = await _caseRepository.GetByTenantIdForPlatformAsync(tenant.Id, cancellationToken);
            var activeCases = cases.Count(c => c.Status == CaseStatus.Active);

            var blockedTasks = 0;
            foreach (var caseEntity in cases.Where(c => c.Status == CaseStatus.Active))
            {
                var steps = await _workflowStepRepository.GetByCaseIdAsync(caseEntity.Id, cancellationToken);
                blockedTasks += steps.Count(s => s.Status == WorkflowStepStatus.Blocked);
            }

            postures.Add(new TenantPostureResponse
            {
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                Location = tenant.Location,
                Status = tenant.Status.ToString(),
                SubscriptionPlan = tenant.SubscriptionPlan,
                ActiveCases = activeCases,
                BlockedTasks = blockedTasks,
                OpenKpiAlerts = 0,
                FailedPaymentAttempts = tenant.FailedPaymentAttempts,
                CreatedAt = tenant.CreatedAt,
                OnboardingCompletedAt = tenant.OnboardingCompletedAt
            });
        }

        return new FleetPostureResponse
        {
            TotalTenants = allTenants.Count,
            ActiveTenants = allTenants.Count(t => t.Status == TenantStatus.Active),
            OnboardingTenants = allTenants.Count(t => t.Status == TenantStatus.Onboarding),
            PaymentIssueTenants = allTenants.Count(t => t.Status == TenantStatus.PaymentIssue),
            SuspendedTenants = allTenants.Count(t => t.Status == TenantStatus.Suspended),
            GeneratedAt = DateTime.UtcNow,
            Tenants = postures
                .OrderByDescending(p => p.BlockedTasks)
                .ThenByDescending(p => p.FailedPaymentAttempts)
                .ThenBy(p => p.TenantName)
                .ToList()
        };
    }

    public async Task<TenantDrilldownResponse> GetTenantDrilldownAsync(
        Guid actorUserId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await EnsureSanzuAdminAccessAsync(actorUserId, cancellationToken);

        var tenant = await _organizationRepository.GetByIdForPlatformAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new CaseStateException("Tenant not found.");
        }

        var cases = await _caseRepository.GetByTenantIdForPlatformAsync(tenantId, cancellationToken);

        var totalDocuments = 0;
        var allBlockedSteps = new List<Entities.WorkflowStepInstance>();

        foreach (var caseEntity in cases)
        {
            var docs = await _caseDocumentRepository.GetByCaseIdForPlatformAsync(caseEntity.Id, cancellationToken);
            totalDocuments += docs.Count;

            var steps = await _workflowStepRepository.GetByCaseIdAsync(caseEntity.Id, cancellationToken);
            allBlockedSteps.AddRange(steps.Where(s => s.Status == WorkflowStepStatus.Blocked));
        }

        var completedTasks = 0;
        foreach (var caseEntity in cases)
        {
            var steps = await _workflowStepRepository.GetByCaseIdAsync(caseEntity.Id, cancellationToken);
            completedTasks += steps.Count(s => s.Status == WorkflowStepStatus.Complete);
        }

        var blockedByReason = allBlockedSteps
            .Where(s => s.BlockedReasonCode.HasValue)
            .GroupBy(s => s.BlockedReasonCode!.Value)
            .Select(g => new ReasonCodeCountResponse
            {
                ReasonCategory = g.Key.ToString(),
                Label = GetReasonLabel(g.Key),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        return new TenantDrilldownResponse
        {
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            Location = tenant.Location,
            Status = tenant.Status.ToString(),
            SubscriptionPlan = tenant.SubscriptionPlan,
            SubscriptionBillingCycle = tenant.SubscriptionBillingCycle,
            FailedPaymentAttempts = tenant.FailedPaymentAttempts,
            CreatedAt = tenant.CreatedAt,
            OnboardingCompletedAt = tenant.OnboardingCompletedAt,
            SubscriptionActivatedAt = tenant.SubscriptionActivatedAt,
            Metrics = new TenantDrilldownMetrics
            {
                TotalCases = cases.Count,
                ActiveCases = cases.Count(c => c.Status == CaseStatus.Active),
                ClosedCases = cases.Count(c => c.ClosedAt.HasValue),
                BlockedTasks = allBlockedSteps.Count,
                CompletedTasks = completedTasks,
                DocumentsUploaded = totalDocuments
            },
            BlockedByReason = blockedByReason
        };
    }

    private static string GetReasonLabel(BlockedReasonCode code) => code switch
    {
        BlockedReasonCode.EvidenceMissing => "Evidence Missing",
        BlockedReasonCode.ExternalDependency => "External Dependency",
        BlockedReasonCode.PolicyRestriction => "Policy Restriction",
        BlockedReasonCode.RolePermission => "Role / Permission",
        BlockedReasonCode.DeadlineRisk => "Deadline Risk",
        BlockedReasonCode.PaymentOrBilling => "Payment or Billing",
        BlockedReasonCode.IdentityOrAuth => "Identity / Auth",
        BlockedReasonCode.DataMismatch => "Data Mismatch",
        BlockedReasonCode.SystemError => "System Error",
        _ => code.ToString()
    };

    private async Task EnsureSanzuAdminAccessAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var actorRoles = await _userRoleRepository.GetByUserIdAsync(actorUserId, cancellationToken);
        var hasPlatformRole = actorRoles.Any(
            role => role.RoleType == PlatformRole.SanzuAdmin
                    && (role.TenantId == null || role.TenantId == Guid.Empty));

        if (!hasPlatformRole)
        {
            throw new TenantAccessDeniedException();
        }
    }
}
