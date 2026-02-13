using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class CaseConfiguration : IEntityTypeConfiguration<Case>
{
    public void Configure(EntityTypeBuilder<Case> builder)
    {
        builder.ToTable("Cases");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.CaseNumber)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.DeceasedFullName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.DateOfDeath)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.CaseType)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Urgency)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.Property(x => x.ManagerUserId)
            .IsRequired();

        builder.Property(x => x.IntakeData)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.IntakeCompletedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.IntakeCompletedByUserId);

        builder.Property(x => x.ClosedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.ArchivedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Tenant)
            .WithMany(x => x.Cases)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ManagerUser)
            .WithMany(x => x.ManagedCases)
            .HasForeignKey(x => x.ManagerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.CaseNumber })
            .IsUnique()
            .HasDatabaseName("IX_Cases_TenantId_CaseNumber");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_Cases_TenantId_Status");
    }
}
