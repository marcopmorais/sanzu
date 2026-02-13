using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class CaseHandoffConfiguration : IEntityTypeConfiguration<CaseHandoff>
{
    public void Configure(EntityTypeBuilder<CaseHandoff> builder)
    {
        builder.ToTable("CaseHandoffs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.CaseId)
            .IsRequired();

        builder.Property(x => x.PacketTitle)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(CaseHandoffStatus.PendingAdvisor);

        builder.Property(x => x.FollowUpRequired)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.StatusNotes)
            .HasMaxLength(1024);

        builder.Property(x => x.LastUpdatedByUserId)
            .IsRequired();

        builder.Property(x => x.LastStatusChangedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Case)
            .WithMany(x => x.Handoffs)
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.CaseId, x.UpdatedAt })
            .HasDatabaseName("IX_CaseHandoffs_TenantId_CaseId_UpdatedAt");
    }
}
