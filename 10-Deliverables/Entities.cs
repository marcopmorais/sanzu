// Sanzu.Core/Entities/Case.cs
using System.ComponentModel.DataAnnotations;

namespace Sanzu.Core.Entities;

public class Case
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid OrgId { get; set; }
    
    [Required, MaxLength(255)]
    public string DeceasedFullName { get; set; } = string.Empty;
    
    [Required]
    public DateTime DateOfDeath { get; set; }
    
    [MaxLength(100)]
    public string? Municipality { get; set; }
    
    [MaxLength(50)]
    public string Status { get; set; } = "Draft"; // Draft/Active/Closing/Archived
    
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User? Creator { get; set; }
    public ICollection<CaseParticipant> Participants { get; set; } = new List<CaseParticipant>();
    public ICollection<WorkflowStepInstance> Steps { get; set; } = new List<WorkflowStepInstance>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}

// Sanzu.Core/Entities/CaseParticipant.cs
public class CaseParticipant
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid CaseId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required, MaxLength(50)]
    public string Role { get; set; } = string.Empty; // Manager/Editor/Reader
    
    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    
    // Navigation
    public Case Case { get; set; } = null!;
    public User User { get; set; } = null!;
}

// Sanzu.Core/Entities/WorkflowStepInstance.cs
public class WorkflowStepInstance
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid CaseId { get; set; }
    
    [Required, MaxLength(100)]
    public string StepKey { get; set; } = string.Empty;
    
    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string OwnerType { get; set; } = "Agency"; // Agency/Family
    
    [MaxLength(50)]
    public string Status { get; set; } = "NotStarted";
    
    [MaxLength(50)]
    public string Criticality { get; set; } = "optional";
    
    public string? Prerequisites { get; set; } // JSON array
    
    public Guid? CompletedBy { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Case Case { get; set; } = null!;
    public User? Completer { get; set; }
}

// Sanzu.Infrastructure/Data/SanzuDbContext.cs
using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data;

public class SanzuDbContext : DbContext
{
    public SanzuDbContext(DbContextOptions<SanzuDbContext> options) : base(options) { }
    
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<CaseParticipant> CaseParticipants => Set<CaseParticipant>();
    public DbSet<WorkflowStepInstance> WorkflowStepInstances => Set<WorkflowStepInstance>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Unique constraint
        modelBuilder.Entity<CaseParticipant>()
            .HasIndex(cp => new { cp.CaseId, cp.UserId })
            .IsUnique();
        
        // Indexes for performance
        modelBuilder.Entity<Case>()
            .HasIndex(c => new { c.OrgId, c.Status, c.UpdatedAt });
        
        modelBuilder.Entity<WorkflowStepInstance>()
            .HasIndex(w => new { w.CaseId, w.Status });
        
        modelBuilder.Entity<AuditEvent>()
            .HasIndex(a => new { a.CaseId, a.CreatedAt });
        
        // Audit trigger (via interceptor)
        modelBuilder.Entity<Case>()
            .HasQueryFilter(c => c.Status != "Deleted");
    }
}
