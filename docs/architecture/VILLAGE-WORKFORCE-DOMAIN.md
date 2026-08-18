# Village workforce Domain contracts

| Field | Value |
|---|---|
| Status | Implemented — immutable contracts and validation |
| Epic | [EPIC-007](../roadmap/epic-007/EPIC.md) |
| Backlog item | [E7-002](../roadmap/epic-007/BACKLOG.md#e7-002--add-immutable-settlement-worker-and-assignment-contracts) |
| Product contract | [Village workforce evaluation contract](./VILLAGE-WORKFORCE-EVALUATION-CONTRACT.md) |
| Namespace | `TaiWu.Domain.VillageWorkforce` |

## Purpose

Provide presentation-neutral, infrastructure-neutral contracts for one
coherent village workforce snapshot and the version-1 occupied shop-manager
replacement comparison. These types establish invariants only. Save projection
is delivered by E7-003, verified rule definitions by E7-004, and rule
evaluation by E7-005.

## Stable identities

| Type | Meaning | Validation |
|---|---|---|
| `SettlementIdentity` | Saved Taiwu settlement identity | Non-negative `Int16` |
| `VillageWorkerIdentity` | Saved character identity | Positive `Int32` |
| `ShopBuildingIdentity` | Area, block and building-block index | Three non-negative `Int16` values |
| `ShopManagerTargetIdentity` | Building plus occupied manager-list position | Position `0..127`; target kind is typed |
| `LifeSkillDisciplineIdentity` | Required installed life-skill type | Verified range `0..15` |
| `WorkforceObjectiveIdentity` | Objective kind plus version | Defined enum and stable version token |
| `WorkforceRuleVersion` | Evaluation-rule version | Valid `MAJOR.MINOR.PATCH` semantic version |
| `WorkforceSourceVersions` | Save revision, GameData, mapping, universe and fingerprint versions | Exact SHA-256 plus stable version tokens |
| `WorkforceResultIdentity` | Snapshot, objective, rule and target boundary | Typed constituent identities |

No identity depends on a localized worker, building, discipline or position
label. Raw source values remain Domain data and are not presentation text.

## Evidence contracts

`WorkforceFactIdentity` names candidate-universe membership,
current-assignment membership, or one discipline-specific base life-skill
qualification. Each kind requires an exact value shape. A qualification cannot
exist without a typed discipline and cannot accept a Boolean or `Int32` value.

`WorkforceFact` has five explicit states:

| State | Required data | Prohibited fallback |
|---|---|---|
| `Confirmed` | Matching value and provenance | No missing reason or conflicts |
| `Incomplete` | Stable unavailable reason | No value or provenance |
| `Unsupported` | Stable unsupported reason | No value or old-version fallback |
| `Stale` | Last observed value, provenance and stale reason | Cannot be presented as current |
| `Conflicting` | At least two typed values with distinct provenance | No selected numeric result |

Evidence and conflict collections reject nulls and duplicates and sort by
stable identity. `WorkforceProvenance` separates configured-save,
installed-GameData and derived-rule evidence. Snapshot construction rejects a
configured-save revision or installed GameData version that does not match the
snapshot source versions.

## Worker profile

`VillageWorkerProfile` owns:

- stable worker identity;
- typed `Eligible`, `CurrentOnly`, `Ineligible`, `Incomplete`, `Unsupported`,
  or `Conflicting` state;
- exact source versions;
- unique canonically ordered facts;
- unique canonically ordered typed diagnostics; and
- a deterministic SHA-256 fingerprint.

The profile has no name, localized explanation, UI state, filesystem path,
GameData object, or runtime handle.

## Target and assignment separation

`ShopManagerTarget` combines the stable occupied slot identity, exact required
life-skill discipline and typed evidence. It rejects missing or duplicate
target evidence.

Two assignment types make origin impossible to blur:

| Type | Origin | Allowed owner |
|---|---|---|
| `CurrentShopManagerAssignment` | `CurrentSave` | `VillageWorkforceSnapshot` |
| `ProposedShopManagerAssignment` | `ProposedHelper` | One result/session artifact only |

A current assignment requires configured-save provenance. A proposed
assignment requires a complete result identity and has no save provenance or
mutation behavior. `VillageWorkforceSnapshot.CurrentAssignments` is statically
an immutable array of the current type, so a proposal cannot enter it.

## Snapshot invariants

`VillageWorkforceSnapshot` contains settlement identity, UTC capture time,
source versions, workers, occupied shop targets, current assignments and
diagnostics. Construction fails when:

- a worker, target, current-assignment target or diagnostic is duplicated;
- any collection contains null;
- a worker uses different source versions;
- save or GameData provenance does not match the snapshot;
- an assignment references a worker or target outside the snapshot; or
- an occupied version-1 target lacks exactly one current assignment.

Every collection is copied into an `ImmutableArray` and canonically sorted.
The snapshot exposes no mutable source collection.

## Evaluation contracts

The Domain defines typed requirement kinds/outcomes, one component identity,
the `BaseQualificationPoint` unit, evaluation states, result value and relative
comparison outcomes.

`WorkforceScoreComponent` enforces the transparent version-1 rule:

- raw `Int16` qualification is preserved exactly;
- normalized value equals raw value;
- weight is exactly `1`;
- contribution equals the normalized value; and
- evidence is immutable and unique.

`WorkforceEvaluation` rejects duplicate requirements or components. A ranked
or tied evaluation requires an eligible worker, every version-1 gate passed,
exactly one component and a result equal to that component. Ineligible,
incomplete, unsupported and conflicting evaluations cannot carry a numeric
result. A current-only evaluation is unranked and may retain one descriptive
qualification result without becoming an eligible proposal.

`WorkforceComparison` requires two different workers from the same immutable
result. It derives `Higher`, `Lower` or `Equal` only from two rankable exact
values. Missing/unsupported states yield `Unavailable`; different component
contracts yield `Incompatible`; ineligible/current-only or conflicting states
yield `NotComparable`.

## Versioned rule definitions

E7-004 adds immutable source-version, hard-requirement, component, limitation,
definition and typed resolution contracts. The verified rule catalogue
resolves only the exact objective, GameData, mapping, candidate-universe,
fingerprint-schema and target-kind tuple documented in
[Village workforce rules](./VILLAGE-WORKFORCE-RULES.md). Unsupported versions
return a typed result without a fallback rule.

The one numeric component names its exact source fact, identity normalization,
base-qualification-point unit, higher-is-better direction and weight one.
Definition construction rejects duplicate identities, invalid component
shapes and inconsistent profile/provenance fact references.

## Deterministic evaluation

E7-005 adds `VillageWorkforceEvaluator` and the immutable
`VillageWorkforceEvaluationSet`. The evaluator records all target-specific
hard gates before creating a component, maps unavailable evidence to typed
unrankable states, preserves current-only values as descriptive, and marks
exact equal totals as ties. The evaluation set retains the rule fingerprint,
current worker and every canonical evaluation fingerprint. See
[Village workforce evaluator](./VILLAGE-WORKFORCE-EVALUATOR.md).

## Shortlist, comparison, and manual review

E7-006 adds canonical competition ranks, unranked state groups, stable counts,
immutable filter views, no-explicit-vacancy scope, result-level limitations,
same-result comparison, and an information-only manual plan. Checklist items
are typed prerequisite/fact/caution identities and carry no completion state.
See [Village workforce shortlist and comparison](./VILLAGE-WORKFORCE-COMPARISON.md).

## Fingerprints

Worker, target, snapshot, proposal, evaluation and comparison fingerprints use
canonical, invariant-culture values. They include every identity, version,
state, fact, evidence reference, conflict, assignment, gate, component, result,
diagnostic and capture fact that can affect their meaning.

Localized names, translated messages, viewport, filters, comparison selection,
focus and other Presentation state are absent and therefore cannot change a
Domain fingerprint.

## Dependency boundary

The contracts reference only .NET base/immutable collections and other
`TaiWu.Domain.VillageWorkforce` types. They reference no Application,
Infrastructure, ASP.NET, Razor, UI, GameData, reflection, filesystem, network,
process, database, serialization or game-control type.

## Verification

Focused tests cover invalid identities, typed fact values, incomplete,
unsupported and conflicting evidence, immutable/canonical collections,
duplicate rejection, mixed-revision rejection, current/proposed separation,
rankable-state validation, transparent scoring, comparisons, equality and
fingerprint sensitivity.

```powershell
dotnet test tests\TaiWu.Domain.UnitTests\TaiWu.Domain.UnitTests.csproj -c Release --no-build -- --filter-namespace TaiWu.Domain.UnitTests.VillageWorkforce
```
