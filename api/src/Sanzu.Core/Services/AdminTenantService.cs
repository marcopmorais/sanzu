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

    public AdminTenantService(
        IOrganizationRepository organizationRepository,
        ITenantHealthScoreRepository healthScoreRepository)
    {
        _organizationRepository = organizationRepository;
        _healthScoreRepository = healthScoreRepository;
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

        var mapped = items.Select(o => MapToResponse(o, scoreLookup)).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new PaginatedResponse<TenantListItemResponse>(mapped, page, pageSize, totalCount, totalPages);
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
            .Select(x => MapToResponse(x.Org, scoreLookup))
            .ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new PaginatedResponse<TenantListItemResponse>(paged, page, pageSize, totalCount, totalPages);
    }

    private static TenantListItemResponse MapToResponse(
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
}
