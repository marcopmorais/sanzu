using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class ExtractionCandidateConfiguration : IEntityTypeConfiguration<ExtractionCandidate>
{
    public void Configure(EntityTypeBuilder<ExtractionCandidate> builder)
    {
        builder.ToTable("ExtractionCandidates");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.CaseId)
            .IsRequired();

        builder.Property(x => x.DocumentId)
            .IsRequired();

        builder.Property(x => x.FieldKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.CandidateValue)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.ConfidenceScore)
            .HasColumnType("decimal(5,4)")
            .IsRequired();

        builder.Property(x => x.SourceVersionNumber)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(ExtractionCandidateStatus.Pending);

        builder.Property(x => x.ReviewedByUserId);

        builder.Property(x => x.ReviewedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Document)
            .WithMany()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.CaseId, x.DocumentId, x.CreatedAt })
            .HasDatabaseName("IX_ExtractionCandidates_TenantId_CaseId_DocumentId_CreatedAt");

        builder.HasIndex(x => new { x.DocumentId, x.Status })
            .HasDatabaseName("IX_ExtractionCandidates_DocumentId_Status");
    }
}
