using FluentValidation;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class KpiDashboardService : IKpiDashboardService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly ICaseDocumentRepository _caseDocumentRepository;

    public KpiDashboardService(
        IOrganizationRepository organizationRepository,
        IUserRoleRepository userRoleRepository,
        ICaseRepository caseRepository,
        ICaseDocumentRepository caseDocumentRepository)
    {
        _organizationRepository = organizationRepository;
        _userRoleRepository = userRoleRepository;
        _caseRepository = caseRepository;
        _caseDocumentRepository = caseDocumentRepository;
    }

    public async Task<PlatformKpiDashboardResponse> GetDashboardAsync(
        Guid actorUserId,
        int periodDays,
        int tenantLimit,
        int caseLimit,
        CancellationToken cancellationToken)
    {
        if (periodDays is < 7 or > 365)
        {
            throw new ValidationException("PeriodDays must be between 7 and 365.");
        }

        if (tenantLimit is < 1 or > 100)
        {
            throw new ValidationException("TenantLimit must be between 1 and 100.");
        }

        if (caseLimit is < 1 or > 100)
        {
            throw new ValidationException("CaseLimit must be between 1 and 100.");
        }

        await EnsureSanzuAdminAccessAsync(actorUserId, cancellationToken);

        var tenants = await _organizationRepository.GetAllAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;
        var periodStart = today.AddDays(-(periodDays - 1));
        var periodEnd = today;
        var periodEndExclusive = today.AddDays(1);
        var baselineStart = periodStart.AddDays(-periodDays);
        var baselineEnd = periodStart.AddDays(-1);
        var baselineEndExclusive = periodStart;

        var tenantContributions = new List<PlatformKpiTenantContributionResponse>();
        var caseContributions = new List<PlatformKpiCaseContributionResponse>();

        var currentCasesCreated = 0;
        var currentCasesClosed = 0;
        var currentActiveCases = 0;
        var currentDocumentsUploaded = 0;

        var baselineCasesCreated = 0;
        var baselineCasesClosed = 0;
        var baselineActiveCases = 0;
        var baselineDocumentsUploaded = 0;

        foreach (var tenant in tenants)
        {
            var tenantCases = await _caseRepository.GetByTenantIdForPlatformAsync(tenant.Id, cancellationToken);
            var documentsByCaseId = await LoadDocumentsByCaseIdAsync(tenantCases, cancellationToken);

            var tenantCurrentCasesCreated = tenantCases.Count(
                x => x.CreatedAt >= periodStart && x.CreatedAt < periodEndExclusive);
            var tenantCurrentCasesClosed = tenantCases.Count(
                x => x.ClosedAt.HasValue && x.ClosedAt.Value >= periodStart && x.ClosedAt.Value < periodEndExclusive);
            var tenantCurrentActiveCases = tenantCases.Count(x => x.Status == CaseStatus.Active);
            var tenantCurrentDocumentsUploaded = documentsByCaseId.Values.Sum(
                documents => documents.Count(x => x.CreatedAt >= periodStart && x.CreatedAt < periodEndExclusive));

            currentCasesCreated += tenantCurrentCasesCreated;
            currentCasesClosed += tenantCurrentCasesClosed;
            currentActiveCases += tenantCurrentActiveCases;
            currentDocumentsUploaded += tenantCurrentDocumentsUploaded;

            baselineCasesCreated += tenantCases.Count(
                x => x.CreatedAt >= baselineStart && x.CreatedAt < baselineEndExclusive);
            baselineCasesClosed += tenantCases.Count(
                x => x.ClosedAt.HasValue && x.ClosedAt.Value >= baselineStart && x.ClosedAt.Value < baselineEndExclusive);
            baselineActiveCases += CountBaselineActiveCases(tenantCases, baselineEndExclusive);
            baselineDocumentsUploaded += documentsByCaseId.Values.Sum(
                documents => documents.Count(x => x.CreatedAt >= baselineStart && x.CreatedAt < baselineEndExclusive));

            tenantContributions.Add(
                new PlatformKpiTenantContributionResponse
                {
                    TenantId = tenant.Id,
                    TenantName = tenant.Name,
                    CasesCreated = tenantCurrentCasesCreated,
                    CasesClosed = tenantCurrentCasesClosed,
                    ActiveCases = tenantCurrentActiveCases,
                    DocumentsUploaded = tenantCurrentDocumentsUploaded
                });

            foreach (var caseEntity in tenantCases)
            {
                var caseDocuments = documentsByCaseId.GetValueOrDefault(caseEntity.Id) ?? [];
                var caseDocumentsUploaded = caseDocuments.Count(
                    x => x.CreatedAt >= periodStart && x.CreatedAt < periodEndExclusive);
                var caseCreatedInPeriod = caseEntity.CreatedAt >= periodStart && caseEntity.CreatedAt < periodEndExclusive;
                var isActiveCase = caseEntity.Status == CaseStatus.Active;

                if (!caseCreatedInPeriod && !isActiveCase && caseDocumentsUploaded == 0)
                {
                    continue;
                }

                caseContributions.Add(
                    new PlatformKpiCaseContributionResponse
                    {
                        CaseId = caseEntity.Id,
                        TenantId = tenant.Id,
                        TenantName = tenant.Name,
                        CaseNumber = caseEntity.CaseNumber,
                        Status = caseEntity.Status,
                        CreatedAt = caseEntity.CreatedAt,
                        DocumentsUploaded = caseDocumentsUploaded
                    });
            }
        }

        var currentMetrics = new PlatformKpiMetricsResponse
        {
            TenantsTotal = tenants.Count,
            TenantsActive = tenants.Count(x => x.Status == TenantStatus.Active),
            CasesCreated = currentCasesCreated,
            CasesClosed = currentCasesClosed,
            ActiveCases = currentActiveCases,
            DocumentsUploaded = currentDocumentsUploaded
        };

        var baselineMetrics = new PlatformKpiMetricsResponse
        {
            TenantsTotal = tenants.Count(x => x.CreatedAt < baselineEndExclusive),
            TenantsActive = tenants.Count(x => x.CreatedAt < baselineEndExclusive && x.Status == TenantStatus.Active),
            CasesCreated = baselineCasesCreated,
            CasesClosed = baselineCasesClosed,
            ActiveCases = baselineActiveCases,
            DocumentsUploaded = baselineDocumentsUploaded
        };

        var trendMetrics = new PlatformKpiTrendResponse
        {
            CasesCreatedChangePercent = CalculatePercentChange(currentMetrics.CasesCreated, baselineMetrics.CasesCreated),
            CasesClosedChangePercent = CalculatePercentChange(currentMetrics.CasesClosed, baselineMetrics.CasesClosed),
            ActiveCasesChangePercent = CalculatePercentChange(currentMetrics.ActiveCases, baselineMetrics.ActiveCases),
            DocumentsUploadedChangePercent = CalculatePercentChange(currentMetrics.DocumentsUploaded, baselineMetrics.DocumentsUploaded)
        };

        var topTenantContributions = tenantContributions
            .OrderByDescending(x => x.CasesCreated + x.CasesClosed + x.ActiveCases + x.DocumentsUploaded)
            .ThenBy(x => x.TenantName)
            .Take(tenantLimit)
            .ToList();

        var topCaseContributions = caseContributions
            .OrderByDescending(x => x.DocumentsUploaded)
            .ThenByDescending(x => x.CreatedAt)
            .Take(caseLimit)
            .ToList();

        return new PlatformKpiDashboardResponse
        {
            PeriodDays = periodDays,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            BaselineStart = baselineStart,
            BaselineEnd = baselineEnd,
            GeneratedAt = DateTime.UtcNow,
            Current = currentMetrics,
            Baseline = baselineMetrics,
            Trend = trendMetrics,
            TenantContributions = topTenantContributions,
            CaseContributions = topCaseContributions
        };
    }

    private async Task<Dictionary<Guid, IReadOnlyList<CaseDocument>>> LoadDocumentsByCaseIdAsync(
        IReadOnlyList<Case> tenantCases,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, IReadOnlyList<CaseDocument>>(tenantCases.Count);

        foreach (var caseEntity in tenantCases)
        {
            var caseDocuments = await _caseDocumentRepository.GetByCaseIdForPlatformAsync(caseEntity.Id, cancellationToken);
            result[caseEntity.Id] = caseDocuments.ToList();
        }

        return result;
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

    private static int CountBaselineActiveCases(IReadOnlyList<Case> tenantCases, DateTime baselineEndExclusive)
    {
        return tenantCases.Count(
            x => x.CreatedAt < baselineEndExclusive
                 && (!x.ClosedAt.HasValue || x.ClosedAt.Value >= baselineEndExclusive)
                 && x.Status != CaseStatus.Archived
                 && x.Status != CaseStatus.Cancelled);
    }

    private static decimal CalculatePercentChange(int currentValue, int baselineValue)
    {
        if (baselineValue == 0)
        {
            return currentValue == 0 ? 0m : 100m;
        }

        return decimal.Round(((currentValue - baselineValue) / (decimal)baselineValue) * 100m, 2);
    }
}
