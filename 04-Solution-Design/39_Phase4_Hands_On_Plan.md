# Phase 4: Hands-On Implementation Plan

## Day 1-2: Database Spike

### Setup
```bash
# Create Azure SQL Database (Basic tier)
az sql server create --name sanzu-dev-sql --resource-group rg-sanzu-dev
az sql db create --name SanzuDb --server sanzu-dev-sql --service-objective Basic
```

### EF Core Models
- Create solution: `dotnet new webapi -n Sanzu.API`
- Add EF Core packages
- Implement entities from Entities.cs
- Create initial migration
- Test CRUD operations

**Validation:** Insert 10 test cases, verify queries <100ms

---

## Day 3-4: Rules Engine

### Implementation
```csharp
// Sanzu.Core/Services/RulesEngine.cs
public List<WorkflowStepInstance> GeneratePlan(QuestionnaireResponse q)
{
    // Implement step generation logic
    // Test 20 scenarios
}
```

### Test Cases
1. All workstreams (banks, insurance, benefits, employer, services)
2. Banks only
3. No optional workstreams
4. Complex prerequisites
5. Status transitions

**Validation:** All tests green, deterministic output

---

## Day 5-6: PDF Spike

### QuestPDF Template
```csharp
// Sanzu.Core/Services/PdfService.cs
public byte[] GenerateBankLetter(CaseDto data)
{
    return Document.Create(container => {
        // Letterhead + content
    }).GeneratePdf();
}
```

### Test
- Generate 3 templates
- Verify PT-PT characters
- Check file size (<500KB)
- Measure time (<5 sec)

**Validation:** Professional output, fast generation

---

## Day 7: Azure Blob

### Upload Flow
```csharp
// Generate SAS token
var sasUri = blobClient.GenerateSasUri(permissions, expiresOn);

// Client uploads directly to blob
// API records metadata in DB
```

**Validation:** 50MB file uploads successfully

---

## Day 8-10: Blazor Prototype

### Pages
- `/cases` - List with filters
- `/cases/create` - Quick form
- `/cases/{id}` - Dashboard
- `/cases/{id}/questionnaire` - 5 questions
- `/cases/{id}/steps` - Checklist

### Components
- ProgressBar
- StepCard
- FileUpload
- NextStepIndicator

**Validation:** Complete flow works end-to-end

---

## Day 11-12: Auth Integration

### Azure AD B2C
- Configure tenant
- Add sign-in/sign-up policies
- Implement in Blazor
- Test role enforcement

**Validation:** RBAC works for 3 roles

---

## Day 13-14: Testing & Decision

### Manual Testing
- 3 complete case flows
- Mobile responsiveness
- Error handling

### Go/No-Go
**Proceed if:**
- Technical feasibility confirmed
- Prototype completable
- No major blockers

**Decision:** Move to full 4-week build or iterate

