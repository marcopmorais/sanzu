using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Notifications;

public sealed record TenantInvitationNotification(
    Guid InvitationId,
    Guid TenantId,
    string Email,
    PlatformRole RoleType,
    DateTime ExpiresAt,
    string InvitationToken);
