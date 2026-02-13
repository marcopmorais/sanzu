using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class SupportDiagnosticSessionConfiguration : IEntityTypeConfiguration<SupportDiagnosticSession>
{
    public void Configure(EntityTypeBuilder<SupportDiagnosticSession> builder)
    {
        builder.ToTable("SupportDiagnosticSessions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.RequestedByUserId)
            .IsRequired();

        builder.Property(x => x.Scope)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.StartedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(x => x.RevokedAt)
            .HasColumnType("datetime2");

        builder.HasIndex(x => new { x.TenantId, x.ExpiresAt })
            .HasDatabaseName("IX_SupportDiagnosticSessions_TenantId_ExpiresAt");
    }
}
