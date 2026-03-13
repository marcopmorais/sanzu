# Sanzu - .NET Azure Architecture

## Tech Stack

**Backend:** ASP.NET Core 8 Web API
**Database:** Azure SQL Database
**Storage:** Azure Blob Storage
**Auth:** Azure AD B2C (or Identity)
**PDF:** QuestPDF or iText7
**Hosting:** Azure App Service
**Cache:** Azure Redis Cache

## Project Structure

```
Sanzu/
├── Sanzu.API/              # ASP.NET Core Web API
├── Sanzu.Core/             # Business logic
│   ├── Entities/           # Domain models
│   ├── Services/           # Rules engine, PDF generator
│   └── Interfaces/
├── Sanzu.Infrastructure/   # Data access
│   ├── Data/               # EF Core DbContext
│   ├── Repositories/
│   └── Migrations/
├── Sanzu.Web/              # Blazor or Next.js frontend
└── Sanzu.Tests/            # Unit + integration tests
```

## Database Schema (SQL Server)

```sql
-- Organizations
CREATE TABLE Organizations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL,
    Location NVARCHAR(100),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Users
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Email NVARCHAR(255) UNIQUE NOT NULL,
    FullName NVARCHAR(255),
    OrgId UNIQUEIDENTIFIER REFERENCES Organizations(Id),
    AzureAdObjectId NVARCHAR(100),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Cases
CREATE TABLE Cases (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OrgId UNIQUEIDENTIFIER NOT NULL REFERENCES Organizations(Id),
    DeceasedFullName NVARCHAR(255) NOT NULL,
    DateOfDeath DATE NOT NULL,
    Municipality NVARCHAR(100),
    Status NVARCHAR(50) DEFAULT 'Draft',
    CreatedBy UNIQUEIDENTIFIER REFERENCES Users(Id),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    INDEX IX_Cases_OrgId_Status (OrgId, Status, UpdatedAt)
);

-- CaseParticipants (RBAC)
CREATE TABLE CaseParticipants (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CaseId UNIQUEIDENTIFIER NOT NULL REFERENCES Cases(Id) ON DELETE CASCADE,
    UserId UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    Role NVARCHAR(50) CHECK (Role IN ('Manager', 'Editor', 'Reader')),
    InvitedAt DATETIME2 DEFAULT GETUTCDATE(),
    AcceptedAt DATETIME2,
    UNIQUE (CaseId, UserId)
);

-- WorkflowStepInstances
CREATE TABLE WorkflowStepInstances (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CaseId UNIQUEIDENTIFIER NOT NULL REFERENCES Cases(Id) ON DELETE CASCADE,
    StepKey NVARCHAR(100) NOT NULL,
    Title NVARCHAR(255) NOT NULL,
    OwnerType NVARCHAR(50) CHECK (OwnerType IN ('Agency', 'Family')),
    Status NVARCHAR(50) DEFAULT 'NotStarted',
    Criticality NVARCHAR(50) CHECK (Criticality IN ('mandatory', 'optional')),
    Prerequisites NVARCHAR(MAX), -- JSON array
    CompletedBy UNIQUEIDENTIFIER REFERENCES Users(Id),
    CompletedAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    INDEX IX_WSI_CaseId_Status (CaseId, Status)
);

-- Documents
CREATE TABLE Documents (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CaseId UNIQUEIDENTIFIER NOT NULL REFERENCES Cases(Id) ON DELETE CASCADE,
    DocType NVARCHAR(100) NOT NULL,
    Sensitivity NVARCHAR(50) DEFAULT 'normal',
    BlobPath NVARCHAR(500), -- Azure Blob Storage path
    LatestVersionId UNIQUEIDENTIFIER,
    CreatedBy UNIQUEIDENTIFIER REFERENCES Users(Id),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    DeletedAt DATETIME2,
    INDEX IX_Documents_CaseId (CaseId) WHERE DeletedAt IS NULL
);

-- AuditEvents
CREATE TABLE AuditEvents (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CaseId UNIQUEIDENTIFIER REFERENCES Cases(Id),
    ActorUserId UNIQUEIDENTIFIER REFERENCES Users(Id),
    EventType NVARCHAR(100) NOT NULL,
    Metadata NVARCHAR(MAX), -- JSON
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    INDEX IX_Audit_CaseId_CreatedAt (CaseId, CreatedAt)
);
```

## Core Services

### RulesEngine.cs
```csharp
public class RulesEngine
{
    public List<WorkflowStepInstance> GeneratePlan(
        Guid caseId, 
        QuestionnaireResponse response)
    {
        var steps = new List<WorkflowStepInstance>();
        
        // Core mandatory steps
        steps.AddRange(GenerateCoreSteps(caseId));
        
        // Conditional workstreams
        if (response.HasBanks ?? true)
            steps.AddRange(GenerateBankSteps(caseId));
            
        if (response.HasInsurance ?? true)
            steps.AddRange(GenerateInsuranceSteps(caseId));
        
        // Set initial status based on prerequisites
        UpdateStepStatuses(steps, new List<string>());
        
        return steps;
    }
}
```

### PdfService.cs (QuestPDF)
```csharp
public class PdfService
{
    public byte[] GenerateBankNotificationLetter(CaseDto caseData)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                
                page.Header().Text("Notification Letter")
                    .FontSize(20).SemiBold();
                
                page.Content().Column(column =>
                {
                    column.Item().Text($"Exmo(a). Senhor(a),");
                    column.Item().PaddingTop(10)
                        .Text($"Falecimento de {caseData.DeceasedName}...");
                });
            });
        }).GeneratePdf();
    }
}
```

## Azure Services Config

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:sanzu.database.windows.net;Database=SanzuDb;"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "{tenant-id}",
    "ClientId": "{client-id}"
  },
  "BlobStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;...",
    "ContainerName": "documents"
  },
  "Redis": {
    "ConnectionString": "{redis-cache}.redis.cache.windows.net:6380,password=..."
  }
}
```

## Deployment

**Azure Resources:**
- Resource Group: `rg-sanzu-prod`
- App Service Plan: `asp-sanzu` (B1 or S1)
- Web App: `app-sanzu-api`
- SQL Database: `sqldb-sanzu` (Basic or S0)
- Storage Account: `stsanzu{random}`
- Redis Cache: `redis-sanzu` (C0 basic)

**Estimated Cost:** €50-100/month for pilot

## Build Timeline with .NET Skills

**Week 1:** EF Core models + migrations, API endpoints  
**Week 2:** Rules engine, PDF service, Azure deployment  
**Week 3:** Frontend (Blazor or React), auth integration  
**Week 4:** QA, security review, pilot launch

**Total: 4 weeks**

