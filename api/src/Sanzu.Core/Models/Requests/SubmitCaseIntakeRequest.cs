namespace Sanzu.Core.Models.Requests;

public sealed class SubmitCaseIntakeRequest
{
    public string PrimaryContactName { get; init; } = string.Empty;
    public string PrimaryContactPhone { get; init; } = string.Empty;
    public string RelationshipToDeceased { get; init; } = string.Empty;
    public bool HasWill { get; init; }
    public bool RequiresLegalSupport { get; init; }
    public bool RequiresFinancialSupport { get; init; }
    public bool ConfirmAccuracy { get; init; }
    public string? Notes { get; init; }
}
