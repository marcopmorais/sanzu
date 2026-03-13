# Portfolio Governance - Sanzu PDLC 3.5

Owner: Portfolio Council
Version: 3.5.1
Review cadence: Monthly

## Portfolio Hierarchy
`Company -> Product Line -> Product -> Initiative -> Feature`

## Capital Allocation Model
| Capital Type | Purpose | Typical horizon | Control intensity |
|---|---|---|---|
| Run | Reliability, compliance, mandatory maintenance | 0-2 quarters | High |
| Grow | Proven demand expansion and conversion gains | 1-4 quarters | Medium |
| Transform | New capability bets and structural market moves | 2-8 quarters | Very high |

## Economic Scoring Model
Weighted score for investment decisions:
- ROI Score (0-5), weight 40%
- Risk Score (0-5, inverted), weight 30%
- Strategic Fit Score (0-5), weight 30%

Decision score:
`Investment_Score = 0.4*ROI + 0.3*(5-Risk) + 0.3*Strategic_Fit`

## Investment Approval Gate
- Require Strategic_Theme, Capital_Type, Investment_Size, Confidence_Level, Strategic_Fit_Score.
- Require initiative hypothesis with expected impact and baseline.
- Require traceability link to strategy document and target KPI.

Approval outcomes:
- Approve
- Approve with constraints
- Defer
- Reject

## Kill Criteria
- Expected impact missed by >30% after agreed observation window.
- Risk trend worsens two consecutive governance cycles.
- Compliance risk unresolved past deadline.
- Strategic fit drops below threshold after portfolio reprioritization.

## Portfolio Dashboard Metrics
- Active initiatives by Capital_Type
- Investment by Strategic_Theme
- Portfolio risk score (weighted by investment)
- Delivery gate failure rate
- Experiment pass rate
- ROI realization by initiative and quarter
