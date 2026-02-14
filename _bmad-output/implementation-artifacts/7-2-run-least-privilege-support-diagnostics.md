# Story 7.2: Run Least-Privilege Support Diagnostics

Status: done

## Story

As a Sanzu Administrator,
I want scoped diagnostic access,
So that support incidents are resolved without overexposure.

## Acceptance Criteria

1. **Given** an escalated support issue, **When** diagnostic mode is activated, **Then** access scope is limited by policy and duration.
2. **And** diagnostic actions are fully logged.

## Implementation Summary

- Added admin diagnostic endpoints:
  - `POST /api/v1/admin/tenants/{tenantId}/diagnostics/sessions`
  - `GET /api/v1/admin/tenants/{tenantId}/diagnostics/sessions/{sessionId}/summary`
- Added diagnostics service contract + implementation:
  - `ISupportDiagnosticsService`
  - `SupportDiagnosticsService`
- Added scoped diagnostics domain model and contracts:
  - `SupportDiagnosticSession`
  - `SupportDiagnosticScope`
  - `StartSupportDiagnosticSessionRequest`
  - `SupportDiagnosticSessionResponse`
  - `SupportDiagnosticSummaryResponse`
  - `SupportDiagnosticAccessException`
- Added request guardrails with policy bounds:
  - `StartSupportDiagnosticSessionRequestValidator`
  - Allowed scope validation and duration window enforcement (5-240 minutes).
- Added persistence for diagnostic sessions:
  - `ISupportDiagnosticSessionRepository`
  - `SupportDiagnosticSessionRepository`
  - `SupportDiagnosticSessionConfiguration`
  - `SanzuDbContext` `DbSet` + tenant query filter.
- Added explicit audit logging for diagnostic actions:
  - Event: `SupportDiagnosticSessionStarted`
  - Event: `SupportDiagnosticSummaryAccessed`
  - Summary retrieval now executes in unit-of-work transaction so logs are committed.
- Added automated coverage:
  - Integration tests for session start, active summary retrieval, and expired session conflict.
  - Unit tests for diagnostics service authorization/audit/session behavior.
  - Unit tests for diagnostics request validation policy bounds.

## Verification

- `dotnet test .\\api\\Sanzu.sln --no-restore`
  - Passed: 272
  - Failed: 0

## File List

- `api/src/Sanzu.API/Configuration/ServiceRegistration.cs`
- `api/src/Sanzu.API/Controllers/AdminController.cs`
- `api/src/Sanzu.Core/Entities/SupportDiagnosticSession.cs`
- `api/src/Sanzu.Core/Enums/SupportDiagnosticScope.cs`
- `api/src/Sanzu.Core/Exceptions/SupportDiagnosticAccessException.cs`
- `api/src/Sanzu.Core/Interfaces/ISupportDiagnosticSessionRepository.cs`
- `api/src/Sanzu.Core/Interfaces/ISupportDiagnosticsService.cs`
- `api/src/Sanzu.Core/Models/Requests/StartSupportDiagnosticSessionRequest.cs`
- `api/src/Sanzu.Core/Models/Responses/SupportDiagnosticSessionResponse.cs`
- `api/src/Sanzu.Core/Models/Responses/SupportDiagnosticSummaryResponse.cs`
- `api/src/Sanzu.Core/Services/SupportDiagnosticsService.cs`
- `api/src/Sanzu.Core/Validators/StartSupportDiagnosticSessionRequestValidator.cs`
- `api/src/Sanzu.Infrastructure/Data/EntityConfigurations/SupportDiagnosticSessionConfiguration.cs`
- `api/src/Sanzu.Infrastructure/Data/SanzuDbContext.cs`
- `api/src/Sanzu.Infrastructure/DependencyInjection/ServiceRegistration.cs`
- `api/src/Sanzu.Infrastructure/Repositories/SupportDiagnosticSessionRepository.cs`
- `api/tests/Sanzu.Tests/Integration/Controllers/AdminControllerTests.cs`
- `api/tests/Sanzu.Tests/Unit/Services/SupportDiagnosticsServiceTests.cs`
- `api/tests/Sanzu.Tests/Unit/Validators/SupportDiagnosticsValidatorTests.cs`
- `_bmad-output/implementation-artifacts/7-2-run-least-privilege-support-diagnostics.md`
