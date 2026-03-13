---
name: analytics-dashboard
description: Design and build product analytics dashboards for monitoring KPIs, user behavior, and business metrics. Covers dashboard architecture, metric definitions, visualization best practices, and alerting. Use when creating dashboards, defining KPIs, setting up monitoring, building data visualizations, or designing metric hierarchies.
triggers:
  - create dashboard
  - define KPIs
  - monitor metrics
  - build visualization
  - set up alerts
---

# Analytics Dashboard

Design effective product analytics dashboards for data-driven decisions.

---

## Dashboard Hierarchy

### Level 1: Executive Dashboard
**Audience:** C-suite, board
**Refresh:** Weekly/Monthly
**Metrics:**
- Revenue (MRR/ARR), growth rate
- Customer count, net retention
- CAC, LTV, LTV:CAC ratio
- Burn rate, runway

### Level 2: Product Health Dashboard
**Audience:** Product leadership
**Refresh:** Daily
**Metrics:**
- DAU, WAU, MAU and ratios
- Activation rate, time-to-value
- Feature adoption rates
- Retention curves (D1, D7, D30)
- NPS / CSAT scores

### Level 3: Feature Dashboard
**Audience:** Product teams
**Refresh:** Real-time / Daily
**Metrics:**
- Feature usage (daily/weekly)
- Funnel conversion per feature
- Error rates, latency
- User feedback sentiment

### Level 4: Operational Dashboard
**Audience:** Engineering, Support
**Refresh:** Real-time
**Metrics:**
- API latency (p50, p95, p99)
- Error rates by endpoint
- Infrastructure utilization
- Support ticket volume and SLA

## Metric Definition Template

```yaml
metric_name: Monthly Active Users (MAU)
definition: Unique users who performed at least 1 core action in a 28-day window
data_source: events table
core_actions:
  - create_document
  - edit_document
  - share_document
excludes:
  - bot_users
  - internal_employees
calculation: COUNT(DISTINCT user_id) WHERE event IN (core_actions)
grain: daily (rolling 28-day window)
owner: Product Analytics
SLA: updated by 6am UTC daily
```

## Visualization Best Practices

| Data Type | Best Chart | Avoid |
|-----------|-----------|-------|
| Trend over time | Line chart | Pie chart |
| Part of whole | Stacked bar, treemap | 3D pie chart |
| Comparison | Bar chart (horizontal) | Radar chart |
| Distribution | Histogram, box plot | Line chart |
| Correlation | Scatter plot | Dual-axis line |
| Single KPI | Big number + sparkline | Gauge chart |
| Funnel | Horizontal funnel | Vertical bar |
| Geo data | Choropleth map | Bubble map |

**Rules:**
- Every chart needs a title that states the insight, not just the metric
- Include time range and data freshness indicator
- Use consistent color coding across dashboards
- Limit to 6-8 charts per dashboard view
- Add comparison period (WoW, MoM, YoY) to every metric

## Alerting Framework

| Severity | Trigger | Response Time | Example |
|----------|---------|---------------|---------|
| P0 Critical | >50% drop in core metric | 15 min | Conversion drops to 0% |
| P1 High | >20% deviation from baseline | 1 hour | DAU drops 25% |
| P2 Medium | >10% deviation sustained 24h | Next business day | Activation rate dips 12% |
| P3 Low | Trend change over 7 days | Weekly review | Gradual engagement decline |

**Alert design:**
- Use rolling averages to reduce noise
- Set dynamic thresholds based on historical variance
- Include context in alerts (metric value, comparison, likely cause)
- Route to Slack/PagerDuty based on severity
