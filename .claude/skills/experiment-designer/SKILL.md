---
name: experiment-designer
description: Design rigorous product experiments including A/B tests, multivariate tests, and feature flags. Covers hypothesis formation, sample size calculation, statistical significance, and experiment analysis. Use when designing experiments, planning A/B tests, calculating sample sizes, analyzing experiment results, or setting up feature flag rollouts.
triggers:
  - design experiment
  - plan A/B test
  - calculate sample size
  - analyze experiment results
  - feature flag rollout
---

# Experiment Designer

Design, run, and analyze product experiments with statistical rigor.

---

## Workflow

### 1. Hypothesis Formation
Structure every experiment as:
- **Null Hypothesis (H0):** No difference between control and variant
- **Alternative Hypothesis (H1):** The change produces a measurable effect
- **Primary Metric:** One metric that determines success/failure
- **Guardrail Metrics:** Metrics that must not degrade

### 2. Experiment Types

| Type | When to Use | Complexity |
|------|-------------|------------|
| A/B Test | Single change, one variant | Low |
| A/B/n Test | Multiple variants of same change | Medium |
| Multivariate (MVT) | Multiple independent changes | High |
| Bandit | Optimize during experiment | Medium |
| Switchback | Time-based alternation | High |
| Feature Flag | Gradual rollout with kill switch | Low |

### 3. Sample Size Calculation

**Required inputs:**
- Baseline conversion rate (p1)
- Minimum detectable effect (MDE) - typically 2-5% relative
- Statistical significance level (alpha) - typically 0.05
- Statistical power (1-beta) - typically 0.80

**Formula (per variant):**
```
n = (Z_alpha/2 + Z_beta)^2 * (p1(1-p1) + p2(1-p2)) / (p2 - p1)^2
```

**Quick reference table (alpha=0.05, power=0.80):**

| Baseline Rate | 5% MDE | 10% MDE | 20% MDE |
|--------------|--------|---------|---------|
| 1% | 380,000 | 96,000 | 24,000 |
| 5% | 70,000 | 18,000 | 4,600 |
| 10% | 33,000 | 8,400 | 2,200 |
| 20% | 15,000 | 3,900 | 1,000 |
| 50% | 6,000 | 1,600 | 400 |

### 4. Experiment Duration

```
Duration (days) = Required sample size per variant * Number of variants / Daily traffic eligible for experiment
```

**Rules of thumb:**
- Minimum 1 full business cycle (7 days for consumer, 14 for B2B)
- Maximum 4-6 weeks to avoid novelty/fatigue effects
- Account for weekday/weekend traffic variation

### 5. Randomization Unit
- **User-level:** Most common, prevents contamination
- **Session-level:** For testing transient UI changes
- **Page-level:** For content experiments
- **Cluster-level:** For network effects (marketplace, social)

### 6. Analysis Framework

**Pre-analysis checks:**
- [ ] Sample ratio mismatch (SRM) test - chi-squared < 0.001 = problem
- [ ] Novelty/primacy effects - check day-over-day trends
- [ ] Segment balance - demographics similar across variants

**Statistical tests:**
- **Proportions:** Z-test or Chi-squared test
- **Continuous metrics:** Two-sample t-test or Welch's t-test
- **Revenue/order metrics:** Mann-Whitney U (non-parametric)
- **Multiple comparisons:** Bonferroni or Benjamini-Hochberg correction

**Decision matrix:**

| p-value | Practical significance | Decision |
|---------|----------------------|----------|
| < 0.05 | Meaningful lift | Ship it |
| < 0.05 | Trivial lift | Consider cost/complexity |
| >= 0.05 | - | Do not ship (inconclusive) |

### 7. Experiment Doc Template

```markdown
## Experiment: [Name]
**Owner:** [Name]
**Date:** [Start] - [End]
**Status:** [Planning / Running / Analyzing / Decided]

### Hypothesis
If we [change], then [metric] will [improve/decrease] by [amount]
because [reasoning].

### Design
- **Type:** A/B Test
- **Primary metric:** [metric name]
- **Guardrail metrics:** [list]
- **Traffic allocation:** [%] per variant
- **Target sample size:** [n] per variant
- **Expected duration:** [days]
- **Randomization unit:** User

### Variants
| Variant | Description |
|---------|-------------|
| Control | Current experience |
| Treatment | [Description of change] |

### Results
| Metric | Control | Treatment | Delta | p-value | CI (95%) |
|--------|---------|-----------|-------|---------|----------|
| Primary | | | | | |
| Guardrail 1 | | | | | |

### Decision
[Ship / Don't Ship / Iterate]
**Rationale:** [Why]
```

### 8. Common Pitfalls
- **Peeking:** Checking results before reaching sample size inflates false positive rate
- **Multiple testing:** Testing many metrics without correction leads to spurious results
- **Selection bias:** Non-random assignment or self-selection into treatment
- **Survivorship bias:** Only analyzing users who completed the flow
- **Interaction effects:** Running overlapping experiments on same users
- **Underpowered tests:** Sample size too small to detect real effects
