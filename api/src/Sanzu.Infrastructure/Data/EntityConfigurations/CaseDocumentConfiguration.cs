using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class CaseDocumentConfiguration : IEntityTypeConfiguration<CaseDocument>
{
    public void Configure(EntityTypeBuilder<CaseDocument> builder)
    {
        builder.ToTable("CaseDocuments");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.CaseId)
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(127)
            .IsRequired();

        builder.Property(x => x.SizeBytes)
            .IsRequired();

        builder.Property(x => x.Content)
            .HasColumnType("varbinary(max)")
            .IsRequired();

        builder.Property(x => x.CurrentVersionNumber)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(x => x.Classification)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(CaseDocumentClassification.Optional);

        builder.Property(x => x.UploadedByUserId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Case)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.CaseId, x.CreatedAt })
            .HasDatabaseName("IX_CaseDocuments_TenantId_CaseId_CreatedAt");

        builder.HasIndex(x => new { x.TenantId, x.CaseId, x.Classification })
            .HasDatabaseName("IX_CaseDocuments_TenantId_CaseId_Classification");
    }
}
