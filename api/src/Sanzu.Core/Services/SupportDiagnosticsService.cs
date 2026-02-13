using System.Text.Json;
using FluentValidation;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class SupportDiagnosticsService : ISupportDiagnosticsService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly ISupportDiagnosticSessionRepository _supportDiagnosticSessionRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<StartSupportDiagnosticSessionRequest> _startSupportDiagnosticSessionValidator;

    public SupportDiagnosticsService(
        IOrganizationRepository organizationRepository,
        IUserRoleRepository userRoleRepository,
        ICaseRepository caseRepository,
        ISupportDiagnosticSessionRepository supportDiagnosticSessionRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IValidator<StartSupportDiagnosticSessionRequest> startSupportDiagnosticSessionValidator)
    {
        _organizationRepository = organizationRepository;
        _userRoleRepository = userRoleRepository;
        _caseRepository = caseRepository;
        _supportDiagnosticSessionRepository = supportDiagnosticSessionRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _startSupportDiagnosticSessionValidator = startSupportDiagnosticSessionValidator;
    }

    public async Task<SupportDiagnosticSessionResponse> StartDiagnosticSessionAsync(
        Guid tenantId,
        Guid actorUserId,
        StartSupportDiagnosticSessionRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _startSupportDiagnosticSessionValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var scope = ParseScope(request.Scope);
        var reason = request.Reason.Trim();
        var durationMinutes = request.DurationMinutes;

        SupportDiagnosticSessionResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var tenant = await LoadTenantAsync(tenantId, token);
                await EnsureSanzuAdminAccessAsync(actorUserId, token);

                var nowUtc = DateTime.UtcNow;
                var session = new SupportDiagnosticSession
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    RequestedByUserId = actorUserId,
                    Scope = scope,
                    Reason = reason,
                    StartedAt = nowUtc,
                    ExpiresAt = nowUtc.AddMinutes(durationMinutes)
                };

                await _supportDiagnosticSessionRepository.CreateAsync(session, token);

                await WriteAuditEventAsync(
                    actorUserId,
                    "SupportDiagnosticSessionStarted",
                    new
                    {
                        TenantId = tenant.Id,
                        SessionId = session.Id,
                        Scope = session.Scope.ToString(),
                        DurationMinutes = durationMinutes,
                        Reason = session.Reason,
                        StartedAt = session.StartedAt,
                        ExpiresAt = session.ExpiresAt
                    },
                    token);

                response = new SupportDiagnosticSessionResponse
                {
                    SessionId = session.Id,
                    TenantId = tenant.Id,
                    RequestedByUserId = actorUserId,
                    Scope = session.Scope,
                    DurationMinutes = durationMinutes,
                    Reason = session.Reason,
                    StartedAt = session.StartedAt,
                    ExpiresAt = session.ExpiresAt
                };
            },
            cancellationToken);

        return response!;
    }

    public async Task<SupportDiagnosticSummaryResponse> GetDiagnosticSummaryAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        SupportDiagnosticSummaryResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var tenant = await LoadTenantAsync(tenantId, token);
                await EnsureSanzuAdminAccessAsync(actorUserId, token);

                var session = await _supportDiagnosticSessionRepository.GetByIdAsync(sessionId, token);
                if (session is null || session.TenantId != tenantId)
                {
                    throw new SupportDiagnosticAccessException("Diagnostic session was not found for the requested tenant.");
                }

                var nowUtc = DateTime.UtcNow;
                if (session.RevokedAt.HasValue || session.ExpiresAt <= nowUtc)
                {
                    throw new SupportDiagnosticAccessException("Diagnostic session is no longer active.");
                }

                var tenantCases = await _caseRepository.GetByTenantIdAsync(tenantId, token);
                var totalCaseCount = session.Scope == SupportDiagnosticScope.TenantOperationalRead
                    ? tenantCases.Count
                    : 0;
                var activeCaseCount = session.Scope == SupportDiagnosticScope.TenantOperationalRead
                    ? tenantCases.Count(x => x.Status == CaseStatus.Active)
                    : 0;
                var diagnosticActionsLast24Hours = await _supportDiagnosticSessionRepository.CountStartedSinceAsync(
                    tenantId,
                    nowUtc.AddHours(-24),
                    token);

                await WriteAuditEventAsync(
                    actorUserId,
                    "SupportDiagnosticSummaryAccessed",
                    new
                    {
                        TenantId = tenantId,
                        SessionId = session.Id,
                        Scope = session.Scope.ToString(),
                        RetrievedAt = nowUtc
                    },
                    token);

                response = new SupportDiagnosticSummaryResponse
                {
                    SessionId = session.Id,
                    TenantId = tenantId,
                    Scope = session.Scope,
                    RetrievedAt = nowUtc,
                    ExpiresAt = session.ExpiresAt,
                    TenantStatus = tenant.Status,
                    ActiveCaseCount = activeCaseCount,
                    TotalCaseCount = totalCaseCount,
                    DiagnosticActionsLast24Hours = diagnosticActionsLast24Hours
                };
            },
            cancellationToken);

        return response!;
    }

    private async Task<Organization> LoadTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _organizationRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            throw new TenantAccessDeniedException();
        }

        return tenant;
    }

    private async Task EnsureSanzuAdminAccessAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var actorRoles = await _userRoleRepository.GetByUserIdAsync(actorUserId, cancellationToken);
        var hasPlatformRole = actorRoles.Any(
            role => role.RoleType == PlatformRole.SanzuAdmin
                    && (role.TenantId == null || role.TenantId == Guid.Empty));

        if (!hasPlatformRole)
        {
            throw new TenantAccessDeniedException();
        }
    }

    private static SupportDiagnosticScope ParseScope(string value)
    {
        if (!Enum.TryParse<SupportDiagnosticScope>(value, ignoreCase: true, out var parsedScope))
        {
            throw new SupportDiagnosticAccessException("Diagnostic scope is not valid.");
        }

        return parsedScope;
    }

    private Task WriteAuditEventAsync(
        Guid actorUserId,
        string eventType,
        object metadata,
        CancellationToken cancellationToken)
    {
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            EventType = eventType,
            Metadata = JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.UtcNow
        };

        return _auditRepository.CreateAsync(auditEvent, cancellationToken);
    }
}
