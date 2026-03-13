# Phase 4: Solution Discovery - Kickoff Plan

**Phase Start:** 2026-02-14
**Objective:** Validate Post-MVP solutions through prototyping, technical spikes, and user testing before full implementation

---

## Phase 4 Overview

**Purpose:** Bridge the gap between Opportunity Shaping (Phase 3) and full Product Definition/Implementation by:
- Testing solution hypotheses with real users
- Validating technical feasibility of Post-MVP features (Epics 9-13)
- De-risking implementation through experimentation
- Gathering evidence to inform design and delivery decisions

**Key Outputs:**
1. Validated prototypes (Figma + code spikes)
2. User testing insights (5-10 bereaved families)
3. Technical feasibility assessments
4. Design System Foundation validation
5. Go/No-Go recommendations for Post-MVP features

---

## Solution Discovery Experiments (Priority Order)

### Experiment 1: Design System Foundation Validation (CRITICAL)

**Context:** Party mode evaluation identified critical gaps:
- Component library: D grade (blocking Post-MVP)
- Typography system: C grade (undefined)
- Content strategy: F grade (missing)
- Accessibility: C- grade (not validated)

**Research Completed:**
- ✅ Comprehensive domain research on Design System Foundation for Grief-Aware UX
- ✅ Market analysis: USD 115B design systems market, no grief-aware systems exist
- ✅ Regulatory requirements: April 24, 2026 WCAG 2.1 AA deadline for government agencies
- ✅ Technology trends: AI copilots, emotionally adaptive UX, W3C Design Tokens

**Hypothesis:**
- Extending existing design system (Chakra UI/Mantine) with grief-aware components can deliver WCAG 2.1 AA compliance in 4-8 weeks vs. 12+ weeks building from scratch
- Emotionally adaptive components (context-aware UI density, empathetic microcopy) will improve user sentiment by 20%+ in testing with bereaved families

**Validation Method:**
1. **Technical Spike (Week 1-2):**
   - Prototype 5 core components using Chakra UI base
   - Implement WCAG 2.1 AA compliance for prototypes
   - Test automated accessibility testing pipeline (axe DevTools)
   - Validate W3C Design Tokens integration

2. **User Testing (Week 3-4):**
   - Test emotionally adaptive components with 5-10 bereaved families
   - Validate grief-aware microcopy (empathetic error messages, reduced-density modes)
   - Measure: user sentiment, task completion, cognitive load indicators

**Success Criteria:**
- ✅ 5/5 components pass automated accessibility scans (WCAG 2.1 AA)
- ✅ Manual testing (keyboard navigation, screen reader) finds <3 critical issues per component
- ✅ User sentiment improvement ≥20% vs. generic UI in controlled testing
- ✅ 8/10 users complete "find next step" task without confusion

**Decision Point:**
- **GO:** Proceed with Extend Chakra UI approach, dedicate 2 FTE developers Q1 2026
- **NO-GO:** Revisit build vs. buy decision, reassess Post-MVP timeline

**Artifacts:**
- `design_system_technical_spike_report.md`
- `grief_aware_ux_user_testing_findings.md`
- `design_system_implementation_decision.md`

---

### Experiment 2: Figma Prototype - Family Dashboard (Epic 9)

**Hypothesis:**
Families can understand case progress and identify next actions without training when presented with:
- Plain-language glossary for legal/bureaucratic terms (Epic 9.1)
- Reason-coded blocked states with guided recovery actions (Epic 9.2)
- Clear visual hierarchy and progress indicators

**Validation Method:**
- **Prototype:** Figma clickable prototype (Family Dashboard with Epic 9 features)
- **Participants:** 10 bereaved families (recruited via pilot agency partnerships)
- **Tasks:**
  1. Find the next action you need to take
  2. Understand why a step is blocked
  3. Identify what action will unblock the step
  4. Access plain-language explanation for a legal term
- **Moderated usability testing:** 30-45 min sessions, recorded

**Success Criteria:**
- ✅ 8/10 complete "find next step" task without help
- ✅ 7/10 correctly identify recovery action for blocked step
- ✅ 9/10 understand plain-language glossary explanations
- ✅ Average task completion time <2 minutes
- ✅ SUS (System Usability Scale) score ≥70

