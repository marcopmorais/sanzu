namespace Sanzu.Core.Enums;

/// <summary>
/// Canonical reason codes for blocked workflow states.
/// Based on mission-control-reason-categories.md
/// </summary>
public enum BlockedReasonCode
{
    /// <summary>
    /// Required information or document has not been provided yet
    /// </summary>
    EvidenceMissing = 0,

    /// <summary>
    /// Waiting on an external institution (registry, bank, consulate, etc.)
    /// </summary>
    ExternalDependency = 1,

    /// <summary>
    /// Policy rule prevents action until conditions are met
    /// </summary>
    PolicyRestriction = 2,

    /// <summary>
    /// Current role does not allow this action
    /// </summary>
    RolePermission = 3,

    /// <summary>
    /// Deadline approaching or breached
    /// </summary>
    DeadlineRisk = 4,

    /// <summary>
    /// Payment status or plan limits prevent progression
    /// </summary>
    PaymentOrBilling = 5,

    /// <summary>
    /// Sign-in, invite, or identity verification issues
    /// </summary>
    IdentityOrAuth = 6,

    /// <summary>
    /// Provided data conflicts and must be reconciled
    /// </summary>
    DataMismatch = 7,

    /// <summary>
    /// Technical issue preventing completion
    /// </summary>
    SystemError = 8
}
