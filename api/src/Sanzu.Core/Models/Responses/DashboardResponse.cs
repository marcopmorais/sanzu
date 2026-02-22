namespace Sanzu.Core.Models.Responses;

public sealed record DashboardResponse<T>(
    T Data,
    DateTime ComputedAt,
    bool IsStale
);
