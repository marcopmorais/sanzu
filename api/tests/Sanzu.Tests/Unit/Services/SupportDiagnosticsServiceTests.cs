using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Services;
using Sanzu.Core.Validators;
using Sanzu.Infrastructure.Data;
using Sanzu.Infrastructure.Repositories;

namespace Sanzu.Tests.Unit.Services;

public sealed class SupportDiagnosticsServiceTests
{
    [Fact]
    public async Task StartDiagnosticSession_ShouldPersistSessionAndAudit_WhenActorIsSanzuAdmin()
    {
        var dbContext = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId, TenantStatus.Active);
        await SeedUserAsync(dbContext, actorUserId, tenantId);
        await SeedRoleAsync(dbContext, actorUserId, PlatformRole.SanzuAdmin, null);
        var service = CreateService(dbContext);

        var response = await service.StartDiagnosticSessionAsync(
            tenantId,
            actorUserId,
            new StartSupportDiagnosticSessionRequest
            {
                Scope = "TenantOperationalRead",
                DurationMinutes = 30,
                Reason = "Escalated support diagnostics."
            },
            CancellationToken.None);

        response.TenantId.Should().Be(tenantId);
        response.RequestedByUserId.Should().Be(actorUserId);
        response.Scope.Should().Be(SupportDiagnosticScope.TenantOperationalRead);
        response.Reason.Should().Be("Escalated support diagnostics.");
        response.ExpiresAt.Should().BeAfter(response.StartedAt);

