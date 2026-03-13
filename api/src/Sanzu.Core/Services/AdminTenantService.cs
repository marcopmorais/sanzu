using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class AdminTenantService : IAdminTenantService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ITenantHealthScoreRepository _healthScoreRepository;
    private readonly IBillingRecordRepository _billingRecordRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly IAuditRepository _auditRepository;

    public AdminTenantService(
        IOrganizationRepository organizationRepository,
        ITenantHealthScoreRepository healthScoreRepository,
        IBillingRecordRepository billingRecordRepository,
        ICaseRepository caseRepository,
        IAuditRepository auditRepository)
    {
        _organizationRepository = organizationRepository;
        _healthScoreRepository = healthScoreRepository;
        _billingRecordRepository = billingRecordRepository;
        _caseRepository = caseRepository;
        _auditRepository = auditRepository;
    }

    public async Task<PaginatedResponse<TenantListItemResponse>> ListTenantsAsync(
        TenantListRequest request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var page = Math.Max(request.Page, 1);

        var latestScores = await _healthScoreRepository.GetLatestForAllTenantsAsync(cancellationToken);
        var scoreLookup = latestScores.ToDictionary(s => s.TenantId);

        // When filtering by healthBand, we need to filter in memory after join
        var needsHealthFilter = !string.IsNullOrWhiteSpace(request.HealthBand)
            && Enum.TryParse<HealthBand>(request.HealthBand, ignoreCase: true, out _);
        var sortByHealth = string.Equals(request.Sort, "healthScore", StringComparison.OrdinalIgnoreCase);

        if (needsHealthFilter || sortByHealth)
        {
            return await ListWithHealthJoinAsync(request, scoreLookup, page, pageSize, cancellationToken);
        }

        var (items, totalCount) = await _organizationRepository.SearchForPlatformAsync(request, cancellationToken);

        var mapped = items.Select(o => MapToListResponse(o, scoreLookup)).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new PaginatedResponse<TenantListItemResponse>(mapped, page, pageSize, totalCount, totalPages);
    }

    public async Task<TenantSummaryResponse?> GetTenantSummaryAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var org = await _organizationRepository.GetByIdForPlatformAsync(tenantId, cancellationToken);
        if (org is null) return null;

        var healthScore = await _healthScoreRepository.GetLatestByTenantIdAsync(tenantId, cancellationToken);

        return new TenantSummaryResponse
        {
            Id = org.Id,
            Name = org.Name,
            Status = org.Status.ToString(),
            PlanTier = org.SubscriptionPlan,
            SignupDate = org.CreatedAt,
            Region = string.IsNullOrWhiteSpace(org.Location) ? null : org.Location,
            ContactEmail = org.InvoiceProfileBillingEmail,
            HealthScore = healthScore?.OverallScore,
            HealthBand = healthScore?.HealthBand.ToString()
        };
    }

    public async Task<TenantBillingResponse?> GetTenantBillingAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var org = await _organizationRepository.GetByIdForPlatformAsync(tenantId, cancellationToken);
        if (org is null) return null;

        var billingRecords = await _billingRecordRepository.GetByTenantIdForPlatformAsync(tenantId, cancellationToken);

        var billingHealth = DeriveBillingHealth(org);
        var gracePeriodActive = org.NextPaymentRetryAt != null && org.NextPaymentRetryAt > DateTime.UtcNow;
        var latestRecord = billingRecords.FirstOrDefault();

        return new TenantBillingResponse
        {
            SubscriptionPlan = org.SubscriptionPlan,
            BillingCycle = org.SubscriptionBillingCycle,
            SubscriptionActivatedAt = org.SubscriptionActivatedAt,
            BillingHealth = billingHealth,
            LastPaymentDate = latestRecord?.CreatedAt,
            NextRenewalDate = latestRecord?.BillingCycleEnd,
            GracePeriodActive = gracePeriodActive,
            GracePeriodRetryAt = org.NextPaymentRetryAt,
            RecentInvoices = billingRecords
                .Take(5)
                .Select(r => new TenantBillingInvoiceItem(
                    r.InvoiceNumber,
                    r.BillingCycleStart,
                    r.BillingCycleEnd,
                    r.TotalAmount,
                    r.Currency,
                    r.Status,
                    r.CreatedAt))
                .ToList()
        };
    }

    public async Task<TenantCasesResponse?> GetTenantCasesAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var org = await _organizationRepository.GetByIdForPlatformAsync(tenantId, cancellationToken);
        if (org is null) return null;

        var cases = await _caseRepository.GetByTenantIdWithStepsForPlatformAsync(tenantId, cancellationToken);

        return new TenantCasesResponse
        {
            Cases = cases.Select(MapToCaseItem).ToList()
        };
    }

    public async Task<TenantActivityResponse?> GetTenantActivityAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var org = await _organizationRepository.GetByIdForPlatformAsync(tenantId, cancellationToken);
        if (org is null) return null;

        var periodEnd = DateTime.UtcNow;
        var periodStart = periodEnd.AddDays(-30);
        var events = await _auditRepository.GetByTenantIdInPeriodAsync(tenantId, periodStart, periodEnd, cancellationToken);

        return new TenantActivityResponse
        {
            Events = events
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new TenantActivityItem
                {
                    EventType = e.EventType,
                    ActorUserId = e.ActorUserId,
                    Timestamp = e.CreatedAt,
                    CaseId = e.CaseId,
                    Metadata = e.Metadata
                })
                .ToList()
        };
    }

    private async Task<PaginatedResponse<TenantListItemResponse>> ListWithHealthJoinAsync(
        TenantListRequest request,
        Dictionary<Guid, TenantHealthScore> scoreLookup,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Fetch all matching orgs (without pagination — we need to join + sort/filter in memory)
        var unbounded = new TenantListRequest
        {
            Name = request.Name,
            Status = request.Status,
            PlanTier = request.PlanTier,
            SignupDateFrom = request.SignupDateFrom,
            SignupDateTo = request.SignupDateTo,
            Page = 1,
            PageSize = 10_000 // upper bound for safety
        };

        var (allItems, _) = await _organizationRepository.SearchForPlatformAsync(unbounded, cancellationToken);

        var joined = allItems.Select(o =>
        {
            scoreLookup.TryGetValue(o.Id, out var score);
            return (Org: o, Score: score);
        });

        // Health band filter
        if (!string.IsNullOrWhiteSpace(request.HealthBand)
            && Enum.TryParse<HealthBand>(request.HealthBand, ignoreCase: true, out var band))
        {
            joined = joined.Where(x => x.Score?.HealthBand == band);
        }

        var filtered = joined.ToList();
        var totalCount = filtered.Count;

        // Sort
        IEnumerable<(Organization Org, TenantHealthScore? Score)> sorted;
        var descending = string.Equals(request.Order, "desc", StringComparison.OrdinalIgnoreCase);

        if (string.Equals(request.Sort, "healthScore", StringComparison.OrdinalIgnoreCase))
        {
            sorted = descending
                ? filtered.OrderByDescending(x => x.Score?.OverallScore ?? -1)
                : filtered.OrderBy(x => x.Score?.OverallScore ?? int.MaxValue);
        }
        else
        {
            sorted = filtered.OrderByDescending(x => x.Org.CreatedAt);
        }

        var paged = sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToListResponse(x.Org, scoreLookup))
            .ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new PaginatedResponse<TenantListItemResponse>(paged, page, pageSize, totalCount, totalPages);
    }

    private static TenantListItemResponse MapToListResponse(
        Organization org,
        Dictionary<Guid, TenantHealthScore> scoreLookup)
    {
        scoreLookup.TryGetValue(org.Id, out var score);

        return new TenantListItemResponse
        {
            Id = org.Id,
            Name = org.Name,
            Status = org.Status.ToString(),
            PlanTier = org.SubscriptionPlan,
            HealthScore = score?.OverallScore,
            HealthBand = score?.HealthBand.ToString(),
            SignupDate = org.CreatedAt,
            Region = string.IsNullOrWhiteSpace(org.Location) ? null : org.Location
        };
    }

    private static string DeriveBillingHealth(Organization org)
    {
        if (org.FailedPaymentAttempts == 0 || org.LastPaymentFailedAt is null)
            return "Paid";

        if (org.NextPaymentRetryAt != null && org.NextPaymentRetryAt > DateTime.UtcNow)
            return "Overdue";

        return "Failed";
    }

    private static TenantCaseItem MapToCaseItem(Case c)
    {
        var steps = c.WorkflowSteps?.ToList() ?? [];

        var completedCount = steps.Count(s =>
            s.Status is WorkflowStepStatus.Complete or WorkflowStepStatus.Skipped);
        var inProgressCount = steps.Count(s =>
            s.Status is WorkflowStepStatus.InProgress or WorkflowStepStatus.AwaitingEvidence);
        var blockedCount = steps.Count(s =>
            s.Status == WorkflowStepStatus.Blocked);

        var blockedSteps = steps
            .Where(s => s.Status == WorkflowStepStatus.Blocked)
            .Select(s => new TenantCaseBlockedStep
            {
                StepKey = s.StepKey,
                Title = s.Title,
                BlockedReasonCode = s.BlockedReasonCode?.ToString(),
                BlockedReasonDetail = s.BlockedReasonDetail
            })
            .ToList();

        return new TenantCaseItem
        {
            CaseId = c.Id,
            CaseNumber = c.CaseNumber,
            DeceasedFullName = c.DeceasedFullName,
            Status = c.Status.ToString(),
            CreatedAt = c.CreatedAt,
            WorkflowKey = c.WorkflowKey,
            WorkflowProgress = new TenantCaseWorkflowProgress
            {
                TotalSteps = steps.Count,
                CompletedSteps = completedCount,
                InProgressSteps = inProgressCount,
                BlockedSteps = blockedCount
            },
            BlockedSteps = blockedSteps
        };
    }
}
