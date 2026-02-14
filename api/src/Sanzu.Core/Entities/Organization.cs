using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public TenantStatus Status { get; set; } = TenantStatus.Pending;
    public DateTime? OnboardingCompletedAt { get; set; }
    public string? DefaultLocale { get; set; }
    public string? DefaultTimeZone { get; set; }
    public string? DefaultCurrency { get; set; }
    public string? DefaultWorkflowKey { get; set; }
    public string? DefaultTemplateKey { get; set; }
    public long CaseDefaultsVersion { get; set; }
    public string? SubscriptionPlan { get; set; }
    public string? SubscriptionBillingCycle { get; set; }
    public string? PaymentMethodType { get; set; }
    public string? PaymentMethodReference { get; set; }
    public string? InvoiceProfileLegalName { get; set; }
    public string? InvoiceProfileVatNumber { get; set; }
    public string? InvoiceProfileBillingEmail { get; set; }
    public string? InvoiceProfileCountryCode { get; set; }
    public DateTime? SubscriptionActivatedAt { get; set; }
    public DateTime? SubscriptionCancelledAt { get; set; }
    public string? SubscriptionCancellationReason { get; set; }
    public string? PreviousSubscriptionPlan { get; set; }
    public int FailedPaymentAttempts { get; set; }
    public DateTime? LastPaymentFailedAt { get; set; }
    public string? LastPaymentFailureReason { get; set; }
    public DateTime? NextPaymentRetryAt { get; set; }
    public DateTime? NextPaymentReminderAt { get; set; }
    public DateTime? LastPaymentReminderSentAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Case> Cases { get; set; } = new List<Case>();
    public ICollection<CaseParticipant> CaseParticipants { get; set; } = new List<CaseParticipant>();
    public ICollection<TenantInvitation> TenantInvitations { get; set; } = new List<TenantInvitation>();
    public ICollection<BillingRecord> BillingRecords { get; set; } = new List<BillingRecord>();
}
