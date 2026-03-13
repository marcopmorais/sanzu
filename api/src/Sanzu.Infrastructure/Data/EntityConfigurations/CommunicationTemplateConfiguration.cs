using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class CommunicationTemplateConfiguration : IEntityTypeConfiguration<CommunicationTemplate>
{
    public void Configure(EntityTypeBuilder<CommunicationTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(256).IsRequired();
        builder.Property(t => t.Subject).HasMaxLength(512).IsRequired();
        builder.Property(t => t.Body).IsRequired();
        builder.Property(t => t.MessageType).HasMaxLength(64).IsRequired();
        builder.HasIndex(t => t.MessageType);
    }
}
