using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class AdminQueueService : IAdminQueueService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly IWorkflowStepRepository _workflowStepRepository;
    private readonly IAuditRepository _auditRepository;

    private static readonly (string Id, string Name, string Scope)[] QueueDefinitions =
    [
        ("ADM_OnboardingStuck", "Onboarding stuck", "tenant"),
        ("ADM_ComplianceException", "Compliance exception", "tenant"),
        ("ADM_KpiThresholdBreach", "KPI threshold breach", "tenant"),
        ("ADM_FailedPayment", "Failed payment", "tenant"),
        ("ADM_SupportEscalation", "Support escalation", "tenant")
    ];

    public AdminQueueService(
        IOrganizationRepository organizationRepository,
        IUserRoleRepository userRoleRepository,
        ICaseRepository caseRepository,
        IWorkflowStepRepository workflowStepRepository,
        IAuditRepository auditRepository)
    {
        _organizationRepository = organizationRepository;
        _userRoleRepository = userRoleRepository;
        _caseRepository = caseRepository;
        _workflowStepRepository = workflowStepRepository;
        _auditRepository = auditRepository;
    }

    public async Task<AdminQueueListResponse> ListQueuesAsync(
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await EnsureSanzuAdminAccessAsync(actorUserId, cancellationToken);

        var tenants = await _organizationRepository.GetAllAsync(cancellationToken);
        var items = await ComputeAllQueueItemsAsync(tenants, cancellationToken);

        var queues = QueueDefinitions
            .Select(q => new AdminQueueSummary
            {
                QueueId = q.Id,
                Name = q.Name,
                Scope = q.Scope,
                ItemCount = items.Count(i => i.QueueId == q.Id)
            })
            .ToList();

        return new AdminQueueListResponse
        {
            GeneratedAt = DateTime.UtcNow,
            Queues = queues
        };
    }

    public async Task<AdminQueueItemsResponse> GetQueueItemsAsync(
        Guid actorUserId,
        string queueId,
        CancellationToken cancellationToken)
    {
        await EnsureSanzuAdminAccessAsync(actorUserId, cancellationToken);

        var queueDef = QueueDefinitions.FirstOrDefault(q => q.Id == queueId);
        if (queueDef == default)
        {
            throw new CaseStateException("Queue not found.");
        }

        var tenants = await _organizationRepository.GetAllAsync(cancellationToken);
        var allItems = await ComputeAllQueueItemsAsync(tenants, cancellationToken);

        var queueItems = allItems
            .Where(i => i.QueueId == queueId)
            .Select(i => new AdminQueueItem
            {
                ItemId = i.ItemId,
                TenantId = i.TenantId,
                TenantName = i.TenantName,
                ReasonCategory = i.ReasonCategory,
                ReasonLabel = i.ReasonLabel,
                Summary = i.Summary,
                DetectedAt = i.DetectedAt
            })
            .OrderByDescending(i => i.DetectedAt)
            .ToList();

        return new AdminQueueItemsResponse
        {
            QueueId = queueId,
            QueueName = queueDef.Name,
            GeneratedAt = DateTime.UtcNow,
            Items = queueItems
        };
    }

    public async Task<AdminEventStreamResponse> GetTenantEventStreamAsync(
        Guid actorUserId,
        Guid tenantId,
        int limit,
        CancellationToken cancellationToken)
    {
        await EnsureSanzuAdminAccessAsync(actorUserId, cancellationToken);

        var tenant = await _organizationRepository.GetByIdForPlatformAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new CaseStateException("Tenant not found.");
        }

        var periodStart = DateTime.UtcNow.AddDays(-90);
        var periodEnd = DateTime.UtcNow.AddDays(1);

        var auditEvents = await _auditRepository.GetByTenantIdInPeriodAsync(
            tenantId, periodStart, periodEnd, cancellationToken);

        var entries = auditEvents
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .Select(e => new AdminEventStreamEntry
            {
                EventId = e.Id,
                EventType = e.EventType,
                ReasonCategory = ExtractReasonCategory(e.Metadata),
                SafeSummary = BuildSafeSummary(e.EventType),
                CreatedAt = e.CreatedAt
            })
            .ToList();

        return new AdminEventStreamResponse
        {
            TenantId = tenantId,
            TenantName = tenant.Name,
            GeneratedAt = DateTime.UtcNow,
            Events = entries
        };
    }

    private async Task<List<QueueItemInternal>> ComputeAllQueueItemsAsync(
        IReadOnlyList<Entities.Organization> tenants,
        CancellationToken cancellationToken)
    {
        var items = new List<QueueItemInternal>();

        foreach (var tenant in tenants)
        {
            // ADM_OnboardingStuck: pending/onboarding for > 7 days
            if (tenant.Status is TenantStatus.Pending or TenantStatus.Onboarding
                && tenant.CreatedAt < DateTime.UtcNow.AddDays(-7))
            {
                items.Add(new QueueItemInternal
                {
                    QueueId = "ADM_OnboardingStuck",
                    ItemId = $"onboarding-{tenant.Id}",
                    TenantId = tenant.Id,
                    TenantName = tenant.Name,
                    ReasonCategory = "OnboardingStalled",
                    ReasonLabel = "Onboarding Stalled",
                    Summary = $"Tenant has been in {tenant.Status} status since {tenant.CreatedAt:yyyy-MM-dd}.",
                    DetectedAt = tenant.CreatedAt
                });
            }

            // ADM_FailedPayment
            if (tenant.FailedPaymentAttempts > 0)
            {
                items.Add(new QueueItemInternal
                {
                    QueueId = "ADM_FailedPayment",
                    ItemId = $"payment-{tenant.Id}",
                    TenantId = tenant.Id,
                    TenantName = tenant.Name,
                    ReasonCategory = "PaymentOrBilling",
                    ReasonLabel = "Payment or Billing",
                    Summary = $"{tenant.FailedPaymentAttempts} failed payment attempt(s).",
                    DetectedAt = tenant.LastPaymentFailedAt ?? tenant.UpdatedAt
                });
            }

            // ADM_ComplianceException: suspended tenants
            if (tenant.Status == TenantStatus.Suspended)
            {
                items.Add(new QueueItemInternal
                {
                    QueueId = "ADM_ComplianceException",
                    ItemId = $"compliance-{tenant.Id}",
                    TenantId = tenant.Id,
                    TenantName = tenant.Name,
                    ReasonCategory = "PolicyRestriction",
                    ReasonLabel = "Policy Restriction",
                    Summary = "Tenant is suspended — compliance review required.",
                    DetectedAt = tenant.UpdatedAt
                });
            }

            // ADM_SupportEscalation: check for blocked tasks with SystemError
            if (tenant.Status == TenantStatus.Active)
            {
                var cases = await _caseRepository.GetByTenantIdForPlatformAsync(tenant.Id, cancellationToken);
                foreach (var caseEntity in cases.Where(c => c.Status == CaseStatus.Active))
                {
                    var steps = await _workflowStepRepository.GetByCaseIdAsync(caseEntity.Id, cancellationToken);
                    var systemErrors = steps.Where(
                        s => s.Status == WorkflowStepStatus.Blocked && s.BlockedReasonCode == BlockedReasonCode.SystemError).ToList();

                    if (systemErrors.Count >= 2)
                    {
                        items.Add(new QueueItemInternal
                        {
                            QueueId = "ADM_SupportEscalation",
                            ItemId = $"escalation-{tenant.Id}-{caseEntity.Id}",
                            TenantId = tenant.Id,
                            TenantName = tenant.Name,
                            ReasonCategory = "SystemError",
                            ReasonLabel = "System Error",
                            Summary = $"{systemErrors.Count} system error blocks in case {caseEntity.CaseNumber}.",
                            DetectedAt = systemErrors.Max(s => s.UpdatedAt)
                        });
                    }
                }
            }
        }

        return items;
    }

    private static string? ExtractReasonCategory(string metadata)
    {
        if (string.IsNullOrEmpty(metadata)) return null;
        // Simple extraction — look for BlockedReasonCode in metadata
        if (metadata.Contains("BlockedReasonCode", StringComparison.OrdinalIgnoreCase))
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                metadata, @"""BlockedReasonCode""\s*:\s*""?(\w+)""?");
            return match.Success ? match.Groups[1].Value : null;
        }
        return null;
    }

    private static string BuildSafeSummary(string eventType) => eventType switch
    {
        "CaseCreated" => "A new case was created.",
        "CaseClosed" => "A case was closed.",
        "DocumentUploaded" => "A document was uploaded.",
        "PlaybookApplied" => "A playbook was applied to a case.",
        "ExportGenerated" => "An audit export was generated.",
        "TaskBlocked" => "A workflow task was blocked.",
        "TaskCompleted" => "A workflow task was completed.",
        _ => $"Event: {eventType}"
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

    private sealed class QueueItemInternal
    {
        public string QueueId { get; init; } = string.Empty;
        public string ItemId { get; init; } = string.Empty;
        public Guid TenantId { get; init; }
        public string TenantName { get; init; } = string.Empty;
        public string ReasonCategory { get; init; } = string.Empty;
        public string ReasonLabel { get; init; } = string.Empty;
        public string Summary { get; init; } = string.Empty;
        public DateTime DetectedAt { get; init; }
    }
}
