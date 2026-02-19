using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.RoleType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.GrantedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.User)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Tenant)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.GrantedByUser)
            .WithMany(x => x.GrantedRoles)
            .HasForeignKey(x => x.GrantedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.UserId, x.RoleType, x.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_UserRoles_UserId_RoleType_TenantId");

        builder.ToTable(
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_UserRoles_RoleType",
                    "[RoleType] IN ('AgencyAdmin','SanzuAdmin','SanzuOps','SanzuFinance','SanzuSupport','SanzuViewer')");
            });
    }
}
