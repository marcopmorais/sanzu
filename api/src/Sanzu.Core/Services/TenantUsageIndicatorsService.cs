using FluentValidation;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class TenantUsageIndicatorsService : ITenantUsageIndicatorsService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly ICaseDocumentRepository _caseDocumentRepository;

    public TenantUsageIndicatorsService(
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

    public async Task<TenantUsageIndicatorsResponse> GetUsageIndicatorsAsync(
        Guid tenantId,
        Guid actorUserId,
        int periodDays,
        CancellationToken cancellationToken)
    {
        if (periodDays is < 1 or > 365)
        {
            throw new ValidationException("PeriodDays must be between 1 and 365.");
        }

        await LoadAuthorizedTenantAsync(tenantId, actorUserId, cancellationToken);

        var today = DateTime.UtcNow.Date;
        var periodStart = today.AddDays(-(periodDays - 1));
        var periodEnd = today;
        var periodEndExclusive = today.AddDays(1);

        var tenantCases = await _caseRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        var documents = new List<CaseDocument>();
        foreach (var caseEntity in tenantCases)
        {
            var caseDocuments = await _caseDocumentRepository.GetByCaseIdAsync(caseEntity.Id, cancellationToken);
            documents.AddRange(caseDocuments);
        }

        var current = new TenantUsageCurrentMetricsResponse
        {
            CasesCreated = tenantCases.Count(x => x.CreatedAt >= periodStart && x.CreatedAt < periodEndExclusive),
            CasesClosed = tenantCases.Count(
                x => x.ClosedAt.HasValue && x.ClosedAt.Value >= periodStart && x.ClosedAt.Value < periodEndExclusive),
            ActiveCases = tenantCases.Count(x => x.Status == CaseStatus.Active),
            DocumentsUploaded = documents.Count(x => x.CreatedAt >= periodStart && x.CreatedAt < periodEndExclusive)
        };

        var history = new List<TenantUsageHistoryPointResponse>(capacity: periodDays);
        for (var offset = 0; offset < periodDays; offset++)
        {
            var day = periodStart.AddDays(offset);
            var nextDay = day.AddDays(1);

            history.Add(
                new TenantUsageHistoryPointResponse
                {
                    Date = day,
                    CasesCreated = tenantCases.Count(x => x.CreatedAt >= day && x.CreatedAt < nextDay),
                    CasesClosed = tenantCases.Count(
                        x => x.ClosedAt.HasValue && x.ClosedAt.Value >= day && x.ClosedAt.Value < nextDay),
                    DocumentsUploaded = documents.Count(x => x.CreatedAt >= day && x.CreatedAt < nextDay)
                });
        }

        return new TenantUsageIndicatorsResponse
        {
            TenantId = tenantId,
            PeriodDays = periodDays,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Current = current,
            History = history
        };
    }

    private async Task<Organization> LoadAuthorizedTenantAsync(
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var tenant = await _organizationRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            throw new TenantAccessDeniedException();
        }

        var hasTenantAdminRole = await _userRoleRepository.HasRoleAsync(
            actorUserId,
            tenantId,
            PlatformRole.AgencyAdmin,
            cancellationToken);

        if (!hasTenantAdminRole)
        {
            throw new TenantAccessDeniedException();
        }

        return tenant;
    }
}
