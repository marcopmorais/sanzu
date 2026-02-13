# Story 7.3: Apply Platform Policy Controls

Status: review

## Story

As a Sanzu Administrator,
I want tenant-level risk and compliance controls,
So that critical situations can be contained quickly.

## Acceptance Criteria

1. **Given** a tenant-level risk condition, **When** a control is applied, **Then** configured restrictions are enforced immediately.
2. **And** applied controls are traceable with reason codes.

## Implementation Summary

- Added admin policy-control endpoint:
  - `PUT /api/v1/admin/tenants/{tenantId}/policy-controls`
- Added tenant policy control service contract + implementation:
  - `ITenantPolicyControlService`
  - `TenantPolicyControlService`
- Added policy control domain model and contracts:
  - `TenantPolicyControl`
  - `TenantPolicyControlType` (`ComplianceFlag`, `SupportEscalation`, `RiskHold`)
  - `ApplyTenantPolicyControlRequest`
  - `TenantPolicyControlResponse`
- Added policy request validation:
  - `ApplyTenantPolicyControlRequestValidator`
  - Validates known control type and reason code format.
- Added persistence for tenant controls:
  - `ITenantPolicyControlRepository`
  - `TenantPolicyControlRepository`
  - `TenantPolicyControlConfiguration`
  - `SanzuDbContext` `DbSet` + tenant query filter.
- Added enforcement path for immediate restrictions:
  - `CaseService.CreateCaseAsync` now checks active `RiskHold` control and rejects case creation with conflict semantics.
- Added audit traceability:
  - Event: `TenantPolicyControlApplied`
  - Metadata includes tenant, control type, enabled state, and reason code.
- Added automated coverage:
  - Integration tests for applying controls and risk-hold case creation blocking.
  - Unit tests for policy control service behavior.
  - Unit tests for policy control request validation.
  - Unit test for case creation blocked by active risk hold.

## Verification

- `dotnet test .\\api\\Sanzu.sln --no-restore`
  - Passed: 281
  - Failed: 0

## File List

- `api/src/Sanzu.API/Configuration/ServiceRegistration.cs`
- `api/src/Sanzu.API/Controllers/AdminController.cs`
- `api/src/Sanzu.Core/Entities/TenantPolicyControl.cs`
- `api/src/Sanzu.Core/Enums/TenantPolicyControlType.cs`
- `api/src/Sanzu.Core/Interfaces/ITenantPolicyControlRepository.cs`
- `api/src/Sanzu.Core/Interfaces/ITenantPolicyControlService.cs`
- `api/src/Sanzu.Core/Models/Requests/ApplyTenantPolicyControlRequest.cs`
- `api/src/Sanzu.Core/Models/Responses/TenantPolicyControlResponse.cs`
- `api/src/Sanzu.Core/Services/CaseService.cs`
- `api/src/Sanzu.Core/Services/TenantPolicyControlService.cs`
- `api/src/Sanzu.Core/Validators/ApplyTenantPolicyControlRequestValidator.cs`
- `api/src/Sanzu.Infrastructure/Data/EntityConfigurations/TenantPolicyControlConfiguration.cs`
- `api/src/Sanzu.Infrastructure/Data/SanzuDbContext.cs`
- `api/src/Sanzu.Infrastructure/DependencyInjection/ServiceRegistration.cs`
- `api/src/Sanzu.Infrastructure/Repositories/TenantPolicyControlRepository.cs`
- `api/tests/Sanzu.Tests/Integration/Controllers/AdminControllerTests.cs`
- `api/tests/Sanzu.Tests/Unit/Services/CaseServiceTests.cs`
- `api/tests/Sanzu.Tests/Unit/Services/TenantPolicyControlServiceTests.cs`
- `api/tests/Sanzu.Tests/Unit/Validators/TenantPolicyControlValidatorTests.cs`
- `_bmad-output/implementation-artifacts/7-3-apply-platform-policy-controls.md`
