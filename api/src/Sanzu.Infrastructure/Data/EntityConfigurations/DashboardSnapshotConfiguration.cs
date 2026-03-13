using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class DashboardSnapshotConfiguration : IEntityTypeConfiguration<DashboardSnapshot>
{
    public void Configure(EntityTypeBuilder<DashboardSnapshot> builder)
    {
        builder.ToTable("DashboardSnapshots");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.ComputedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(x => x.IsStale)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.TotalTenants).IsRequired();
        builder.Property(x => x.ActiveTenants).IsRequired();
        builder.Property(x => x.GreenTenants).IsRequired();
        builder.Property(x => x.YellowTenants).IsRequired();
        builder.Property(x => x.RedTenants).IsRequired();

        builder.Property(x => x.TotalRevenueMtd)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(x => x.OpenAlerts).IsRequired();

        builder.Property(x => x.AvgHealthScore)
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(x => x.Metadata)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            .HasDefaultValue("{}");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        builder.HasIndex(x => x.ComputedAt)
            .HasDatabaseName("IX_DashboardSnapshots_ComputedAt");
    }
}