**Timeline:** Week 2-3

**Artifacts:**
- `family_dashboard_figma_prototype.fig` (link)
- `epic_9_usability_testing_report.md`
- `plain_language_glossary_validation.md`

---

### Experiment 3: Template Quality Test (Epic 13.1)

**Hypothesis:**
AI-generated evidence request drafts (Epic 13.1) reduce agency review time by 50% and maintain <10% error rate compared to manual creation.

**Validation Method:**
- **Test:** 5 pilot agencies test AI-generated evidence request templates for 10 real cases each
- **Comparison:** AI-generated vs. manually created evidence requests
- **Metrics:**
  - Review time per template (target: <5 min)
  - Error rate (missing info, incorrect tone, legal inaccuracies)
  - Acceptance rate (% templates sent without major edits)

**Success Criteria:**
- ✅ Review time <5 minutes (50% reduction vs. manual baseline ~10 min)
- ✅ Error rate <10%
- ✅ Acceptance rate ≥70% (sent with minor or no edits)
- ✅ Agency satisfaction score ≥4/5

**Timeline:** Week 3-4

**Artifacts:**
- `epic_13_ai_template_quality_report.md`
- `evidence_request_error_analysis.md`

---

### Experiment 4: Rules Engine Accuracy (Epic 9.2 + 10)

**Hypothesis:**
The rules engine generates correct recovery plans for 90% of blocked cases and correctly applies agency playbooks (Epic 10) to case creation.

**Validation Method:**
- **Test:** Run 20 real historical cases through the rules engine
- **Comparison:** Engine-generated plan vs. agency manual plan
- **Metrics:**
  - Step match percentage (% of steps correctly identified)
  - Missing critical steps (steps engine failed to include)
  - Incorrect dependencies or ordering

**Success Criteria:**
- ✅ >90% step match rate
- ✅ <5% missing critical steps
- ✅ 0 incorrect legal/compliance steps (CRITICAL: legal accuracy)

**Timeline:** Week 5-6

**Artifacts:**
- `rules_engine_accuracy_validation.md`
- `playbook_application_test_results.md`

---

### Experiment 5: Platform Mission Control UX (Epic 12)

**Hypothesis:**
Admin users can diagnose issues and execute closed-loop remediation in ≤2 minutes using:
- Fleet posture dashboards with segmentation (Epic 12.1)
- Admin queues with event stream drilldown (Epic 12.2)
- Closed-loop remediation with audit notes (Epic 12.3)

**Validation Method:**
- **Prototype:** Low-fidelity wireframes or Figma prototype of Epic 12 surfaces
- **Participants:** 3-5 internal Sanzu administrators (simulated)
- **Scenarios:**
  1. Identify tenant with onboarding stuck >7 days
  2. Drill down into event stream to diagnose root cause
  3. Execute remediation action with audit note
- **Task completion time and accuracy measured**

**Success Criteria:**
- ✅ Time-to-diagnosis ≤60 seconds
- ✅ Time-to-remediation ≤2 minutes (including audit note)
- ✅ 100% of test scenarios result in correct remediation action
- ✅ No safety violations (e.g., skipping audit notes, incorrect tenant targeted)

**Timeline:** Week 4-5

**Artifacts:**
- `epic_12_mission_control_wireframes.fig` (or Figma link)
- `platform_admin_ux_validation.md`

---

## Technical Feasibility Spikes

### Spike 1: AI Copilot Integration (Epic 13)

**Question:** Can we integrate AI drafting capabilities with acceptable latency, cost, and safety?

**Approach:**
- Test Claude API or OpenAI GPT-4 for evidence request generation
- Measure: response time, token cost per request, output quality
- Validate: explainability ("based on..."), role-safety (no private data in prompts)

**Success Criteria:**
- Response time <3 seconds for 90% of requests
- Cost <€0.10 per draft
- Output passes grief counselor review (no insensitive language)

**Timeline:** Week 2-3

**Artifacts:**
- `ai_copilot_technical_spike.md`

---

### Spike 2: Design Tokens + Theming (Epic 10.2)

**Question:** Can W3C Design Tokens support agency-specific theming while maintaining grief-aware patterns and WCAG compliance?

