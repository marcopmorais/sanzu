# V1.0 Detailed Backlog - Sanzu

**Version:** V1.0 Foundation  
**Sprint Duration:** 2 weeks  
**Team:** 2 engineers (full-stack)  
**Timeline:** 10-12 weeks (5-6 sprints)

---

## EPIC 1: Auth & Invitations

### Story 1.1: Agency Manager Creates Account
**As** an Agency Manager  
**I want to** create an organization account  
**So that** I can start managing cases

**Acceptance Criteria:**
- [ ] Sign up with email via Clerk
- [ ] Create organization during onboarding
- [ ] Organization name, location captured
- [ ] Manager role automatically assigned
- [ ] Email verification required
- [ ] Audit: `org_created`, `user_registered`

**Technical:** Clerk integration, Prisma schema setup  
**Estimate:** 5 points

---

### Story 1.2: Manager Invites Family Editor
**As** a Manager  
**I want to** invite a family member as Editor  
**So that** they can manage case documents

**Acceptance Criteria:**
- [ ] Invite from case creation screen
- [ ] Email sent within 30 seconds
- [ ] Secure invite link (7-day expiry)
- [ ] Only 1 Editor per case (system enforces)
- [ ] Error if email already invited
- [ ] Audit: `invite_sent`

**Edge Cases:**
- Invite expired → show error, allow resend
- Invalid email → validation error
- Already a participant → error message

**Technical:** Clerk invitations API, email template  
**Estimate:** 8 points

---

### Story 1.3: Family Accepts Invite
**As** a Family member  
**I want to** accept an invite easily  
**So that** I can access the case

**Acceptance Criteria:**
- [ ] Click link → passwordless signup
- [ ] Auto-assigned Editor role
- [ ] Redirect to case dashboard
- [ ] Audit: `invite_accepted`
- [ ] Works on mobile

**Technical:** Clerk invite acceptance flow  
**Estimate:** 3 points

---

### Story 1.4: Manager Invites Readers
**As** a Manager  
**I want to** invite multiple family members as Readers  
**So that** they have transparency

**Acceptance Criteria:**
- [ ] Add multiple Reader emails (comma-separated)
- [ ] Readers have view-only access
- [ ] No limit on Reader count
- [ ] Email notification sent
- [ ] Audit: `invite_sent` (role=Reader)

**Estimate:** 5 points

---

## EPIC 2: Case Management

### Story 2.1: Manager Creates Case
**As** a Manager  
**I want to** create a case in <2 minutes  
**So that** I can start the process quickly

**Acceptance Criteria:**
- [ ] Required fields: deceased name, date of death
- [ ] Optional: municipality, notes
- [ ] Auto-generate case_id (UUID)
- [ ] Status = Draft until Editor invited
- [ ] Redirect to invite screen
- [ ] Audit: `case_created`

**Performance:** <500ms response time  
**Estimate:** 5 points

---

### Story 2.2: Manager Views Case List
**As** a Manager  
**I want to** see all my cases  
**So that** I can track workload

**Acceptance Criteria:**
- [ ] Table view: case ID, deceased name, status, last updated
- [ ] Sort by: status, date created, last updated
- [ ] Filter by: status (Draft/Active/Closing/Archived)
- [ ] Pagination (20 per page)
- [ ] Click row → case dashboard

**Performance:** <1s load time for 100 cases  
**Estimate:** 8 points

---

### Story 2.3: Manager Archives Case
**As** a Manager  
**I want to** archive completed cases  
**So that** they don't clutter my active list

**Acceptance Criteria:**
- [ ] Archive button (only for Closing status)
- [ ] Confirmation modal
- [ ] Status → Archived
- [ ] Archived cases hidden from default view
- [ ] Can filter to see archived
- [ ] Audit: `case_archived`

**Estimate:** 3 points

---

## EPIC 3: Workflow Engine

### Story 3.1: Generate Case Plan
**As** the system  
**I want to** generate a step plan from inputs  
**So that** users know what to do

**Acceptance Criteria:**
- [ ] Input: questionnaire responses
- [ ] Output: list of WorkflowStepInstances
- [ ] Apply triggering rules (has_banks, has_insurance, etc.)
- [ ] Set dependencies (prerequisites)
- [ ] Assign owners (Agency/Family)
- [ ] Default status = NotStarted or Blocked

**Technical:** Rules engine implementation  
**Estimate:** 13 points (complex)

---

### Story 3.2: Display Checklist Dashboard
**As** a Family Editor  
**I want to** see all steps in a checklist  
**So that** I know what needs to be done

**Acceptance Criteria:**
- [ ] Group steps by workstream (A, B, C, D...)
- [ ] Show: step title, owner, status, criticality
- [ ] Blocked steps clearly marked
- [ ] Completed steps greyed out
- [ ] Progress bar (% complete)
- [ ] Mobile-responsive

