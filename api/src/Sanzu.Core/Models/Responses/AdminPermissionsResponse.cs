namespace Sanzu.Core.Models.Responses;

public sealed record AdminPermissionsResponse(
    string Role,
    IReadOnlyList<string> AccessibleEndpoints,
    IReadOnlyList<string> AccessibleWidgets,
    bool CanTakeActions
);
