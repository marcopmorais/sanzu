---
name: metrics-tracking
description: Define, instrument, and track product metrics across the PDLC. Covers metric taxonomies (AARRR, HEART, NSM), instrumentation plans, event schemas, and goal setting with OKR alignment. Use when defining metrics, creating tracking plans, designing event schemas, setting goals, or building metric trees.
triggers:
  - define metrics
  - create tracking plan
  - design event schema
  - set OKRs
  - north star metric
  - AARRR framework
---

# Metrics Tracking

Define, instrument, and track the right metrics at every PDLC stage.

---

## Metric Frameworks

### AARRR (Pirate Metrics)
| Stage | Question | Example Metrics |
|-------|----------|-----------------|
| **Acquisition** | How do users find us? | Signups, visits, channel attribution |
| **Activation** | Do they have a great first experience? | Onboarding completion, time-to-value, "aha moment" rate |
| **Retention** | Do they come back? | D1/D7/D30 retention, DAU/MAU ratio |
| **Revenue** | Do they pay? | Conversion rate, ARPU, MRR, expansion revenue |
| **Referral** | Do they tell others? | NPS, referral rate, viral coefficient |

### HEART Framework (Google)
| Dimension | Signal | Metric |
|-----------|--------|--------|
| **Happiness** | Satisfaction surveys | NPS, CSAT, SUS score |
| **Engagement** | Depth of interaction | Sessions/week, actions/session, feature usage |
| **Adoption** | New feature uptake | % users trying feature in first 7 days |
| **Retention** | Continued use | Week-over-week retention rate |
| **Task Success** | Completion efficiency | Task completion rate, time-on-task, error rate |

### North Star Metric (NSM)
A single metric that best captures the core value delivered to customers.

**Criteria for a good NSM:**
1. Reflects customer value (not vanity)
2. Leading indicator of revenue
3. Measurable within product
4. Actionable by teams

**Examples:**
- Spotify: Time spent listening
- Airbnb: Nights booked
- Slack: Messages sent in team channels
- Figma: Weekly active editors

## Event Tracking Plan Template

```yaml
event_name: document_created
description: User creates a new document
category: core_action
properties:
  - name: document_type
    type: string
    enum: [blank, template, import]
    required: true
  - name: template_id
    type: string
    required: false
  - name: source
    type: string
    enum: [dashboard, quick_action, api]
    required: true
triggers:
  - frontend: on successful document creation API response
  - backend: on document row inserted (server-side validation)
implemented_in: v2.3.0
owner: Core Product Team
```

## Instrumentation Checklist

- [ ] Define event taxonomy (object_action format: `document_created`, `button_clicked`)
- [ ] Standardize property naming (snake_case, consistent enums)
- [ ] Set up identity resolution (anonymous → identified user)
- [ ] Implement server-side validation for critical events
- [ ] Add QA environment for event testing
- [ ] Create event documentation and data dictionary
- [ ] Set up event volume monitoring (detect instrumentation breakage)
- [ ] Define PII handling rules (hash, redact, or exclude)

## Goal Setting with Metrics

### Input vs Output Metrics
| Type | Definition | Example |
|------|-----------|---------|
| **Input** (leading) | Actions teams control | Feature releases, experiments run |
| **Output** (lagging) | Results we want | Revenue, retention rate |

### Metric Tree
```
North Star: Weekly Active Creators
├── New creator activation rate
│   ├── Signup rate
│   │   ├── Landing page conversion
│   │   └── Referral signups
│   └── Onboarding completion rate
│       ├── Step 1 completion
│       └── First creation within 24h
├── Existing creator retention (W1)
│   ├── Feature engagement depth
│   └── Collaboration rate
└── Reactivated creators
    ├── Email campaign click rate
    └── Return visit rate
```

## Data Quality Checks
- **Completeness:** No null values in required fields
- **Freshness:** Events arriving within SLA (typically < 5 min)
- **Volume:** Daily event counts within 2 standard deviations of rolling average
- **Accuracy:** Spot-check against source of truth (e.g., database vs event stream)
- **Consistency:** Same event definition produces same count across tools
