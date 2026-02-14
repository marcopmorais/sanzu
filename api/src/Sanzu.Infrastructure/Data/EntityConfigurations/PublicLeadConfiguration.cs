using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class PublicLeadConfiguration : IEntityTypeConfiguration<PublicLead>
{
    public void Configure(EntityTypeBuilder<PublicLead> builder)
    {
        builder.ToTable("PublicLeads");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.IntentType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.FullName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.OrganizationName)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.TeamSize)
            .IsRequired();

        builder.Property(x => x.TermsAccepted)
            .IsRequired();

        builder.Property(x => x.Qualified)
            .IsRequired();

        builder.Property(x => x.RouteTarget)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.RouteStatus)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.RouteFailureReason)
            .HasMaxLength(160);

        builder.Property(x => x.UtmSource)
            .HasMaxLength(120);

        builder.Property(x => x.UtmMedium)
            .HasMaxLength(120);

        builder.Property(x => x.UtmCampaign)
            .HasMaxLength(160);

        builder.Property(x => x.ReferrerPath)
            .HasMaxLength(512);

        builder.Property(x => x.LandingPath)
            .HasMaxLength(512);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(512);

        builder.Property(x => x.ClientIp)
            .HasMaxLength(64);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasIndex(x => new { x.IntentType, x.CreatedAt })
            .HasDatabaseName("IX_PublicLeads_IntentType_CreatedAt");
    }
}
