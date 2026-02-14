using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class CaseParticipantConfiguration : IEntityTypeConfiguration<CaseParticipant>
{
    public void Configure(EntityTypeBuilder<CaseParticipant> builder)
    {
        builder.ToTable("CaseParticipants");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.CaseId)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.AcceptedAt)
            .HasColumnType("datetime2");

        builder.HasOne(x => x.Tenant)
            .WithMany(x => x.CaseParticipants)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Case)
            .WithMany(x => x.Participants)
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InvitedByUser)
            .WithMany(x => x.InvitedCaseParticipants)
            .HasForeignKey(x => x.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ParticipantUser)
            .WithMany(x => x.ParticipatingCases)
            .HasForeignKey(x => x.ParticipantUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CaseId, x.Email, x.Status })
            .HasDatabaseName("IX_CaseParticipants_CaseId_Email_Status");

        builder.HasIndex(x => new { x.CaseId, x.ParticipantUserId, x.Status })
            .HasDatabaseName("IX_CaseParticipants_CaseId_ParticipantUserId_Status");
    }
}
