using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class RemediationActionConfiguration : IEntityTypeConfiguration<RemediationAction>
{
    public void Configure(EntityTypeBuilder<RemediationAction> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.QueueId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.QueueItemId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ActionType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AuditNote).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ImpactSummary).HasMaxLength(2000);
        builder.Property(x => x.VerificationType).HasMaxLength(100);
        builder.Property(x => x.VerificationResult).HasMaxLength(2000);

        builder.Property(x => x.Status)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<RemediationStatus>(v))
            .HasMaxLength(50);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.QueueId, x.QueueItemId });
    }
}
