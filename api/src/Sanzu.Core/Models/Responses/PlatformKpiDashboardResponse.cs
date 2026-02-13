using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class PlatformKpiDashboardResponse
{
    public int PeriodDays { get; init; }
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public DateTime BaselineStart { get; init; }
    public DateTime BaselineEnd { get; init; }
    public DateTime GeneratedAt { get; init; }
    public PlatformKpiMetricsResponse Current { get; init; } = new();
    public PlatformKpiMetricsResponse Baseline { get; init; } = new();
    public PlatformKpiTrendResponse Trend { get; init; } = new();
    public IReadOnlyList<PlatformKpiTenantContributionResponse> TenantContributions { get; init; } =
        Array.Empty<PlatformKpiTenantContributionResponse>();

    public IReadOnlyList<PlatformKpiCaseContributionResponse> CaseContributions { get; init; } =
        Array.Empty<PlatformKpiCaseContributionResponse>();
}

public sealed class PlatformKpiMetricsResponse
{
    public int TenantsTotal { get; init; }
    public int TenantsActive { get; init; }
    public int CasesCreated { get; init; }
    public int CasesClosed { get; init; }
    public int ActiveCases { get; init; }
    public int DocumentsUploaded { get; init; }
}

public sealed class PlatformKpiTrendResponse
{
    public decimal CasesCreatedChangePercent { get; init; }
    public decimal CasesClosedChangePercent { get; init; }
    public decimal ActiveCasesChangePercent { get; init; }
    public decimal DocumentsUploadedChangePercent { get; init; }
}

public sealed class PlatformKpiTenantContributionResponse
{
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public int CasesCreated { get; init; }
    public int CasesClosed { get; init; }
    public int ActiveCases { get; init; }
    public int DocumentsUploaded { get; init; }
}

public sealed class PlatformKpiCaseContributionResponse
{
    public Guid CaseId { get; init; }
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public string CaseNumber { get; init; } = string.Empty;
    public CaseStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public int DocumentsUploaded { get; init; }
}
