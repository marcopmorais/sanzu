using System.Text.Json;
using FluentValidation;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class KpiAlertService : IKpiAlertService
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IKpiThresholdRepository _kpiThresholdRepository;
    private readonly IKpiAlertLogRepository _kpiAlertLogRepository;
    private readonly IKpiDashboardService _kpiDashboardService;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpsertKpiThresholdRequest> _upsertKpiThresholdValidator;
    private readonly IValidator<EvaluateKpiAlertsRequest> _evaluateKpiAlertsValidator;

    public KpiAlertService(
        IUserRoleRepository userRoleRepository,
        IKpiThresholdRepository kpiThresholdRepository,
        IKpiAlertLogRepository kpiAlertLogRepository,
        IKpiDashboardService kpiDashboardService,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpsertKpiThresholdRequest> upsertKpiThresholdValidator,
        IValidator<EvaluateKpiAlertsRequest> evaluateKpiAlertsValidator)
    {
        _userRoleRepository = userRoleRepository;
        _kpiThresholdRepository = kpiThresholdRepository;
        _kpiAlertLogRepository = kpiAlertLogRepository;
        _kpiDashboardService = kpiDashboardService;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _upsertKpiThresholdValidator = upsertKpiThresholdValidator;
        _evaluateKpiAlertsValidator = evaluateKpiAlertsValidator;
    }

    public async Task<KpiThresholdResponse> UpsertThresholdAsync(
        Guid actorUserId,
        UpsertKpiThresholdRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _upsertKpiThresholdValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        await EnsureSanzuAdminAccessAsync(actorUserId, cancellationToken);

        var metricKey = ParseMetricKey(request.MetricKey);
        var severity = ParseSeverity(request.Severity);
        var routeTarget = request.RouteTarget.Trim();
        KpiThresholdResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var nowUtc = DateTime.UtcNow;
                var threshold = await _kpiThresholdRepository.GetByMetricAsync(metricKey, token);

                if (threshold is null)
                {
                    threshold = new KpiThresholdDefinition
                    {
                        Id = Guid.NewGuid(),
                        MetricKey = metricKey,
                        ThresholdValue = request.ThresholdValue,
                        Severity = severity,
                        RouteTarget = routeTarget,
                        IsEnabled = request.IsEnabled,
                        UpdatedByUserId = actorUserId,
                        UpdatedAt = nowUtc
                    };

                    await _kpiThresholdRepository.CreateAsync(threshold, token);
                }
                else
                {
                    threshold.ThresholdValue = request.ThresholdValue;
                    threshold.Severity = severity;
                    threshold.RouteTarget = routeTarget;
                    threshold.IsEnabled = request.IsEnabled;
                    threshold.UpdatedByUserId = actorUserId;
                    threshold.UpdatedAt = nowUtc;
                }

                await WriteAuditEventAsync(
                    actorUserId,
                    "KpiThresholdConfigured",
                    new
                    {
                        threshold.Id,
                        MetricKey = threshold.MetricKey.ToString(),
                        threshold.ThresholdValue,
                        Severity = threshold.Severity.ToString(),
                        threshold.RouteTarget,
                        threshold.IsEnabled,
                        threshold.UpdatedAt
                    },
                    token);

                response = new KpiThresholdResponse
                {
                    ThresholdId = threshold.Id,
                    MetricKey = threshold.MetricKey,
                    ThresholdValue = threshold.ThresholdValue,
                    Severity = threshold.Severity,
                    RouteTarget = threshold.RouteTarget,
                    IsEnabled = threshold.IsEnabled,
                    UpdatedByUserId = actorUserId,
                    UpdatedAt = threshold.UpdatedAt
                };
            },
            cancellationToken);

        return response!;
    }

    public async Task<KpiAlertEvaluationResponse> EvaluateThresholdsAsync(
        Guid actorUserId,
        EvaluateKpiAlertsRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _evaluateKpiAlertsValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        await EnsureSanzuAdminAccessAsync(actorUserId, cancellationToken);

        var dashboard = await _kpiDashboardService.GetDashboardAsync(
            actorUserId,
            request.PeriodDays,
            request.TenantLimit,
            request.CaseLimit,
            cancellationToken);

        var generatedAlerts = new List<KpiAlertLogResponse>();

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var thresholds = await _kpiThresholdRepository.GetEnabledAsync(token);
                var nowUtc = DateTime.UtcNow;

                foreach (var threshold in thresholds)
                {
                    var actualValue = ResolveMetricValue(dashboard.Current, threshold.MetricKey);
                    if (actualValue < threshold.ThresholdValue)
                    {
                        continue;
                    }

                    var context = new
                    {
                        dashboard.PeriodDays,
                        MetricKey = threshold.MetricKey.ToString(),
                        threshold.ThresholdValue,
                        ActualValue = actualValue,
                        Current = dashboard.Current,
                        Baseline = dashboard.Baseline,
                        TopTenants = dashboard.TenantContributions.Take(3).ToList(),
                        TopCases = dashboard.CaseContributions.Take(3).ToList()
                    };

                    var contextJson = JsonSerializer.Serialize(context);
                    var alertLog = new KpiAlertLog
                    {
                        Id = Guid.NewGuid(),
                        ThresholdId = threshold.Id,
                        MetricKey = threshold.MetricKey,
                        ThresholdValue = threshold.ThresholdValue,
                        ActualValue = actualValue,
                        Severity = threshold.Severity,
                        RouteTarget = threshold.RouteTarget,
                        ContextJson = contextJson,
                        TriggeredByUserId = actorUserId,
                        TriggeredAt = nowUtc
                    };

                    await _kpiAlertLogRepository.CreateAsync(alertLog, token);

                    await WriteAuditEventAsync(
                        actorUserId,
                        "KpiThresholdAlertTriggered",
                        new
                        {
                            AlertId = alertLog.Id,
                            threshold.ThresholdValue,
                            ActualValue = actualValue,
                            MetricKey = threshold.MetricKey.ToString(),
                            Severity = threshold.Severity.ToString(),
                            threshold.RouteTarget,
                            TriggeredAt = nowUtc
                        },
                        token);

                    generatedAlerts.Add(
                        new KpiAlertLogResponse
                        {
                            AlertId = alertLog.Id,
                            ThresholdId = alertLog.ThresholdId,
                            MetricKey = alertLog.MetricKey,
                            ThresholdValue = alertLog.ThresholdValue,
                            ActualValue = alertLog.ActualValue,
                            Severity = alertLog.Severity,
                            RouteTarget = alertLog.RouteTarget,
                            Context = contextJson,
                            TriggeredAt = alertLog.TriggeredAt
                        });
                }
            },
            cancellationToken);

        return new KpiAlertEvaluationResponse
        {
            PeriodDays = request.PeriodDays,
            EvaluatedAt = DateTime.UtcNow,
            GeneratedAlerts = generatedAlerts
        };
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

    private static KpiMetricKey ParseMetricKey(string value)
    {
        if (!Enum.TryParse<KpiMetricKey>(value, ignoreCase: true, out var metricKey))
        {
            throw new ValidationException("MetricKey must be a valid KPI metric.");
        }

        return metricKey;
    }

    private static KpiAlertSeverity ParseSeverity(string value)
    {
        if (!Enum.TryParse<KpiAlertSeverity>(value, ignoreCase: true, out var severity))
        {
            throw new ValidationException("Severity must be a valid KPI alert severity.");
        }

        return severity;
    }

    private static int ResolveMetricValue(PlatformKpiMetricsResponse metrics, KpiMetricKey metricKey)
    {
        return metricKey switch
        {
            KpiMetricKey.CasesCreated => metrics.CasesCreated,
            KpiMetricKey.CasesClosed => metrics.CasesClosed,
            KpiMetricKey.ActiveCases => metrics.ActiveCases,
            KpiMetricKey.DocumentsUploaded => metrics.DocumentsUploaded,
            _ => throw new ValidationException("MetricKey must be a valid KPI metric.")
        };
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
