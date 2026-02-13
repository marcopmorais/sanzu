using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class TenantPolicyControlConfiguration : IEntityTypeConfiguration<TenantPolicyControl>
{
    public void Configure(EntityTypeBuilder<TenantPolicyControl> builder)
    {
        builder.ToTable("TenantPolicyControls");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.ControlType)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .IsRequired();

        builder.Property(x => x.ReasonCode)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.AppliedByUserId)
            .IsRequired();

        builder.Property(x => x.AppliedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.ControlType })
            .IsUnique()
            .HasDatabaseName("IX_TenantPolicyControls_TenantId_ControlType");
    }
}
