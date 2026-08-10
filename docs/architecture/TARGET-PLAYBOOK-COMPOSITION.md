# Target playbook composition and exact-target adjustment

This document defines the pure composition and target-adjustment boundary
delivered by
[E5-005](../roadmap/epic-005/BACKLOG.md#e5-005--compose-overlapping-playbooks-and-apply-exact-target-adjustments).
It consumes the verified catalogue from
[TARGET-COUNTER-PLAYBOOKS.md](TARGET-COUNTER-PLAYBOOKS.md) and the exact profile
and match set from
[TARGET-ARCHETYPE-MATCHING.md](TARGET-ARCHETYPE-MATCHING.md).

## Decision

Composition and exact-target adjustment are separate claims:

1. `TargetPlaybookComposer` combines every confirmed matched playbook into a
   reusable strategy.
2. `TargetSpecificPlaybookAdjuster` compares that strategy with the exact
   profile, threats, source facts, observations, matches, and gaps that
   produced it.

Neither service selects a complete loadout or evaluates the current player's
ownership and capacity. E5-006 remains responsible for passing adjusted
options through the existing recommendation and feasibility pipeline.

## Composition input gate

The composer receives one immutable `TargetArchetypeMatchSet`, one exact-
version `TargetCounterPlaybookCatalog`, and the observed GameData version.

Only `Matched` results may contribute a mechanical goal. `Partial`,
`NotMatched`, `Unsupported`, and `Conflicting` results each produce an
`ARCHETYPE_MATCH_NOT_CONFIRMED` diagnostic and contribute no playbook, goal,
threat, option, priority, or score. A matched archetype whose playbook cannot
resolve for the exact GameData version produces `PLAYBOOK_UNAVAILABLE`.

Every input match therefore remains visible even when it cannot contribute.

## Composed strategy contract

`TargetPlaybookComposition` retains:

- the exact profile fingerprint and match-set stable key;
- all resolved source playbooks;
- canonically merged response goals;
- globally deduplicated typed threats and counter options;
- explicit known gaps;
- typed composition conflicts;
- non-contributing match and catalogue diagnostics; and
- a deterministic SHA-256 stable key.

### Shared-goal merge rules

Goals deduplicate by stable goal code. For every group of shared
contributions:

- sequence becomes the smallest explicit sequence;
- priority becomes the strongest value in this order: `Critical`, `High`,
  `Normal`, `Fallback`;
- response timing becomes the earliest value in this order:
  `CombatStartPassive`, `EquippedPassive`, `ActiveDefense`, `ActiveAgility`,
  `ActiveAttack`;
- source playbooks, facets, threats, conflict groups, evidence, and gaps form
  canonical ordinal unions; and
- identical counter rules deduplicate by their stable counter code while
  retaining all source playbook, source goal, and conflict-group references.

The composed strategy also exposes one global option per counter code and one
global threat per threat code. A counter appearing under several goals remains
one candidate with several goal references.

These rules resolve only priority and response timing. They do not resolve a
mechanical incompatibility.

## Explicit conflict rules

Conflict groups are reviewed mechanical declarations, not free-form display
tags. Their prefixes define typed conflict categories:

| Prefix | Conflict kind |
|---|---|
| `ACTIVE_` | `ActiveRole` |
| `TIMING_` | `Timing` |
| `CAPACITY_` or `SLOT_` | `Capacity` |
| Any other reviewed group | `Requirement` |

A conflict is emitted when:

- two distinct composed goals own the same goal-level conflict group; or
- options on two distinct goals own the same option-level conflict group and
  no identical option can satisfy both goals.

If the same verified option appears under both goals, it is shared rather than
reported as a conflict. Otherwise the result retains the conflict kind, group,
every affected goal code, and every affected option code. The composer never
chooses one side, drops a requirement, guesses available capacity, or converts
a conflict into a lower priority.

## Exact-target adjustment gate

The adjuster accepts a composition only with the exact
`TargetCombatProfileAnalysis` whose profile fingerprint and match-set stable
key produced it. A stale profile, rebuilt observation, different definition
set, or old match set is rejected. This prevents a broad archetype result from
surviving contrary newer target evidence.

The pass extracts canonical evidence in these typed categories:

| Evidence kind | Exact source |
|---|---|
| `ProfileFacet` | Immutable confirmed, incomplete, unsupported, or conflicting profile facet |
| `Threat` | Current `AnalyzedTargetThreat` |
| `Skill` | Typed active target-skill membership or threat source |
| `Effect` | Installed typed mechanic evidence or exact threat effect source |
| `Equipment` | Saved typed equipment/weapon facet source |
| `Observation` | Accepted current-screen observation source |
| `Gap` | Explicit composed playbook gap |
| `ArchetypeMatch` | Exact matched, contrary, or unresolved archetype result |

Each evidence item is `Confirmed`, `Contrary`, or `Incomplete`, has a stable
non-localized identity, and retains opaque evidence references. Target display
names, local paths, timestamps, and raw descriptions do not create adjustment
facts.

## Automatic adjustments

The initial exact-target pass applies four conservative rules:

- `Retained`: a goal still has confirmed exact facet or threat support;
- `Elevated`: a non-critical goal has confirmed current-screen observation
  evidence in addition to its typed facet;
- `Added`: an exact analyzed threat is not covered by any composed goal; and
- `Unresolved`: a playbook gap or incomplete exact goal evidence remains.

Automatic adjustment never invents a replacement counter. It does not reduce
a response merely because evidence is absent.

## Reviewed adjustment rules

`TargetPlaybookAdjustmentRule` represents an explicit, separately reviewed
relationship between exact evidence and a response. It can produce all six
product actions:

| Action | Response references | Required evidence state |
|---|---|---|
| `Retained` | Existing response | Confirmed |
| `Elevated` | Existing response | Confirmed |
| `Reduced` | Existing response | Contrary |
| `Added` | New response | Confirmed |
| `Replaced` | Distinct existing and replacement responses | Confirmed |
| `Unresolved` | Existing response | Incomplete |

Rules reference exact evidence identities rather than names or descriptions.
A missing response, missing evidence identity, or wrong evidence state creates
a stable diagnostic and leaves the automatic decision intact.

When multiple reviewed rules address the same response, deterministic action
precedence is `Replaced`, `Reduced`, `Elevated`, `Added`, `Retained`, then
`Unresolved`, followed by ordinal rule code. Losing rules remain visible as
`ADJUSTMENT_RULE_SHADOWED` diagnostics. This is the only reviewed-rule
precedence; input declaration order has no effect.

## Broad versus exact evidence

The reusable playbook cannot override exact evidence:

- non-matched archetypes never enter composition;
- stale profile or match identities cannot enter adjustment;
- a `Reduced` decision requires explicit `Contrary` evidence;
- an `Added`, `Elevated`, or `Replaced` decision requires confirmed evidence;
  and
- unresolved or conflicting evidence cannot masquerade as confirmation.

This permits a broad archetype to suggest a response while preserving a typed,
explainable exact-target correction before player personalization.

## Determinism

Composition canonicalizes playbooks, goals, threats, options, sources,
conflicts, gaps, evidence, and diagnostics before hashing. Adjustment
canonicalizes exact evidence, rules, actions, and diagnostics before hashing.

Equivalent reordered catalogues, definitions, playbooks, goals, options,
evidence, and reviewed rules therefore produce identical stable keys.
Applying or clearing an Epic 3 observation rebuilds the upstream profile and
match identities, so no mutable composition or adjustment state leaks into the
new result.

## Verification

Focused Domain tests cover:

- all four delivered playbooks composed together;
- global shared-goal, threat, and counter deduplication;
- strongest-priority and earliest-timing resolution;
- partial, unsupported, conflicting, and exact-version exclusion paths;
- active-role, requirement, timing, and capacity conflicts;
- shared options satisfying overlapping goals without false conflicts;
- all typed exact-evidence kinds and all six adjustment actions;
- exact threats outside a playbook;
- contrary evidence reducing a broad goal;
- missing evidence, wrong state, missing response, and shadowed-rule
  diagnostics;
- stale profile/match rejection; and
- deterministic results for reordered catalogues and reviewed rules.
