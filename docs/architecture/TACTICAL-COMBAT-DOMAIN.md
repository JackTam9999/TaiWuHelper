# Tactical combat Domain contracts

| Field | Value |
|---|---|
| Status | Implemented |
| Epic | [EPIC-008](../roadmap/epic-008/EPIC.md) |
| Backlog item | [E8-002](../roadmap/epic-008/BACKLOG.md#e8-002--add-immutable-tactical-state-transition-and-plan-contracts) |
| Product contract | [Tactical combat planning contract](./TACTICAL-COMBAT-PLANNING-CONTRACT.md) |
| Evidence boundary | [E8-000 tactical evidence](../scenarios/E8-000-tactical-combat-evidence.md) |

## Purpose

Provide one immutable, presentation-neutral vocabulary for a bounded tactical
state model, exact versioned transitions, requirement outcomes, skill roles,
candidate decisions, search coverage, and a conditional manual plan.

The contracts describe evidence relationships. They do not read a save,
inspect a process, advance a combat clock, choose hidden target behavior,
calculate damage, predict an outcome, mutate a loadout, or send an action to
the game.

## Aggregate

`TacticalCombatPlan` is the validated aggregate root. It owns:

| Contract | Responsibility |
|---|---|
| `TacticalEvidenceReference` | Stable evidence identity, source kind, exact GameData version, rule version, and scope |
| `TacticalStateFact` | One typed fact in an available, incomplete, unsupported, or conflicting state |
| `TacticalRequirementDefinition` | A typed fact predicate without an evaluation result |
| `TacticalRequirementEvaluation` | Satisfied, unsatisfied, unknown, unsupported, or conflicting outcome with evidence |
| `TacticalTransition` | Preconditions, resulting facts, timing, expected verified purpose, limitation, and evidence |
| `TacticalSkillRole` | Exact skill, direction, effect, timing, transition, requirements, and tactical role identity |
| `TacticalCandidateConsideration` | One terminal admitted, rejected, unsupported, irrelevant, or dominated decision per learned direction |
| `TacticalSearchCoverage` | Candidate accounting, configured bounds, exploration/result counts, first terminator, elapsed diagnostic, and cache reuse |
| `TacticalPlanStageDefinition` | One supported, omitted, or unsupported canonical plan stage |
| `TacticalPlanStep` | Condition facts, evaluated requirements, transition, manual action, expected purpose, limitation, evidence, and outgoing branches |
| `TacticalPlanBranch` | Continue, fallback, unresolved, or stop outcome with an optional typed target step |

All collections exposed by these contracts are `ImmutableArray<T>` values
copied during construction. Source collection mutation cannot change a result.

## Stable identities

Facts combine `TacticalFactKind` and an uppercase stable code. Roles combine
`TacticalRoleKind` and a stable code. A candidate combines a non-negative
skill ID and exact Direct or Reverse `PracticeDirection`. Requirements,
transitions, and plan steps have their own stable identities.

Identity, reason, condition, manual-action, purpose, limitation, evidence, and
scope codes accept uppercase ASCII letters, digits, underscore, hyphen,
period, and colon. These values are semantic references, not localized display
copy. Versions remain exact opaque non-blank strings so their case and product
metadata are preserved.

## Evidence states and values

`TacticalFactValue` explicitly distinguishes Boolean, Integer, and Code values.
It has no untyped object value, localized string fallback, or default sentinel.

| Fact state | Selected value | Conflict values | Meaning |
|---|---:|---:|---|
| `Available` | Required | Forbidden | One applicable value is supported by evidence |
| `Incomplete` | Forbidden | Forbidden | Required source coverage is incomplete |
| `Unsupported` | Forbidden | Forbidden | No approved source or rule mapping can decide the fact |
| `Conflicting` | Forbidden | At least two distinct values | Applicable sources disagree without safe precedence |

Every state retains evidence and a stable reason. Conflict values retain their
individual evidence sources. A conflicting state cannot select a winning
value. Missing, incomplete, and unsupported facts cannot become zero, false,
or an empty code.

Every evidence reference names both the aggregate GameData version and rule
version. The root rejects any reference whose versions differ from its exact
versions, including evidence nested in facts, conflicts, requirements, roles,
candidates, stages, and steps.

## Requirements

Definitions support `Present`, `Absent`, `Equal`, `AtLeast`, and `AtMost`.
Presence predicates forbid an expected value. Equality requires a typed value;
range predicates require an Integer value.

Evaluation outcomes are separate from definitions:

| Outcome | Meaning |
|---|---|
| `Satisfied` | Applicable evidence proves the requirement passes |
| `Unsatisfied` | Applicable evidence proves the requirement fails |
| `Unknown` | A required value is not currently available |
| `Unsupported` | No approved mapping or rule can evaluate it |
| `Conflicting` | At least two applicable evidence sources prevent one outcome |

Only `Unsatisfied` is a verified false gate. Unknown, unsupported, and
conflicting results cannot be treated as failed numeric values or scoring
penalties.

## Transitions are relationships, not simulation

A `TacticalTransition` requires:

- one or more referenced requirement definitions;
- one or more referenced resulting fact identities;
- one exact `TacticalTransitionTiming`;
- a stable expected-purpose identity;
- a stable limitation identity; and
- version-matched evidence.

The type exposes no method to apply a result, mutate a state, advance time,
choose a branch, or calculate frequency. A resulting fact says only what the
verified relationship supports when its preconditions apply. The Application
layer must still evaluate current requirements and preserve unknowns.

## Exact tactical roles

`TacticalSkillRole` binds one non-negative skill ID, Direct or Reverse
direction, non-negative raw effect ID, role kind, timing, transitions,
requirements, limitation, and evidence. Neutral practice is invalid for a
direction-specific role.

The aggregate verifies every transition and requirement reference. A
candidate may reference a role only when its skill and direction are exactly
the same. Names, categories, and localized descriptions cannot establish a
role.

## Candidate decisions

Each `TacticalCandidateConsideration` has exactly one terminal decision:

| Decision | Construction invariant |
|---|---|
| `Admitted` | At least one exact role, every retained requirement satisfied, no dominator |
| `Rejected` | At least one exact role and at least one unsatisfied hard requirement |
| `Unsupported` | No exact role or at least one unknown, unsupported, or conflicting requirement |
| `Irrelevant` | At least one exact role, every retained requirement satisfied, no applicable chain contribution |
| `Dominated` | Comparable roles, satisfied requirements, and one distinct admitted dominator |

The aggregate rejects a missing or non-admitted dominator. These states apply
only to the exact target, evidence, direction, and context; they do not express
general skill or character quality.

## Search coverage

`TacticalSearchBounds` uses explicit units:

- maximum admitted options;
- maximum explored combinations;
- maximum monotonic elapsed `TimeSpan`; and
- maximum retained results.

Coverage accounts for the candidate universe exactly once as admitted,
rejected, unsupported, irrelevant, or dominated. Role-supported, searched
option, explored-combination, feasible-result, and retained-result counts keep
their different units.

The first terminator is `None`, `OptionLimit`, `ExplorationLimit`, `TimeLimit`,
`ResultLimit`, or `Cancelled`. Construction verifies the associated bound:

- `None` requires every admitted option searched and every feasible result
  retained;
- `OptionLimit` requires the option maximum to be full while admitted options
  remain;
- `ExplorationLimit` requires the exploration maximum to be reached;
- `TimeLimit` requires measured elapsed time to reach the configured budget;
- `ResultLimit` requires the result maximum to be full while feasible results
  remain; and
- `Cancelled` is never complete.

Only `None` makes `IsComplete` true. Cache kinds and hit/miss counts are
canonical diagnostics but cannot affect result identity or ranking.

## Plan stages and branches

The aggregate requires each stage exactly once and sorts them by canonical
ordinal:

1. `Preparation`;
2. `Opening`;
3. `TargetStateResponse`;
4. `Recovery`;
5. `Finish`; and
6. `Fallback`.

A supported stage requires one or more steps. Omitted and unsupported stages
require no steps and retain evidence plus an exact limitation. Later stage
ordinals never change when an earlier stage is omitted.

Each supported step keeps observed facts, evaluated requirements, transitions,
manual action, expected purpose, limitation, outgoing branch outcomes, and
evidence in separate properties. Every reference must resolve inside the same
aggregate.

Continue and fallback branches require a target. Unresolved and stop branches
are terminal and forbid one. Fallback outcomes must target the Fallback stage;
a normal continue outcome cannot silently enter that stage. Only fallback
stage steps use the fallback branch kind.

Targets must be later in the same stage or in a later canonical stage. The
root performs explicit depth-first cycle validation as well. Self-targets,
backward edges, dangling targets, and cycles fail construction.

## Canonical ordering and validation

Construction rejects:

- null collection members;
- duplicate facts, requirements, transitions, roles, candidates, stages,
  steps, branches, evidence, conflicts, or cache identities;
- missing required state values or invalid conflict sets;
- invalid enum values, IDs, directions, operators, counts, and bounds;
- dangling fact, requirement, transition, role, dominator, or step references;
- candidate roles belonging to another skill or direction;
- search counts that disagree with candidate decisions;
- source or rule version mismatches;
- a missing or duplicate canonical stage;
- a step stored under another stage or duplicate step order; and
- invalid or cyclic branch topology.

Facts, requirements, transitions, roles, candidates, evidence, and diagnostics
sort by stable identity. Stages sort by ordinal; steps sort by explicit order
then identity. Equivalent shuffled inputs therefore produce equivalent
collections and fingerprints.

## Fingerprints

`TacticalCombatPlan.Fingerprint` is an uppercase SHA-256 over schema marker
`TACTICAL_COMBAT_PLAN_V1` and every semantic value that can change the result:

- exact GameData and rule versions;
- shared evidence identities and scopes;
- facts, values, states, conflicts, and evidence;
- requirements and evaluations;
- transitions, roles, and candidate decisions;
- configured search bounds, semantic counts, and first terminator; and
- stage states, steps, conditions, actions, purposes, branches, evidence, and
  limitations.

The model contains no localized display text or capture time. Measured elapsed
time and cache hit/miss counts are diagnostic and intentionally excluded from
both the search and aggregate fingerprints. The configured elapsed bound is a
semantic input and remains included.

## Dependency boundary

The implementation lives entirely in `TaiWu.Domain.TacticalCombat`. It uses
base-library types plus the existing Domain `PracticeDirection`. It references
no Application, Infrastructure, ASP.NET, Razor, filesystem, process,
reflection, SQLite, or GameData type.

## Verification

Focused tests cover evidence-state invariants, all requirement outcomes,
transition claim separation, all candidate decisions, search accounting,
immutability, canonical ordering, semantic and diagnostic fingerprint inputs,
dangling references, duplicates, incompatible versions, invalid stage sets,
and backward/cyclic branches:

```powershell
dotnet test tests\TaiWu.Domain.UnitTests\TaiWu.Domain.UnitTests.csproj -c Release --no-restore -- --filter-class '*TacticalCombatContractsTests*'
```

The completed verification passed 21 focused tests, all 570 Domain tests, and
all 105 architecture tests with no failures or skips. Solution formatting was
clean, and the Release solution build completed with no warnings or errors.
