using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class AdminAlertConfiguration : IEntityTypeConfiguration<AdminAlert>
{
    public void Configure(EntityTypeBuilder<AdminAlert> builder)
    {
        builder.ToTable("AdminAlerts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.TenantId);

        builder.Property(x => x.AlertType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Severity)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Detail)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.RoutedToRole)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.OwnedByUserId);

        builder.Property(x => x.FiredAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(x => x.AcknowledgedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.ResolvedAt)
            .HasColumnType("datetime2");

        builder.HasIndex(x => new { x.Status, x.FiredAt })
            .HasDatabaseName("IX_AdminAlert_Status_FiredAt");
    }
}
