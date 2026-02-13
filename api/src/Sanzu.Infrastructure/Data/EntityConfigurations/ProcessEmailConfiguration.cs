using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class ProcessEmailConfiguration : IEntityTypeConfiguration<ProcessEmail>
{
    public void Configure(EntityTypeBuilder<ProcessEmail> builder)
    {
        builder.ToTable("ProcessEmails");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.CaseId)
            .IsRequired();

        builder.Property(x => x.ProcessAliasId)
            .IsRequired();

        builder.Property(x => x.ThreadId)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Direction)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Subject)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.SenderEmail)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.RecipientEmails)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.BodyPreview)
            .HasMaxLength(4096);

        builder.Property(x => x.ExternalMessageId)
            .HasMaxLength(256);

        builder.Property(x => x.SentAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Case)
            .WithMany(x => x.ProcessEmails)
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ProcessAlias)
            .WithMany()
            .HasForeignKey(x => x.ProcessAliasId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.CaseId, x.ThreadId, x.SentAt })
            .HasDatabaseName("IX_ProcessEmails_TenantId_CaseId_ThreadId_SentAt");
    }
}
