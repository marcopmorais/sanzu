# Sanzu UX Screen Definitions

## 1. Agency - Case List Dashboard
**User:** Manager
**Purpose:** View and manage all cases

**Elements:**
- Header: Logo, org name, user menu
- Filters: Status (Draft/Active/Closing/Archived), Date range, Search
- Table: Deceased name, Date of death, Status, Next step, Assignee, Actions
- CTA: "Create New Case" button
- Stats: Active cases, Blocked steps, Avg cycle time

## 2. Agency - Create Case
**User:** Manager
**Purpose:** Quick case creation

**Form Fields:**
- Deceased full name*
- Date of death*
- Municipality
- Editor email (to invite)*

**Actions:**
- Create & Invite
- Cancel

## 3. Family - Accept Invite
**User:** Editor (new)
**Purpose:** Onboard to case

**Elements:**
- Welcome message with case context
- Deceased name, Date
- Next steps preview
- CTA: "Accept & Continue"

## 4. Family - Questionnaire
**User:** Editor
**Purpose:** Collect case information

**Questions (branching):**
1. Does deceased have bank accounts? (Yes/No/Unknown)
2. Does deceased have insurance policies? (Yes/No/Unknown)
3. Does deceased have pension/benefits? (Yes/No/Unknown)
4. Was deceased employed? (Yes/No)
5. Does deceased have utilities/services? (Yes/No/Unknown)

**Progress:** 5 steps, save & resume

## 5. Family/Agency - Case Dashboard
**User:** All roles
**Purpose:** See progress and next actions

**Layout:**
- Progress bar (% complete)
- Next Best Step card (highlighted)
- Missing Documents alert
- Step checklist (grouped by workstream)
- Activity feed
- Document vault quick access

## 6. Family/Agency - Step Detail
**User:** Manager/Editor
**Purpose:** Execute a step

**Elements:**
- Step title & description
- Owner badge (Agency/Family)
- Prerequisites (with status)
- Required documents (upload if missing)
- CTA: "Mark as Complete" (if ready)
- Notes/comments

## 7. Family/Agency - Document Vault
**User:** All roles (permissions vary)
**Purpose:** Upload, view, download documents

**Elements:**
- Upload zone (drag & drop)
- Document list: Name, Type, Date, Size, Uploaded by
- Filter by type
- Sensitivity indicators (restricted)
- Download/preview actions

## 8. Manager - Generate Document
**User:** Manager
**Purpose:** Create PDF from template

**Flow:**
- Select template (Bank letter, Insurance claim, etc.)
- Preview pre-filled data
- Edit if needed
- Generate PDF
- Auto-save to vault

## 9. All Roles - Activity Feed
**User:** All roles
**Purpose:** See what changed

**Items:**
- Timestamp
- Actor (who)
- Action (what)
- Icon
- Filter by type

## 10. Manager - Close Case
**User:** Manager
**Purpose:** Archive completed case

**Checklist:**
- All mandatory steps complete
- Documents uploaded
- Confirmation
- Archive reason (optional)

