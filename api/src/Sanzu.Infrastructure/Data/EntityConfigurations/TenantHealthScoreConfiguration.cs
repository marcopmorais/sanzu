using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class TenantHealthScoreConfiguration : IEntityTypeConfiguration<TenantHealthScore>
{
    public void Configure(EntityTypeBuilder<TenantHealthScore> builder)
    {
        builder.ToTable("TenantHealthScores");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.OverallScore)
            .IsRequired();

        builder.Property(x => x.BillingScore)
            .IsRequired();

        builder.Property(x => x.CaseCompletionScore)
            .IsRequired();

        builder.Property(x => x.OnboardingScore)
            .IsRequired();

        builder.Property(x => x.HealthBand)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.PrimaryIssue)
            .HasMaxLength(200);

        builder.Property(x => x.ComputedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(x => new { x.TenantId, x.ComputedAt })
            .HasDatabaseName("IX_TenantHealthScore_TenantId_ComputedAt");
    }
}
