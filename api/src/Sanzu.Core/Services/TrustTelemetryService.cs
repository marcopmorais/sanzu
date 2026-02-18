using FluentValidation;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class TrustTelemetryService : ITrustTelemetryService
{
    private readonly IAuditRepository _auditRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly ICaseDocumentRepository _caseDocumentRepository;
    private readonly IWorkflowStepRepository _workflowStepRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IAgencyPlaybookRepository _agencyPlaybookRepository;

    public TrustTelemetryService(
        IAuditRepository auditRepository,
        ICaseRepository caseRepository,
        ICaseDocumentRepository caseDocumentRepository,
        IWorkflowStepRepository workflowStepRepository,
        IUserRoleRepository userRoleRepository,
        IAgencyPlaybookRepository agencyPlaybookRepository)
    {
        _auditRepository = auditRepository;
        _caseRepository = caseRepository;
        _caseDocumentRepository = caseDocumentRepository;
        _workflowStepRepository = workflowStepRepository;
        _userRoleRepository = userRoleRepository;
        _agencyPlaybookRepository = agencyPlaybookRepository;
    }

    public async Task<TrustTelemetryResponse> GetTenantTelemetryAsync(
        Guid tenantId,
        Guid actorUserId,
        int periodDays,
        CancellationToken cancellationToken)
    {
        ValidatePeriodDays(periodDays);
        await EnsureAgencyAdminAccessAsync(actorUserId, tenantId, cancellationToken);

        var today = DateTime.UtcNow.Date;
        var periodStart = today.AddDays(-(periodDays - 1));
        var periodEndExclusive = today.AddDays(1);

        var cases = await _caseRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        var auditEvents = await _auditRepository.GetByTenantIdInPeriodAsync(tenantId, periodStart, periodEndExclusive, cancellationToken);
        var workflowSteps = await _workflowStepRepository.GetByTenantIdInPeriodAsync(tenantId, periodStart, periodEndExclusive, cancellationToken);

        var casesInPeriod = cases.Where(c => c.CreatedAt >= periodStart && c.CreatedAt < periodEndExclusive).ToList();
        var casesClosedInPeriod = cases.Where(c => c.ClosedAt.HasValue && c.ClosedAt.Value >= periodStart && c.ClosedAt.Value < periodEndExclusive).ToList();

        var documentsUploaded = 0;
        foreach (var caseEntity in casesInPeriod)
        {
            var docs = await _caseDocumentRepository.GetByCaseIdAsync(caseEntity.Id, cancellationToken);
            documentsUploaded += docs.Count(d => d.CreatedAt >= periodStart && d.CreatedAt < periodEndExclusive);
        }

        var metrics = new TrustTelemetryMetricsResponse
        {
            CasesCreated = casesInPeriod.Count,
            CasesClosed = casesClosedInPeriod.Count,
            TasksBlocked = workflowSteps.Count(s => s.Status == WorkflowStepStatus.Blocked),
            TasksCompleted = workflowSteps.Count(s => s.Status == WorkflowStepStatus.Complete),
            PlaybooksApplied = auditEvents.Count(e => e.EventType == "PlaybookApplied"),
            DocumentsUploaded = documentsUploaded
        };

        var blockedByReason = BuildBlockedByReason(workflowSteps);
        var eventSummary = BuildEventSummary(auditEvents);

        return new TrustTelemetryResponse
        {
            TenantId = tenantId,
            PeriodDays = periodDays,
            PeriodStart = periodStart,
            PeriodEnd = today,
            GeneratedAt = DateTime.UtcNow,
            Metrics = metrics,
            BlockedByReason = blockedByReason,
            EventSummary = eventSummary
        };
    }

    public async Task<TrustTelemetryResponse> GetPlatformTelemetryAsync(
        Guid actorUserId,
        int periodDays,
        CancellationToken cancellationToken)
    {
        ValidatePeriodDays(periodDays);
        await EnsureSanzuAdminAccessAsync(actorUserId, cancellationToken);

        var today = DateTime.UtcNow.Date;
        var periodStart = today.AddDays(-(periodDays - 1));
        var periodEndExclusive = today.AddDays(1);

        var auditEvents = await _auditRepository.GetAllInPeriodAsync(periodStart, periodEndExclusive, cancellationToken);
        var workflowSteps = await _workflowStepRepository.GetAllInPeriodAsync(periodStart, periodEndExclusive, cancellationToken);

        var metrics = new TrustTelemetryMetricsResponse
        {
            CasesCreated = auditEvents.Count(e => e.EventType == "CaseCreated"),
            CasesClosed = auditEvents.Count(e => e.EventType == "CaseClosed"),
            TasksBlocked = workflowSteps.Count(s => s.Status == WorkflowStepStatus.Blocked),
            TasksCompleted = workflowSteps.Count(s => s.Status == WorkflowStepStatus.Complete),
            PlaybooksApplied = auditEvents.Count(e => e.EventType == "PlaybookApplied"),
            DocumentsUploaded = auditEvents.Count(e => e.EventType == "DocumentUploaded")
        };

        var blockedByReason = BuildBlockedByReason(workflowSteps);
        var eventSummary = BuildEventSummary(auditEvents);

        return new TrustTelemetryResponse
        {
            TenantId = null,
            PeriodDays = periodDays,
            PeriodStart = periodStart,
            PeriodEnd = today,
            GeneratedAt = DateTime.UtcNow,
            Metrics = metrics,
            BlockedByReason = blockedByReason,
            EventSummary = eventSummary
        };
    }

    private static void ValidatePeriodDays(int periodDays)
    {
        if (periodDays is < 7 or > 365)
        {
            throw new ValidationException("PeriodDays must be between 7 and 365.");
        }
    }

    private static IReadOnlyList<ReasonCodeCountResponse> BuildBlockedByReason(
        IReadOnlyList<Entities.WorkflowStepInstance> workflowSteps)
    {
        return workflowSteps
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
    }

    private static IReadOnlyList<TrustTelemetryEventSummaryResponse> BuildEventSummary(
        IReadOnlyList<Entities.AuditEvent> auditEvents)
    {
        return auditEvents
            .GroupBy(e => e.EventType)
            .Select(g => new TrustTelemetryEventSummaryResponse
            {
                EventType = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();
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

    private async Task EnsureAgencyAdminAccessAsync(Guid actorUserId, Guid tenantId, CancellationToken cancellationToken)
    {
        var hasRole = await _userRoleRepository.HasRoleAsync(actorUserId, tenantId, PlatformRole.AgencyAdmin, cancellationToken);
        if (!hasRole)
        {
            throw new TenantAccessDeniedException();
        }
    }

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
