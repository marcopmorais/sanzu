using System.Text.Json;
using FluentValidation;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class TenantSubscriptionService : ITenantSubscriptionService
{
    private static readonly Dictionary<string, decimal> PlanMonthlyPrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["STARTER"] = 149m,
        ["GROWTH"] = 399m,
        ["ENTERPRISE"] = 0m
    };

    private const int AnnualMultiplierMonths = 10;

    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<PreviewPlanChangeRequest> _previewValidator;
    private readonly IValidator<ChangePlanRequest> _changePlanValidator;
    private readonly IValidator<CancelSubscriptionRequest> _cancelValidator;

    public TenantSubscriptionService(
        IOrganizationRepository organizationRepository,
        IUserRoleRepository userRoleRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IValidator<PreviewPlanChangeRequest> previewValidator,
        IValidator<ChangePlanRequest> changePlanValidator,
        IValidator<CancelSubscriptionRequest> cancelValidator)
    {
        _organizationRepository = organizationRepository;
        _userRoleRepository = userRoleRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _previewValidator = previewValidator;
        _changePlanValidator = changePlanValidator;
        _cancelValidator = cancelValidator;
    }

    public async Task<PlanChangePreviewResponse> PreviewPlanChangeAsync(
        Guid tenantId,
        Guid actorUserId,
        PreviewPlanChangeRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _previewValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var tenant = await LoadAuthorizedTenantAsync(tenantId, actorUserId, cancellationToken);
        EnsureActiveState(tenant);

        var newPlan = request.PlanCode.Trim().ToUpperInvariant();
        var newCycle = request.BillingCycle.Trim().ToUpperInvariant();

        if (string.Equals(tenant.SubscriptionPlan, newPlan, StringComparison.OrdinalIgnoreCase)
            && string.Equals(tenant.SubscriptionBillingCycle, newCycle, StringComparison.OrdinalIgnoreCase))
        {
            throw new TenantOnboardingStateException(
                "The requested plan and billing cycle are the same as the current subscription.");
        }

        var currentMonthlyPrice = GetMonthlyPrice(tenant.SubscriptionPlan!);
        var newMonthlyPrice = GetMonthlyPrice(newPlan);
        var prorationAmount = CalculateProration(tenant, currentMonthlyPrice, newMonthlyPrice, newCycle);

        var description = newMonthlyPrice > currentMonthlyPrice
            ? $"Upgrade from {tenant.SubscriptionPlan} to {newPlan}. A prorated charge of {prorationAmount:F2} applies."
            : $"Downgrade from {tenant.SubscriptionPlan} to {newPlan}. A prorated credit of {Math.Abs(prorationAmount):F2} applies.";

        return new PlanChangePreviewResponse
        {
            CurrentPlan = tenant.SubscriptionPlan!,
            NewPlan = newPlan,
            CurrentBillingCycle = tenant.SubscriptionBillingCycle!,
            NewBillingCycle = newCycle,
            CurrentMonthlyPrice = currentMonthlyPrice,
            NewMonthlyPrice = newMonthlyPrice,
            ProrationAmount = prorationAmount,
            EffectiveDate = DateTime.UtcNow,
            Description = description
        };
    }

    public async Task<ChangePlanResponse> ChangePlanAsync(
        Guid tenantId,
        Guid actorUserId,
        ChangePlanRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _changePlanValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        ChangePlanResponse? response = null;
        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var tenant = await LoadAuthorizedTenantAsync(tenantId, actorUserId, token);
                EnsureActiveState(tenant);

                var newPlan = request.PlanCode.Trim().ToUpperInvariant();
                var newCycle = request.BillingCycle.Trim().ToUpperInvariant();

                if (string.Equals(tenant.SubscriptionPlan, newPlan, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(tenant.SubscriptionBillingCycle, newCycle, StringComparison.OrdinalIgnoreCase))
                {
                    throw new TenantOnboardingStateException(
                        "The requested plan and billing cycle are the same as the current subscription.");
                }

                var currentMonthlyPrice = GetMonthlyPrice(tenant.SubscriptionPlan!);
                var newMonthlyPrice = GetMonthlyPrice(newPlan);
                var calculatedProration = CalculateProration(tenant, currentMonthlyPrice, newMonthlyPrice, newCycle);

                if (request.ConfirmedProrationAmount != calculatedProration)
                {
                    throw new TenantOnboardingConflictException(
                        "The confirmed proration amount does not match the calculated amount. Please preview the change again.");
                }

                var nowUtc = DateTime.UtcNow;
                var previousPlan = tenant.SubscriptionPlan!;
                var previousCycle = tenant.SubscriptionBillingCycle!;

                tenant.PreviousSubscriptionPlan = previousPlan;
                tenant.SubscriptionPlan = newPlan;
                tenant.SubscriptionBillingCycle = newCycle;
                tenant.UpdatedAt = nowUtc;

                await WriteAuditEventAsync(
                    actorUserId,
                    "TenantSubscriptionPlanChanged",
                    new
                    {
                        OrganizationId = tenant.Id,
                        PreviousPlan = previousPlan,
                        PreviousBillingCycle = previousCycle,
                        NewPlan = newPlan,
                        NewBillingCycle = newCycle,
                        ProrationAmount = calculatedProration,
                        ChangedAt = nowUtc
                    },
                    token);

                response = new ChangePlanResponse
                {
                    TenantId = tenant.Id,
                    PlanCode = newPlan,
                    BillingCycle = newCycle,
                    PreviousPlan = previousPlan,
                    PreviousBillingCycle = previousCycle,
                    EffectiveDate = nowUtc,
                    ProrationAmount = calculatedProration,
                    ChangedAt = nowUtc
                };
            },
            cancellationToken);

        return response!;
    }

    public async Task<CancelSubscriptionResponse> CancelSubscriptionAsync(
        Guid tenantId,
        Guid actorUserId,
        CancelSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _cancelValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        CancelSubscriptionResponse? response = null;
        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var tenant = await LoadAuthorizedTenantAsync(tenantId, actorUserId, token);
                EnsureActiveState(tenant);

                if (tenant.SubscriptionCancelledAt.HasValue)
                {
                    throw new TenantOnboardingStateException(
                        "The subscription has already been cancelled.");
                }

                var nowUtc = DateTime.UtcNow;
                tenant.SubscriptionCancelledAt = nowUtc;
                tenant.SubscriptionCancellationReason = request.Reason.Trim();
                tenant.Status = TenantStatus.Suspended;
                tenant.UpdatedAt = nowUtc;

                await WriteAuditEventAsync(
                    actorUserId,
                    "TenantSubscriptionCancelled",
                    new
                    {
                        OrganizationId = tenant.Id,
                        Reason = request.Reason.Trim(),
                        CancelledAt = nowUtc,
                        PreviousStatus = TenantStatus.Active.ToString(),
                        NewStatus = TenantStatus.Suspended.ToString()
                    },
                    token);

                response = new CancelSubscriptionResponse
                {
                    TenantId = tenant.Id,
                    TenantStatus = tenant.Status,
                    Reason = tenant.SubscriptionCancellationReason,
                    CancelledAt = nowUtc
                };
            },
            cancellationToken);

        return response!;
    }

    private async Task<Organization> LoadAuthorizedTenantAsync(
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var tenant = await _organizationRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            throw new TenantAccessDeniedException();
        }

        var hasTenantAdminRole = await _userRoleRepository.HasRoleAsync(
            actorUserId,
            tenantId,
            PlatformRole.AgencyAdmin,
            cancellationToken);

        if (!hasTenantAdminRole)
        {
            throw new TenantAccessDeniedException();
        }

        return tenant;
    }

    private static void EnsureActiveState(Organization tenant)
    {
        if (tenant.Status != TenantStatus.Active)
        {
            throw new TenantOnboardingStateException(
                "Subscription changes can only be made for tenants with an active subscription.");
        }
    }

    private static decimal GetMonthlyPrice(string planCode)
    {
        return PlanMonthlyPrices.TryGetValue(planCode, out var price)
            ? price
            : 0m;
    }

    private static decimal CalculateProration(
        Organization tenant,
        decimal currentMonthlyPrice,
        decimal newMonthlyPrice,
        string newBillingCycle)
    {
        if (!tenant.SubscriptionActivatedAt.HasValue)
        {
            return 0m;
        }

        var currentCycle = tenant.SubscriptionBillingCycle!;
        var currentPeriodPrice = GetPeriodPrice(currentMonthlyPrice, currentCycle);
        var newPeriodPrice = GetPeriodPrice(newMonthlyPrice, newBillingCycle);

        var totalDaysInPeriod = GetTotalDaysInPeriod(currentCycle);
        var activatedAt = tenant.SubscriptionActivatedAt.Value;
        var now = DateTime.UtcNow;

        var elapsedDays = (now - activatedAt).TotalDays % totalDaysInPeriod;
        var remainingDays = totalDaysInPeriod - elapsedDays;

        var proration = (newPeriodPrice - currentPeriodPrice) * (decimal)(remainingDays / totalDaysInPeriod);
        return Math.Round(proration, 2);
    }

    private static decimal GetPeriodPrice(decimal monthlyPrice, string billingCycle)
    {
        return string.Equals(billingCycle, "ANNUAL", StringComparison.OrdinalIgnoreCase)
            ? monthlyPrice * AnnualMultiplierMonths
            : monthlyPrice;
    }

    private static double GetTotalDaysInPeriod(string billingCycle)
    {
        return string.Equals(billingCycle, "ANNUAL", StringComparison.OrdinalIgnoreCase)
            ? 365.0
            : 30.0;
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
            Metadata = JsonSerializer.Serialize(metadata)
        };

        return _auditRepository.CreateAsync(auditEvent, cancellationToken);
    }
}
