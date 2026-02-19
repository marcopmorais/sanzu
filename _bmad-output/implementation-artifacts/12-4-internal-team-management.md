# Story 12.4: Internal Team Management

Status: done

## Story

As a SanzuAdmin,
I want to view all internal team members with their roles and grant/revoke admin roles,
So that I can manage team access as the company grows.

## Acceptance Criteria

1. **Given** a SanzuAdmin is authenticated
   **When** they call GET /api/v1/admin/team
   **Then** the response lists all Sanzu internal team members with their admin role and last active timestamp

2. **Given** a SanzuAdmin
   **When** they grant a SanzuSupport role to a team member via POST /api/v1/admin/team/{userId}/roles
   **Then** the role is assigned in the UserRoles table with TenantId = NULL
   **And** an audit event Admin.Team.RoleGranted is logged with the target user and role in metadata

3. **Given** a SanzuAdmin
   **When** they revoke an admin role from a team member via DELETE /api/v1/admin/team/{userId}/roles/{role}
   **Then** the role is removed from UserRoles
   **And** an audit event Admin.Team.RoleRevoked is logged

4. **Given** a non-SanzuAdmin user
   **When** they attempt to grant or revoke roles
   **Then** they receive 403 Forbidden

5. **Given** a SanzuAdmin navigates to the team management page
   **When** the page loads
   **Then** the team list is displayed with role badges and last-active timestamps
   **And** grant/revoke role actions are available inline

## Tasks / Subtasks

