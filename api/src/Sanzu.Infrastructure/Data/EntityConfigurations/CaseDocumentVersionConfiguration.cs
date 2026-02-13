using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class CaseDocumentVersionConfiguration : IEntityTypeConfiguration<CaseDocumentVersion>
{
    public void Configure(EntityTypeBuilder<CaseDocumentVersion> builder)
    {
        builder.ToTable("CaseDocumentVersions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.CaseId)
            .IsRequired();

        builder.Property(x => x.DocumentId)
            .IsRequired();

        builder.Property(x => x.VersionNumber)
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

        builder.Property(x => x.UploadedByUserId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Document)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.DocumentId, x.VersionNumber })
            .IsUnique()
            .HasDatabaseName("IX_CaseDocumentVersions_DocumentId_VersionNumber");

        builder.HasIndex(x => new { x.TenantId, x.CaseId, x.DocumentId, x.CreatedAt })
            .HasDatabaseName("IX_CaseDocumentVersions_TenantId_CaseId_DocumentId_CreatedAt");
    }
}
