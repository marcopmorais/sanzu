using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class BillingRecordConfiguration : IEntityTypeConfiguration<BillingRecord>
{
    public void Configure(EntityTypeBuilder<BillingRecord> builder)
    {
        builder.ToTable("BillingRecords");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.HasOne(x => x.Tenant)
            .WithMany(x => x.BillingRecords)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.InvoiceNumber)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.BillingCycleStart)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(x => x.BillingCycleEnd)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(x => x.PlanCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.BillingCycle)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.BaseAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.OverageUnits)
            .IsRequired();

        builder.Property(x => x.OverageAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.TaxRate)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.TaxAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.InvoiceSnapshot)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("IX_BillingRecords_TenantId");

        builder.HasIndex(x => new { x.TenantId, x.InvoiceNumber })
            .IsUnique()
            .HasDatabaseName("IX_BillingRecords_TenantId_InvoiceNumber");
    }
}
