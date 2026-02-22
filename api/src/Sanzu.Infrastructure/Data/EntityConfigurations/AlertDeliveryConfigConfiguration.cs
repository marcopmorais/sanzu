using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class AlertDeliveryConfigConfiguration : IEntityTypeConfiguration<AlertDeliveryConfig>
{
    public void Configure(EntityTypeBuilder<AlertDeliveryConfig> builder)
    {
        builder.ToTable("AlertDeliveryConfigs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");

        builder.Property(x => x.Channel).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Target).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2").IsRequired();

        builder.HasIndex(x => x.Channel).IsUnique();
    }
}
