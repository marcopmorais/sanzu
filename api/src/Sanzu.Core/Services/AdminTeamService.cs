using System.Text.Json;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class AdminTeamService : IAdminTeamService
{
    private static readonly PlatformRole[] InternalAdminRoles =
    {
        PlatformRole.SanzuAdmin,
        PlatformRole.SanzuOps,
        PlatformRole.SanzuFinance,
        PlatformRole.SanzuSupport,
        PlatformRole.SanzuViewer
    };

    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminTeamService(
        IUserRoleRepository userRoleRepository,
        IUserRepository userRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork)
    {
        _userRoleRepository = userRoleRepository;
        _userRepository = userRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AdminTeamMemberResponse>> ListTeamMembersAsync(CancellationToken cancellationToken)
    {
        var platformRoles = await _userRoleRepository.GetAllPlatformScopedAsync(cancellationToken);

        return platformRoles
            .Where(r => InternalAdminRoles.Contains(r.RoleType))
            .Select(r => new AdminTeamMemberResponse
            {
                UserId = r.UserId,
                Email = r.User?.Email ?? string.Empty,
                FullName = r.User?.FullName ?? string.Empty,
                Role = r.RoleType.ToString(),
                GrantedAt = r.GrantedAt
            })
            .ToList();
    }

    public async Task GrantRoleAsync(Guid targetUserId, string role, Guid grantedByUserId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(targetUserId, cancellationToken)
            ?? throw new InvalidOperationException($"User {targetUserId} not found.");

        if (!Enum.TryParse<PlatformRole>(role, out var platformRole))
        {
            throw new InvalidOperationException($"Invalid role: {role}");
        }

        var existingRoles = await _userRoleRepository.GetByUserIdAsync(targetUserId, cancellationToken);
        if (existingRoles.Any(r => r.RoleType == platformRole && r.TenantId == null))
        {
            throw new InvalidOperationException($"User already has role {role}.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                await _userRoleRepository.CreateAsync(
                    new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = targetUserId,
                        RoleType = platformRole,
                        TenantId = null,
                        GrantedBy = grantedByUserId,
                        GrantedAt = DateTime.UtcNow
                    },
                    ct);

                await _auditRepository.CreateAsync(
                    new AuditEvent
                    {
                        Id = Guid.NewGuid(),
                        ActorUserId = grantedByUserId,
                        EventType = "Admin.Team.RoleGranted",
                        Metadata = JsonSerializer.Serialize(new
                        {
                            targetUserId,
                            targetEmail = user.Email,
                            role,
                            grantedBy = grantedByUserId
                        }),
                        CreatedAt = DateTime.UtcNow
                    },
                    ct);
            },
            cancellationToken);
    }

    public async Task RevokeRoleAsync(Guid targetUserId, string role, Guid revokedByUserId, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PlatformRole>(role, out var platformRole))
        {
            throw new InvalidOperationException($"Invalid role: {role}");
        }

        var existingRoles = await _userRoleRepository.GetByUserIdAsync(targetUserId, cancellationToken);
        var roleToRevoke = existingRoles.FirstOrDefault(r => r.RoleType == platformRole && r.TenantId == null)
            ?? throw new InvalidOperationException($"User does not have role {role}.");

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                await _userRoleRepository.DeleteAsync(roleToRevoke.Id, ct);

                await _auditRepository.CreateAsync(
                    new AuditEvent
                    {
                        Id = Guid.NewGuid(),
                        ActorUserId = revokedByUserId,
                        EventType = "Admin.Team.RoleRevoked",
                        Metadata = JsonSerializer.Serialize(new
                        {
                            targetUserId,
                            role,
                            revokedBy = revokedByUserId
                        }),
                        CreatedAt = DateTime.UtcNow
                    },
                    ct);
            },
            cancellationToken);
    }
}
