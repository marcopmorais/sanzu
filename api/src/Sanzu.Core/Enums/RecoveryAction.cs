namespace Sanzu.Core.Enums;

/// <summary>
/// Allowed recovery actions for blocked workflow steps.
/// Actions are role-safe and do not allow privilege escalation.
/// </summary>
public enum RecoveryAction
{
    /// <summary>
    /// Upload or provide missing document/evidence
    /// </summary>
    UploadEvidence = 0,

    /// <summary>
    /// Contact assigned case manager for assistance
    /// </summary>
    ContactManager = 1,

    /// <summary>
    /// Request an override from authorized user
    /// </summary>
    RequestOverride = 2,

    /// <summary>
    /// Complete a prerequisite task first
    /// </summary>
    CompletePrerequisite = 3,

    /// <summary>
    /// Wait for external dependency to resolve
    /// </summary>
    WaitForExternal = 4,

    /// <summary>
    /// Review and correct conflicting data
    /// </summary>
    CorrectData = 5,

    /// <summary>
    /// Contact support for technical assistance
    /// </summary>
    ContactSupport = 6,

    /// <summary>
    /// Update billing or payment method
    /// </summary>
    UpdateBilling = 7,

    /// <summary>
    /// Request role permission from administrator
    /// </summary>
    RequestPermission = 8
}
