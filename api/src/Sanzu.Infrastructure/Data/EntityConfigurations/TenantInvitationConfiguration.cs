using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class TenantInvitationConfiguration : IEntityTypeConfiguration<TenantInvitation>
{
    public void Configure(EntityTypeBuilder<TenantInvitation> builder)
    {
        builder.ToTable("TenantInvitations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.RoleType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.AcceptedAt)
            .HasColumnType("datetime2");

        builder.HasOne(x => x.Tenant)
            .WithMany(x => x.TenantInvitations)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InvitedByUser)
            .WithMany(x => x.IssuedTenantInvitations)
            .HasForeignKey(x => x.InvitedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.Email })
            .HasDatabaseName("IX_TenantInvitations_TenantId_Email");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_TenantInvitations_TenantId_Status");

        builder.HasIndex(x => new { x.TenantId, x.Email, x.Status })
            .IsUnique()
            .HasFilter("[Status] = 'Pending'")
            .HasDatabaseName("IX_TenantInvitations_TenantId_Email_Pending");
    }
}
