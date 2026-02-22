using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;

namespace Sanzu.Core.Services;

public sealed class AdminAlertService : IAdminAlertService
{
    private readonly IAdminAlertRepository _alertRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ITenantHealthScoreRepository _healthScoreRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;

    private const int HealthScoreRedThreshold = 30;
    private const int CaseStalledDaysThreshold = 14;
    private const int OnboardingStalledDaysThreshold = 21;

    public AdminAlertService(
        IAdminAlertRepository alertRepository,
        IOrganizationRepository organizationRepository,
        ITenantHealthScoreRepository healthScoreRepository,
        ICaseRepository caseRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork)
    {
        _alertRepository = alertRepository;
        _organizationRepository = organizationRepository;
        _healthScoreRepository = healthScoreRepository;
        _caseRepository = caseRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task EvaluateAlertRulesAsync(CancellationToken cancellationToken)
    {
        var allOrgs = await _organizationRepository.GetAllAsync(cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var org in allOrgs.Where(o => o.Status == TenantStatus.Active))
        {
            await EvaluateHealthDropAsync(org, cancellationToken);
            await EvaluateBillingFailureAsync(org, now, cancellationToken);
            await EvaluateOnboardingStalledAsync(org, now, cancellationToken);
        }

        await EvaluateCaseStuckAsync(now, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminAlert>> GetAlertsAsync(
        AlertStatus? status,
        AlertSeverity? severity,
        string? alertType,
        CancellationToken cancellationToken)
    {
        return await _alertRepository.GetAllAsync(status, severity, alertType, cancellationToken);
    }

    public async Task<AdminAlert?> GetAlertByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _alertRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task AcknowledgeAlertAsync(Guid alertId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var alert = await _alertRepository.GetByIdAsync(alertId, cancellationToken);
        if (alert is null || alert.Status != AlertStatus.Fired)
            return;

        alert.Status = AlertStatus.Acknowledged;
        alert.AcknowledgedAt = DateTime.UtcNow;
        alert.OwnedByUserId = actorUserId;
        await _alertRepository.UpdateAsync(alert, cancellationToken);

        await LogAuditAsync("Admin.Alert.Acknowledged", actorUserId,
            $"{{\"alertId\":\"{alertId}\",\"ownedByUserId\":\"{actorUserId}\"}}", cancellationToken);
    }

    public async Task ResolveAlertAsync(Guid alertId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var alert = await _alertRepository.GetByIdAsync(alertId, cancellationToken);
        if (alert is null || alert.Status == AlertStatus.Resolved)
            return;

        alert.Status = AlertStatus.Resolved;
        alert.ResolvedAt = DateTime.UtcNow;
        await _alertRepository.UpdateAsync(alert, cancellationToken);

        await LogAuditAsync("Admin.Alert.Resolved", actorUserId,
            $"{{\"alertId\":\"{alertId}\",\"resolvedAt\":\"{alert.ResolvedAt:O}\"}}", cancellationToken);
    }

    private async Task EvaluateHealthDropAsync(Organization org, CancellationToken cancellationToken)
    {
        var latestScores = await _healthScoreRepository.GetLatestForAllTenantsAsync(cancellationToken);
        var score = latestScores.FirstOrDefault(s => s.TenantId == org.Id);
        if (score is null || score.OverallScore >= HealthScoreRedThreshold)
            return;

        if (await _alertRepository.ExistsFiredAsync("HealthDrop", org.Id, cancellationToken))
            return;

        await FireAlertAsync(new AdminAlert
        {
            Id = Guid.NewGuid(),
            TenantId = org.Id,
            AlertType = "HealthDrop",
            Severity = AlertSeverity.Warning,
            Title = $"Tenant health critical: {org.Name}",
            Detail = $"Health score dropped to {score.OverallScore}",
            Status = AlertStatus.Fired,
            RoutedToRole = "SanzuOps",
            FiredAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task EvaluateBillingFailureAsync(Organization org, DateTime now, CancellationToken cancellationToken)
    {
        if (org.FailedPaymentAttempts <= 0 || org.LastPaymentFailedAt is null)
            return;

        if (await _alertRepository.ExistsFiredAsync("BillingFailure", org.Id, cancellationToken))
            return;

        await FireAlertAsync(new AdminAlert
        {
            Id = Guid.NewGuid(),
            TenantId = org.Id,
            AlertType = "BillingFailure",
            Severity = AlertSeverity.Critical,
            Title = $"Billing payment failed: {org.Name}",
            Detail = $"Failed {org.FailedPaymentAttempts} attempt(s). Last failed at {org.LastPaymentFailedAt:O}",
            Status = AlertStatus.Fired,
            RoutedToRole = "SanzuFinance",
            FiredAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task EvaluateCaseStuckAsync(DateTime now, CancellationToken cancellationToken)
    {
        var allOrgs = await _organizationRepository.GetAllAsync(cancellationToken);
        var activeOrgIds = allOrgs.Where(o => o.Status == TenantStatus.Active).Select(o => o.Id).ToHashSet();
        var orgLookup = allOrgs.ToDictionary(o => o.Id, o => o.Name);

        foreach (var orgId in activeOrgIds)
        {
            var cases = await _caseRepository.GetByTenantIdWithStepsForPlatformAsync(orgId, cancellationToken);
            var stuckCases = cases.Where(c =>
                c.Status == CaseStatus.Active
                && c.WorkflowSteps.Any(s =>
                    s.Status == WorkflowStepStatus.Blocked
                    && s.UpdatedAt < now.AddDays(-CaseStalledDaysThreshold)));

            foreach (var stuck in stuckCases)
            {
                if (await _alertRepository.ExistsFiredAsync("CaseStuck", orgId, cancellationToken))
                    continue;

                await FireAlertAsync(new AdminAlert
                {
                    Id = Guid.NewGuid(),
                    TenantId = orgId,
                    AlertType = "CaseStuck",
                    Severity = AlertSeverity.Warning,
                    Title = $"Case stuck: {stuck.CaseNumber} at {orgLookup.GetValueOrDefault(orgId, "Unknown")}",
                    Detail = $"Case {stuck.CaseNumber} has been blocked for over {CaseStalledDaysThreshold} days",
                    Status = AlertStatus.Fired,
                    RoutedToRole = "SanzuOps",
                    FiredAt = DateTime.UtcNow
                }, cancellationToken);
            }
        }
    }

    private async Task EvaluateOnboardingStalledAsync(Organization org, DateTime now, CancellationToken cancellationToken)
    {
        if (org.Status != TenantStatus.Active)
            return;

        if (org.OnboardingCompletedAt.HasValue)
            return;

        if (org.CreatedAt >= now.AddDays(-OnboardingStalledDaysThreshold))
            return;

        if (await _alertRepository.ExistsFiredAsync("OnboardingStalled", org.Id, cancellationToken))
            return;

        await FireAlertAsync(new AdminAlert
        {
            Id = Guid.NewGuid(),
            TenantId = org.Id,
            AlertType = "OnboardingStalled",
            Severity = AlertSeverity.Warning,
            Title = $"Onboarding stalled: {org.Name}",
            Detail = $"Tenant created {(now - org.CreatedAt).Days} days ago but onboarding not completed",
            Status = AlertStatus.Fired,
            RoutedToRole = "SanzuOps",
            FiredAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task FireAlertAsync(AdminAlert alert, CancellationToken cancellationToken)
    {
        await _alertRepository.CreateAsync(alert, cancellationToken);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = Guid.Empty,
                EventType = "Admin.Alert.Fired",
                Metadata = $"{{\"alertType\":\"{alert.AlertType}\",\"severity\":\"{alert.Severity}\",\"tenantId\":\"{alert.TenantId}\",\"routedToRole\":\"{alert.RoutedToRole}\"}}",
                CreatedAt = DateTime.UtcNow
            }, ct);
        }, cancellationToken);
    }

    private async Task LogAuditAsync(string eventType, Guid actorUserId, string metadata, CancellationToken cancellationToken)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = eventType,
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow
            }, ct);
        }, cancellationToken);
    }
}
