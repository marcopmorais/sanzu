using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class AgencyPlaybookConfiguration : IEntityTypeConfiguration<AgencyPlaybook>
{
    public void Configure(EntityTypeBuilder<AgencyPlaybook> builder)
    {
        builder.ToTable("AgencyPlaybooks");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Version)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.ChangeNotes)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedByUserId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.ActivatedAt)
            .HasColumnType("datetime2");

        builder.HasIndex(x => new { x.TenantId, x.Name, x.Version })
            .IsUnique()
            .HasDatabaseName("IX_AgencyPlaybooks_TenantId_Name_Version");
    }
}