**Estimate:** 8 points

---

### Story 3.3: Update Step Status
**As** a Manager or Editor  
**I want to** mark steps as complete  
**So that** progress is tracked

**Acceptance Criteria:**
- [ ] Manager can update any step
- [ ] Editor can only update Family-owned steps
- [ ] Statuses: NotStarted → Ready → InProgress → Completed
- [ ] Blocked if prerequisites not met
- [ ] Audit: `step_status_changed`

**Edge Cases:**
- Try to complete when prerequisites missing → error
- Try to complete as Reader → 403 error

**Estimate:** 8 points

---

### Story 3.4: Show Next Best Step
**As** a User  
**I want to** see the next step I should take  
**So that** I'm not overwhelmed

**Acceptance Criteria:**
- [ ] Dashboard shows "Próximo passo" card
- [ ] Logic: first Ready step, prioritize by criticality
- [ ] If none Ready, show "waiting on..." message
- [ ] Click → step detail page

**Estimate:** 5 points

---

## EPIC 4: Document Vault

### Story 4.1: Upload Document
**As** an Editor or Manager  
**I want to** upload a document  
**So that** I can provide required evidence

**Acceptance Criteria:**
- [ ] Drag-and-drop or file picker
- [ ] Allowed types: PDF, JPG, PNG
- [ ] Max 50MB per file
- [ ] Show upload progress
- [ ] Assign doc_type from dropdown
- [ ] Mark sensitivity (normal/restricted)
- [ ] Audit: `document_uploaded`

**Technical:** S3 presigned POST, BullMQ job  
**Estimate:** 13 points

---

### Story 4.2: Download Document
**As** a User with access  
**I want to** download a document  
**So that** I can review or print it

**Acceptance Criteria:**
- [ ] Download button → signed URL
- [ ] 15-min expiry on URL
- [ ] Restricted docs only visible to Manager/Editor
- [ ] Audit: `document_downloaded`

**Estimate:** 5 points

---

### Story 4.3: Document Versioning
**As** a Manager  
**I want to** replace a document  
**So that** I can correct errors

**Acceptance Criteria:**
- [ ] Upload new version of existing doc
- [ ] Old version preserved (soft delete)
- [ ] Can view version history
- [ ] Download any version

**Estimate:** 8 points

---

## EPIC 5: Template Engine

### Story 5.1: Generate Bank Notification Letter
**As** a Manager  
**I want to** generate a pre-filled bank letter  
**So that** I save time and avoid errors

