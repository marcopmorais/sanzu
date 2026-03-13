# CHANGELOG

## 2026-02-12 - PDLC 3.5 Infrastructure Integrity Execution
- Updated `ops/config/clickup_schema.json` to include `10_Knowledge_Base`, exact lifecycle statuses (`Iterating`, `Closed`), and `Decision_Log_Entry_ID` field enforcement.
- Updated `ops/config/bmbuilder_workflow.yaml` to require `Decision_Log_Entry_ID` for structural and sensitive-scope guards.
- Added required dashboard definitions:
  - `dashboards/Portfolio_Health_Dashboard_Definition.md`
  - `dashboards/Delivery_Health_Dashboard_Definition.md`
  - `dashboards/Governance_Compliance_Dashboard_Definition.md`
  - `dashboards/Knowledge_Integrity_Dashboard_Definition.md`
  - `dashboards/Figma_Compliance_Dashboard_Definition.md`
- Added Knowledge OS docs tree under `docs/knowledge_os/` with metadata headers (`Owner`, `Version`, `Last Updated`, `Review cadence`).
- Aligned governance documentation to exact status flow and decision log field usage:
  - `docs/pdlc/ClickUp_Implementation_Guide.md`
  - `docs/pdlc/Automation_Definitions.md`
  - `docs/pdlc/PDLC_Operating_Model.md`
  - `docs/pdlc/Experimentation_System.md`
  - `docs/governance/Phase_Gates_and_Checklists.md`
  - `dashboards/Capacity_Dashboard_Definition.md`
- Generated execution report: `_bmad-output/status/infrastructure_integrity_report.md`.

## 2026-02-12 - PDLC 3.5 Enforcement Validation, Migration Audit, and Controlled Relaunch
- Tightened `Ready for Release -> Released` gate in `ops/config/clickup_schema.json` to require `Release_Metric`, `Baseline_Value`, and `Metrics_Doc_Link` in addition to rollback controls.
- Updated governance documentation to align with enforced release and transition rules in `docs/pdlc/Automation_Definitions.md`.
- Restored and normalized PDLC governance docs with metadata headers (Owner, Version, Review cadence):
  - `docs/pdlc/ClickUp_Implementation_Guide.md`
  - `docs/pdlc/Experimentation_System.md`
  - `docs/pdlc/Figma_Operating_Model.md`
  - `docs/pdlc/PDLC_Operating_Model.md`
  - `docs/pdlc/Release_Control_Model.md`
  - `docs/governance/Phase_Gates_and_Checklists.md`
  - `docs/governance/Risk_Taxonomy.md`
  - `docs/portfolio/Portfolio_Governance.md`
- Extended capacity dashboard governance signals and schema KPI mapping for `% initiatives missing doc links`, `% blocked by compliance`, and `Docs past review cadence`.
- Generated PDLC Master operational artifacts in `_bmad-output/status/`.
## 2026-02-12 - PDLC 3.2 Operating System Implementation
- Added deterministic PDLC operating model for Sanzu under `docs/pdlc/`.
- Added portfolio governance model under `docs/portfolio/`.
- Added gate and risk governance controls under `docs/governance/`.
- Added ClickUp enterprise implementation guide with phase-template mapping and automation rules.
- Added Figma operating model with build-approved quality controls.
- Added release control and experimentation system definitions.
- Added capacity dashboard specification under `dashboards/`.
- Added enforceable machine configs: `ops/config/clickup_schema.json`, `ops/config/bmbuilder_workflow.yaml`.
- Added operational templates/checklists under `ops/templates/` and `ops/checklists/`.
- Added PDLC system ASCII diagram in `docs/pdlc/PDLC_System_Diagram.txt`.
## 2026-02-12 - PDLC 3.5 Deterministic Figma-Gated Upgrade
- Upgraded `ops/config/clickup_schema.json` to v3.5 with mandatory Figma fields (`Figma_File_Link`, `Figma_Flow_Link`, `Figma_Frame_IDs`, `Figma_Version`) and strict transition gates.
- Upgraded `ops/config/bmbuilder_workflow.yaml` to v3.5 with hard pre-build Figma/doc/architecture validation and explicit hard-stop output.
- Added `docs/pdlc/Automation_Definitions.md` with deterministic automation rules.
- Updated `docs/pdlc/PDLC_Operating_Model.md` with ClickUp + Docs + Figma alignment contract.
- Updated `docs/pdlc/ClickUp_Implementation_Guide.md` for PDLC 3.5 gates and traceability requirements.
- Updated `docs/pdlc/Figma_Operating_Model.md` with mandatory naming/page/build-approved criteria.
- Updated `docs/governance/Phase_Gates_and_Checklists.md` with strict Figma and docs gate conditions.
- Added `dashboards/Figma_Governance_Dashboard_Definition.md` and linked it from capacity dashboard.
- Updated `docs/pdlc/PDLC_System_Diagram.txt` to show ClickUp Space + ClickUp Docs + Figma feeding BMBuilder.
