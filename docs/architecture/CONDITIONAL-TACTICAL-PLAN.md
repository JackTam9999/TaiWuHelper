# Conditional tactical battle-plan compilation

| Field | Value |
|---|---|
| Status | Implemented |
| Epic | [EPIC-008](../roadmap/epic-008/EPIC.md) |
| Backlog item | [E8-008](../roadmap/epic-008/BACKLOG.md#e8-008--compile-a-conditional-preparation-to-fallback-battle-plan) |
| Product semantics | [Tactical combat planning contract](./TACTICAL-COMBAT-PLANNING-CONTRACT.md#plan-stage-contract) |
| Score input | [Evidence-aware tactical recommendation scoring](./TACTICAL-RECOMMENDATION-SCORING.md) |

## Purpose and boundary

`TacticalCombatPlanCompiler` converts one explicitly selected, scored,
feasible loadout into an immutable information-only manual plan. The compiler
does not choose another candidate, run another search, modify the feasible
proposal, control the game, simulate time, choose target behaviour, record
completion, or claim success.

The input must retain one coherent chain:

- the exact tactical execution context and applicable rule resolution;
- candidate decisions and a completed bounded search result;
- one scoring request and its exact scoring result;
- one selected loadout stable key present in that result; and
- optional typed trigger and finish proofs already accepted by scoring.

Compilation rejects a mixed context, search, score, rule version, or selected
proposal. It rechecks every selected `CombatSkillCandidate` with the existing
feasibility gate and requires the selected directions to equal those in the
accepted `ProposedCombatLoadout`.

## One six-stage plan

Every compiled plan contains each canonical stage exactly once. Supported
stages contain ordered steps; omitted and unsupported stages contain no
placeholder action.

| Stage | Compiler source | Unsupported or omitted boundary |
|---|---|---|
| Preparation | Exact current/proposed loadout delta, candidate direction validation, accepted capacity and configuration context, and selected `BeforeCombat` roles | Never omitted for a selected loadout |
| Opening | Selected roles timed `CombatStart` or `BeforeFirstUse` | Omitted when no selected role has verified opening timing |
| Target-state response | Other selected applicable roles and their exact transitions | Omitted when the selection has no such role |
| Recovery | Verified self-lock plus the general recovery transition and three separately selected feasible active Reverse candidates | Unsupported when the exact three-cast sequence is not selected; omitted when the selected response has no recovery cost |
| Finish | An available `FinishPath` score component and its exact selected channel, finish role, transition, five typed inputs, context, and version proof | Unsupported when any proof is absent; no generic attack action is inserted |
| Fallback | A separately selected feasible mitigation, recovery, or fallback role | Unsupported when the selected loadout supplies no separate verified action |

The E8-000 historical vertical therefore remains `FallbackOnly` when it has a
selected mitigation fallback and no finish proof. If neither finish nor a
separate fallback is supported, the disposition is `Unsupported`, not an
invented fallback.

## Preparation and Epic 4 parity

Preparation checks are derived from the same accepted
`FeasibleCombatLoadout.Proposal` used by search:

- removals and additions are exact current/proposed category set differences;
- breakthrough and direction checks come from
  `CombatSkillCandidateValidator`;
- category capacity, universal-slot allocation, legendary assignments,
  equipment, weapon, and execution context are retained as explicit manual
  confirmation checks; and
- selected before-combat roles retain their exact skill and direction.

`TacticalCompiledCombatPlan.SelectedLoadout` exposes that unchanged feasible
proposal. `PreparationChecks` contains exactly one entry for every preparation
step, so the existing manual-plan and Epic 4 comparison builders can compare
against the same current and proposed skill sets, direction requirements,
universal allocation, legendary assignments, and slot budgets without a
second projection.

## Step and branch semantics

Each `TacticalPlanStep` retains a stable identity, canonical stage and order,
observed or manually confirmable facts, requirement evaluations, exact
transition identities, manual action identity, bounded expected purpose,
limitation, branches, and version-matched evidence.

Available trigger evidence produces a satisfied requirement. Incomplete or
absent trigger evidence stays unknown and manually confirmable; unsupported
and conflicting evidence retain their separate outcomes. No missing fact is
converted to false or zero.

A failed or unknown condition targets the supported fallback step when one
exists. Otherwise it remains `Unresolved`. Branch targets move only forward
through the canonical stage order. A supported finish stops after its exact
condition succeeds and branches to fallback only when its condition fails or
remains unknown.

## Recovery boundary

Reverse `604` carries the verified three-layer Direct-practice self-lock. The
general recovery rule proves that a feasible Reverse-practice cast removes one
layer, but it does not select executable casts. The compiler emits three
ordered recovery steps only when the selected feasible loadout contains three
distinct active Reverse candidates. Each step retains the general recovery
transition and the exact selected skill used for that conditional cast.

With fewer than three, `Recovery` is explicit `Unsupported` with
`THREE_EXACT_EXECUTABLE_CASTS_NOT_PRESELECTED`. It does not repeat one skill,
borrow an unselected candidate, or silently ignore the lock.

## Coherent identity and lifecycle

The compiled result publishes separate fingerprints for context, observation,
search, scoring, selected loadout, plan, and the complete compiled result. The
observation fingerprint combines the context observation revision with the
ordered typed trigger observations. Applying, replacing, or clearing an
observation therefore creates a new immutable score/plan aggregate; a caller
cannot mutate an already published result into a mixed state.

Selected-loadout identity includes the selected candidate directions, proposed
skills by category, universal allocation, candidate preparation permissions,
and legendary assignments. The plan fingerprint excludes elapsed search time,
capture time, localized text, and presentation state.

Input collections are canonicalized by stable typed identities. Equivalent
source order produces the same selected-loadout, plan, and compiled-result
fingerprints. Cancellation is checked before compilation and throughout rule,
selection, recovery, and step projection; a cancelled call publishes no
partial plan.

## Verification

Focused Domain tests cover:

- the six-stage golden fallback-only plan;
- available and unavailable trigger states;
- the exact three-step recovery boundary;
- unsupported recovery, finish, and fallback stages without placeholder
  actions;
- selected proposal and direction-change parity;
- typed finish support;
- immutable observation replacement;
- shuffled-source determinism; and
- pre-cancellation.

The tactical architecture safety scan includes plan compiler and plan-contract
sources and forbids filesystem mutation, process access, input injection,
network access, registry access, runtime patching, and database access.
