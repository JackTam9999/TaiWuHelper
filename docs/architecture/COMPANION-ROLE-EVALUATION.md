# Companion role definition and evaluation architecture

| Field | Value |
|---|---|
| Status | Implemented for E6-003, E6-006, E6-007, and E6-014 |
| Epic | [EPIC-006](../roadmap/epic-006/EPIC.md) |
| Backlog items | [E6-003](../roadmap/epic-006/BACKLOG.md#e6-003--define-versioned-role-definitions-and-evaluation-rules), [E6-006](../roadmap/epic-006/BACKLOG.md#e6-006--evaluate-role-suitability-and-rank-comparable-candidates), [E6-007](../roadmap/epic-006/BACKLOG.md#e6-007--build-evidence-aware-shortlist-and-candidate-comparison-explanations) |
| Product contract | [Companion role evaluation and shortlist contract](./COMPANION-ROLE-EVALUATION-CONTRACT.md) |
| Profile contract | [Companion-candidate source boundary](./COMPANION-CANDIDATE-SOURCES.md) |
| Shortlist and comparison | [Companion candidate shortlist and comparison](./COMPANION-CANDIDATE-COMPARISON.md) |

## Purpose and boundary

E6-003 turns the accepted role semantics into presentation-neutral Domain
definitions and a pure single-candidate evaluator. E6-006 adds the pure
multi-candidate ranking operation over those immutable evaluations. Together
they establish one authoritative path for eligibility gates, required fact
availability, provenance compatibility, normalization, weighting,
contribution, total, exact merit comparison, typed exclusions, competition
rank, and ties.

E6-005 supporting enrichment is documented in the
[companion-candidate enrichment architecture](./COMPANION-CANDIDATE-ENRICHMENT.md).
It attaches compatible combat-skill definitions to saved learned/equipped
identities without changing any profile fact or role score. Detailed character
progress is not required by either version-1 role and remains explicitly not
requested.

## Implemented contracts

The `TaiWu.Domain.CompanionRoles` namespace contains:

- `CompanionRoleIdentity`, a stable non-localized role code;
- `CompanionRoleDefinition`, an immutable versioned role and rule set;
- `CompanionRoleScoreDimension`, a typed field, unit, direction,
  normalization range, weight, missing behavior, and explanation identity;
- `CompanionRoleHardRequirement`, the exposed ordered hard-gate plan;
- `VerifiedCompanionRoleDefinitions`, the exact approved role catalogue and
  fail-closed definition resolver;
- `CompanionRoleEvaluator`, the pure evaluator for one profile, definition,
  and discipline;
- `CompanionRoleGateEvaluation`, which retains each evaluated requirement,
  typed outcome, reason identity, and evidence;
- `CompanionRoleScoreComponent`, which retains the raw value, normalized
  value, direction-aware weighted contribution, and evidence;
- `CompanionRoleEvaluation`, which retains role, profile, discipline, gates,
  components, role-local total, outcome identity, and fingerprint;
- `CompanionRoleMeritComparer`, which compares only rankable evaluations from
  the same exact role definition and discipline;
- `CompanionRoleCandidateRanking`, which retains one immutable evaluation,
  its `Ranked`, `Tied`, `Ineligible`, `Incomplete`, `Unsupported`, or
  `Conflicting` state, and a nullable competition rank;
- `CompanionRoleRanking`, which retains the exact definition, discipline,
  canonically ordered ranked and unranked collections, and fingerprint; and
- `CompanionRoleShortlistBuilder`, which evaluates every unique candidate once
  and constructs those validated ranking contracts without re-scoring.

All collections are copied and canonically ordered. Definitions reject blank
or path-shaped stable identities, invalid enums, empty or duplicate version
sets, invalid discipline ranges, empty or duplicate dimensions, incompatible
typed fields, invalid normalization ranges, and non-positive or excessive
weights.

## Verified version-1 catalogue

All three definitions require profile mapping version `1`, fingerprint schema
version `1`, evaluation rule version `1`, and GameData version
`1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20`.

| Role identity | Discipline range | Required typed fact | Component identity |
|---|---:|---|---|
| `MARTIAL_DISCIPLINE_APTITUDE` | Martial `0..13` | `BaseMartialQualification` for the selected martial discipline | `BASE_MARTIAL_QUALIFICATION` |
| `LIFE_SKILL_DISCIPLINE_APTITUDE` | Life skill `0..15` | `BaseLifeSkillQualification` for the selected life-skill discipline | `BASE_LIFE_SKILL_QUALIFICATION` |
| `COMPREHENSIVE_BASE_CAPABILITY` | Aggregate `Capability/0` | Complete summary over six base attributes, 14 martial aptitudes, and 16 life-skill aptitudes | `CAPABILITY_BREADTH_INDEX` |

Each dimension uses unit `BASE_QUALIFICATION_POINT`, higher-is-better
direction, identity normalization over the complete saved `Int16` type range,
weight `1`, and missing behavior `EvaluationIncomplete`. Their different typed
fields and discipline domains are different hard requirements over the same
candidate-profile contract.

The comprehensive dimension uses `BREADTH_INDEX_X100` as its raw unit,
hundredth normalization, higher-is-better direction, and weight `1`, so its
role-local total is the two-decimal breadth index. It requires all 36 source
facts to be confirmed and provenance-compatible; no synthetic source fact is
stored in the profile.

The catalogue resolver returns one of `Supported`, `UnknownIdentity`, or
`UnsupportedVersion` with a stable diagnostic identity. It never silently
selects a nearby role version.

## Ordered evaluation algorithm

`CompanionRoleEvaluator.Evaluate` processes these gates in order and stops at
the first outcome other than `Passed`:

1. map the explicit `CandidateUniverseState` without inspecting name, age,
   location, or another descriptive fact;
2. require exact GameData, profile-mapping, and fingerprint-schema versions;
3. require the selected discipline or aggregate objective domain and type to
   be inside the role's verified range;
4. resolve the exact typed fact, or derive the comprehensive summary from all
   36 typed fields, and require complete confirmed evidence; and
5. require every contributing configured-save provenance to match the profile
   save SHA-256 and profile-mapping version.

The profile contract already rejects duplicate field identities, so an exact
lookup can produce one fact or no fact. A missing fact maps through the
dimension's explicit missing behavior. `Incomplete` and `Stale` evidence
produce an incomplete evaluation, `Unsupported` evidence produces an
unsupported evaluation, and `Conflicting` evidence produces a conflicting
evaluation. None has a component or total.

A confirmed value outside the dimension's declared normalization range is a
conflict between the typed source fact and rule definition. It is never
clamped, converted to zero, or scored.

## Score arithmetic

Only after every hard gate passes does the evaluator create components:

```text
normalized = identity(raw saved Int16 value)
directional value = normalized              when higher is better
directional value = -normalized             when lower is better
contribution = directional value * weight
role-local total = sum(contribution)
```

Decimal arithmetic is checked and deterministic. The first verified roles
therefore retain the exact saved aptitude as normalized value, contribution,
and total. A confirmed zero remains zero. Missing evidence has no component
and no total.

For `COMPREHENSIVE_BASE_CAPABILITY`, `raw = breadth index * 100`, hundredth
normalization restores the breadth index, and weight `1` makes that value the
role-local total. It is comparable only inside this explicit objective and
cannot enter either selected-discipline evaluation.

Every component retains its dimension identity, typed profile field,
`BASE_QUALIFICATION_POINT` unit, direction, normalization rule and range,
weight, raw value, normalized value, contribution, explanation identity, and
source evidence. A large score cannot hide a failed earlier gate because
scoring never starts after a non-passing gate.

## Merit and ties

`CompanionRoleMeritComparer` accepts only two rankable evaluations with the
same role-definition fingerprint and exact discipline identity. Higher
direction-aware total is preferred. Equal totals return `ExactTie`.

Character ID, localized name, source enumeration order, request order, and
location are not merit tie breakers. E6-006 may order equal-score entries by
stable candidate ID only after it preserves the explicit tie and shared rank.
Evaluations from different roles or disciplines return `NotComparable`.

## Multi-candidate ranking algorithm

`CompanionRoleShortlistBuilder.EvaluateAndRank` copies the supplied candidate
universe, rejects null or duplicate character identities and mixed source
versions, canonicalizes it by stable character ID, and calls
`CompanionRoleEvaluator.Evaluate` exactly once per profile. It then:

1. takes only `Rankable` evaluations into merit grouping;
2. groups by exact decimal total and orders groups by descending total;
3. assigns competition rank using the number of preceding candidates, so
   score groups of sizes `1, 2, 1` receive ranks `1, 2, 2, 4`;
4. marks a one-member score group `Ranked` and a multi-member group `Tied`;
5. uses character ID only for canonical ordering inside an established group;
   and
6. maps every unranked evaluation to its exact `Ineligible`, `Incomplete`,
   `Unsupported`, or `Conflicting` candidate state with no rank or total.

`CompanionRoleRanking` independently validates every definition, discipline,
and candidate source-version identity, unique candidate identity, state
mapping, score group, tie marker, and competition rank before exposing
immutable arrays. A hard-gate failure can therefore never enter a numeric rank,
and a display-order stabilizer can never change merit.

## Deterministic identity

A definition fingerprint includes:

- stable role identity and role/evaluation versions;
- the sorted supported GameData versions;
- supported profile and fingerprint-schema versions;
- discipline domain and range;
- ordered hard requirements;
- every typed score-dimension rule; and
- tie policy.

An evaluation fingerprint additionally includes the definition fingerprint,
candidate-profile fingerprint, selected discipline, state, evaluated gates,
components, total, and outcome identity. A ranking fingerprint includes the
definition, discipline, canonical candidate order, each exact evaluation
fingerprint, typed ranking state, and competition rank. These identities change
with semantic source, rule, fact, evidence, or rank changes and remain stable
across equivalent input enumeration order. None contains localized text, local
paths, exception text, or capture timestamps.

## Dependency and safety boundary

The implementation is pure Domain code over the E6-002 candidate-profile
contracts and .NET base class libraries. It has no Application,
Infrastructure, Presentation, persistence, filesystem, process, reflection,
network, archive, or GameData dependency. It performs no save or game action.

Synthetic unit coverage includes both verified roles over one shared profile,
valid definitions, invalid versions, weights, ranges and duplicates, every
candidate and evidence gate outcome, missing facts, stale and conflicting
facts, provenance mismatch, out-of-range facts, higher- and lower-is-better
arithmetic, exact ties, cross-role non-comparability, and deterministic rule
and evaluation fingerprints. Sixteen E6-006 cases additionally cover
competition ranks, canonical tie order, all typed exclusions, hard-gate
precedence, both verified roles, complete component evidence, score extremes,
irrelevant optional-field absence, unsupported disciplines, semantic
fingerprint changes, deterministic reruns, unsupported and mixed source
versions, duplicate candidates, cancellation, and the empty candidate universe.

## E6-007 shortlist and comparison

E6-007 implements the explanation and comparison model described in
[Companion candidate shortlist and comparison](./COMPANION-CANDIDATE-COMPARISON.md).
The model:

1. consumes one immutable `CompanionRoleRanking` without re-evaluating facts;
2. retains definition, discipline, total source count, rank, tie, and exclusion
   identities unchanged;
3. derives strengths, limitations, and comparison rows from existing gates,
   components, facts, and evidence references; and
4. keeps filtering, presentation, and localized display values outside merit
   and ranking fingerprints.

It must not rescore raw facts, infer unavailable values, substitute another
version, or compare totals across role or discipline identities.

## E6-008 handoff

The Application workflow must resolve the role, project and enrich one
snapshot, build one ranking and shortlist, and apply view/comparison selections
without creating another evaluation or source-read path.
