# Assumption Map - Sanzu

**Purpose:** Track and validate critical assumptions before V1 launch  
**Owner:** Product Team  
**Last Updated:** 2026-02-11

---

## CRITICAL ASSUMPTIONS (MUST VALIDATE BEFORE V1)

These assumptions carry **high risk** if wrong. Invalidation requires major pivot.

### A1: Families are overwhelmed by post-loss admin
**Current Evidence:** Anecdotal; documented in vision  
**Risk if Wrong:** Core value prop invalid  
**Validation Method:** 
- 15-20 family interviews (recently bereaved, <6 months)
- Quantify: hours spent, tasks abandoned, stress rating (1-10)
- Target: >70% report "overwhelmed" or "very stressed"

**Validation Questions:**
- How many hours did you spend on bureaucracy in the first month?
- What tasks felt most confusing or stressful?
- Did you need to take time off work? How much?
- What would have helped most?

**Status:** ❌ Not validated  
**Deadline:** Week 4  
**Owner:** [Product Manager]

---

### A2: Agencies lose 30%+ time to status calls and rework
**Current Evidence:** None; industry estimate  
**Risk if Wrong:** Agency pain point weaker than expected; lower willingness to pay  
**Validation Method:**
- Shadow 3-5 agencies for 5 cases each
- Time-tracking study: categorize agency tasks (calls, emails, doc review, rework)
- Target: >25% time on "low-value coordination"

**Validation Questions:**
- How many calls/emails per case on average?
- What % of documents require resubmission?
- What tasks feel repetitive or preventable?

**Status:** ❌ Not validated  
**Deadline:** Week 6  
**Owner:** [Product Manager]

---

### A3: Families will adopt a digital tool during grief
**Current Evidence:** None  
**Risk if Wrong:** Adoption barrier kills product; families default to agency-led manual process  
**Validation Method:**
- Prototype testing with 10 families (Figma clickable)
- Measure: task completion rate, time to first action, emotional response
- Target: >70% complete onboarding without frustration

**Prototype Tasks:**
- Accept invite and log in
- Answer 5 intake questions
- Upload 1 document
- View next step

**Status:** ❌ Not validated  
**Deadline:** Week 8  
**Owner:** [Designer + PM]

---

### A4: €45-65/case pricing is acceptable to agencies
**Current Evidence:** None; placeholder from competitive benchmarking  
**Risk if Wrong:** Pricing too high → no adoption; too low → unsustainable unit economics  
**Validation Method:**
- Van Westendorp Price Sensitivity Meter survey (50+ agency decision-makers)
- Follow-up: 5 agencies discuss pricing models (per-case vs subscription)
- Target: Optimal Price Point (OPP) within €40-70 range

**Survey Questions:**
- At what price would this be too expensive to consider?
- At what price would it start to seem expensive but still worth it?
- At what price is it a great value?
- At what price would it seem too cheap to trust?

**Status:** ❌ Not validated  
**Deadline:** Week 10  
**Owner:** [Product Manager + Founder]

---

### A5: PT step library is legally accurate
**Current Evidence:** None; drafted from general research  
**Risk if Wrong:** **CRITICAL** - Legal liability, compliance failures, reputation damage  
**Validation Method:**
- Contract PT-qualified legal expert (post-loss admin specialist)
- Expert review of all 18 steps in Phase 1 library
- Red/yellow/green rating for each step
- Target: >90% green (accurate), 0% red (critical error)

**Deliverables:**
- Legal validation report
- Required corrections to step library
- Recommended disclaimers/caveats

**Status:** ❌ Not validated  
**Deadline:** Week 2 (URGENT)  
**Owner:** [Founder]  
**Budget:** €2,000-€5,000

---

## IMPORTANT ASSUMPTIONS (VALIDATE DURING PILOT)

These assumptions are **medium risk**. Can be adjusted based on pilot data.

### B1: Single Editor role (1 person) is sufficient
**Current Evidence:** Product principle; simplifies RBAC  
**Risk if Wrong:** Families need multiple people to execute (e.g., siblings sharing tasks)  
**Validation Method:** Pilot observation + user feedback  
**Success Criteria:** <20% of families request multi-Editor support  
**Status:** ❌ Not validated  
**Deadline:** Pilot end (Week 16)

---

