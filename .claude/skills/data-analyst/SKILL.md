---
name: data-analyst
description: Product data analysis toolkit for querying, analyzing, and visualizing product data. Covers SQL query building, cohort analysis, funnel analysis, retention curves, segmentation, and data storytelling. Use when analyzing product data, building SQL queries, creating cohort analyses, performing funnel analysis, building retention reports, or segmenting users.
triggers:
  - analyze data
  - write SQL query
  - cohort analysis
  - funnel analysis
  - retention analysis
  - segment users
---

# Data Analyst

Analyze product data to drive decisions across the PDLC.

---

## Core Analysis Types

### 1. Funnel Analysis

**Template SQL (event-based):**
```sql
WITH funnel AS (
  SELECT
    user_id,
    MAX(CASE WHEN event = 'page_view' THEN 1 ELSE 0 END) AS step_1_view,
    MAX(CASE WHEN event = 'add_to_cart' THEN 1 ELSE 0 END) AS step_2_cart,
    MAX(CASE WHEN event = 'checkout_start' THEN 1 ELSE 0 END) AS step_3_checkout,
    MAX(CASE WHEN event = 'purchase' THEN 1 ELSE 0 END) AS step_4_purchase
  FROM events
  WHERE event_date BETWEEN :start_date AND :end_date
  GROUP BY user_id
)
SELECT
  COUNT(*) AS total_users,
  SUM(step_1_view) AS viewed,
  SUM(step_2_cart) AS added_to_cart,
  SUM(step_3_checkout) AS started_checkout,
  SUM(step_4_purchase) AS purchased,
  ROUND(100.0 * SUM(step_2_cart) / NULLIF(SUM(step_1_view), 0), 1) AS view_to_cart_pct,
  ROUND(100.0 * SUM(step_4_purchase) / NULLIF(SUM(step_1_view), 0), 1) AS overall_conversion_pct
FROM funnel;
```

### 2. Cohort Retention Analysis

```sql
WITH user_cohorts AS (
  SELECT
    user_id,
    DATE_TRUNC('month', first_activity_date) AS cohort_month
  FROM users
),
activity AS (
  SELECT
    user_id,
    DATE_TRUNC('month', activity_date) AS activity_month
  FROM events
  GROUP BY 1, 2
)
SELECT
  uc.cohort_month,
  DATEDIFF('month', uc.cohort_month, a.activity_month) AS month_number,
  COUNT(DISTINCT a.user_id) AS active_users,
  COUNT(DISTINCT a.user_id)::FLOAT / MAX(cohort_size.cnt) AS retention_rate
FROM user_cohorts uc
JOIN activity a ON uc.user_id = a.user_id
JOIN (
  SELECT cohort_month, COUNT(*) AS cnt
  FROM user_cohorts GROUP BY 1
) cohort_size ON uc.cohort_month = cohort_size.cohort_month
GROUP BY 1, 2
ORDER BY 1, 2;
```

### 3. Segmentation Framework

| Dimension | Segments | Use Case |
|-----------|----------|----------|
| Behavioral | Power/Core/Casual/Dormant | Feature adoption analysis |
| Lifecycle | New/Active/At-risk/Churned | Retention campaigns |
| Value | High LTV / Low LTV | Monetization strategy |
| Demographic | By plan, geo, company size | Market sizing |
| Engagement | DAU/WAU/MAU ratios | Product health |

**RFM Scoring (Recency, Frequency, Monetary):**
```sql
SELECT
  user_id,
  NTILE(5) OVER (ORDER BY days_since_last_activity DESC) AS recency_score,
  NTILE(5) OVER (ORDER BY activity_count ASC) AS frequency_score,
  NTILE(5) OVER (ORDER BY total_revenue ASC) AS monetary_score
FROM user_summary;
```

### 4. Key Product Metrics

| Category | Metric | Formula |
|----------|--------|---------|
| Engagement | DAU/MAU | Daily active / Monthly active |
| Engagement | L7/L28 | Days active in 7 / Days active in 28 |
| Retention | D1/D7/D30 | Users returning on day N / Cohort size |
| Growth | Quick Ratio | (New + Resurrected) / (Churned + Contracted) |
| Revenue | ARPU | Revenue / Active users |
| Revenue | LTV | ARPU * Avg lifetime |
| Activation | Activation Rate | Users reaching "aha moment" / Signups |

### 5. Data Storytelling Structure

1. **Context:** What question are we answering and why?
2. **Key finding:** One-sentence headline insight
3. **Evidence:** 2-3 supporting data points with visualizations
4. **So what:** Business implication of the finding
5. **Recommendation:** Specific next action with expected impact

### 6. SQL Performance Patterns
- Use `EXPLAIN ANALYZE` before running heavy queries
- Prefer `EXISTS` over `IN` for subqueries
- Use window functions instead of self-joins
- Partition large tables by date
- Index columns used in WHERE and JOIN clauses