- [ ] Task 1: Create request/response DTOs (AC: #1, #2, #3)
  - [ ] 1.1 Create `GrantAdminRoleRequest.cs` — Role (string, required)
  - [ ] 1.2 Create `AdminTeamMemberResponse.cs` — UserId, Email, FullName, Role, LastActiveAt
  - [ ] 1.3 Create `GrantAdminRoleRequestValidator.cs` — Role must be valid admin role (not AgencyAdmin, not SanzuAdmin)

- [ ] Task 2: Create IAdminTeamService and implementation (AC: #1, #2, #3)
  - [ ] 2.1 Create `IAdminTeamService.cs` interface
  - [ ] 2.2 Create `AdminTeamService.cs`
  - [ ] 2.3 ListTeamMembers: query UserRoles where TenantId == NULL and RoleType is internal admin role
  - [ ] 2.4 GrantRole: create UserRole with TenantId = NULL, log audit event
  - [ ] 2.5 RevokeRole: remove UserRole, log audit event

- [ ] Task 3: Create AdminTeamController (AC: #1, #2, #3, #4)
  - [ ] 3.1 Create `AdminTeamController.cs`
  - [ ] 3.2 Route: `api/v1/admin/team`
  - [ ] 3.3 GET `/` — list team members (AdminFull policy)
  - [ ] 3.4 POST `/{userId:guid}/roles` — grant role (AdminFull policy)
  - [ ] 3.5 DELETE `/{userId:guid}/roles/{role}` — revoke role (AdminFull policy)

- [ ] Task 4: Create frontend API client and page (AC: #5)
  - [ ] 4.1 Add team API functions to `admin.ts`
  - [ ] 4.2 Create `app/app/admin/team/page.tsx` — team list with grant/revoke actions

- [ ] Task 5: Write tests (AC: #1, #2, #3, #4)
  - [ ] 5.1 Integration test: SanzuAdmin can list team members
  - [ ] 5.2 Integration test: SanzuAdmin can grant admin role
  - [ ] 5.3 Integration test: SanzuAdmin can revoke admin role
  - [ ] 5.4 Integration test: Non-SanzuAdmin gets 403
  - [ ] 5.5 Integration test: Cannot grant SanzuAdmin role (prevent privilege escalation)
  - [ ] 5.6 Integration test: Audit events logged for grant/revoke
  - [ ] 5.7 Frontend unit test: page and API client exports

## Dev Notes

### Critical Architecture Patterns

**FR-A36 + FR-A37:** Team management is SanzuAdmin-only (AdminFull policy). Only internal admin roles (SanzuOps, SanzuFinance, SanzuSupport, SanzuViewer) can be granted/revoked. SanzuAdmin role cannot be granted through this endpoint (prevent privilege escalation).

### Existing Code to Leverage

1. **IUserRoleRepository** — `api/src/Sanzu.Core/Interfaces/IUserRoleRepository.cs`
   - Already has methods for querying and managing UserRole entities
   - Check for: `GetByUserIdAsync`, `CreateAsync` or similar

2. **UserRole Entity** — `api/src/Sanzu.Core/Entities/UserRole.cs`
   - Fields: Id, UserId, RoleType (PlatformRole), TenantId (null for platform-scoped), GrantedBy, GrantedAt

3. **IUserRepository** — `api/src/Sanzu.Core/Interfaces/IUserRepository.cs`
   - For looking up user details (FullName, Email) to include in response

4. **IAuditRepository + IUnitOfWork** — for audit logging in service (not controller — grant/revoke need explicit audit with metadata)
   - Use [SkipAdminAudit] on grant/revoke actions since they log custom audit events
   - Keep GET /team using auto-audit from filter

5. **AdminPermissionsController** — pattern reference for admin controller

### Internal Admin Roles (Grantable)

Only these roles can be granted/revoked via the team management endpoint:
- `SanzuOps`
- `SanzuFinance`
- `SanzuSupport`
- `SanzuViewer`

**SanzuAdmin cannot be granted** — this is a security boundary. The validator should reject `SanzuAdmin` and `AgencyAdmin`.

### Audit Event Patterns

- `Admin.Team.RoleGranted` — metadata: `{ "targetUserId": "...", "role": "SanzuSupport", "grantedBy": "..." }`
- `Admin.Team.RoleRevoked` — metadata: `{ "targetUserId": "...", "role": "SanzuSupport", "revokedBy": "..." }`
- Grant/Revoke use manual audit via `_auditRepository.CreateAsync()` + `_unitOfWork.ExecuteInTransactionAsync()` because they need specific metadata
- Use `[SkipAdminAudit]` attribute on grant/revoke endpoints

### "Last Active" Timestamp

FR-A36 mentions "last active timestamps" for team members. Options:
- If User entity has a `LastActiveAt` or `LastLoginAt` field, use it
- If not, use the latest AuditEvent.CreatedAt for that user as a proxy
- Check User entity fields before implementing

### Navigation Tab Mapping

Admin layout (Story 12.2) maps `/admin/team` endpoint to the "Team" tab. Only SanzuAdmin has `/admin/team` in accessibleEndpoints, so the tab is only visible to SanzuAdmin.

### Previous Story Learnings

- AdminAuditActionFilter handles auto-audit — use [SkipAdminAudit] for manual audit
- `AuditRepository.CreateAsync()` only adds to change tracker — wrap in `IUnitOfWork.ExecuteInTransactionAsync()` to persist
- `HeaderAuthenticationHandler` requires BOTH `X-User-Id` AND `X-Tenant-Id` headers in tests
- Organization entity has NO `Slug` — use `Location = "Test"` in seeds
- Frontend tests: use module import assertions, not SSR rendering

### DI Registration

- Register `IAdminTeamService` → `AdminTeamService` in `ServiceRegistration.cs`
- Register `IValidator<GrantAdminRoleRequest>` → `GrantAdminRoleRequestValidator`

### Testing Standards

- Backend: `api/tests/Sanzu.Tests/Admin/AdminTeamManagementTests.cs`
- Frontend: `api/src/Sanzu.Web/tests/unit/story-12-4-team-management.test.tsx`
- Seed multiple users with different admin roles, verify list returns them
- Verify grant creates UserRole with TenantId = NULL
- Verify revoke deletes the UserRole
- Verify SanzuAdmin grant attempt returns validation error

### References

- [Source: _bmad-output/planning-artifacts/epics-admin.md#Story 12.4]
- [Source: _bmad-output/planning-artifacts/architecture-admin.md#Authentication & Security]
- [Source: api/src/Sanzu.Core/Entities/UserRole.cs]
- [Source: api/src/Sanzu.Core/Interfaces/IUserRoleRepository.cs]
- [Source: api/src/Sanzu.Core/Interfaces/IUserRepository.cs]
- [Source: api/src/Sanzu.API/Controllers/Admin/AdminPermissionsController.cs]
- [Source: api/src/Sanzu.API/Filters/SkipAdminAuditAttribute.cs]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

- All 5 tasks completed: DTOs, validator, service, controller, frontend, tests
- Added GetAllPlatformScopedAsync() and DeleteAsync() to IUserRoleRepository
- GrantAdminRoleRequestValidator prevents SanzuAdmin and AgencyAdmin grants (privilege escalation)
- Grant/Revoke use [SkipAdminAudit] + manual audit with rich metadata (targetUserId, role)
- GET /team uses auto-audit from AdminAuditActionFilter
- Backend: 386/386 tests pass (11 new)
- Frontend: 2/2 unit tests pass

### Change Log

- `api/src/Sanzu.Core/Models/Responses/AdminTeamMemberResponse.cs` — NEW
- `api/src/Sanzu.Core/Models/Requests/GrantAdminRoleRequest.cs` — NEW
- `api/src/Sanzu.Core/Validators/GrantAdminRoleRequestValidator.cs` — NEW
- `api/src/Sanzu.Core/Interfaces/IAdminTeamService.cs` — NEW
- `api/src/Sanzu.Core/Services/AdminTeamService.cs` — NEW
- `api/src/Sanzu.Core/Interfaces/IUserRoleRepository.cs` — MODIFIED: added GetAllPlatformScopedAsync, DeleteAsync
- `api/src/Sanzu.Infrastructure/Repositories/UserRoleRepository.cs` — MODIFIED: implemented new methods
- `api/src/Sanzu.API/Controllers/Admin/AdminTeamController.cs` — NEW
- `api/src/Sanzu.API/Configuration/ServiceRegistration.cs` — MODIFIED: added AdminTeamService + validator DI
- `api/src/Sanzu.Web/lib/api-client/generated/admin.ts` — MODIFIED: added team management functions
- `api/src/Sanzu.Web/app/app/admin/team/page.tsx` — NEW
- `api/tests/Sanzu.Tests/Admin/AdminTeamManagementTests.cs` — NEW: 11 integration tests
- `api/src/Sanzu.Web/tests/unit/story-12-4-team-management.test.tsx` — NEW: 2 frontend tests
