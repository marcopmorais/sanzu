# Capacity Dashboard Definition - Sanzu

## Dashboard Name
`Sanzu Portfolio Capacity and Gate Health`

## Data Source
ClickUp tasks in `Sanzu Product System` Space, folders `00_Portfolio` through `10_Knowledge_Base`.

## Widgets

### 1. Active Initiatives by Capital_Type
- Type: Stacked bar
- Group by: Capital_Type
- Filter: Status not in {Closed}
- Output: Count by Run/Grow/Transform

### 2. Dev Load
- Type: Gauge
- Formula: `Committed_Story_Points / Available_Story_Points`
- Thresholds: Green <=0.80, Yellow 0.81-0.90, Red >0.90

### 3. % Blocked by Architecture
- Formula: `Tasks blocked in Architecture Review / Tasks in Architecture Review`

### 4. % Without Validated Discovery
- Formula: `Tasks in Definition+ where Evidence_Level < Medium or Interview_Count < 5 / Tasks in Definition+`

### 5. Portfolio Risk Score
- Formula: `SUM(Risk_Score * Investment_Size) / SUM(Investment_Size)`

### 6. DoR Failure Breakdown
- Columns: missing field, blocked task count
- Fields: strategic doc link, economic hypothesis, Figma fields, UX maturity, architecture approvals, NFR, dependencies

### 7. Release Outcome Health
- Formula: `% releases meeting Expected_Impact within Observation_Window_Days`

### 8. ROI Recalculation Compliance
- Formula: `Closed lifecycle tasks with ROI_Recalculated=true / Closed lifecycle tasks`

### 9. Linked Figma Governance Dashboard
- Source: `dashboards/Figma_Governance_Dashboard_Definition.md`
- Purpose: UX gate and Figma linkage compliance

### 10. % Initiatives Missing Doc Links
- Formula: `Active initiatives with missing Strategic_Doc_Link OR UX_Doc_Link OR Metrics_Doc_Link / Active initiatives`

### 11. % Blocked by Compliance
- Formula: `Tasks blocked where Security_Approved=false OR Data_Compliance_Approved=false / Tasks in Architecture Review + Ready for Build`

### 12. Docs Past Review Cadence
- Formula: `Count of linked docs where Next_Review_Date < today AND linked initiative still active`

### 13. Blocked Items
- Formula: `Count of tasks currently reverted by transition gates in the last 7 days`

## Alert Rules
- Portfolio risk above threshold -> notify Portfolio Council
- DoR failures > 10 active tasks -> notify PMO
- Missing baseline metric in Ready for Release -> notify Release Manager
- Missing doc links > 0% -> notify Product Ops
- Compliance block rate > 5% -> notify Compliance Lead
- Docs past review cadence > 0 -> notify document owners
- Figma governance alerts follow `dashboards/Figma_Governance_Dashboard_Definition.md`

## Refresh Cadence
- Operational: every 2 hours
- Executive snapshot: daily 08:00 local
