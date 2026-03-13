namespace Sanzu.Core.Services;

public static class PlanCatalog
{
    public const int AnnualMultiplierMonths = 10;
    public const decimal VatRate = 0.23m;

    private static readonly Dictionary<string, decimal> PlanMonthlyPrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["INICIAL"] = 49m,
        ["PROFISSIONAL"] = 99m,
        ["AGENCIA"] = 199m,
        ["ENTERPRISE"] = 0m
    };

    private static readonly Dictionary<string, int> PlanIncludedCases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["INICIAL"] = 10,
        ["PROFISSIONAL"] = 30,
        ["AGENCIA"] = 80,
        ["ENTERPRISE"] = 0
    };

    private static readonly Dictionary<string, decimal> PlanOverageUnitPrice = new(StringComparer.OrdinalIgnoreCase)
    {
        ["INICIAL"] = 4.50m,
        ["PROFISSIONAL"] = 3.00m,
        ["AGENCIA"] = 2.00m,
        ["ENTERPRISE"] = 0m
    };

    public static readonly IReadOnlySet<string> SupportedPlanCodes =
        new HashSet<string>(PlanMonthlyPrices.Keys, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlySet<string> SupportedPaymentMethodTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CARD" };

    public static decimal GetMonthlyPrice(string? planCode)
    {
        if (planCode is null) return 0m;
        return PlanMonthlyPrices.TryGetValue(planCode, out var price) ? price : 0m;
    }

    public static int GetIncludedCases(string? planCode)
    {
        if (planCode is null) return 0;
        return PlanIncludedCases.TryGetValue(planCode, out var cases) ? cases : 0;
    }

    public static decimal GetOverageUnitPrice(string? planCode)
    {
        if (planCode is null) return 0m;
        return PlanOverageUnitPrice.TryGetValue(planCode, out var price) ? price : 0m;
    }
}
