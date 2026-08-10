# Target playbook personalization

## Purpose

E5-006 connects the Epic 5 target profile, archetype, playbook, and exact-
target adjustment result to the existing combat-loadout recommendation engine.
It does not introduce a second candidate generator, feasibility model, scoring
policy, manual plan, explanation, or comparison builder.

## Immutable recommendation boundary

`TargetPlaybookRecommendationPersonalizer.Prepare` derives one immutable plan
from the same `CombatSnapshot` used by the recommendation:

1. extract the typed target profile and evaluate every registered archetype;
2. compose only `Matched` archetype playbooks for the exact GameData version;
3. apply exact-target adjustments to that exact profile and match set;
4. retain only confirmed `Retained`, `Elevated`, reviewed `Added`, or reviewed
   `Replaced` goals that still identify a goal in the matched composition;
5. retain only exact registered counter-rule instances belonging to those
   goals; and
6. evaluate current player access with the same direction, effect, and
   requirement semantics used by loadout generation.

Partial, unsupported, conflicting, and not-matched archetypes remain visible
in the analysis but cannot supply candidates or affect scoring. An exact threat
outside a matched playbook may remain an `Added` adjustment for explanation,
but it cannot manufacture a playable option.

## Existing engine remains authoritative

Eligible verified counter rules become ordinary `CombatLoadoutOption` values.
The existing `CombatLoadoutGenerator` remains responsible for:

- learned-skill ownership and mastery;
- direct/reverse direction and breakthrough availability;
- exact raw-effect identity;
- hard activation and equipment requirements;
- category, generic-slot, and Neigong-derived capacity;
- active defense/agility role conflicts;
- active-use inner-power backlash;
- complete-loadout feasibility; and
- bounded exploration and result limits.

`CombatRecommendationScorer`, `ManualCombatPlanBuilder`,
`CombatRecommendationExplanationBuilder`, and
`CombatLoadoutComparisonBuilder` consume the same feasible candidates as
before. Policy weights, component meanings, stable tie-breakers, exploration
limits, and truncation diagnostics are unchanged.

## Player availability and gaps

`TargetPlaybookCounterAvailability` records the exact composed option, its
access evaluation, generation-linked diagnostics, and one of four states:

| State | Meaning |
|---|---|
| `Feasible` | At least one returned feasible candidate contains the exact option |
| `Inaccessible` | Ownership, direction, exact effect, or hard requirements reject it |
| `Infeasible` | Access passes, but complete loadout feasibility rejects it |
| `Unresolved` | Search/result truncation prevents a complete availability claim |

Every non-feasible verified option materializes an
`InaccessibleVerifiedOption` gap referencing the exact counter code. No
name-similar skill, guessed effect, or lower-ranked unverified replacement is
introduced. Catalogue `NoVerifiedOption` and `IncompleteEvidence` gaps remain
alongside player-specific gaps.

Access evaluation can model the proposed selection and an immediately
available breakthrough. This mirrors the existing manual-plan behavior:
equipping a passive option or assigning its active agility/defense role is
evaluated as part of the proposal rather than incorrectly requiring it to be
in the current loadout already.

## Observation lifecycle

`CombatLoadoutRecommendation.TargetPlaybook` owns the profile analysis,
composition, adjustments, eligible goals, counter availability, and gaps that
produced the recommendation. The target-observation workflow builds the save-
only and merged-observation recommendations independently from their own
snapshots before computing impact. Clearing an observation therefore rebuilds
the save-only profile, matches, playbooks, adjustments, recommendation, and
Epic 4 comparison without retaining mutable observation state.

## Verification

Application coverage proves matched versus partial eligibility, accessible and
unowned counters, breakthrough feasibility, hard-filter rejection,
deterministic policy results, manual-plan/comparison parity, and observation
apply/repeat/clear replacement. The full release matrix on 2026-08-10 passed
1,030 tests: 1,021 passed, 0 failed, and 9 expected opt-in integration tests
were skipped.