### B2: Templates reduce rework by 20%+
**Current Evidence:** None  
**Risk if Wrong:** Core value prop (efficiency) unproven  
**Validation Method:** A/B test in pilot (5 agencies manual vs 5 with templates)  
**Success Criteria:** Template group has <10% doc resubmission rate vs >30% manual  
**Status:** ❌ Not validated  
**Deadline:** Pilot end (Week 16)

---

### B3: Mobile-first UX is critical
**Current Evidence:** Assumption based on user context (families on-the-go)  
**Risk if Wrong:** Over-invest in mobile; desktop may be primary usage  
**Validation Method:** Device analytics from pilot (PostHog)  
**Success Criteria:** >60% of family sessions on mobile devices  
**Status:** ❌ Not validated  
**Deadline:** Pilot Week 8

---

### B4: Agencies will onboard in <2 hours
**Current Evidence:** Design goal; no user testing  
**Risk if Wrong:** High friction → slow adoption  
**Validation Method:** Pilot onboarding time tracking (5 agencies)  
**Success Criteria:** Median time to first case created <2 hours  
**Status:** ❌ Not validated  
**Deadline:** Pilot start (Week 12)

---

### B5: Families trust agency-branded tool more than standalone
**Current Evidence:** B2B2C strategy assumption  
**Risk if Wrong:** Families prefer direct-to-consumer offering  
**Validation Method:** Pilot NPS + interviews (ask about trust/credibility)  
**Success Criteria:** >80% families say agency recommendation was key factor  
**Status:** ❌ Not validated  
**Deadline:** Pilot end (Week 16)

---

## VALIDATION ROADMAP

### Weeks 1-4: Critical De-risking
- [ ] **Week 2:** Contract PT legal expert, start step library review
- [ ] **Week 3-4:** Conduct 15 family interviews (pain validation)
- [ ] **Week 4:** Synthesize interview findings → validate/invalidate A1

### Weeks 5-8: Solution Validation
- [ ] **Week 5-6:** Agency shadowing study (validate A2)
- [ ] **Week 7:** Build Figma clickable prototype
- [ ] **Week 8:** Prototype testing with 10 families (validate A3)

### Weeks 9-11: Business Model Validation
- [ ] **Week 9-10:** Pricing survey (50+ agencies)
- [ ] **Week 10:** Pricing follow-up interviews (5 agencies)
- [ ] **Week 11:** Finalize pricing model (validate A4)

### Weeks 12-16: Pilot Validation
- [ ] **Week 12:** Onboard 5 pilot agencies (validate B4)
- [ ] **Week 12-16:** Run pilot cases with data collection
- [ ] **Week 16:** Pilot retrospective → validate B1, B2, B3, B5

---

## DECISION RULES

### If A1 (family overwhelm) is INVALID:
- Pivot to agency-only tool (admin efficiency play)
- Redesign for Manager as primary user
- Rethink B2B2C model

### If A2 (agency pain) is WEAK (<15% time on coordination):
- Reduce pricing by 30-40%
- Focus value prop on family experience, not agency efficiency
- May need to go direct-to-consumer

### If A3 (digital adoption) is INVALID:
- Simplify onboarding to <5 min
- Add phone/SMS-based flows
- Consider "concierge" mode with agency doing data entry

### If A4 (pricing) is OUT OF RANGE:
- **Too high:** Reduce to acceptable range OR add more value (V2 features in V1)
- **Too low:** Increase pricing OR validate willingness to pay for premium tier

### If A5 (legal accuracy) has CRITICAL ERRORS:
- **STOP development** until fixed
- Contract ongoing legal advisory retainer
- Add disclaimer: "Tool provides guidance; consult legal expert"

---

## CONFIDENCE TRACKING

| Assumption | Confidence (0-100%) | Last Updated |
|---|---:|---|
| A1: Family overwhelm | 60% | 2026-02-11 |
| A2: Agency pain | 50% | 2026-02-11 |
| A3: Digital adoption | 40% | 2026-02-11 |
| A4: Pricing | 30% | 2026-02-11 |
| A5: Legal accuracy | 20% | 2026-02-11 |
| B1: Single Editor | 70% | 2026-02-11 |
| B2: Template value | 60% | 2026-02-11 |
| B3: Mobile-first | 50% | 2026-02-11 |
| B4: Onboarding speed | 65% | 2026-02-11 |
| B5: Agency trust | 70% | 2026-02-11 |

**Overall Confidence to Proceed:** 50% (increase to 80%+ before V1 launch)

