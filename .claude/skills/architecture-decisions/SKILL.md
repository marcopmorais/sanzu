---
name: architecture-decisions
description: Create and manage Architecture Decision Records (ADRs) for documenting technical decisions. Covers ADR templates, decision frameworks, trade-off analysis, and decision logs. Use when documenting architecture decisions, evaluating technical trade-offs, creating ADRs, or reviewing past decisions.
triggers:
  - architecture decision
  - ADR
  - technical decision
  - trade-off analysis
  - decision record
---

# Architecture Decisions

Document, evaluate, and track architecture decisions using ADRs.

---

## ADR Template

```markdown
# ADR-[NNN]: [Short Title]

## Status
[Proposed | Accepted | Deprecated | Superseded by ADR-XXX]

## Date
YYYY-MM-DD

## Context
What is the issue we are facing? What forces are at play?
Include technical constraints, business requirements, team capabilities,
timeline pressures, and any relevant prior decisions.

## Decision
What is the change that we are proposing and/or doing?
State the decision in active voice: "We will..."

## Consequences

### Positive
- [Benefit 1]
- [Benefit 2]

### Negative
- [Trade-off 1]
- [Trade-off 2]

### Risks
- [Risk 1 with mitigation]

## Alternatives Considered

### Option A: [Name]
- Pros: ...
- Cons: ...
- Why rejected: ...

### Option B: [Name]
- Pros: ...
- Cons: ...
- Why rejected: ...
```

## Decision Framework

### Weighted Scoring Matrix

| Criteria | Weight | Option A | Option B | Option C |
|----------|--------|----------|----------|----------|
| Performance | 25% | 4 (1.0) | 3 (0.75) | 5 (1.25) |
| Maintainability | 20% | 3 (0.6) | 5 (1.0) | 2 (0.4) |
| Team expertise | 20% | 5 (1.0) | 2 (0.4) | 3 (0.6) |
| Cost | 15% | 3 (0.45) | 4 (0.6) | 2 (0.3) |
| Time to implement | 10% | 4 (0.4) | 3 (0.3) | 2 (0.2) |
| Scalability | 10% | 3 (0.3) | 4 (0.4) | 5 (0.5) |
| **Total** | **100%** | **3.75** | **3.45** | **3.25** |

Score each option 1-5 per criteria, multiply by weight.

### Reversibility Assessment

| Decision Type | Reversibility | Approach |
|--------------|---------------|----------|
| **One-way door** | Hard to reverse (database choice, API contract) | Invest heavily in analysis, seek consensus |
| **Two-way door** | Easy to reverse (UI library, internal tool) | Decide quickly, iterate based on feedback |

### DACI Roles
- **Driver:** Owns the decision process and timeline
- **Approver:** Has final say (typically 1 person)
- **Contributors:** Provide input and expertise
- **Informed:** Need to know the outcome

## Common Architecture Decision Categories

| Category | Key Decisions | Typical Trade-offs |
|----------|--------------|-------------------|
| **Data storage** | SQL vs NoSQL, single vs distributed | Consistency vs availability, cost vs scale |
| **API design** | REST vs GraphQL vs gRPC | Simplicity vs flexibility, performance vs ease |
| **Frontend** | SPA vs SSR vs hybrid | SEO vs interactivity, bundle size vs DX |
| **Infrastructure** | Cloud vs hybrid, serverless vs containers | Cost vs control, vendor lock-in vs speed |
| **Integration** | Sync vs async, events vs API | Consistency vs decoupling, latency vs reliability |
| **Security** | Auth approach, data encryption scope | UX friction vs security, cost vs compliance |

## ADR File Organization

```
docs/
  adr/
    0001-use-postgresql-as-primary-database.md
    0002-adopt-event-driven-architecture.md
    0003-choose-react-for-frontend.md
    0004-implement-cqrs-for-reporting.md
    README.md  (index with status summary)
```

## Decision Log Quick Reference

Maintain a living index:

| # | Decision | Status | Date | Impact |
|---|----------|--------|------|--------|
| 001 | Use PostgreSQL | Accepted | 2025-01-15 | High |
| 002 | Event-driven architecture | Accepted | 2025-02-01 | High |
| 003 | React + Next.js frontend | Accepted | 2025-02-10 | Medium |
