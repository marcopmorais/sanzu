using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class KpiAlertLogConfiguration : IEntityTypeConfiguration<KpiAlertLog>
{
    public void Configure(EntityTypeBuilder<KpiAlertLog> builder)
    {
        builder.ToTable("KpiAlerts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.ThresholdId)
            .IsRequired();

        builder.Property(x => x.MetricKey)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.ThresholdValue)
            .IsRequired();

        builder.Property(x => x.ActualValue)
            .IsRequired();

        builder.Property(x => x.Severity)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.RouteTarget)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ContextJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.TriggeredByUserId)
            .IsRequired();

        builder.Property(x => x.TriggeredAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasIndex(x => new { x.MetricKey, x.TriggeredAt })
            .HasDatabaseName("IX_KpiAlerts_MetricKey_TriggeredAt");
    }
}
