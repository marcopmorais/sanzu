using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Location)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.OnboardingCompletedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.DefaultLocale)
            .HasMaxLength(16);

        builder.Property(x => x.DefaultTimeZone)
            .HasMaxLength(64);

        builder.Property(x => x.DefaultCurrency)
            .HasMaxLength(8);

        builder.Property(x => x.DefaultWorkflowKey)
            .HasMaxLength(128);

        builder.Property(x => x.DefaultTemplateKey)
            .HasMaxLength(128);

        builder.Property(x => x.CaseDefaultsVersion)
            .HasDefaultValue(0L)
            .IsRequired();

        builder.Property(x => x.SubscriptionPlan)
            .HasMaxLength(32);

        builder.Property(x => x.SubscriptionBillingCycle)
            .HasMaxLength(16);

        builder.Property(x => x.PaymentMethodType)
            .HasMaxLength(32);

        builder.Property(x => x.PaymentMethodReference)
            .HasMaxLength(128);

        builder.Property(x => x.InvoiceProfileLegalName)
            .HasMaxLength(255);

        builder.Property(x => x.InvoiceProfileVatNumber)
            .HasMaxLength(64);

        builder.Property(x => x.InvoiceProfileBillingEmail)
            .HasMaxLength(255);

        builder.Property(x => x.InvoiceProfileCountryCode)
            .HasMaxLength(2);

        builder.Property(x => x.SubscriptionActivatedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.SubscriptionCancelledAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.SubscriptionCancellationReason)
            .HasMaxLength(2000);

        builder.Property(x => x.PreviousSubscriptionPlan)
            .HasMaxLength(32);

        builder.Property(x => x.FailedPaymentAttempts)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.LastPaymentFailedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.LastPaymentFailureReason)
            .HasMaxLength(2000);

        builder.Property(x => x.NextPaymentRetryAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.NextPaymentReminderAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.LastPaymentReminderSentAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(x => x.Name)
            .HasDatabaseName("IX_Organizations_Name");
    }
}
