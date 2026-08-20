# Evidence-aware tactical recommendation scoring

| Field | Value |
|---|---|
| Status | Implemented |
| Epic | [EPIC-008](../roadmap/epic-008/EPIC.md) |
| Backlog item | [E8-007](../roadmap/epic-008/BACKLOG.md#e8-007--score-causal-value-execution-reliability-and-supported-finish-paths) |
| Product semantics | [Tactical combat planning contract](./TACTICAL-COMBAT-PLANNING-CONTRACT.md#tactical-score-semantics) |
| Search input | [Deterministic tactical loadout search](./TACTICAL-LOADOUT-SEARCH.md) |

## Purpose and boundary

`TacticalCombatScorer` orders complete loadouts already accepted by E8-006.
It cannot add a candidate, repair a rejected direction, ignore a requirement,
change a slot allocation, or turn a partial combination into a feasible one.
The ranked output contains exactly the search result's retained feasible set.

Scoring version `TACTICAL_SCORING@1.0.0` is deterministic and
presentation-neutral. Every component exposes:

- its available or unavailable state;
- typed raw inputs and their evidence state;
- a stable normalization identity;
- base and applied policy weight;
- normalized value and contribution when available;
- version-matched evidence; and
- limitations, including unresolved facts and claim boundaries.

An unavailable component has no numeric value, applied weight, contribution,
or implicit zero. Available weights are renormalized by dividing each base
weight by the sum of base weights for available components. Contributions are
rounded to four decimal places, away from zero; ranking never uses localized
text, measured elapsed time, or cache diagnostics.

## Causal value without duplicate threat counting

The causal universe is the distinct set of applicable transitions referenced
by post-pruning admitted roles. For each feasible loadout, the numerator is the
distinct subset referenced by its selected roles:

`covered distinct transitions / applicable distinct transitions × 100`.

One transition is counted once even if two roles cover it. The disclosure also
lists each transition's trigger and resulting causal-state identities, so the
score is based on the typed chain rather than repeated flat target-goal codes.
A loadout with no admitted causal transition receives a documented available
zero, not invented target value.

## Layered protection requires a proof

Distinct causal transitions receive causal value through the formula above.
Additional layered-protection value exists only through a
`TacticalLayeringProof` bound to the exact context, two post-pruning admitted
candidates, one applicable transition of the layered candidate, matching rule
versions, evidence, and a limitation.

Version 1 uses fixed documented marginal units:

| Proof kind | Marginal units |
|---|---:|
| Verified interaction | 100 |
| Failure fallback | 80 |
| Different timing window | 60 |
| Separate mitigation | 50 |

Units sum and clamp at 100. They are versioned scorer semantics, not
caller-authored weights. Without a proof, layered protection is an available
zero with `NO_DOCUMENTED_LAYERING_VALUE`. Duplicate transition coverage can
therefore gain no second causal reward; it gains fallback value only when an
explicit fallback proof exists.

## Timing and execution reliability

Typed role timing starts from these versioned opportunity values:

| Timing | Value |
|---|---:|
| Combat start | 100 |
| Before combat | 95 |
| Before first use | 85 |
| During cast | 70 |
| After cast | 65 |
| On observed state | 60 |
| After manual action | 50 |

Timing opportunity averages selected role values and deducts 10 per required
direction change or immediate breakthrough. A target-state trigger during a
cast or on an observed state requires a matching `TacticalTriggerObservability`
input. Missing, incomplete, conflicting, or unsupported observability makes
the component unavailable; it is never averaged as a guessed delay.

Execution reliability begins at 100 and applies only typed deductions:

- 15 per direction preparation;
- 5 per active during-cast, after-cast, or after-manual-action role; and
- 10 per exact resource readiness requirement.

The accepted proposal's requirements are reevaluated. Any unresolved
conditional requirement or required trigger observability makes reliability
unavailable and remains visible as a raw input and limitation. A feasible
action with no such uncertainty is not described as guaranteed to work.

## Recovery cost

Recovery cost is a preference for lower verified burden, expressed as
`100 − typed deductions`:

- 15 per direction preparation; and
- 45 for an applicable `DirectPracticeSelfLock` transition.

When self-lock is selected, the applicable typed recovery transition is
disclosed separately. The historical Reverse `604` route therefore exposes
both the three-layer self-lock limitation and the general Reverse-cast
recovery limitation; the scorer does not invent three executable recovery
skills. A resource readiness requirement is disclosed but is not treated as
resource consumption without a separate typed consumption rule.

## Finish-path evidence

Finish value is available only when the selected loadout contains:

1. an admitted applicable `DamageChannelChoice` role;
2. an admitted applicable `FinishWindowSupport` role and transition;
3. typed positive attack-channel strength;
4. typed hit or cast reliability from 0 through 100;
5. typed non-negative target defense or resistance;
6. an explicitly true applicable condition; and
7. an explicitly true finish window.

Every input must match the exact context's GameData and rule versions. Among
multiple supported channel proofs, the scorer selects the greatest normalized
contribution, breaking ties by canonical proof identity. The dimensionless
normalization is:

`max(0, channel strength − resistance) / channel strength × reliability`.

This is a relative supported finish contribution, not predicted damage, hit
chance, time to defeat, or probability of victory. The production E8-000 rule
set contains no approved finish roles or inputs, so its `FinishPath` component
is `Unavailable`, all five missing evidence classes remain visible, and its
weight is excluded. Its plan remains fallback-only.

## Neutral unused capacity

Every scored result reports remaining and total capacity for all five skill
categories as `TacticalUnusedCapacityFact`. Version 1 always marks this fact
neutral with `HasDocumentedMarginalValue = false`. It has no score component,
bonus, penalty, reserve label, or duplicate-coverage alias. Two otherwise
identical candidates therefore retain identical component values and totals
when only unused capacity differs.

## Policies, claims, and ranking

The published base weights remain:

| Component | Safe | Balanced | Aggressive |
|---|---:|---:|---:|
| Causal value | 28 | 29 | 28 |
| Layered protection | 24 | 18 | 10 |
| Timing opportunity | 10 | 16 | 24 |
| Execution reliability | 20 | 16 | 12 |
| Recovery cost | 15 | 13 | 8 |
| Finish path | 3 | 8 | 18 |

Each policy totals 100 and publishes an explicit claim limitation:

- Safe is not guaranteed survival;
- Balanced is not an outcome prediction; and
- Aggressive is not a victory or damage prediction.

Candidates order by total contribution, causal value, layered protection,
timing opportunity, then canonical candidate identity. The supported fixture
matrix proves that Safe and Aggressive can prefer different feasible loadouts;
policy names are not cosmetic. A bounded search remains bounded after scoring:
the result is the highest-ranked retained result found within the reported
bounds, never an optimality claim.

## Bounds and verification

One scoring request accepts at most 2,048 layering proofs, 1,024 trigger
observations, and 1,024 finish proofs. Cancellation is checked before proof
validation and between candidate scores, so no partial ranking is returned.

Domain tests cover duplicate transition coverage and causal states, useful
layering, unknown timing, self-lock recovery cost, unavailable finish evidence,
a fully typed synthetic supported channel, neutral unused capacity, all three
weight sets, distinct Safe/Aggressive ranking, deterministic ties and shuffled
inputs, hard-feasibility parity, component disclosure, and pre-cancellation.
Architecture tests include tactical scoring files in the mutation,
persistence, network, process, game-control, and unbounded-source scan.