**Approach:**
- Implement W3C Design Tokens 2025.10 spec
- Create 2 agency theme variants (different colors, logos)
- Validate: theming doesn't break accessibility, grief-aware patterns preserved

**Success Criteria:**
- Theming works without code changes (tokens only)
- All themed components pass WCAG 2.1 AA scans
- Theme switching <100ms latency

**Timeline:** Week 1-2

**Artifacts:**
- `design_tokens_theming_spike.md`

---

### Spike 3: Reason-Coded Event Taxonomy (Epic 11.2)

**Question:** Can we emit and aggregate reason-coded events without performance degradation or privacy violations?

**Approach:**
- Prototype event emission for canonical taxonomy
- Test: event ingestion rate, query performance, payload sanitization
- Validate: no sensitive data in analytics, aggregation performance acceptable

**Success Criteria:**
- Event ingestion ≥1000 events/sec
- Dashboard query response time <500ms (P95)
- Zero sensitive data leaks in test payload review

**Timeline:** Week 3-4

**Artifacts:**
- `telemetry_performance_spike.md`

---

## User Testing Plan

### Participant Recruitment

**Target:** 5-10 bereaved families (past 6 months) for Experiments 1-2

**Recruitment Strategy:**
1. Partner with pilot agencies for referrals
2. Compensate participants (€50-€100 per session)
3. Trauma-informed consent process (opt-out anytime, no pressure)

**Inclusion Criteria:**
- Experienced death of family member in past 6 months
- Involved in post-loss bureaucracy (death registration, estate, etc.)
- Basic digital literacy (able to use email, web browser)
- Located in Lisbon/Porto area (for in-person sessions if needed)

**Exclusion Criteria:**
- Currently in acute grief crisis (ethical concern)
- No experience with post-loss admin (not target user)

---

### Testing Protocol (Trauma-Informed)

**Session Structure:**
1. **Introduction (5 min):** Build rapport, explain purpose, obtain consent
2. **Context gathering (5 min):** Understand participant's experience with post-loss admin
3. **Prototype testing (20 min):** Task-based usability testing
4. **Debrief (10 min):** Open-ended feedback, emotional check-in
5. **Close (5 min):** Thank participant, provide resources (grief support contacts)

**Ethical Safeguards:**
- Sessions moderated by trained facilitator (ideally with grief counseling background)
- Participant can pause or stop anytime
- Avoid questions that trigger re-traumatization
- Provide grief support resource list at end of session

---

## A/B Test Plan (Post-Discovery, During Pilot)

### Test 1: Grief-Aware Microcopy vs. Generic Copy

**Hypothesis:** Empathetic microcopy reduces abandonment and increases task completion.

**Variants:**
- **A (Control):** Generic UI copy ("Error: Upload failed")
- **B (Treatment):** Grief-aware copy ("We couldn't upload the death certificate. This happens sometimes—let's try again.")

**Metrics:**
- Task completion rate
- Time on task
- Error recovery rate
- User sentiment (post-task survey)

**Sample Size:** 100 users per variant (200 total)

**Timeline:** Week 6-8 (during pilot)

---

### Test 2: Emotionally Adaptive UI Density

**Hypothesis:** Context-aware UI density (fewer options when user overwhelmed) improves task completion.

**Variants:**
- **A (Control):** Static UI (all options visible)
- **B (Treatment):** Adaptive UI (reduced options based on user state)

**Metrics:**
- Task completion rate
- Clicks to completion
- User-reported cognitive load (NASA-TLX scale)

**Sample Size:** 100 users per variant (200 total)

**Timeline:** Week 6-8 (during pilot)

---

## Phase 4 Timeline (6 Weeks)

| Week | Focus | Experiments/Spikes | Deliverables |
|------|-------|-------------------|--------------|
| **Week 1-2** | Design System + Technical Foundation | Spike 1 (AI Copilot), Spike 2 (Design Tokens), Experiment 1 (Design System validation) | Technical spike reports, component prototypes |
| **Week 3** | User Testing Prep + Family Dashboard | Experiment 2 (Figma prototype testing), Spike 3 (Telemetry) | Usability testing report, telemetry spike report |
| **Week 4** | Template Quality + Mission Control | Experiment 3 (AI templates), Experiment 5 (Admin UX) | Template quality report, mission control wireframes |
| **Week 5** | Rules Engine Validation | Experiment 4 (Rules accuracy) | Rules engine validation report |
| **Week 6** | Synthesis + Decision | Consolidate findings, Go/No-Go decisions | Phase 4 findings report, Post-MVP implementation decision |

