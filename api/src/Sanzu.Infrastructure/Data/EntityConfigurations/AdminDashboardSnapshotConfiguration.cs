using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class AdminDashboardSnapshotConfiguration : IEntityTypeConfiguration<AdminDashboardSnapshot>
{
    public void Configure(EntityTypeBuilder<AdminDashboardSnapshot> builder)
    {
        builder.ToTable("AdminDashboardSnapshots");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.SnapshotType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.JsonPayload)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.ComputedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnType("datetime2")
            .IsRequired();
    }
}