**Acceptance Criteria:**
- [ ] Template: tmpl_bank_notification_letter
- [ ] Pre-fill: deceased name, date, requester info
- [ ] Prompt for missing fields (bank name, account #)
- [ ] Generate PDF via Puppeteer
- [ ] Save to vault as generated_document
- [ ] Download link
- [ ] Audit: `template_generated`

**Technical:** React template, Puppeteer, BullMQ  
**Estimate:** 13 points

---

### Story 5.2: Generate Insurance Claim Request
**As** a Manager  
**I want to** generate an insurance claim letter  
**So that** I can submit claims faster

**Acceptance Criteria:**
- [ ] Template: tmpl_insurance_claim_request
- [ ] Pre-fill case data
- [ ] Optional: policy number, insurer name
- [ ] PDF generation <10 sec
- [ ] Save to vault

**Estimate:** 8 points (reuse engine from 5.1)

---

### Story 5.3: Generate Case Checklist PDF
**As** a Manager  
**I want to** print a case checklist  
**So that** I can work offline

**Acceptance Criteria:**
- [ ] Template: tmpl_case_document_checklist
- [ ] Lists all steps + required docs
- [ ] Includes checkboxes for manual tracking
- [ ] PDF format

**Estimate:** 5 points

---

## EPIC 6: Questionnaire

### Story 6.1: Family Completes Intake
**As** an Editor  
**I want to** answer intake questions  
**So that** the system knows my situation

**Acceptance Criteria:**
- [ ] Progressive disclosure (one question at a time)
- [ ] Save progress automatically
- [ ] Can resume later
- [ ] Branching logic (if has_banks → ask which banks)
- [ ] Audit: `questionnaire_completed`

**Questions (V1):**
1. Banks/accounts? (yes/no/unknown)
2. Insurance policies? (yes/no/unknown)
3. Benefits/pension? (yes/no/unknown)
4. Employer? (yes/no/unknown)
5. Recurring services? (yes/no/unknown)

**Estimate:** 13 points

---

### Story 6.2: Manager Reviews Responses
**As** a Manager  
**I want to** see questionnaire responses  
**So that** I can verify the plan is correct

**Acceptance Criteria:**
- [ ] View all responses on case dashboard
- [ ] Highlight missing answers
- [ ] Can edit responses (triggers plan regeneration)

**Estimate:** 5 points

---

## EPIC 7: Audit & Compliance

### Story 7.1: Audit Log
**As** a Manager  
**I want to** see an audit trail  
**So that** I can prove compliance

**Acceptance Criteria:**
- [ ] Show all events for a case
- [ ] Columns: timestamp, actor, event type, details
- [ ] Filter by: event type, date range
- [ ] Export as CSV

**Estimate:** 8 points

---

### Story 7.2: Sensitive Doc Access Control
**As** the system  
**I want to** restrict sensitive documents  
**So that** privacy is protected

**Acceptance Criteria:**
- [ ] Restricted docs only visible to Manager/Editor
- [ ] Readers cannot see restricted docs
- [ ] API enforces access check
- [ ] Audit: `document_accessed` (failed attempts logged)

**Estimate:** 5 points

---

## EPIC 8: Notifications (Lite)

### Story 8.1: Invite Email
**As** a User  
**I want to** receive an invite email  
**So that** I know I have a case to access

**Acceptance Criteria:**
- [ ] Email sent within 30 sec
- [ ] Subject: "Convite para acompanhar um processo no Sanzu.ai"
- [ ] Body: invite link, case context
- [ ] From: noreply@sanzu.ai

**Technical:** Email service (Resend or SendGrid)  
**Estimate:** 5 points

---

### Story 8.2: Step Unblocked Notification
**As** an Editor  
**I want to** be notified when a step is unblocked  
**So that** I can act quickly

**Acceptance Criteria:**
- [ ] Email when step changes Blocked → Ready
- [ ] Only if Editor is the owner
- [ ] Max 1 email/day (digest)

**Estimate:** 8 points

---

## EPIC 9: Dashboard & UI

### Story 9.1: Case Dashboard
**As** a User  
**I want to** see case overview  
**So that** I understand status

**Acceptance Criteria:**
- [ ] Progress bar (% complete)
- [ ] Next step card
- [ ] Recent activity (last 5 events)
- [ ] Document count
- [ ] Quick links (upload, generate)

**Estimate:** 8 points

---

### Story 9.2: Activity Feed
**As** a User  
**I want to** see recent changes  
**So that** I stay updated

**Acceptance Criteria:**
- [ ] Show last 20 audit events
- [ ] Human-readable messages ("João uploaded death_certificate.pdf")
- [ ] Timestamp (relative: "2 hours ago")
- [ ] Filter by: document, step, user

**Estimate:** 5 points

---

## SPRINT PLANNING

### Sprint 1 (Weeks 1-2): Foundation
- Epic 1: Auth & Invitations (all stories)
- Epic 2: Case Management (Stories 2.1, 2.2)
- **Total:** 34 points

### Sprint 2 (Weeks 3-4): Workflow Core
- Epic 3: Workflow Engine (Stories 3.1, 3.2, 3.3)
- Epic 6: Questionnaire (Story 6.1)
- **Total:** 42 points

### Sprint 3 (Weeks 5-6): Documents
- Epic 4: Document Vault (Stories 4.1, 4.2)
- Epic 5: Template Engine (Story 5.1)
- **Total:** 31 points

### Sprint 4 (Weeks 7-8): Templates & Audit
- Epic 5: Templates (Stories 5.2, 5.3)
- Epic 7: Audit (Stories 7.1, 7.2)
- Epic 3: Next Step (Story 3.4)
- **Total:** 26 points

### Sprint 5 (Weeks 9-10): Polish & Notifications
- Epic 8: Notifications (all stories)
- Epic 9: Dashboard (all stories)
- Epic 4: Versioning (Story 4.3)
- **Total:** 34 points

### Sprint 6 (Weeks 11-12): QA & Pilot Prep
- Bug fixes from testing
- Performance optimization
- Pilot onboarding materials
- Deployment & monitoring setup

---

## DEFINITION OF DONE (ALL STORIES)

- [ ] Code reviewed by peer
- [ ] Unit tests written (>80% coverage for business logic)
- [ ] API endpoint tested with Postman
- [ ] Frontend tested on Chrome, Safari, iOS Safari
- [ ] Audit events emitted correctly
- [ ] RBAC enforced and tested
- [ ] Merged to main (CI passing)
- [ ] Deployed to staging
- [ ] Manual QA sign-off

---

## RISKS & DEPENDENCIES

| Risk | Impact | Mitigation |
|---|---|---|
| Puppeteer PDF generation slow | HIGH | Spike in Sprint 1; fallback to simpler lib if needed |
| S3 upload complexity | MEDIUM | Use battle-tested library (aws-sdk v3) |
| Rules engine logic errors | HIGH | Extensive unit tests; pilot validates correctness |
| Questionnaire UX confusing | MEDIUM | Prototype test before Sprint 2 |

