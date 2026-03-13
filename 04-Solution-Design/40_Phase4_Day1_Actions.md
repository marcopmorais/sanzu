# Phase 4 - Day 1 Actions (TODAY)

## Morning: Azure Setup

### 1. Create Resource Group
```bash
az group create --name rg-sanzu-dev --location westeurope
```

### 2. Create SQL Server + Database
```bash
az sql server create \
  --name sanzu-dev-sql \
  --resource-group rg-sanzu-dev \
  --location westeurope \
  --admin-user sanzuadmin \
  --admin-password [secure-password]

az sql db create \
  --name SanzuDb \
  --server sanzu-dev-sql \
  --resource-group rg-sanzu-dev \
  --service-objective Basic
```

**Cost: €4/month**

### 3. Configure Firewall
```bash
az sql server firewall-rule create \
  --server sanzu-dev-sql \
  --resource-group rg-sanzu-dev \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

---

## Afternoon: Code Setup

### 1. Create Solution
```bash
dotnet new sln -n Sanzu
dotnet new webapi -n Sanzu.API
dotnet new classlib -n Sanzu.Core
dotnet new classlib -n Sanzu.Infrastructure
dotnet new xunit -n Sanzu.Tests

dotnet sln add **/*.csproj
```

### 2. Add Packages
```bash
cd Sanzu.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design

cd ../Sanzu.API
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

### 3. Copy Entities.cs
- Copy entities from `/mnt/user-data/outputs/Entities.cs`
- Place in `Sanzu.Core/Entities/`

### 4. Create DbContext
- Implement `SanzuDbContext` from entities file
- Configure connection string

### 5. Create Migration
```bash
dotnet ef migrations add InitialCreate --project Sanzu.Infrastructure --startup-project Sanzu.API
dotnet ef database update --project Sanzu.Infrastructure --startup-project Sanzu.API
```

---

## Evening: Validation

### Test Queries
```csharp
// Create test case
var org = new Organization { Name = "Test Agency" };
context.Organizations.Add(org);

var case = new Case { 
    OrgId = org.Id,
    DeceasedFullName = "João Silva",
    DateOfDeath = DateTime.Today
};
context.Cases.Add(case);
context.SaveChanges();

// Query
var cases = context.Cases
    .Include(c => c.Organization)
    .Where(c => c.OrgId == org.Id)
    .ToList();
```

**Success:** Insert + query working, <100ms

---

## Deliverable
✅ Azure SQL Database running  
✅ EF Core migrations applied  
✅ Test data created  
✅ Basic CRUD working

**Next: Day 2 - Rules Engine implementation**

