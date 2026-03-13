# Experimentation System - Sanzu PDLC 3.5

Owner: Growth Lead
Version: 3.5.1
Review cadence: Weekly

## Trigger
Every item entering `Released` must automatically enter `Measuring`.

## Experiment Record Template
- Initiative_ID
- Feature_ID
- Hypothesis
- Release_Metric
- Baseline_Value
- Expected_Impact
- Observation_Window_Days
- Confidence_Level
- UX_Spec_Link
- UX_Flow_Link
- Experiment_Variant_ID

## Hypothesis Format
`If <change> for <target segment>, then <metric> moves from <baseline> to <expected> within <window>, because <causal logic>.`

## Gating Rules
- Experiment cannot start without baseline and target metric.
- Experiment cannot close without observed value and statistical check.
- `Measuring -> Iterating` is blocked unless:
  - ROI_Recalculated=true
  - Kill_or_Scale_Decision in {Scale, Iterate, Kill}

## Statistical Validation Gate
- Minimum observation window must be met.
- Minimum sample threshold must be met for the metric type.
- If sample threshold not met, decision defaults to `Iterate`.

## Decision Tree
- `Scale`: impact >= expected and risk stable.
- `Iterate`: partial impact, confidence improving, no critical risk.
- `Kill`: impact materially below expected or risk increased beyond threshold.

## Local Automation Contract
- On `Released`: create/attach Growth experiment task in folder `08_Growth`.
- On window end: trigger review task for PM + Growth + Finance.
- On close: enforce ROI recalculation writeback to Lifecycle fields.

## Financial Closure
- Compare actual impact vs expected impact.
- Recalculate initiative ROI using actual cost-to-date.
- Persist final decision rationale in linked decision log/doc.
