using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class GlossaryTermConfiguration : IEntityTypeConfiguration<GlossaryTerm>
{
    public void Configure(EntityTypeBuilder<GlossaryTerm> builder)
    {
        builder.ToTable("GlossaryTerms");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Key)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Term)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Definition)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.WhyThisMatters)
            .HasMaxLength(400);

        builder.Property(x => x.Locale)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.Visibility)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(x => new { x.TenantId, x.Key, x.Locale })
            .IsUnique()
            .HasDatabaseName("IX_GlossaryTerms_TenantId_Key_Locale");
    }
}

