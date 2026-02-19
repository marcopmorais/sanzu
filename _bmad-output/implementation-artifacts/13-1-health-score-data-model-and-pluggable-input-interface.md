# Story 13.1: Health Score Data Model & Pluggable Input Interface

Status: done

## Story

As a platform operator,
I want a health score computed for every active tenant based on billing, case completion, and onboarding signals,
So that I can identify at-risk tenants across any Admin view.

## Acceptance Criteria

1. **Given** the database has no TenantHealthScore table
   **When** the EF migration runs
   **Then** a TenantHealthScore table is created with: Id (UNIQUEIDENTIFIER PK), TenantId (FK), OverallScore (INT 0-100), BillingScore (INT), CaseCompletionScore (INT), OnboardingScore (INT), HealthBand (NVARCHAR Green/Yellow/Red), PrimaryIssue (NVARCHAR NULL), ComputedAt (DATETIME2)
   **And** index IX_TenantHealthScore_TenantId_ComputedAt is created

2. **Given** the IHealthScoreInput interface is defined
   **When** 3 implementations are registered (BillingStatusInput, CaseCompletionInput, OnboardingCompletionInput)
   **Then** each returns a score (0-100), optional FloorCap, and optional FloorReason
   **And** they are registered in DI as IEnumerable\<IHealthScoreInput\>

3. **Given** a tenant with a failed billing payment
   **When** BillingStatusInput computes for that tenant
   **Then** it returns a low score AND a FloorCap (e.g., 30) with FloorReason "BillingFailed"
   **And** the overall score is capped at the FloorCap regardless of other inputs

4. **Given** a SanzuAdmin or SanzuOps user
   **When** they call GET /api/v1/admin/health-scores
   **Then** the response lists computed health scores for all active tenants

5. **Given** a non-admin user
   **When** they attempt to access health score endpoints
   **Then** they receive 403 Forbidden

## Tasks / Subtasks

