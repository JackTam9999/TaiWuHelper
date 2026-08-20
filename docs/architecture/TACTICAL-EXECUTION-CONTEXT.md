# Coherent tactical execution context

| Field | Value |
|---|---|
| Status | Implemented |
| Epic | [EPIC-008](../roadmap/epic-008/EPIC.md) |
| Backlog item | [E8-004](../roadmap/epic-008/BACKLOG.md#e8-004--project-one-coherent-tactical-execution-context) |
| Evidence boundary | [E8-000 tactical evidence](../scenarios/E8-000-tactical-combat-evidence.md) |
| Rule contract | [Versioned tactical rules](./TACTICAL-COMBAT-RULES.md) |

## Purpose

Project one immutable, privacy-reduced planning context from exactly one
existing `CombatSnapshot`. The context carries the stable save revision, one
atomic current-screen observation set, the installed GameData version, one
exact tactical rule-set resolution, fixed loadout mechanics, and an optional
explicit proposal. It does not reread the save or catalogue during mapping.

`ReadTacticalExecutionContext` owns the application flow:

1. honor pre-cancellation;
2. call `ICombatSnapshotReader.ReadAsync` exactly once;
3. select the installed version reported by that snapshot;
4. resolve `VerifiedTacticalCombatRuleSets.HistoricalMagicSound` once; and
5. project the snapshot, rule result, and optional proposal once.

The use case depends only on the existing read-only snapshot port. Its unit
tests pin the one-call boundary, and the DI composition registers no mutable
source or game-control capability.

## Revision and time model

`SourceRevisionFingerprint` is the uppercase SHA-256 already produced by the
guarded archive session. `ObservationRevisionFingerprint` hashes the canonical
field identity, typed source, and opaque evidence reference for each field
source. It deliberately excludes observation time. `SemanticFingerprint`
combines those two revisions with the rule-set fingerprint and all current and
proposed semantic facts.

`CapturedAtUtc` and `LatestObservationAtUtc` remain diagnostic metadata on the
separate Application `TacticalExecutionContextReadResult`, not the Domain
context. Changing only those times does not change the observation or semantic
fingerprint. A changed save, observation identity/source, rule set, current
fact, or proposal does.

The projector requires the rule-resolution GameData version to equal the
snapshot version. A missing snapshot version uses the stable
`UNAVAILABLE` identity. A different resolution is rejected rather than mixed
with the snapshot.

## Fact contract

Every value is a `TacticalContextFact<T>` with:

- state: `Available`, `Unknown`, `Unsupported`, or `Conflicting`;
- origin: save, current-screen observation, proposal, installed
  configuration, verified rule, manual confirmation, or runtime unavailable;
- availability: fixed for this request, pre-combat configurable, manually
  observable, or unavailable to runtime-independent planning;
- a stable reason identity; and
- one or more stable evidence identities.

Unavailable facts cannot expose a selected value. A conflicting fact requires
at least two evidence identities. Therefore missing distance, resources,
active roles, budgets, or assignments cannot silently become zero, an empty
set, or a satisfied requirement.

`CurrentTacticalExecutionFacts` and `ProposedTacticalExecutionFacts` are
different public types. Proposed facts receive `ProposedPlan` origin only when
the proposal explicitly supplies them. Fixed inner-power mechanics and
legendary cost-slot definitions retain their source origin when reused by the
proposal.

## Initial vertical projection

| Fact | Current source | Proposed behavior |
|---|---|---|
| Equipped weapon types | Complete typed equipment/subtype snapshot; unknown if any relevant kind/subtype is missing | Explicit requirement-context set |
| Unlocked weapon types | Unknown; requires manual confirmation | Explicit requirement-context set |
| Usable combat styles | Unsupported | Unsupported |
| Distance | Live value not captured; manually observable | Explicit value or unknown |
| Stance and breath | Live values not captured; manually observable | Live values remain unknown |
| Required resources | Live values not captured; manually observable | Explicit complete supplied amounts or unknown |
| Active defense/agility | Live roles not captured; manually observable | Explicit equipped role or unknown |
| Inner power | Mechanical state ID and typed element adjustments only | Fixed current mechanics |
| Category budgets | Save or newer current-screen observation | Explicit proposal or unknown |
| Universal slots | Save or newer current-screen observation | Explicit proposal or unknown |
| Legendary cost slots | Complete current snapshot | Fixed current slot definitions |
| Legendary assignments | Complete current snapshot | Explicit proposal or unknown |
| Equipped skills | Save or newer current-screen observation | Explicit requirement-context set |

Inner-power display name and raw effect description are stripped. Player and
target character IDs, names, ages, features, target payload, equipment instance
IDs, save path, raw GameData objects, exceptions, and process state are not
properties of the result.

## Observation precedence and atomicity

The existing `CombatSnapshotObservationMerger` remains the sole precedence
boundary. It applies a current-screen loadout only when it is newer than the
disk save, or by explicit source precedence when the save timestamp is
unavailable. It validates all observed skills before constructing a replacement
player and snapshot, then replaces the complete affected loadout field set and
its source identities together. Invalid observations do not produce a partial
snapshot.

`CombatSnapshot` rejects duplicate field-source identities. The tactical fact
contract can retain a conflict supplied by a future observation adapter, but
the projector never invents a winner or converts conflict to absence.

## Rule projection

For the historical verified GameData version, transition and skill-role
matches are projected as stable rule identity, kind, applicability, and unmet
evidence identities. They contain no raw description or effect payload.

For the installed newer version, resolution is
`UnsupportedGameDataVersion`. The context exposes an empty resolved-rule list
and `HasCompatibleRules == false`; historical rules are not surfaced as stale
fallbacks.

## Cancellation and bounded work

Cancellation is checked before the source read, after it, before projection,
and within equipment, observation-source, transition, and role loops. Every
loop is bounded by an immutable request or snapshot collection. The feature
contains no file enumeration, network, process/input control, database,
reflection-based GameData access, persistence, or mutation operation.

## Verification

Focused Domain tests cover source/origin mapping, explicit unknown and
unsupported facts, proposal separation, conflicting evidence, version
mismatch, cancellation, semantic stability across capture-time changes, and
fingerprint changes after semantic proposal changes. Application tests prove
one snapshot-port call and zero calls after pre-cancellation. Architecture tests
forbid mutation, game-control, network, persistence, and unbounded source APIs
and inspect the public result boundary.

The guarded local integration test performs a pre-cancelled request and two
complete reads. It verifies identical save, observation, and semantic
fingerprints and compares every inspected source length, SHA-256, and last-write
timestamp before and after. The configured local run passed against the newer
installed GameData version and correctly produced typed unsupported rules.
