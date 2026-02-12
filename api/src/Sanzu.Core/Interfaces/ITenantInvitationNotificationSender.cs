using Sanzu.Core.Models.Notifications;

namespace Sanzu.Core.Interfaces;

public interface ITenantInvitationNotificationSender
{
    Task SendTenantInviteAsync(TenantInvitationNotification notification, CancellationToken cancellationToken);
}
