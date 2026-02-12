using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Notifications;

namespace Sanzu.Infrastructure.Repositories;

public sealed class NoOpTenantInvitationNotificationSender : ITenantInvitationNotificationSender
{
    public Task SendTenantInviteAsync(TenantInvitationNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
