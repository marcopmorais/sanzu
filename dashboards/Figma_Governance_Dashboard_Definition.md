# Figma Governance Dashboard Definition - Sanzu

## Dashboard Name
`Sanzu Figma Governance Dashboard`

## Data Source
ClickUp tasks in `Sanzu Product System` where phase in {Design, Architecture Review, Ready for Build, Building}.

## KPIs (Mandatory)

### 1) % features without Figma links
Formula:
- Numerator: tasks with Figma_File_Link empty
- Denominator: all active feature tasks in covered phases

### 2) % features without frame IDs
Formula:
- Numerator: tasks with Figma_Frame_IDs empty
- Denominator: all active feature tasks in covered phases

### 3) % features without Build-approved UX
Formula:
- Numerator: tasks where UX_Maturity != Build-approved
- Denominator: all tasks in Design+ phases

### 4) % features using outdated Figma version
Formula:
- Numerator: tasks where UX_Revalidation_Required=true and UX_Revalidated=false
- Denominator: tasks with Figma_Version populated

### 5) % builds blocked by UX gate
Formula:
- Numerator: tasks reverted by Design/Architecture UX gate automations
- Denominator: total tasks attempting entry to Building

## Supporting Widgets
- Count of tasks missing Figma_Flow_Link
- Count of tasks missing Dev_Handoff_Confirmed
- Trend of UX gate failures by week

## Alert Thresholds
- % without Figma links > 0%: immediate alert to Product Ops
- % without frame IDs > 5%: alert UX Lead
- % without Build-approved UX > 10%: alert Product Director
- % builds blocked by UX gate > 10% in rolling 14 days: trigger governance review

## Refresh
- Refresh frequency: hourly
- Executive snapshot: daily
