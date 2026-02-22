using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class TenantCommunicationConfiguration : IEntityTypeConfiguration<TenantCommunication>
{
    public void Configure(EntityTypeBuilder<TenantCommunication> builder)
    {
        builder.ToTable("TenantCommunications");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWID()");

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.SenderUserId).IsRequired();
        builder.Property(x => x.MessageType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Body).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.TemplateId).HasMaxLength(100);
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.CreatedAt })
            .HasDatabaseName("IX_TenantCommunications_TenantId_CreatedAt");
    }
}
