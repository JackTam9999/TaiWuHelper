# Companion role definition and evaluation architecture

| Field | Value |
|---|---|
| Status | Implemented for E6-003 |
| Epic | [EPIC-006](../roadmap/epic-006/EPIC.md) |
| Backlog item | [E6-003](../roadmap/epic-006/BACKLOG.md#e6-003--define-versioned-role-definitions-and-evaluation-rules) |
| Product contract | [Companion role evaluation and shortlist contract](./COMPANION-ROLE-EVALUATION-CONTRACT.md) |
| Profile contract | [Companion-candidate source boundary](./COMPANION-CANDIDATE-SOURCES.md) |

## Purpose and boundary

E6-003 turns the accepted role semantics into presentation-neutral Domain
definitions and a pure single-candidate evaluator. It establishes one
authoritative path for eligibility gates, required fact availability,
provenance compatibility, normalization, weighting, contribution, total, and
exact merit comparison.

This slice does not assign shortlist rank, competition rank, or `Ranked` and
`Tied` collection states. E6-006 will build those multi-candidate operations
over immutable E6-003 evaluations. It must not reimplement role gates or score
arithmetic.

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
  components, role-local total, outcome identity, and fingerprint; and
- `CompanionRoleMeritComparer`, which compares only rankable evaluations from
  the same exact role definition and discipline.

All collections are copied and canonically ordered. Definitions reject blank
or path-shaped stable identities, invalid enums, empty or duplicate version
sets, invalid discipline ranges, empty or duplicate dimensions, incompatible
typed fields, invalid normalization ranges, and non-positive or excessive
weights.

## Verified version-1 catalogue

Both definitions require profile mapping version `1`, fingerprint schema
version `1`, evaluation rule version `1`, and GameData version
`1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20`.

| Role identity | Discipline range | Required typed fact | Component identity |
|---|---:|---|---|
| `MARTIAL_DISCIPLINE_APTITUDE` | Martial `0..13` | `BaseMartialQualification` for the selected martial discipline | `BASE_MARTIAL_QUALIFICATION` |
| `LIFE_SKILL_DISCIPLINE_APTITUDE` | Life skill `0..15` | `BaseLifeSkillQualification` for the selected life-skill discipline | `BASE_LIFE_SKILL_QUALIFICATION` |

Each dimension uses unit `BASE_QUALIFICATION_POINT`, higher-is-better
direction, identity normalization over the complete saved `Int16` type range,
weight `1`, and missing behavior `EvaluationIncomplete`. Their different typed
fields and discipline domains are different hard requirements over the same
candidate-profile contract.

The catalogue resolver returns one of `Supported`, `UnknownIdentity`, or
`UnsupportedVersion` with a stable diagnostic identity. It never silently
selects a nearby role version.

## Ordered evaluation algorithm

`CompanionRoleEvaluator.Evaluate` processes these gates in order and stops at
the first outcome other than `Passed`:

1. map the explicit `CandidateUniverseState` without inspecting name, age,
   location, or another descriptive fact;
2. require exact GameData, profile-mapping, and fingerprint-schema versions;
3. require the selected discipline domain and type to be inside the role's
   verified range;
4. resolve the dimension's exact typed field identity for that discipline and
   require one confirmed profile fact; and
5. require configured-save provenance whose revision matches the profile save
   SHA-256 and whose source version matches the profile-mapping version.

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
components, total, and outcome identity. It therefore changes with semantic
source, rule, fact, or evidence changes and remains stable across equivalent
input enumeration order. Neither fingerprint contains localized text, local
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
and evaluation fingerprints.

## E6-006 handoff

The shortlist evaluator must:

1. resolve one verified role definition;
2. call `CompanionRoleEvaluator.Evaluate` exactly once per immutable candidate
   profile for the selected discipline;
3. retain every returned unranked state and gate reason unchanged;
4. rank only `Rankable` evaluations by descending direction-aware total;
5. preserve equal totals as explicit ties with competition ranking; and
6. use stable candidate ID only to canonicalize entries inside an already
   established tie group.

It must not rescore raw facts, infer unavailable values, substitute another
version, or compare totals across role or discipline identities.
