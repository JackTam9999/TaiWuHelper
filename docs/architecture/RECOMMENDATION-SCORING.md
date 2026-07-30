# Evidence-aware recommendation scoring

## Purpose

Recommendation scoring orders already feasible combat loadouts according to a
chosen policy. It does not repair invalid proposals, simulate combat, predict a
win, equip skills, change practice directions, write a save, or control the
game.

## Hard boundary

`CombatRecommendationScorer` accepts only `GeneratedCombatLoadout` instances.
These are internally created by `CombatLoadoutGenerator` only after the full
proposal passes `CombatLoadoutFeasibilityValidator`.

Ownership, mastery, exact effect identity, direction, slot budgets, legendary
book costs, active-role limits, and hard combat requirements are therefore
filters before scoring. They are never represented as score penalties.

## Visible components

Every scored candidate exposes the following 0–100 components:

| Component | Meaning |
|---|---|
| Threat coverage | Share of severity-weighted target threats covered |
| Survival | Best verified protection per threat: hard counter 100, mitigation 60 |
| Execution reliability | Starts at 100; minus 15 per manual direction change and 5 per active-attack step |
| Current-loadout compatibility | Share of selected skills already equipped |
| Damage potential | Caller-supplied evidence-backed damage score |
| Opportunity cost | Share of available slot capacity left unused |
| Conditional risk | Starts at 100; minus 25 per unresolved conditional requirement |

Threat severities use fixed weights: Informational 1, Moderate 2, High 4, and
Critical 8. All component scores are clamped to 0–100.

Damage is deliberately optional. When no verified damage evidence exists, its
component is visible as unavailable and its policy weight is excluded from the
normalized total. Absence is never converted into an invented zero or average.

## Policies

Each policy has a stable weight set totaling 100:

| Component | Safe | Balanced | Aggressive |
|---|---:|---:|---:|
| Threat coverage | 30 | 25 | 15 |
| Survival | 30 | 20 | 10 |
| Execution reliability | 15 | 15 | 10 |
| Current-loadout compatibility | 5 | 10 | 10 |
| Damage potential | 5 | 15 | 40 |
| Opportunity cost | 5 | 10 | 10 |
| Conditional risk | 10 | 5 | 5 |

Safe emphasizes verified threat handling and survival. Balanced gives more
weight to compatibility, damage, and unused capacity while preserving a
defensive bias. Aggressive makes verified damage evidence the largest
component without weakening any hard feasibility rule.

## Total and ranking

The normalized total is:

`sum(available component score × policy weight) / sum(available weights)`

Results are ordered by:

1. total score, descending;
2. threat-coverage score, descending;
3. retained current-skill count, descending; and
4. candidate stable key, ordinal ascending.

This guarantees the same order regardless of input enumeration order.

## Golden-target review

For the verified critical mind-resonance fixture, Safe policy ranks an
otherwise comparable hard-counter candidate above a mitigation-only
candidate. The review establishes that the score reflects the evidence model;
it does not infer unobserved enemy equipment, damage numbers, or combat
outcomes.
