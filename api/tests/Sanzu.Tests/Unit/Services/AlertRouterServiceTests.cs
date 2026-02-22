using FluentAssertions;
using Moq;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Services;

namespace Sanzu.Tests.Unit.Services;

public sealed class AlertRouterServiceTests
{
    private readonly Mock<IUserRoleRepository> _userRoleRepo = new();
    private readonly AlertRouterService _sut;

    public AlertRouterServiceTests()
    {
        _sut = new AlertRouterService(_userRoleRepo.Object);
    }

    [Fact]
    public async Task ResolveRecipients_Should_ReturnUsersWithTargetRole()
    {
        var opsUserId = Guid.NewGuid();
        _userRoleRepo.Setup(r => r.GetAllPlatformScopedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRole>
            {
                new() { Id = Guid.NewGuid(), UserId = opsUserId, RoleType = PlatformRole.SanzuOps, TenantId = null, GrantedBy = Guid.NewGuid(), GrantedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), RoleType = PlatformRole.SanzuFinance, TenantId = null, GrantedBy = Guid.NewGuid(), GrantedAt = DateTime.UtcNow }
            });

        var result = await _sut.ResolveRecipientsAsync("SanzuOps", false, CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Be(opsUserId);
    }

    [Fact]
    public async Task ResolveRecipients_Should_FallbackToSanzuAdmin_WhenNoUsersForRole()
    {
        var adminUserId = Guid.NewGuid();
        _userRoleRepo.Setup(r => r.GetAllPlatformScopedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRole>
            {
                new() { Id = Guid.NewGuid(), UserId = adminUserId, RoleType = PlatformRole.SanzuAdmin, TenantId = null, GrantedBy = Guid.NewGuid(), GrantedAt = DateTime.UtcNow }
            });

        var result = await _sut.ResolveRecipientsAsync("SanzuSupport", false, CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Be(adminUserId);
    }

    [Fact]
    public async Task ResolveRecipients_Should_IncludeSanzuAdmin_WhenCritical()
    {
        var opsUserId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        _userRoleRepo.Setup(r => r.GetAllPlatformScopedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRole>
            {
                new() { Id = Guid.NewGuid(), UserId = opsUserId, RoleType = PlatformRole.SanzuOps, TenantId = null, GrantedBy = Guid.NewGuid(), GrantedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), UserId = adminUserId, RoleType = PlatformRole.SanzuAdmin, TenantId = null, GrantedBy = Guid.NewGuid(), GrantedAt = DateTime.UtcNow }
            });

        var result = await _sut.ResolveRecipientsAsync("SanzuOps", true, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(opsUserId);
        result.Should().Contain(adminUserId);
    }

    [Fact]
    public async Task ResolveRecipients_Should_DeduplicateUserIds()
    {
        var userId = Guid.NewGuid();
        _userRoleRepo.Setup(r => r.GetAllPlatformScopedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRole>
            {
                new() { Id = Guid.NewGuid(), UserId = userId, RoleType = PlatformRole.SanzuAdmin, TenantId = null, GrantedBy = Guid.NewGuid(), GrantedAt = DateTime.UtcNow }
            });

        // SanzuAdmin is both the target and the fallback — should still be 1
        var result = await _sut.ResolveRecipientsAsync("SanzuAdmin", true, CancellationToken.None);

        result.Should().ContainSingle();
    }
}
