using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class ProcessAliasConfiguration : IEntityTypeConfiguration<ProcessAlias>
{
    public void Configure(EntityTypeBuilder<ProcessAlias> builder)
    {
        builder.ToTable("ProcessAliases");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.CaseId)
            .IsRequired();

        builder.Property(x => x.AliasEmail)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(ProcessAliasStatus.Active);

        builder.Property(x => x.RotatedFromAliasId)
            .IsRequired(false);

        builder.Property(x => x.LastUpdatedByUserId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Case)
            .WithMany(x => x.ProcessAliases)
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RotatedFromAlias)
            .WithMany()
            .HasForeignKey(x => x.RotatedFromAliasId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AliasEmail)
            .IsUnique()
            .HasDatabaseName("UX_ProcessAliases_AliasEmail");

        builder.HasIndex(x => new { x.TenantId, x.CaseId, x.UpdatedAt })
            .HasDatabaseName("IX_ProcessAliases_TenantId_CaseId_UpdatedAt");
    }
}
