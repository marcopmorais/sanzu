namespace Sanzu.Core.Exceptions;

public sealed class CaseAccessDeniedException : Exception
{
    public Guid ActorUserId { get; }
    public Guid CaseId { get; }
    public string AttemptedAction { get; }
    public string RequiredRole { get; }
    public string? ActualRole { get; }
    public string ReasonCode { get; }

    public CaseAccessDeniedException(
        Guid actorUserId,
        Guid caseId,
        string attemptedAction,
        string requiredRole,
        string? actualRole,
        string reasonCode)
        : base($"Access denied: {reasonCode}. Action '{attemptedAction}' requires '{requiredRole}' role.")
    {
        ActorUserId = actorUserId;
        CaseId = caseId;
        AttemptedAction = attemptedAction;
        RequiredRole = requiredRole;
        ActualRole = actualRole;
        ReasonCode = reasonCode;
    }
}
