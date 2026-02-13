# Story 7.4: View KPI Dashboard with Drilldown

Status: review

## Story

As a Sanzu Administrator,
I want KPI dashboard visibility with drilldown,
So that platform performance can be monitored and explained.

## Acceptance Criteria

1. **Given** a platform performance review, **When** admin views KPI dashboard, **Then** KPIs display current and baseline trends.
2. **And** drilldown reveals tenant and case contributions.

## Implementation Summary

- Added platform KPI dashboard endpoint:
  - `GET /api/v1/admin/kpi/dashboard`
  - Query params: `periodDays`, `tenantLimit`, `caseLimit`.
- Added KPI dashboard service contract + implementation:
  - `IKpiDashboardService`
  - `KpiDashboardService`
- Added dashboard response contracts:
  - `PlatformKpiDashboardResponse`
  - `PlatformKpiMetricsResponse`
  - `PlatformKpiTrendResponse`
  - `PlatformKpiTenantContributionResponse`
  - `PlatformKpiCaseContributionResponse`
- Added platform-scope repository access paths (ignore tenant query filters) for cross-tenant aggregation:
  - `IOrganizationRepository.GetAllAsync` now uses `IgnoreQueryFilters()`
  - `ICaseRepository.GetByTenantIdForPlatformAsync`
  - `ICaseDocumentRepository.GetByCaseIdForPlatformAsync`
- Implemented KPI computation:
  - Current window metrics across tenants.
  - Baseline window metrics for trend comparison.
  - Percent-change trend calculations.
- Implemented drilldown:
  - Top tenant contribution rows.
  - Top case contribution rows with tenant context.
- Added access control and validation:
  - `SanzuAdmin`-only access.
  - Period and drilldown limit validation with bad request responses.
- Added automated coverage:
  - Integration tests for successful dashboard retrieval and forbidden non-admin access.
  - Unit tests for dashboard aggregation, access control, and validation.

## Verification

- `dotnet test .\\api\\Sanzu.sln --no-restore`
  - Passed: 286
  - Failed: 0

## File List

- `api/src/Sanzu.API/Configuration/ServiceRegistration.cs`
- `api/src/Sanzu.API/Controllers/KpiController.cs`
- `api/src/Sanzu.Core/Interfaces/ICaseDocumentRepository.cs`
- `api/src/Sanzu.Core/Interfaces/ICaseRepository.cs`
- `api/src/Sanzu.Core/Interfaces\IKpiDashboardService.cs`
- `api/src/Sanzu.Core/Interfaces/IOrganizationRepository.cs`
- `api/src/Sanzu.Core/Models/Responses/PlatformKpiDashboardResponse.cs`
- `api/src/Sanzu.Core/Services/KpiDashboardService.cs`
- `api/src/Sanzu.Infrastructure/Repositories/CaseDocumentRepository.cs`
- `api/src/Sanzu.Infrastructure/Repositories/CaseRepository.cs`
- `api/src/Sanzu.Infrastructure/Repositories/OrganizationRepository.cs`
- `api/tests/Sanzu.Tests/Integration/Controllers/KpiControllerTests.cs`
- `api/tests/Sanzu.Tests/Unit/Services/KpiDashboardServiceTests.cs`
- `_bmad-output/implementation-artifacts/7-4-view-kpi-dashboard-with-drilldown.md`
