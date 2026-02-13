# Story 7.5: Configure KPI Threshold Alerts

Status: review

## Story

As a Sanzu Administrator,
I want configurable KPI threshold alerts,
So that operational degradations are escalated with actionable context.

## Acceptance Criteria

1. **Given** a KPI threshold is defined, **When** metric breach occurs, **Then** alert is routed with severity and context.
2. **And** alerts are logged for follow-up.

## Implementation Summary

- Extended KPI admin API with threshold and alert endpoints:
  - `PUT /api/v1/admin/kpi/thresholds`
  - `POST /api/v1/admin/kpi/alerts/evaluate`
- Added KPI alert service contract + implementation:
  - `IKpiAlertService`
  - `KpiAlertService`
- Added threshold and alert domain models:
  - `KpiThresholdDefinition`
  - `KpiAlertLog`
  - `KpiMetricKey`
  - `KpiAlertSeverity`
- Added request/response contracts:
  - `UpsertKpiThresholdRequest`
  - `EvaluateKpiAlertsRequest`
  - `KpiThresholdResponse`
  - `KpiAlertEvaluationResponse`
  - `KpiAlertLogResponse`
- Added validation:
  - `UpsertKpiThresholdRequestValidator`
  - `EvaluateKpiAlertsRequestValidator`
- Added persistence:
  - `IKpiThresholdRepository` + `KpiThresholdRepository`
  - `IKpiAlertLogRepository` + `KpiAlertLogRepository`
  - `KpiThresholdConfiguration`
  - `KpiAlertLogConfiguration`
  - `SanzuDbContext` sets for thresholds and alerts.
- Added routing and follow-up logging behavior:
  - Alerts include route target, severity, and serialized context payload.
  - Alert logs persisted in `KpiAlerts` table.
  - Audit events:
    - `KpiThresholdConfigured`
    - `KpiThresholdAlertTriggered`
- Added automated coverage:
  - Integration tests for threshold upsert and alert evaluation.
  - Unit tests for KPI alert service behavior.
  - Unit tests for KPI alert validators.

## Verification

- `dotnet test .\\api\\Sanzu.sln --no-restore`
  - Passed: 295
  - Failed: 0

## File List

- `api/src/Sanzu.API/Configuration/ServiceRegistration.cs`
- `api/src/Sanzu.API/Controllers/KpiController.cs`
- `api/src/Sanzu.Core/Entities/KpiAlertLog.cs`
- `api/src/Sanzu.Core/Entities/KpiThresholdDefinition.cs`
- `api/src/Sanzu.Core/Enums/KpiAlertSeverity.cs`
- `api/src/Sanzu.Core/Enums/KpiMetricKey.cs`
- `api/src/Sanzu.Core/Interfaces/IKpiAlertLogRepository.cs`
- `api/src/Sanzu.Core/Interfaces/IKpiAlertService.cs`
- `api/src/Sanzu.Core/Interfaces/IKpiThresholdRepository.cs`
- `api/src/Sanzu.Core/Models/Requests/EvaluateKpiAlertsRequest.cs`
- `api/src/Sanzu.Core/Models/Requests/UpsertKpiThresholdRequest.cs`
- `api/src/Sanzu.Core/Models/Responses/KpiAlertResponses.cs`
- `api/src/Sanzu.Core/Services/KpiAlertService.cs`
- `api/src/Sanzu.Core/Validators/EvaluateKpiAlertsRequestValidator.cs`
- `api/src/Sanzu.Core/Validators/UpsertKpiThresholdRequestValidator.cs`
- `api/src/Sanzu.Infrastructure/Data/EntityConfigurations/KpiAlertLogConfiguration.cs`
- `api/src/Sanzu.Infrastructure/Data/EntityConfigurations/KpiThresholdConfiguration.cs`
- `api/src/Sanzu.Infrastructure/Data/SanzuDbContext.cs`
- `api/src/Sanzu.Infrastructure/DependencyInjection/ServiceRegistration.cs`
- `api/src/Sanzu.Infrastructure/Repositories/KpiAlertLogRepository.cs`
- `api/src/Sanzu.Infrastructure/Repositories/KpiThresholdRepository.cs`
- `api/tests/Sanzu.Tests/Integration/Controllers/KpiControllerTests.cs`
- `api/tests/Sanzu.Tests/Unit/Services/KpiAlertServiceTests.cs`
- `api/tests/Sanzu.Tests/Unit/Validators/KpiAlertValidatorsTests.cs`
- `_bmad-output/implementation-artifacts/7-5-configure-kpi-threshold-alerts.md`