- [ ] Task 1: Create entity, enum, and response DTO (AC: #1)
  - [ ] 1.1 Create `HealthBand.cs` enum — Green, Yellow, Red
  - [ ] 1.2 Create `TenantHealthScore.cs` entity — Id, TenantId, OverallScore, BillingScore, CaseCompletionScore, OnboardingScore, HealthBand, PrimaryIssue, ComputedAt
  - [ ] 1.3 Create `TenantHealthScoreResponse.cs` — all entity fields + TenantName for display

- [ ] Task 2: Create IHealthScoreInput interface and 3 implementations (AC: #2, #3)
  - [ ] 2.1 Create `IHealthScoreInput.cs` interface — `Task<HealthScoreInputResult> ComputeAsync(Guid tenantId, CancellationToken ct)`
  - [ ] 2.2 Create `HealthScoreInputResult.cs` — Score (int 0-100), FloorCap (int?), FloorReason (string?)
  - [ ] 2.3 Create `BillingStatusInput.cs` — scores based on Organization.FailedPaymentAttempts, SubscriptionPlan, PaymentMethodType; FloorCap=30 if FailedPaymentAttempts > 0
  - [ ] 2.4 Create `CaseCompletionInput.cs` — scores based on ratio of closed/completed cases vs total active cases
  - [ ] 2.5 Create `OnboardingCompletionInput.cs` — scores based on OnboardingCompletedAt presence, subscription activation, profile completeness

- [ ] Task 3: Create repository and EF configuration (AC: #1)
  - [ ] 3.1 Create `ITenantHealthScoreRepository.cs` — GetLatestByTenantIdAsync, GetLatestForAllTenantsAsync, CreateAsync
  - [ ] 3.2 Create `TenantHealthScoreConfiguration.cs` — table "TenantHealthScores", HealthBand as string conversion, unique index IX_TenantHealthScore_TenantId_ComputedAt
  - [ ] 3.3 Create `TenantHealthScoreRepository.cs`
  - [ ] 3.4 Add `DbSet<TenantHealthScore> TenantHealthScores` to SanzuDbContext + query filter
  - [ ] 3.5 Register repository in Infrastructure ServiceRegistration.cs

- [ ] Task 4: Create IHealthScoreComputeService and implementation (AC: #2, #3)
  - [ ] 4.1 Create `IHealthScoreComputeService.cs` — ComputeForTenantAsync, ComputeForAllTenantsAsync, GetLatestScoresAsync
  - [ ] 4.2 Create `HealthScoreComputeService.cs` — injects IEnumerable<IHealthScoreInput>, IOrganizationRepository, ITenantHealthScoreRepository, IAuditRepository, IUnitOfWork
  - [ ] 4.3 ComputeForTenantAsync: runs all IHealthScoreInput implementations, calculates weighted average, applies FloorCap logic, classifies HealthBand (>=70 Green, 40-69 Yellow, <40 Red), persists TenantHealthScore
  - [ ] 4.4 GetLatestScoresAsync: returns latest scores for all active tenants with tenant name

- [ ] Task 5: Create AdminHealthScoreController (AC: #4, #5)
  - [ ] 5.1 Create `AdminHealthScoreController.cs` at route `api/v1/admin/health-scores`
  - [ ] 5.2 GET `/` — list latest health scores (AdminOps policy: SanzuAdmin + SanzuOps)
  - [ ] 5.3 POST `/compute` — trigger compute for all tenants (AdminFull policy: SanzuAdmin only)
  - [ ] 5.4 Register service + inputs in API ServiceRegistration.cs

- [ ] Task 6: Create EF migration (AC: #1)
  - [ ] 6.1 Run `dotnet ef migrations add Story13_1_TenantHealthScore` from Infrastructure project

- [ ] Task 7: Write tests (AC: #1, #2, #3, #4, #5)
  - [ ] 7.1 Integration test: SanzuAdmin can list health scores (200)
  - [ ] 7.2 Integration test: SanzuOps can list health scores (200)
  - [ ] 7.3 Integration test: SanzuFinance gets 403 on health scores
  - [ ] 7.4 Integration test: SanzuAdmin can trigger compute (204)
  - [ ] 7.5 Integration test: Compute creates TenantHealthScore rows
  - [ ] 7.6 Integration test: BillingStatusInput applies FloorCap when FailedPaymentAttempts > 0
  - [ ] 7.7 Integration test: HealthBand classification (Green >= 70, Yellow 40-69, Red < 40)
  - [ ] 7.8 Integration test: Unauthenticated gets 401

## Dev Notes

### Critical Architecture Patterns

**FR-A5 through FR-A8:** Health scores are computed per-tenant, stored as snapshots (append-only), and classified into Green/Yellow/Red bands. The scoring system uses a pluggable input interface so new scoring dimensions can be added without changing the compute engine.

**FloorCap logic:** If any IHealthScoreInput returns a FloorCap, the overall score is capped at `min(weightedAverage, lowestFloorCap)`. The FloorReason from the lowest FloorCap becomes the PrimaryIssue.

### Existing Code to Leverage

1. **Organization entity** — `api/src/Sanzu.Core/Entities/Organization.cs`
   - Has `FailedPaymentAttempts`, `LastPaymentFailedAt`, `SubscriptionPlan`, `PaymentMethodType`
   - Has `OnboardingCompletedAt`, `SubscriptionActivatedAt`
   - Has `Status` (TenantStatus enum: Pending, Onboarding, Active, PaymentIssue, Suspended, Terminated)

2. **FleetPostureService** — `api/src/Sanzu.Core/Services/FleetPostureService.cs`
   - Already computes per-tenant metrics (ActiveCases, BlockedTasks, OpenKpiAlerts, FailedPaymentAttempts)
   - Pattern reference for cross-tenant aggregation

3. **KpiAlertService** — `api/src/Sanzu.Core/Services/KpiAlertService.cs`
   - Pattern for threshold-based evaluation and classification
   - Audit trail integration pattern

4. **IOrganizationRepository.GetAllAsync()** — returns all tenants for iteration

5. **ICaseRepository** — for case completion ratio calculations

6. **AdminPlatformController** — pattern reference for admin controller with AdminFull policy

7. **AdminPermissionsController** — shows "HealthOverview" widget is accessible to SanzuAdmin, SanzuOps, SanzuFinance, SanzuViewer; "TopAtRisk" to SanzuAdmin, SanzuOps

### IHealthScoreInput Interface Design

```csharp
public interface IHealthScoreInput
{
    string Name { get; }  // e.g., "Billing", "CaseCompletion", "Onboarding"
    int Weight { get; }   // relative weight for weighted average (e.g., 40, 35, 25)
    Task<HealthScoreInputResult> ComputeAsync(Guid tenantId, CancellationToken cancellationToken);
}

public sealed class HealthScoreInputResult
{
    public int Score { get; init; }         // 0-100
    public int? FloorCap { get; init; }     // optional cap on overall score
    public string? FloorReason { get; init; } // reason for floor cap
}
```

### Scoring Logic

**BillingStatusInput (Weight: 40):**
- FailedPaymentAttempts == 0, has active subscription → 100
- FailedPaymentAttempts == 0, no subscription yet → 50
- FailedPaymentAttempts > 0 → score = max(0, 100 - (attempts * 30)), FloorCap = 30, FloorReason = "BillingFailed"

**CaseCompletionInput (Weight: 35):**
- No cases → 50 (neutral)
- Ratio of closed/archived to total cases → score = ratio * 100
- Many blocked tasks relative to active → lower score

**OnboardingCompletionInput (Weight: 25):**
- OnboardingCompletedAt is set → 80 base
- SubscriptionActivatedAt is set → +10
- Profile fields populated (locale, timezone, currency, defaults) → +10
- OnboardingCompletedAt is null → 20

### HealthBand Classification

- OverallScore >= 70 → Green
- OverallScore 40-69 → Yellow
- OverallScore < 40 → Red

### Authorization

- GET /admin/health-scores → **AdminOps** policy (SanzuAdmin + SanzuOps)
- POST /admin/health-scores/compute → **AdminFull** policy (SanzuAdmin only)

### Audit Event

- `Admin.HealthScore.Computed` — metadata: `{ "tenantsScored": N, "green": X, "yellow": Y, "red": Z }`
- Auto-audit via AdminAuditActionFilter for GET endpoint

### DI Registration

**Infrastructure ServiceRegistration.cs:**
- `ITenantHealthScoreRepository` → `TenantHealthScoreRepository`

**API ServiceRegistration.cs:**
- `IHealthScoreComputeService` → `HealthScoreComputeService`
- `IHealthScoreInput` → `BillingStatusInput` (use `AddScoped<IHealthScoreInput, BillingStatusInput>()`)
- `IHealthScoreInput` → `CaseCompletionInput`
- `IHealthScoreInput` → `OnboardingCompletionInput`

### Previous Story Learnings (Epic 12)

- AdminAuditActionFilter handles auto-audit — use [SkipAdminAudit] for manual audit
- `AuditRepository.CreateAsync()` only adds to change tracker — wrap in `IUnitOfWork.ExecuteInTransactionAsync()` to persist
- `HeaderAuthenticationHandler` requires BOTH `X-User-Id` AND `X-Tenant-Id` headers in tests
- Organization entity has NO `Slug` — use `Location = "Test"` in seeds
- Frontend tests: use module import assertions, not SSR rendering
- No background services exist yet in codebase — Story 13.2 will add the background service

### Testing Standards

- Backend: `api/tests/Sanzu.Tests/Admin/AdminHealthScoreTests.cs`
- Seed tenants with various states (active with payments, failed payments, no onboarding, etc.)
- Verify FloorCap logic: tenant with failed billing should have overall score capped at 30 regardless of other good scores
- Verify HealthBand classification boundaries
- Use the same BuildAuthorizedRequest helper pattern from AdminTeamManagementTests

### References

- [Source: _bmad-output/planning-artifacts/epics-admin.md#Epic 13]
- [Source: _bmad-output/planning-artifacts/architecture-admin.md#Tenant Health Scoring]
- [Source: api/src/Sanzu.Core/Entities/Organization.cs]
- [Source: api/src/Sanzu.Core/Services/FleetPostureService.cs]
- [Source: api/src/Sanzu.Core/Services/KpiAlertService.cs]
- [Source: api/src/Sanzu.API/Controllers/Admin/AdminPlatformController.cs]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

- All 7 tasks completed: entity, enum, IHealthScoreInput interface + 3 implementations, repository, EF config, compute service, controller, frontend, tests
- BillingStatusInput applies FloorCap=30 when FailedPaymentAttempts > 0
- CaseCompletionInput scores based on closed/archived ratio
- OnboardingCompletionInput scores based on profile completeness
- HealthBand classification: >= 70 Green, 40-69 Yellow, < 40 Red
- Backend: 397/397 tests pass (11 new)
- Frontend: 2/2 unit tests pass

### Change Log

- `api/src/Sanzu.Core/Enums/HealthBand.cs` — NEW
- `api/src/Sanzu.Core/Entities/TenantHealthScore.cs` — NEW
- `api/src/Sanzu.Core/Models/Responses/TenantHealthScoreResponse.cs` — NEW
- `api/src/Sanzu.Core/Interfaces/IHealthScoreInput.cs` — NEW (+ HealthScoreInputResult)
- `api/src/Sanzu.Core/Services/BillingStatusInput.cs` — NEW
- `api/src/Sanzu.Core/Services/CaseCompletionInput.cs` — NEW
- `api/src/Sanzu.Core/Services/OnboardingCompletionInput.cs` — NEW
- `api/src/Sanzu.Core/Interfaces/ITenantHealthScoreRepository.cs` — NEW
- `api/src/Sanzu.Core/Interfaces/IHealthScoreComputeService.cs` — NEW
- `api/src/Sanzu.Core/Services/HealthScoreComputeService.cs` — NEW
- `api/src/Sanzu.Infrastructure/Data/EntityConfigurations/TenantHealthScoreConfiguration.cs` — NEW
- `api/src/Sanzu.Infrastructure/Repositories/TenantHealthScoreRepository.cs` — NEW
- `api/src/Sanzu.Infrastructure/Data/SanzuDbContext.cs` — MODIFIED: added DbSet + query filter
- `api/src/Sanzu.Infrastructure/DependencyInjection/ServiceRegistration.cs` — MODIFIED: added repo DI
- `api/src/Sanzu.API/Controllers/Admin/AdminHealthScoreController.cs` — NEW
- `api/src/Sanzu.API/Configuration/ServiceRegistration.cs` — MODIFIED: added service + inputs DI
- `api/src/Sanzu.Web/lib/api-client/generated/admin.ts` — MODIFIED: added health score functions
- `api/src/Sanzu.Web/app/app/admin/health/page.tsx` — NEW
- `api/tests/Sanzu.Tests/Admin/AdminHealthScoreTests.cs` — NEW: 11 integration tests
- `api/src/Sanzu.Web/tests/unit/story-13-1-health-score.test.tsx` — NEW: 2 frontend tests
- EF Migration: Story13_1_TenantHealthScore
