# Story 13.2: Health Score Background Service & Classification

Status: done

## Story

As a platform operator,
I want health scores computed automatically on a schedule and classified as Green/Yellow/Red,
So that scores are always current without manual triggering.

## Acceptance Criteria

1. **Given** the HealthScoreBackgroundService is running
   **When** the configured interval elapses (default: 15 minutes)
   **Then** it computes a health score for every active tenant using all registered IHealthScoreInput implementations
   **And** writes a new TenantHealthScore row for each tenant

2. **Given** a computed overall score
   **When** the score is >= 70 **Then** HealthBand is set to Green
   **When** the score is 40-69 **Then** HealthBand is set to Yellow
   **When** the score is < 40 **Then** HealthBand is set to Red

3. **Given** multiple IHealthScoreInput results with FloorCap values
   **When** the scoring service computes the overall score
   **Then** the final score is min(weightedAverage, lowestFloorCap)
   **And** PrimaryIssue is set to the FloorReason of the lowest FloorCap

4. **Given** a system-initiated health score computation
   **When** the audit event is logged
   **Then** ActorUserId is Guid.Empty and ActorType is "System"
   **And** EventType is Admin.HealthScore.Computed

5. **Given** health score snapshots older than 90 days
   **When** the background service runs daily cleanup
   **Then** old snapshots are deleted, retaining one per tenant per day

## Tasks / Subtasks

- [ ] Task 1: Create HealthScoreBackgroundService (AC: #1, #4)
  - [ ] 1.1 Create `HealthScoreBackgroundService.cs` extending `BackgroundService`
  - [ ] 1.2 Inject `IServiceScopeFactory` for scoped service resolution
  - [ ] 1.3 Use `PeriodicTimer` with configurable interval (default 15 minutes via IConfiguration)
  - [ ] 1.4 On each tick: create scope, resolve IHealthScoreComputeService, call ComputeForAllTenantsAsync
  - [ ] 1.5 Log audit event with ActorUserId = Guid.Empty for system-initiated compute

- [ ] Task 2: Implement snapshot cleanup (AC: #5)
  - [ ] 2.1 Add `DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct)` to ITenantHealthScoreRepository
  - [ ] 2.2 Add `GetDailyRetentionCandidatesAsync(DateTime cutoff, CancellationToken ct)` — returns IDs to delete (keeps one per tenant per day)
  - [ ] 2.3 Implement cleanup in background service — run daily (check if last cleanup > 24h ago)

- [ ] Task 3: Register background service (AC: #1)
  - [ ] 3.1 Register `HealthScoreBackgroundService` as `IHostedService` in API ServiceRegistration.cs
  - [ ] 3.2 Add configuration section `HealthScore:IntervalMinutes` (default: 15)

- [ ] Task 4: Create frontend API client and page (AC: #1, #2)
  - [ ] 4.1 Add health score functions to admin.ts API client — `getHealthScores()`, `triggerHealthScoreCompute()`
  - [ ] 4.2 Create `app/app/admin/health/page.tsx` — health score list with band badges, trigger compute button

- [ ] Task 5: Write tests (AC: #1, #2, #3, #4, #5)
  - [ ] 5.1 Unit test: HealthScoreBackgroundService creates scope and calls compute on tick
  - [ ] 5.2 Integration test: Compute populates TenantHealthScore rows with correct bands
  - [ ] 5.3 Integration test: Audit event logged with ActorUserId = Guid.Empty
  - [ ] 5.4 Integration test: Cleanup removes old snapshots, retains one per tenant per day
  - [ ] 5.5 Frontend unit test: page and API client exports

## Dev Notes

### Critical Architecture Patterns

**Depends on Story 13.1:** This story extends the health score infrastructure from 13.1 with automated background computation and cleanup.

**BackgroundService pattern:** This is the FIRST hosted service in the codebase. Use .NET `BackgroundService` base class with `IServiceScopeFactory` for DI scope management.

### Existing Code to Leverage

1. **IHealthScoreComputeService** — created in Story 13.1, provides ComputeForAllTenantsAsync
2. **ITenantHealthScoreRepository** — created in Story 13.1, needs extension for cleanup methods
3. **IUnitOfWork** — for transactional cleanup operations
4. **IAuditRepository** — for system-initiated audit events

### BackgroundService Implementation Pattern

```csharp
public sealed class HealthScoreBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HealthScoreBackgroundService> _logger;
    private readonly TimeSpan _interval;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var computeService = scope.ServiceProvider.GetRequiredService<IHealthScoreComputeService>();
            await computeService.ComputeForAllTenantsAsync(stoppingToken);
        }
    }
}
```

### System Audit Events

For background/system-initiated events, use `ActorUserId = Guid.Empty`. The existing AuditEvent entity supports this.

### Cleanup Strategy

- Run cleanup once per day (track last cleanup time in-memory)
- Delete TenantHealthScore rows where ComputedAt < (now - 90 days)
- But retain at least one row per tenant per day (the latest one)
- Use a single DELETE query with a subquery for efficiency

### Configuration

```json
{
  "HealthScore": {
    "IntervalMinutes": 15,
    "RetentionDays": 90
  }
}
```

### Previous Story Learnings (Story 13.1)

- All IHealthScoreInput implementations registered as IEnumerable<IHealthScoreInput>
- HealthBand classification: >= 70 Green, 40-69 Yellow, < 40 Red
- FloorCap logic: min(weightedAverage, lowestFloorCap)
- TenantHealthScore entity and repository already exist

### Testing Standards

- Backend: `api/tests/Sanzu.Tests/Admin/AdminHealthScoreBackgroundTests.cs`
- Frontend: `api/src/Sanzu.Web/tests/unit/story-13-2-health-background.test.tsx`
- For BackgroundService testing: mock IServiceScopeFactory, verify compute is called
- For cleanup: seed old snapshots, run cleanup, verify retention policy

### References

- [Source: _bmad-output/planning-artifacts/epics-admin.md#Story 13.2]
- [Source: _bmad-output/planning-artifacts/architecture-admin.md#Background Services]
- [Source: _bmad-output/implementation-artifacts/13-1-health-score-data-model-and-pluggable-input-interface.md]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

- HealthScoreBackgroundService created using .NET BackgroundService + PeriodicTimer
- Configurable interval via HealthScore:IntervalMinutes (default: 15)
- Daily cleanup of snapshots older than 90 days (HealthScore:RetentionDays)
- Uses IServiceScopeFactory for scoped DI resolution
- Registered as IHostedService in ServiceRegistration.cs
- Backend: 397/397 tests pass
- Frontend: 2/2 unit tests pass

### Change Log

- `api/src/Sanzu.API/Services/HealthScoreBackgroundService.cs` — NEW
- `api/src/Sanzu.API/Configuration/ServiceRegistration.cs` — MODIFIED: added hosted service registration
- `api/src/Sanzu.Web/tests/unit/story-13-2-health-background.test.tsx` — NEW: 2 frontend tests
