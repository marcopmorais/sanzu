# Feature Prioritization - RICE Framework

**Date:** 2026-02-11  
**Context:** V1.0 MVP scope definition (AI-native, 6-week dev timeline)

---

## RICE Scoring Formula
**RICE = (Reach × Impact × Confidence) / Effort**

- **Reach:** % of users affected per quarter (0-100)
- **Impact:** Value delivered (3=Massive, 2=High, 1=Medium, 0.5=Low, 0.25=Minimal)
- **Confidence:** Data certainty (100%=High, 80%=Medium, 50%=Low)
- **Effort:** Person-weeks (with AI: divide traditional estimate by 2)

---

## V1.0 CANDIDATE FEATURES

| Feature | Reach | Impact | Confidence | Effort | RICE | Priority |
|---|---:|---:|---:|---:|---:|---|
| **CORE (Must-Have)** |
| Case creation + RBAC | 100% | 3 | 100% | 0.5 | 600 | P0 |
| Step library + rules engine | 100% | 3 | 80% | 1.5 | 160 | P0 |
| Document vault + upload | 100% | 3 | 90% | 1.0 | 270 | P0 |
| Template engine (3 core) | 100% | 3 | 70% | 1.0 | 210 | P0 |
| Questionnaire (5 questions) | 100% | 2 | 90% | 0.5 | 360 | P0 |
| Audit logging | 100% | 2 | 100% | 0.25 | 800 | P0 |
| **POLISH (Should-Have)** |
| Dashboard (progress view) | 90% | 2 | 80% | 0.5 | 288 | P1 |
| Next-best-step indicator | 80% | 2 | 70% | 0.25 | 448 | P1 |
| Activity feed | 70% | 1 | 80% | 0.5 | 112 | P1 |
| Email notifications | 60% | 2 | 80% | 0.5 | 192 | P1 |
| **DEFERRED (Nice-to-Have)** |
| Document versioning | 40% | 1 | 70% | 0.5 | 56 | P2 |
| Multi-language (EN/PT) | 10% | 1 | 50% | 1.0 | 5 | P3 |
| Mobile app (native) | 30% | 2 | 30% | 4.0 | 4.5 | P3 |
| Offline mode | 20% | 1 | 40% | 2.0 | 4 | P3 |

---

## V1.0 FINAL SCOPE

### P0 (Must Ship)
1. Case + RBAC + Audit (0.5 weeks)
2. Questionnaire (0.5 weeks)
3. Rules engine + Step library (1.5 weeks)
4. Document vault (1.0 week)
5. Template engine - 3 templates (1.0 week)

**Total: 4.5 weeks**

### P1 (Should Ship if time allows)
6. Dashboard (0.5 weeks)
7. Next-best-step (0.25 weeks)
8. Activity feed (0.5 weeks)
9. Email notifications (0.5 weeks)

**Total: 1.75 weeks**

### Combined P0+P1: 6.25 weeks → **6-week sprint**

---

## BUILD VS BUY VS PARTNER

| Capability | Build | Buy | Partner | Decision | Rationale |
|---|:---:|:---:|:---:|---|---|
| **Auth + RBAC** | ❌ | ✅ | ❌ | **Buy** (Supabase) | Commodity, focus on product |
| **Database** | ❌ | ✅ | ❌ | **Buy** (Supabase) | Managed PostgreSQL |
| **File storage** | ❌ | ✅ | ❌ | **Buy** (S3/Supabase) | Commodity |
| **Email** | ❌ | ✅ | ❌ | **Buy** (Resend) | Reliable, cheap |
| **PDF generation** | ✅ | ❌ | ❌ | **Build** | Core IP, templates are product |
| **Rules engine** | ✅ | ❌ | ❌ | **Build** | Core differentiation |
| **Step library** | ✅ | ❌ | ⚠️ | **Build+Partner** | Build engine, legal expert validates |
| **Analytics** | ❌ | ✅ | ❌ | **Buy** (PostHog) | Open-source, GDPR-friendly |
| **Monitoring** | ❌ | ✅ | ❌ | **Buy** (Sentry) | Error tracking standard |
| **Bank integrations** | ❌ | ❌ | ✅ | **Partner** (V3+) | Requires formal partnerships |

**Philosophy:** Buy commodity, build differentiation.

---

## ASSUMPTION VALIDATION (Revised without interviews)

### Skip Full Research, Use Proxy Validation

**A1: Family overwhelm**
- Proxy: Agency feedback (ask 3-5 agencies anecdotally)
- Confidence: 60% → Good enough for V1

**A2: Agency pain**
- Proxy: Agency interest in pilot (conversion rate)
- Target: 5/20 agencies agree to pilot (25%)

**A3: Digital adoption**
- Proxy: Pilot metrics (onboarding completion rate)
- Defer validation to pilot

**A4: Pricing**
- Proxy: Ask pilot agencies willingness-to-pay
- Test €50/case during pilot

**A5: Legal accuracy** ← CRITICAL, CANNOT SKIP
- Action: Contract PT legal expert (Week 1-2)

---

## REVISED TIMELINE (Without Interviews)

**Week 1-2: Foundation**
- Legal expert review (critical)
- Tech stack setup
- ~~User interviews~~ → Agency conversations (informal)

**Week 3-4: Prototype**
- Figma prototype (optional, use v0 to skip)
- ~~Usability testing~~ → Build directly with AI

**Week 5-10: Development** (6 weeks)
- Sprint as planned with AI tools

**Week 11-14: Pilot** (4 weeks)
- Learn from real usage
- Validate assumptions in production

**Total: 14 weeks → 10-12 weeks** (skip research)

---

## RISK ASSESSMENT (No Research)

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Build wrong features | MEDIUM | HIGH | Tight pilot feedback loop |
| Pricing too high/low | MEDIUM | MEDIUM | Test in pilot, adjust quickly |
| UX confusing | MEDIUM | MEDIUM | Use v0 best practices, iterate |
| Legal errors | HIGH | CRITICAL | Expert validation (non-negotiable) |

**Decision:** Acceptable risk for speed. Pilot = research phase.

---

## NEXT ACTIONS

**This Week:**
1. ✅ Contract PT legal expert
2. ✅ Set up Supabase + GitHub
3. ✅ Informal agency calls (5 conversations, gauge interest)
4. ✅ Finalize V1 scope (P0 features locked)

**Week 2:**
5. ✅ Legal review complete
6. ✅ Start Sprint 1 (Auth + RBAC)