---

## Success Criteria (Phase 4 Exit)

**Must Achieve:**
- ✅ Design System approach validated (extend Chakra UI decision confirmed)
- ✅ User testing with ≥5 bereaved families completed
- ✅ AI copilot technical feasibility confirmed (latency, cost, safety acceptable)
- ✅ WCAG 2.1 AA compliance achievable with chosen design system
- ✅ Go/No-Go decision for Post-MVP Epics 9-13 implementation

**Should Achieve:**
- ✅ Rules engine accuracy ≥90%
- ✅ Template quality error rate <10%
- ✅ User sentiment improvement ≥20% with grief-aware UX

**Nice to Have:**
- ✅ A/B test plans validated with pilot agencies
- ✅ Platform mission control UX wireframes tested with admins

---

## Decision Framework (Go/No-Go for Post-MVP)

### GO Criteria:
1. Design System validation successful (WCAG compliance achievable, user sentiment positive)
2. AI copilot technical feasibility confirmed (cost <€0.10/draft, latency <3s)
3. User testing shows ≥70% task completion for Epic 9 features
4. No critical safety/legal/compliance blockers identified

### NO-GO Criteria:
1. Design System cannot achieve WCAG 2.1 AA by April 24, 2026 deadline
2. AI copilot cost or latency unacceptable for production use
3. User testing shows confusion or negative sentiment with Epic 9 features
4. Legal/compliance risks identified that cannot be mitigated

### DEFER Criteria:
1. Technical feasibility proven but user validation weak → iterate prototypes, re-test
2. Cost/latency acceptable but safety concerns → add guardrails, constrain scope

---

## Next Actions (Week 1)

**Immediate:**
1. ✅ Create 04-Solution-Discovery/ folder structure
2. ✅ Update Sanzu PDLC status tracking

**This Week:**
- [ ] Recruit 5-10 bereaved families for user testing (via pilot agency partnerships)
- [ ] Start Design System technical spike (Chakra UI + WCAG compliance)
- [ ] Start AI copilot integration spike (Claude API or GPT-4)
- [ ] Create Figma prototype for Experiment 2 (Family Dashboard with Epic 9 features)
- [ ] Schedule user testing sessions (Week 3 target)

**Week 2:**
- [ ] Complete technical spikes (Design System, AI Copilot, Design Tokens)
- [ ] Finalize Figma prototypes for user testing
- [ ] Conduct first round of user testing (3-5 participants)

---

## Risk Mitigation

**Risk 1: Cannot recruit enough bereaved families for user testing**
- **Mitigation:** Partner with multiple pilot agencies, offer higher compensation (€100 vs. €50), allow remote sessions
- **Contingency:** Test with fewer participants (3-5 minimum) and supplement with agency staff proxy testing

**Risk 2: Design System spike reveals WCAG compliance unachievable by April deadline**
- **Mitigation:** Start spike in Week 1 (early detection), have backup "buy commercial design system" option ready
- **Contingency:** Defer Post-MVP Epics 9-13, focus on MVP stabilization and pilot success

**Risk 3: User testing reveals negative sentiment or confusion with grief-aware UX**
- **Mitigation:** Trauma-informed testing protocol, grief counselor review of prototypes before testing
- **Contingency:** Iterate prototypes based on feedback, re-test with new participants in Week 4-5

**Risk 4: AI copilot cost or latency unacceptable for production**
- **Mitigation:** Test multiple providers (Claude, GPT-4, open-source models), optimize prompts for token efficiency
- **Contingency:** Reduce AI scope (fewer features auto-generated), use templates + manual creation hybrid approach

---

**Phase 4 Owner:** Product Manager (you)
**Phase 4 Duration:** 6 weeks (Feb 14 - Mar 28, 2026)
**Phase 4 Budget:** €5,000 (user testing compensation, prototype tools, API credits)

**Phase 4 Output:** Go/No-Go decision for Post-MVP Epics 9-13 implementation with validated prototypes and user evidence.
