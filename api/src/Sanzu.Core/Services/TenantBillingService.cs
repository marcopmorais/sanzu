using System.Text.Json;
using FluentValidation;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class TenantBillingService : ITenantBillingService
{
    private static readonly Dictionary<string, decimal> PlanMonthlyPrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["STARTER"] = 149m,
        ["GROWTH"] = 399m,
        ["ENTERPRISE"] = 0m
    };

    private static readonly Dictionary<string, int> PlanIncludedCases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["STARTER"] = 20,
        ["GROWTH"] = 75,
        ["ENTERPRISE"] = 0
    };

    private static readonly Dictionary<string, decimal> PlanOverageUnitPrice = new(StringComparer.OrdinalIgnoreCase)
    {
        ["STARTER"] = 6m,
        ["GROWTH"] = 4m,
        ["ENTERPRISE"] = 0m
    };

    private const int AnnualMultiplierMonths = 10;
    private const decimal VatRate = 0.23m;
    private static readonly TimeSpan[] RetrySchedule = new[]
    {
        TimeSpan.FromDays(1),
        TimeSpan.FromDays(3)
    };
    private const int MaxFailedPaymentAttempts = 3;

    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IBillingRecordRepository _billingRecordRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RegisterFailedPaymentRequest> _registerFailedPaymentValidator;
    private readonly IValidator<ExecutePaymentRecoveryRequest> _executePaymentRecoveryValidator;

    public TenantBillingService(
        IOrganizationRepository organizationRepository,
        IUserRoleRepository userRoleRepository,
        IBillingRecordRepository billingRecordRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IValidator<RegisterFailedPaymentRequest> registerFailedPaymentValidator,
        IValidator<ExecutePaymentRecoveryRequest> executePaymentRecoveryValidator)
    {
        _organizationRepository = organizationRepository;
        _userRoleRepository = userRoleRepository;
        _billingRecordRepository = billingRecordRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _registerFailedPaymentValidator = registerFailedPaymentValidator;
        _executePaymentRecoveryValidator = executePaymentRecoveryValidator;
    }

    public async Task<BillingHistoryResponse> GetBillingHistoryAsync(
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var tenant = await LoadAuthorizedTenantAsync(tenantId, actorUserId, cancellationToken);
        EnsureActiveOrSuspended(tenant);

        var records = await _billingRecordRepository.GetByTenantIdAsync(tenantId, cancellationToken);

        var recordResponses = records.Select(r => new BillingRecordResponse
        {
            Id = r.Id,
            InvoiceNumber = r.InvoiceNumber,
            BillingCycleStart = r.BillingCycleStart,
            BillingCycleEnd = r.BillingCycleEnd,
            PlanCode = r.PlanCode,
            BillingCycle = r.BillingCycle,
            BaseAmount = r.BaseAmount,
            OverageUnits = r.OverageUnits,
            OverageAmount = r.OverageAmount,
            TaxRate = r.TaxRate,
            TaxAmount = r.TaxAmount,
            TotalAmount = r.TotalAmount,
            Currency = r.Currency,
            Status = r.Status,
            CreatedAt = r.CreatedAt
        }).ToList();

        return new BillingHistoryResponse
        {
            TenantId = tenant.Id,
            Records = recordResponses,
            CurrentPlan = tenant.SubscriptionPlan ?? string.Empty,
            CurrentBillingCycle = tenant.SubscriptionBillingCycle ?? string.Empty,
            CurrentMonthlyPrice = GetMonthlyPrice(tenant.SubscriptionPlan)
        };
    }

    public async Task<BillingUsageSummaryResponse> GetUsageSummaryAsync(
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var tenant = await LoadAuthorizedTenantAsync(tenantId, actorUserId, cancellationToken);
        EnsureActiveState(tenant);

        var (periodStart, periodEnd) = CalculateCurrentBillingPeriod(tenant);

        return new BillingUsageSummaryResponse
        {
            TenantId = tenant.Id,
            PlanCode = tenant.SubscriptionPlan ?? string.Empty,
            BillingCycle = tenant.SubscriptionBillingCycle ?? string.Empty,
            MonthlyPrice = GetMonthlyPrice(tenant.SubscriptionPlan),
            IncludedCases = GetIncludedCases(tenant.SubscriptionPlan),
            UsedCases = 0,
            OverageCases = 0,
            OverageUnitPrice = GetOverageUnitPrice(tenant.SubscriptionPlan),
            CurrentPeriodStart = periodStart,
            CurrentPeriodEnd = periodEnd,
            SubscriptionActivatedAt = tenant.SubscriptionActivatedAt!.Value
        };
    }

    public async Task<InvoiceDownloadResponse> GetInvoiceAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var tenant = await LoadAuthorizedTenantAsync(tenantId, actorUserId, cancellationToken);

        var record = await _billingRecordRepository.GetByIdAsync(invoiceId, cancellationToken);
        if (record is null || record.TenantId != tenant.Id)
        {
            throw new TenantOnboardingStateException("The requested invoice was not found.");
        }

        return new InvoiceDownloadResponse
        {
            InvoiceNumber = record.InvoiceNumber,
            InvoiceSnapshot = record.InvoiceSnapshot,
            TenantId = record.TenantId,
            CreatedAt = record.CreatedAt
        };
    }

    public async Task<BillingRecordResponse> CreateBillingRecordAsync(
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        BillingRecordResponse? response = null;
        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var tenant = await LoadAuthorizedTenantAsync(tenantId, actorUserId, token);
                EnsureActiveState(tenant);

                var planCode = tenant.SubscriptionPlan!;
                var billingCycle = tenant.SubscriptionBillingCycle!;
                var monthlyPrice = GetMonthlyPrice(planCode);
                var baseAmount = GetPeriodPrice(monthlyPrice, billingCycle);
                var overageUnits = 0;
                var overageAmount = 0m;
                var subtotal = baseAmount + overageAmount;
                var taxAmount = Math.Round(subtotal * VatRate, 2);
                var totalAmount = subtotal + taxAmount;

                var (periodStart, periodEnd) = CalculateCurrentBillingPeriod(tenant);
                var nextNumber = await _billingRecordRepository.GetNextInvoiceNumberAsync(tenantId, token);
                var invoiceNumber = $"INV-{nextNumber:D5}";

                var snapshot = JsonSerializer.Serialize(new
                {
                    InvoiceNumber = invoiceNumber,
                    TenantId = tenant.Id,
                    TenantName = tenant.Name,
                    InvoiceProfileLegalName = tenant.InvoiceProfileLegalName,
                    InvoiceProfileVatNumber = tenant.InvoiceProfileVatNumber,
                    InvoiceProfileBillingEmail = tenant.InvoiceProfileBillingEmail,
                    InvoiceProfileCountryCode = tenant.InvoiceProfileCountryCode,
                    PlanCode = planCode,
                    BillingCycle = billingCycle,
                    BillingCycleStart = periodStart,
                    BillingCycleEnd = periodEnd,
                    LineItems = new[]
                    {
                        new { Description = $"{planCode} plan ({billingCycle})", Amount = baseAmount }
                    },
                    BaseAmount = baseAmount,
                    OverageUnits = overageUnits,
                    OverageAmount = overageAmount,
                    TaxRate = VatRate,
                    TaxAmount = taxAmount,
                    TotalAmount = totalAmount,
                    Currency = "EUR",
                    GeneratedAt = DateTime.UtcNow
                });

                var nowUtc = DateTime.UtcNow;
                var record = new BillingRecord
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    InvoiceNumber = invoiceNumber,
                    BillingCycleStart = periodStart,
                    BillingCycleEnd = periodEnd,
                    PlanCode = planCode,
                    BillingCycle = billingCycle,
                    BaseAmount = baseAmount,
                    OverageUnits = overageUnits,
                    OverageAmount = overageAmount,
                    TaxRate = VatRate,
                    TaxAmount = taxAmount,
                    TotalAmount = totalAmount,
                    Currency = "EUR",
                    Status = "FINALIZED",
                    InvoiceSnapshot = snapshot,
                    CreatedAt = nowUtc
                };

                await _billingRecordRepository.CreateAsync(record, token);

                await WriteAuditEventAsync(
                    actorUserId,
                    "TenantBillingRecordCreated",
                    new
                    {
                        OrganizationId = tenant.Id,
                        record.InvoiceNumber,
                        record.PlanCode,
                        record.BillingCycle,
                        record.BaseAmount,
                        record.TaxAmount,
                        record.TotalAmount,
                        record.CreatedAt
                    },
                    token);

                response = new BillingRecordResponse
                {
                    Id = record.Id,
                    InvoiceNumber = record.InvoiceNumber,
                    BillingCycleStart = record.BillingCycleStart,
                    BillingCycleEnd = record.BillingCycleEnd,
                    PlanCode = record.PlanCode,
                    BillingCycle = record.BillingCycle,
                    BaseAmount = record.BaseAmount,
                    OverageUnits = record.OverageUnits,
                    OverageAmount = record.OverageAmount,
                    TaxRate = record.TaxRate,
                    TaxAmount = record.TaxAmount,
                    TotalAmount = record.TotalAmount,
                    Currency = record.Currency,
                    Status = record.Status,
                    CreatedAt = record.CreatedAt
                };
            },
            cancellationToken);

        return response!;
    }

    public async Task<PaymentRecoveryStatusResponse> RegisterFailedPaymentAsync(
        Guid tenantId,
        Guid actorUserId,
        RegisterFailedPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _registerFailedPaymentValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        PaymentRecoveryStatusResponse? response = null;
        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var tenant = await LoadAuthorizedTenantAsync(tenantId, actorUserId, token);
                EnsureRecoveryInitializationState(tenant);

                var nowUtc = DateTime.UtcNow;
                var previousStatus = tenant.Status;
                var failureReason = request.Reason.Trim();

                ApplyFailedPaymentState(tenant, failureReason, nowUtc);

                await WriteAuditEventAsync(
                    actorUserId,
                    "TenantPaymentFailed",
                    new
                    {
                        OrganizationId = tenant.Id,
                        PreviousStatus = previousStatus.ToString(),
                        NewStatus = tenant.Status.ToString(),
                        tenant.FailedPaymentAttempts,
                        tenant.LastPaymentFailedAt,
                        tenant.NextPaymentRetryAt,
                        tenant.NextPaymentReminderAt,
                        FailureReason = failureReason,
                        request.PaymentReference
                    },
                    token);

                response = BuildRecoveryStatusResponse(
                    tenant,
                    recoveryComplete: false,
                    message: tenant.Status == TenantStatus.Suspended
                        ? "Maximum failed payment attempts reached. Tenant has been suspended."
                        : "Payment failure recorded. Retry and reminder schedule has been applied.");
            },
            cancellationToken);

        return response!;
    }

    public async Task<PaymentRecoveryStatusResponse> ExecutePaymentRecoveryAsync(
        Guid tenantId,
        Guid actorUserId,
        ExecutePaymentRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _executePaymentRecoveryValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        PaymentRecoveryStatusResponse? response = null;
        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var tenant = await LoadAuthorizedTenantAsync(tenantId, actorUserId, token);
                EnsureRecoveryExecutionState(tenant, request);

                var nowUtc = DateTime.UtcNow;

                if (request.ReminderSent && tenant.Status == TenantStatus.PaymentIssue)
                {
                    tenant.LastPaymentReminderSentAt = nowUtc;
                    tenant.UpdatedAt = nowUtc;

                    await WriteAuditEventAsync(
                        actorUserId,
                        "TenantPaymentReminderSent",
                        new
                        {
                            OrganizationId = tenant.Id,
                            tenant.FailedPaymentAttempts,
                            SentAt = nowUtc,
                            ScheduledReminderAt = tenant.NextPaymentReminderAt
                        },
                        token);
                }

                if (request.RetrySucceeded)
                {
                    var previousStatus = tenant.Status;
                    tenant.Status = TenantStatus.Active;
                    tenant.FailedPaymentAttempts = 0;
                    tenant.LastPaymentFailedAt = null;
                    tenant.LastPaymentFailureReason = null;
                    tenant.NextPaymentRetryAt = null;
                    tenant.NextPaymentReminderAt = null;
                    tenant.UpdatedAt = nowUtc;

                    await WriteAuditEventAsync(
                        actorUserId,
                        "TenantPaymentRecovered",
                        new
                        {
                            OrganizationId = tenant.Id,
                            PreviousStatus = previousStatus.ToString(),
                            NewStatus = tenant.Status.ToString(),
                            RecoveredAt = nowUtc
                        },
                        token);

                    response = BuildRecoveryStatusResponse(
                        tenant,
                        recoveryComplete: true,
                        message: "Payment recovered and tenant has been restored to active status.");

                    return;
                }

                var previousFailedAttempts = tenant.FailedPaymentAttempts;
                var previousStatusForFailure = tenant.Status;
                var retryFailureReason = request.FailureReason!.Trim();
                ApplyFailedPaymentState(tenant, retryFailureReason, nowUtc);

                await WriteAuditEventAsync(
                    actorUserId,
                    "TenantPaymentRetryFailed",
                    new
                    {
                        OrganizationId = tenant.Id,
                        PreviousStatus = previousStatusForFailure.ToString(),
                        NewStatus = tenant.Status.ToString(),
                        PreviousFailedAttempts = previousFailedAttempts,
                        CurrentFailedAttempts = tenant.FailedPaymentAttempts,
                        FailureReason = retryFailureReason,
                        FailedAt = nowUtc,
                        tenant.NextPaymentRetryAt,
                        tenant.NextPaymentReminderAt
                    },
                    token);

                response = BuildRecoveryStatusResponse(
                    tenant,
                    recoveryComplete: false,
                    message: tenant.Status == TenantStatus.Suspended
                        ? "Retry attempts exhausted. Tenant has been suspended."
                        : "Retry failed. Recovery schedule has been updated.");
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
                "Billing operations can only be performed for tenants with an active subscription.");
        }
    }

    private static void EnsureActiveOrSuspended(Organization tenant)
    {
        if (tenant.Status is not (TenantStatus.Active or TenantStatus.PaymentIssue or TenantStatus.Suspended))
        {
            throw new TenantOnboardingStateException(
                "Billing history can only be viewed for tenants with an active, payment-issue, or suspended subscription.");
        }
    }

    private static void EnsureRecoveryInitializationState(Organization tenant)
    {
        if (tenant.Status is not (TenantStatus.Active or TenantStatus.PaymentIssue))
        {
            throw new TenantOnboardingStateException(
                "Failed payment events can only be registered for active or payment-issue tenants.");
        }
    }

    private static void EnsureRecoveryExecutionState(Organization tenant, ExecutePaymentRecoveryRequest request)
    {
        if (tenant.Status is not (TenantStatus.PaymentIssue or TenantStatus.Suspended))
        {
            throw new TenantOnboardingStateException(
                "Payment recovery can only be executed when the tenant is in payment-issue or suspended status.");
        }

        if (!request.RetrySucceeded && tenant.Status == TenantStatus.Suspended)
        {
            throw new TenantOnboardingStateException(
                "The tenant is already suspended due to failed payments. A successful retry is required to recover.");
        }
    }

    private static void ApplyFailedPaymentState(
        Organization tenant,
        string failureReason,
        DateTime nowUtc)
    {
        tenant.FailedPaymentAttempts += 1;
        tenant.LastPaymentFailedAt = nowUtc;
        tenant.LastPaymentFailureReason = failureReason;
        tenant.UpdatedAt = nowUtc;

        if (tenant.FailedPaymentAttempts >= MaxFailedPaymentAttempts)
        {
            tenant.Status = TenantStatus.Suspended;
            tenant.NextPaymentRetryAt = null;
            tenant.NextPaymentReminderAt = null;
            return;
        }

        var delay = RetrySchedule[tenant.FailedPaymentAttempts - 1];
        tenant.Status = TenantStatus.PaymentIssue;
        tenant.NextPaymentRetryAt = nowUtc.Add(delay);
        tenant.NextPaymentReminderAt = nowUtc.Add(TimeSpan.FromTicks(delay.Ticks / 2));
    }

    private static PaymentRecoveryStatusResponse BuildRecoveryStatusResponse(
        Organization tenant,
        bool recoveryComplete,
        string message)
    {
        return new PaymentRecoveryStatusResponse
        {
            TenantId = tenant.Id,
            TenantStatus = tenant.Status,
            FailedPaymentAttempts = tenant.FailedPaymentAttempts,
            LastPaymentFailedAt = tenant.LastPaymentFailedAt,
            LastPaymentFailureReason = tenant.LastPaymentFailureReason,
            NextPaymentRetryAt = tenant.NextPaymentRetryAt,
            NextPaymentReminderAt = tenant.NextPaymentReminderAt,
            LastPaymentReminderSentAt = tenant.LastPaymentReminderSentAt,
            RecoveryComplete = recoveryComplete,
            Message = message
        };
    }

    private static decimal GetMonthlyPrice(string? planCode)
    {
        if (planCode is null) return 0m;
        return PlanMonthlyPrices.TryGetValue(planCode, out var price) ? price : 0m;
    }

    private static int GetIncludedCases(string? planCode)
    {
        if (planCode is null) return 0;
        return PlanIncludedCases.TryGetValue(planCode, out var cases) ? cases : 0;
    }

    private static decimal GetOverageUnitPrice(string? planCode)
    {
        if (planCode is null) return 0m;
        return PlanOverageUnitPrice.TryGetValue(planCode, out var price) ? price : 0m;
    }

    private static decimal GetPeriodPrice(decimal monthlyPrice, string billingCycle)
    {
        return string.Equals(billingCycle, "ANNUAL", StringComparison.OrdinalIgnoreCase)
            ? monthlyPrice * AnnualMultiplierMonths
            : monthlyPrice;
    }

    private static (DateTime Start, DateTime End) CalculateCurrentBillingPeriod(Organization tenant)
    {
        if (!tenant.SubscriptionActivatedAt.HasValue)
        {
            var now = DateTime.UtcNow;
            return (now, now.AddMonths(1));
        }

        var activatedAt = tenant.SubscriptionActivatedAt.Value;
        var isAnnual = string.Equals(tenant.SubscriptionBillingCycle, "ANNUAL", StringComparison.OrdinalIgnoreCase);
        var periodMonths = isAnnual ? 12 : 1;
        var currentTime = DateTime.UtcNow;

        var periodStart = activatedAt;
        while (periodStart.AddMonths(periodMonths) <= currentTime)
        {
            periodStart = periodStart.AddMonths(periodMonths);
        }

        return (periodStart, periodStart.AddMonths(periodMonths));
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
