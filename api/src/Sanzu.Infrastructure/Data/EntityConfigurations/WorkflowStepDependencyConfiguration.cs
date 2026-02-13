using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data.EntityConfigurations;

public sealed class WorkflowStepDependencyConfiguration : IEntityTypeConfiguration<WorkflowStepDependency>
{
    public void Configure(EntityTypeBuilder<WorkflowStepDependency> builder)
    {
        builder.ToTable("WorkflowStepDependencies");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.CaseId)
            .IsRequired();

        builder.Property(x => x.StepId)
            .IsRequired();

        builder.Property(x => x.DependsOnStepId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(x => x.Step)
            .WithMany(x => x.Dependencies)
            .HasForeignKey(x => x.StepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DependsOnStep)
            .WithMany(x => x.Dependents)
            .HasForeignKey(x => x.DependsOnStepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CaseId, x.StepId, x.DependsOnStepId })
            .IsUnique()
            .HasDatabaseName("IX_WorkflowStepDependencies_CaseId_StepId_DependsOnStepId");

        builder.HasIndex(x => new { x.TenantId, x.CaseId })
            .HasDatabaseName("IX_WorkflowStepDependencies_TenantId_CaseId");
    }
}
