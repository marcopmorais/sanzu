using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class KpiThresholdConfiguration : IEntityTypeConfiguration<KpiThresholdDefinition>
{
    public void Configure(EntityTypeBuilder<KpiThresholdDefinition> builder)
    {
        builder.ToTable("KpiThresholds");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.MetricKey)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.ThresholdValue)
            .IsRequired();

        builder.Property(x => x.Severity)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.RouteTarget)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .IsRequired();

        builder.Property(x => x.UpdatedByUserId)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasIndex(x => x.MetricKey)
            .IsUnique()
            .HasDatabaseName("IX_KpiThresholds_MetricKey");
    }
}
