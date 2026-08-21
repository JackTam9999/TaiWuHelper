# Coherent tactical execution context

| Field | Value |
|---|---|
| Status | Implemented |
| Epic | [EPIC-008](../roadmap/epic-008/EPIC.md) |
| Backlog items | [E8-004](../roadmap/epic-008/BACKLOG.md#e8-004--project-one-coherent-tactical-execution-context), [E8-F04](../roadmap/epic-008/BACKLOG.md#e8-f04--project-a-complete-current-and-proposed-execution-context) |
| Evidence boundary | [E8-F04 current execution context](../scenarios/E8-F04-current-execution-context.md) |
| Rule contract | [Versioned tactical rules](./TACTICAL-COMBAT-RULES.md) |

## Purpose

Project one immutable, privacy-reduced planning context from exactly one
existing `CombatSnapshot`. The context carries the stable save revision, one
atomic current-screen observation set, an optional confirmed newer manual
execution observation, the installed GameData version, one exact tactical
rule-set resolution, fixed loadout mechanics, and an optional explicit
proposal. It does not reread the save or catalogue during mapping.

`ReadTacticalExecutionContext` owns the application flow:

1. honor pre-cancellation;
2. call `ICombatSnapshotReader.ReadAsync` exactly once;
3. select the installed version reported by that snapshot;
4. select and resolve the exact historical or current rule set once; and
5. project the snapshot, rule result, and optional proposal once.

The use case depends only on the existing read-only snapshot port. Its unit
tests pin the one-call boundary, and the DI composition registers no mutable
source or game-control capability.

## Revision and time model

`SourceRevisionFingerprint` is the uppercase SHA-256 already produced by the
guarded archive session. `ObservationRevisionFingerprint` hashes the canonical
field identity, typed source, opaque evidence reference, and manual execution
observation values for each field source. It deliberately excludes observation
time. `SemanticFingerprint` uses context schema V2 and combines those two
revisions with the rule-set fingerprint and all current and proposed semantic
facts.

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

## Current E8-F04 projection

| Fact | Current source | Proposed behavior |
|---|---|---|
| Equipped weapon types | Newer manual observation or complete typed equipment/subtype snapshot | Explicit requirement-context set |
| Unlocked weapon types | Newer manual observation or unknown | Explicit requirement-context set |
| Tricks and combat styles | Newer manual observation or unknown | Explicit trick set; explicit style set or unknown |
| Distance | Newer manual observation or unknown | Explicit opening value or unknown |
| Stance and breath | Individual typed resource amounts or unknown | Individual typed resource amounts or unknown |
| Required resources | Newer manual observation or unknown | Explicit supplied collection, including a deliberately empty set |
| Active defense/agility | Newer manual observation or unknown; must match current loadout | Explicit equipped role or unknown |
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

`CombatSnapshotObservationMerger` remains the precedence boundary for loadout,
budget, universal-slot and legendary-book screen fields. It applies a
current-screen loadout only when it is newer than the disk save, or by explicit
source precedence when the save timestamp is unavailable. It validates all
observed skills before constructing a replacement player and snapshot, then
replaces the complete affected field set and source identities together.

`TacticalExecutionObservation` separately carries only explicitly supplied
live fields. It requires newer-than-save confirmation; the Application boundary
rejects a supplied timestamp that is not newer than the save. Collections are
replaced as atomic values, never unioned with save values. An active defense or
agility absent from the same current loadout becomes `Conflicting`.

`CombatSnapshot` rejects duplicate field-source identities. The tactical fact
contract can retain a conflict supplied by a future observation adapter, but
the projector never invents a winner or converts conflict to absence.

## Rule projection

For the historical verified GameData version, transition and skill-role
matches are projected as stable rule identity, kind, applicability, and unmet
evidence identities. They contain no raw description or effect payload.

For the exact installed current version, `ResolveExact` selects
`CurrentLaterMagicSound` and exposes its 21 transitions and 19 roles with
applicability/unmet evidence. Any other version remains
`UnsupportedGameDataVersion`; historical rules are never surfaced as stale
fallbacks.

## Cancellation and bounded work

Cancellation is checked before the source read, after it, before projection,
and within equipment, observation-source, transition, and role loops. Every
loop is bounded by an immutable request or snapshot collection. The feature
contains no file enumeration, network, process/input control, database,
reflection-based GameData access, persistence, or mutation operation.

## Verification

Focused Domain tests cover source/origin mapping, complete and partial manual
observations, explicit empty versus unknown collections, trick/style/resource
transport, proposal separation, active-role conflicts, alternative slot
allocations, version mismatch, cancellation and fingerprints. Application
tests prove exact current rule selection, stale-time rejection, one
snapshot-port call and zero calls after pre-cancellation. Architecture tests
forbid mutation, game-control, network, persistence, public Domain time types
and unbounded source APIs.

Two guarded local checks perform repeated reads, verify the current rule set,
screen capacities `6/10/7/9/4`, unknown live-current boundary and explicit
representative proposal. They compare every inspected source length, SHA-256
and last-write timestamp before and after; all sources remain unchanged.
