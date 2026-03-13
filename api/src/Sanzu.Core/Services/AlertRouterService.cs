using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;

namespace Sanzu.Core.Services;

public sealed class AlertRouterService : IAlertRouterService
{
    private readonly IUserRoleRepository _userRoleRepository;

    public AlertRouterService(IUserRoleRepository userRoleRepository)
    {
        _userRoleRepository = userRoleRepository;
    }

    public async Task<IReadOnlyList<Guid>> ResolveRecipientsAsync(
        string targetRole,
        bool includeSanzuAdmin,
        CancellationToken cancellationToken)
    {
        var allPlatformRoles = await _userRoleRepository.GetAllPlatformScopedAsync(cancellationToken);

        if (!Enum.TryParse<PlatformRole>(targetRole, out var parsedRole))
            parsedRole = PlatformRole.SanzuAdmin;

        var recipients = allPlatformRoles
            .Where(r => r.RoleType == parsedRole)
            .Select(r => r.UserId)
            .ToHashSet();

        // Fallback: if no users found for the target role, route to SanzuAdmin
        if (recipients.Count == 0)
        {
            var adminUsers = allPlatformRoles
                .Where(r => r.RoleType == PlatformRole.SanzuAdmin)
                .Select(r => r.UserId);
            foreach (var uid in adminUsers)
                recipients.Add(uid);
        }
        else if (includeSanzuAdmin)
        {
            // For Critical severity, also include SanzuAdmin users
            var adminUsers = allPlatformRoles
                .Where(r => r.RoleType == PlatformRole.SanzuAdmin)
                .Select(r => r.UserId);
            foreach (var uid in adminUsers)
                recipients.Add(uid);
        }

        return recipients.ToList();
    }
}