        dbContext.SupportDiagnosticSessions.Should().Contain(x => x.Id == response.SessionId);
        dbContext.AuditEvents.Should().Contain(
            x => x.ActorUserId == actorUserId
                 && x.EventType == "SupportDiagnosticSessionStarted"
                 && x.Metadata.Contains("Escalated support diagnostics."));
    }

    [Fact]
    public async Task StartDiagnosticSession_ShouldThrowAccessDenied_WhenActorIsNotSanzuAdmin()
    {
        var dbContext = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId, TenantStatus.Active);
        await SeedUserAsync(dbContext, actorUserId, tenantId);
        await SeedRoleAsync(dbContext, actorUserId, PlatformRole.AgencyAdmin, tenantId);
        var service = CreateService(dbContext);

        var act = () => service.StartDiagnosticSessionAsync(
            tenantId,
            actorUserId,
            new StartSupportDiagnosticSessionRequest
            {
                Scope = "TenantStatusRead",
                DurationMinutes = 15,
                Reason = "Needs restricted diagnostics."
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<TenantAccessDeniedException>();
    }

    [Fact]
    public async Task GetDiagnosticSummary_ShouldReturnScopeCountsAndAudit_WhenSessionIsActive()
    {
        var dbContext = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId, TenantStatus.Active);
        await SeedUserAsync(dbContext, actorUserId, tenantId);
        await SeedRoleAsync(dbContext, actorUserId, PlatformRole.SanzuAdmin, null);
        await SeedCaseAsync(dbContext, tenantId, actorUserId, "CASE-0001", CaseStatus.Active);
        await SeedCaseAsync(dbContext, tenantId, actorUserId, "CASE-0002", CaseStatus.Draft);

        var nowUtc = DateTime.UtcNow;
        var sessionId = Guid.NewGuid();
        dbContext.SupportDiagnosticSessions.Add(
            new SupportDiagnosticSession
            {
                Id = sessionId,
                TenantId = tenantId,
                RequestedByUserId = actorUserId,
                Scope = SupportDiagnosticScope.TenantOperationalRead,
                Reason = "Active session seed.",
                StartedAt = nowUtc.AddMinutes(-5),
                ExpiresAt = nowUtc.AddMinutes(25)
            });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var response = await service.GetDiagnosticSummaryAsync(
            tenantId,
            actorUserId,
            sessionId,
            CancellationToken.None);

        response.SessionId.Should().Be(sessionId);
        response.TenantId.Should().Be(tenantId);
        response.Scope.Should().Be(SupportDiagnosticScope.TenantOperationalRead);
        response.TenantStatus.Should().Be(TenantStatus.Active);
        response.TotalCaseCount.Should().Be(2);
        response.ActiveCaseCount.Should().Be(1);
        response.DiagnosticActionsLast24Hours.Should().Be(1);

        dbContext.AuditEvents.Should().Contain(
            x => x.ActorUserId == actorUserId
                 && x.EventType == "SupportDiagnosticSummaryAccessed"
                 && x.Metadata.Contains(sessionId.ToString()));
    }

    [Fact]
    public async Task GetDiagnosticSummary_ShouldThrowAccessException_WhenSessionIsExpired()
    {
        var dbContext = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId, TenantStatus.Active);
        await SeedUserAsync(dbContext, actorUserId, tenantId);
        await SeedRoleAsync(dbContext, actorUserId, PlatformRole.SanzuAdmin, null);

        var nowUtc = DateTime.UtcNow;
        var sessionId = Guid.NewGuid();
        dbContext.SupportDiagnosticSessions.Add(
            new SupportDiagnosticSession
            {
                Id = sessionId,
                TenantId = tenantId,
                RequestedByUserId = actorUserId,
                Scope = SupportDiagnosticScope.TenantStatusRead,
                Reason = "Expired session seed.",
                StartedAt = nowUtc.AddMinutes(-60),
                ExpiresAt = nowUtc.AddMinutes(-15)
            });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var act = () => service.GetDiagnosticSummaryAsync(
            tenantId,
            actorUserId,
            sessionId,
            CancellationToken.None);

        await act.Should().ThrowAsync<SupportDiagnosticAccessException>()
            .WithMessage("*no longer active*");
    }

    [Fact]
    public async Task StartDiagnosticSession_ShouldThrowValidation_WhenRequestIsInvalid()
    {
        var dbContext = CreateContext();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await SeedTenantAsync(dbContext, tenantId, TenantStatus.Active);
        await SeedUserAsync(dbContext, actorUserId, tenantId);
        await SeedRoleAsync(dbContext, actorUserId, PlatformRole.SanzuAdmin, null);
        var service = CreateService(dbContext);

        var act = () => service.StartDiagnosticSessionAsync(
            tenantId,
            actorUserId,
            new StartSupportDiagnosticSessionRequest
            {
                Scope = string.Empty,
                DurationMinutes = 1,
                Reason = string.Empty
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private static SupportDiagnosticsService CreateService(SanzuDbContext dbContext)
    {
        return new SupportDiagnosticsService(
            new OrganizationRepository(dbContext),
            new UserRoleRepository(dbContext),
            new CaseRepository(dbContext),
            new SupportDiagnosticSessionRepository(dbContext),
            new AuditRepository(dbContext),
            new EfUnitOfWork(dbContext),
            new StartSupportDiagnosticSessionRequestValidator());
    }

    private static SanzuDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SanzuDbContext>()
            .UseInMemoryDatabase($"sanzu-support-diagnostics-tests-{Guid.NewGuid()}")
            .Options;

        return new SanzuDbContext(options);
    }

    private static async Task SeedTenantAsync(SanzuDbContext dbContext, Guid tenantId, TenantStatus status)
    {
        dbContext.Organizations.Add(
            new Organization
            {
                Id = tenantId,
                Name = $"Tenant-{tenantId:N}",
                Location = "Lisbon",
                Status = status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedUserAsync(
        SanzuDbContext dbContext,
        Guid userId,
        Guid tenantId,
        string? email = null,
        string? fullName = null)
    {
        dbContext.Users.Add(
            new User
            {
                Id = userId,
                Email = email ?? $"user-{userId:N}@agency.pt",
                FullName = fullName ?? "Sanzu Admin",
                OrgId = tenantId,
                CreatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedRoleAsync(
        SanzuDbContext dbContext,
        Guid userId,
        PlatformRole roleType,
        Guid? tenantId)
    {
        dbContext.UserRoles.Add(
            new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RoleType = roleType,
                TenantId = tenantId,
                GrantedBy = userId,
                GrantedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedCaseAsync(
        SanzuDbContext dbContext,
        Guid tenantId,
        Guid managerUserId,
        string caseNumber,
        CaseStatus status)
    {
        dbContext.Cases.Add(
            new Case
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ManagerUserId = managerUserId,
                CaseNumber = caseNumber,
                DeceasedFullName = "Jane Doe",
                DateOfDeath = DateTime.UtcNow.Date.AddDays(-3),
                CaseType = "GENERAL",
                Urgency = "NORMAL",
                Status = status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();
    }
}
