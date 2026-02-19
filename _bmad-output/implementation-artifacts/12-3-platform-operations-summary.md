# Story 12.3: Platform Operations Summary

Status: done

## Story

As a SanzuAdmin,
I want to view a platform operations summary showing total tenants, cases, workflow steps, and documents,
So that I have at-a-glance visibility into platform-wide activity.

## Acceptance Criteria

1. **Given** a SanzuAdmin is authenticated
   **When** they call GET /api/v1/admin/platform/summary
   **Then** the response contains: total active tenants, total active cases, total workflow steps by status (completed/active/blocked), and total documents stored
   **And** the data is computed using IgnoreQueryFilters() for cross-tenant aggregation

2. **Given** a SanzuAdmin navigates to the platform operations section
   **When** the page loads
   **Then** the summary metrics are displayed with clear labels and counts

3. **Given** a non-SanzuAdmin role (SanzuOps, SanzuFinance, SanzuSupport, SanzuViewer)
   **When** they call GET /api/v1/admin/platform/summary
   **Then** they receive 403 Forbidden (AdminFull policy)

## Tasks / Subtasks

- [ ] Task 1: Create PlatformOperationsSummaryResponse DTO (AC: #1)
  - [ ] 1.1 Create `Sanzu.Core/Models/Responses/PlatformOperationsSummaryResponse.cs`
  - [ ] 1.2 Fields: TotalActiveTenants, TotalActiveCases, WorkflowStepsCompleted, WorkflowStepsActive, WorkflowStepsBlocked, TotalDocuments

- [ ] Task 2: Create IPlatformSummaryService and implementation (AC: #1)
  - [ ] 2.1 Create `Sanzu.Core/Interfaces/IPlatformSummaryService.cs`
  - [ ] 2.2 Create `Sanzu.Core/Services/PlatformSummaryService.cs`
  - [ ] 2.3 Query Organizations (Status == Active), Cases (non-archived), WorkflowStepInstances by status, CaseDocuments
  - [ ] 2.4 Use IgnoreQueryFilters() for all cross-tenant queries
  - [ ] 2.5 Register service in DI

- [ ] Task 3: Create AdminPlatformController (AC: #1, #3)
  - [ ] 3.1 Create `Sanzu.API/Controllers/Admin/AdminPlatformController.cs`
  - [ ] 3.2 Route: `api/v1/admin/platform`
  - [ ] 3.3 GET `/summary` with `[Authorize(Policy = "AdminFull")]`
  - [ ] 3.4 Return `ApiEnvelope<PlatformOperationsSummaryResponse>`
  - [ ] 3.5 AdminAuditActionFilter handles audit logging automatically

- [ ] Task 4: Create frontend API client and page (AC: #2)
  - [ ] 4.1 Add `getPlatformSummary()` to `lib/api-client/generated/admin.ts`
  - [ ] 4.2 Create `app/app/admin/platform/page.tsx` — displays summary metrics

- [ ] Task 5: Write tests (AC: #1, #2, #3)
  - [ ] 5.1 Integration test: SanzuAdmin gets 200 with correct summary shape
  - [ ] 5.2 Integration test: Non-SanzuAdmin roles get 403
  - [ ] 5.3 Integration test: Summary counts match seeded data
  - [ ] 5.4 Integration test: Audit event logged for summary access
  - [ ] 5.5 Frontend unit test: page and API client module exports

## Dev Notes

### Critical Architecture Patterns

**FR-A35 is the sole requirement:** "SanzuAdmin can view a platform operations summary: total active tenants, total active cases, total workflow steps by status (completed/active/blocked), total documents stored."

**Policy:** AdminFull (SanzuAdmin only) — this is an admin-exclusive overview, NOT visible to other roles.

### Existing Code to Leverage (NOT Recreate)

1. **KpiDashboardService** — `api/src/Sanzu.Core/Services/KpiDashboardService.cs`
   - Already computes `TenantsTotal`, `TenantsActive`, `CasesCreated`, `ActiveCases`, `DocumentsUploaded`
   - Uses `IgnoreQueryFilters()` pattern for cross-tenant queries
   - **Study this service for the exact query patterns and repository usage**
   - DO NOT duplicate — create a new focused service that queries only what 12.3 needs

2. **FleetPostureService** — `api/src/Sanzu.Core/Services/FleetPostureService.cs`
   - Computes `TotalTenants`, `ActiveTenants`, `OnboardingTenants`, etc.
   - Another reference for cross-tenant aggregation patterns

3. **AdminPermissionsController** — `api/src/Sanzu.API/Controllers/Admin/AdminPermissionsController.cs`
   - Pattern reference for admin controller: `ApiEnvelope<T>`, `TryGetActorUserId()`, policy attributes
   - AdminAuditActionFilter handles audit — NO manual audit logging needed

4. **AdminAuditActionFilter** — `api/src/Sanzu.API/Filters/AdminAuditActionFilter.cs`
   - Globally registered — will auto-derive EventType as `Admin.Platform.GetSummary`
   - No [SkipAdminAudit] needed on this endpoint

### Existing Entities for Aggregation

All entities are in `Sanzu.Core/Entities/`:

| Entity | Key Fields for Aggregation | Repository |
|--------|---------------------------|------------|
| `Organization` | `Status` (TenantStatus enum: Pending, Active, Onboarding, PaymentIssue, Suspended) | `IOrganizationRepository` |
| `Case` | `Status` (CaseStatus enum) | `ICaseRepository` |
| `WorkflowStepInstance` | `Status` (WorkflowStepStatus enum — check for Completed, Active/InProgress, Blocked values) | Check if repository exists or query DbContext directly |
| `CaseDocument` | Count all | Check if repository exists or query DbContext directly |

### WorkflowStepInstance Status Values

**CRITICAL: Verify the actual enum values before implementing!**

Check `api/src/Sanzu.Core/Enums/` for WorkflowStepStatus enum. The AC says "completed/active/blocked" but the actual enum values may differ. Common patterns in the codebase:
- Look at `WorkflowStepStatus.cs` or similar
- The service must group by actual enum values, not assumed ones

### New Files to Create

1. **Response DTO** — `api/src/Sanzu.Core/Models/Responses/PlatformOperationsSummaryResponse.cs`
   ```csharp
   public sealed record PlatformOperationsSummaryResponse(
       int TotalActiveTenants,
       int TotalActiveCases,
       int WorkflowStepsCompleted,
       int WorkflowStepsActive,
       int WorkflowStepsBlocked,
       int TotalDocuments
   );
   ```

2. **Service Interface** — `api/src/Sanzu.Core/Interfaces/IPlatformSummaryService.cs`
   ```csharp
   public interface IPlatformSummaryService
   {
       Task<PlatformOperationsSummaryResponse> GetSummaryAsync(CancellationToken ct);
   }
   ```

3. **Service Implementation** — `api/src/Sanzu.Core/Services/PlatformSummaryService.cs`
   - Inject DbContext directly (like KpiDashboardService does for complex aggregations)
   - Use `IgnoreQueryFilters()` on every query
   - Count Organizations where Status == Active
   - Count Cases where lifecycle is active (not Closed/Archived)
   - Group WorkflowStepInstances by status
   - Count CaseDocuments

4. **Controller** — `api/src/Sanzu.API/Controllers/Admin/AdminPlatformController.cs`
   ```csharp
   [ApiController]
   [Route("api/v1/admin/platform")]
   [Authorize(Policy = "AdminFull")]
   public sealed class AdminPlatformController : ControllerBase
   {
       [HttpGet("summary")]
       public async Task<IActionResult> GetSummary(CancellationToken ct)
       {
           var summary = await _service.GetSummaryAsync(ct);
           return Ok(ApiEnvelope<PlatformOperationsSummaryResponse>.Ok(summary));
       }
   }
   ```

5. **Frontend API client addition** — Edit `api/src/Sanzu.Web/lib/api-client/generated/admin.ts`
   ```typescript
   export interface PlatformOperationsSummaryResponse {
     totalActiveTenants: number;
     totalActiveCases: number;
     workflowStepsCompleted: number;
     workflowStepsActive: number;
     workflowStepsBlocked: number;
     totalDocuments: number;
   }

   export async function getPlatformSummary(): Promise<PlatformOperationsSummaryResponse> { ... }
   ```

6. **Frontend page** — `api/src/Sanzu.Web/app/app/admin/platform/page.tsx`
   - Server component (no "use client" needed if just displaying)
   - Or client component calling `getPlatformSummary()` on mount
   - Display 6 metric cards using existing `panel` and `grid` CSS classes
   - Pattern: follow `platform-governance/page.tsx` layout structure

7. **Tests** — `api/tests/Sanzu.Tests/Admin/AdminPlatformSummaryTests.cs`
   - Follow `AdminRbacTests.cs` patterns exactly
   - Use `CustomWebApplicationFactory`, `SeedUserWithRoleAsync`, `BuildAuthorizedRequest`

### Admin Navigation Tab Mapping

The admin layout (Story 12.2) already maps `/admin/platform/*` to the "Platform" tab. Creating a page at `/app/admin/platform/page.tsx` will automatically appear under this tab for SanzuAdmin users.

### Previous Story (12.2) Learnings

- AdminAuditActionFilter handles all audit logging — NO manual audit code in controllers
- `HeaderAuthenticationHandler` requires BOTH `X-User-Id` AND `X-Tenant-Id` headers in tests
- Organization entity has NO `Slug` property — use `Location = "Test"` in test seeds
- Frontend tests: use module import assertions, not SSR rendering (Next.js router limitation)
- Event type derived from controller+action: `AdminPlatformController.GetSummary` → `Admin.Platform.GetSummary`

### DI Registration

- Register `IPlatformSummaryService` → `PlatformSummaryService` in `api/src/Sanzu.API/Configuration/ServiceRegistration.cs`
- Follow existing pattern: `services.AddScoped<IPlatformSummaryService, PlatformSummaryService>();`

### Testing Standards

- Backend test file: `api/tests/Sanzu.Tests/Admin/AdminPlatformSummaryTests.cs`
- Use WebApplicationFactory pattern from existing `AdminRbacTests.cs`
- Test naming: `{MethodName}_Should_{ExpectedBehavior}_When_{Condition}`
- Seed test data: create organizations, cases, workflow steps, documents — then verify counts match
- Test all 5 admin roles: only SanzuAdmin should get 200, others get 403
- Frontend test: `api/src/Sanzu.Web/tests/unit/story-12-3-platform-summary.test.tsx`

### References

- [Source: _bmad-output/planning-artifacts/epics-admin.md#Story 12.3]
- [Source: _bmad-output/planning-artifacts/architecture-admin.md#Admin Controller Pattern]
- [Source: _bmad-output/planning-artifacts/architecture-admin.md#Authentication & Security]
- [Source: api/src/Sanzu.Core/Services/KpiDashboardService.cs — cross-tenant aggregation pattern]
- [Source: api/src/Sanzu.Core/Services/FleetPostureService.cs — fleet aggregation pattern]
- [Source: api/src/Sanzu.API/Controllers/Admin/AdminPermissionsController.cs — admin controller pattern]
- [Source: api/src/Sanzu.API/Filters/AdminAuditActionFilter.cs — auto audit logging]
- [Source: api/src/Sanzu.Infrastructure/Data/SanzuDbContext.cs — available DbSets]
- [Source: _bmad-output/implementation-artifacts/12-2-admin-layout-and-audit-foundation.md — previous story learnings]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

- All 5 tasks completed: DTO, service, controller, frontend, tests
- PlatformSummaryService follows KpiDashboardService pattern: repos + iterate tenants → cases
- WorkflowStepStatus grouping: Complete → completed, InProgress/Ready/AwaitingEvidence → active, Blocked/Overdue → blocked
- AdminFull policy (SanzuAdmin only) correctly enforces 403 for all other roles
- AdminAuditActionFilter auto-derives EventType as Admin.Platform.GetSummary
- Backend: 375/375 tests pass (9 new)
- Frontend: 2/2 unit tests pass

### Change Log

- `api/src/Sanzu.Core/Models/Responses/PlatformOperationsSummaryResponse.cs` — NEW
- `api/src/Sanzu.Core/Interfaces/IPlatformSummaryService.cs` — NEW
- `api/src/Sanzu.Core/Services/PlatformSummaryService.cs` — NEW
- `api/src/Sanzu.API/Controllers/Admin/AdminPlatformController.cs` — NEW
- `api/src/Sanzu.API/Configuration/ServiceRegistration.cs` — MODIFIED: added PlatformSummaryService DI
- `api/src/Sanzu.Web/lib/api-client/generated/admin.ts` — MODIFIED: added getPlatformSummary()
- `api/src/Sanzu.Web/app/app/admin/platform/page.tsx` — NEW
- `api/tests/Sanzu.Tests/Admin/AdminPlatformSummaryTests.cs` — NEW: 9 integration tests
- `api/src/Sanzu.Web/tests/unit/story-12-3-platform-summary.test.tsx` — NEW: 2 frontend tests
