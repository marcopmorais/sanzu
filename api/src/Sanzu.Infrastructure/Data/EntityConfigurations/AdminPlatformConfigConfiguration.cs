using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class AdminPlatformConfigConfiguration : IEntityTypeConfiguration<AdminPlatformConfig>
{
    public void Configure(EntityTypeBuilder<AdminPlatformConfig> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.ConfigKey).IsUnique();
        builder.Property(c => c.ConfigKey).HasMaxLength(128).IsRequired();
        builder.Property(c => c.ConfigValue).HasMaxLength(1024).IsRequired();
    }
}
