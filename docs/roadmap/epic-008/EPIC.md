# EPIC-008: Evidence-backed exact-target tactical combat planner

| Field | Value |
|---|---|
| Status | Complete |
| Milestone | 8 |
| Target release | TBD |
| Last updated | 2026-08-21 |

## Summary

Turn one verified exact-target combat profile into a conditional,
information-only battle plan that explains not only which feasible 功法 fit,
but also when and why the player should use them.

Epic 8 extends the existing recommendation flow:

```text
Immutable player and target evidence
    -> versioned causal combat chain
    -> verified learned-skill role discovery
    -> bounded target-aware candidate search
    -> evidence-aware selection
    -> conditional preparation, opening, trigger, recovery, and finish plan
```

The first delivery remains deliberately narrow. It uses one already verified
high-value magic-sound target family from Epics 1 and 5, then verifies the
additional transition, execution-context, recovery, and finish evidence needed
for a useful tactical plan. A representative target validates the planner; it
does not establish universal combat rules or complete target coverage.

The planner never controls combat, equips a skill, changes practice direction,
allocates slots, reads process memory, predicts a win, or modifies any save,
game file, configuration, runtime state, or in-game state.

## Context

Epics 1 through 5 established immutable combat snapshots, exact effect and
counter rules, feasible loadout generation, policy scoring, manual preparation,
observations, comparisons, target archetypes, and reusable counter playbooks.
Those foundations answer which verified loadout options are feasible and why.
They do not yet model the target's mechanics as an ordered causal chain or
produce a conditional plan for reacting to combat states.

The current manual plan orders selected counters by broad activation timing.
The current candidate generator starts from a curated option set rather than
reviewing the complete learned-skill atlas for verified tactical roles. The
current scoring model treats threats mostly as flat coverage and accepts an
optional caller-supplied damage component. These are valid completed Epic 1
boundaries, but they are insufficient for claims about interrupts, recovery,
finish windows, or exact execution reliability.

Epic 8 therefore began with an evidence gate. E8-000 identified the exact
historical-version target transitions, runtime-independent player context,
selected counter costs, recovery boundary, and honest fallback supported by
the available evidence. The installed runtime is newer than the verified
mechanical rules, so current-version production behavior remains unsupported
until it is reverified. No tactical rule, score, or instruction may be
implemented merely to fill a desired six-stage plan.

## Primary user story

> As a player preparing for a selected target, I want a conditional plan that
> tells me what to prepare, how to open, which verified target states should
> trigger a response, how to recover from execution costs, and when a supported
> finish or fallback is feasible, so I can carry out the plan manually in the
> game.

## Supporting user stories

- As a player, I can distinguish verified transitions from incomplete,
  unsupported, or conflicting parts of the target chain.
- As a player, I can see which facts came from the save, local GameData, a
  confirmed observation, or a versioned tactical rule.
- As a player, I receive only learned skills whose exact direction, effect,
  role, requirements, and timing are verified for the supported version.
- As a player, I can see why a learned skill was rejected or left unsupported
  instead of assuming that it was overlooked.
- As a player, I can see required weapon, style, distance, resource, active-role,
  inner-power, capacity, and preparation conditions without optimistic defaults.
- As a player, I can follow separate preparation, opening, trigger, recovery,
  finish, and fallback guidance.
- As a player, I am told when a required transition or finish condition is not
  supported instead of receiving a simulated or invented step.
- As a player, Safe, Balanced, and Aggressive retain distinct disclosed
  meanings even when one score component is unavailable.
- As a player, I can see how much of the eligible search space was explored and
  which bound or pruning rule affected the result.
- As an API consumer, I receive typed chain, context, candidate, search, score,
  transition, step, evidence, and unavailable-state semantics.
- As a bilingual, keyboard, or mobile user, I receive the same plan and
  evidence without relying on color or a wide graph.

## Goals

1. Verify one exact target's minimum causal chain, response timing, execution
   costs, recovery path, and feasible finish evidence.
2. Define immutable, versioned target-state and transition contracts with
   explicit provenance, unknowns, conflicts, and stable fingerprints.
3. Carry every available execution fact required to decide whether a proposed
   plan can actually be followed.
4. Consider the complete learned-skill snapshot while admitting only skills
   with exact verified typed roles, effects, timing, and requirements.
5. Preserve all existing ownership, mastery, direction, breakthrough, raw
   effect, backlash, active-role, inner-power, and capacity hard gates.
6. Prune demonstrably irrelevant or dominated options before bounded search.
7. Report candidate-universe, pruning, exploration, time, result, and
   cancellation diagnostics deterministically.
8. Score causal coverage, timing, interactions, layered protection, execution
   cost, and verified damage without double-counting flat threat codes.
9. Produce a conditional plan covering preparation, opening, target-state
   triggers, recovery, finish, and evidence-backed fallback.
10. Reuse one immutable request snapshot and catalogue projection throughout
    the recommendation.
11. Deliver typed API contracts and a concise bilingual, responsive,
    accessible UI.
12. Preserve absolute game non-interference and make no win-probability claim.

## Non-goals

- Simulating combat or reproducing hidden game mechanics.
- Predicting a probability of victory, damage distribution, expected turns,
  or universal target difficulty.
- Supporting every target, combat style, 功法, effect, resource, or transition
  in the first delivery.
- Admitting a skill because of its localized name, category, raw effect text,
  apparent similarity, or community reputation.
- Treating missing damage evidence as zero, average, safety, or failure.
- Treating unused capacity as automatically valuable or automatically wasteful.
- Replacing existing feasibility, comparison, observation, or playbook
  foundations with a parallel recommendation engine.
- Learning tactical rules automatically from reported wins or losses.
- Persisting battle plans, observations, preferences, or outcomes in the first
  delivery.
- Capturing screenshots, performing OCR, accepting uploads, or interpreting
  images automatically.
- Reading runtime memory, attaching to the process, capturing input, simulating
  input, executing combat, changing equipment, changing direction, allocating
  slots, or modifying game-owned state.
- Companion development, village planning, library planning, or other product
  areas outside combat preparation.

## Product principles

### Evidence precedes desired plan shape

Preparation, opening, trigger, recovery, finish, and fallback are output
categories, not permission to invent six complete stages. If E8-000 cannot
verify a transition or condition, the plan exposes the gap and omits the claim.

### A causal chain is not a simulator

The model contains only selected versioned states and transitions needed for
the supported scenario. It does not advance time, calculate hidden AI choices,
or claim to reproduce combat. A transition says that verified evidence supports
one condition-and-effect relationship; it does not guarantee that the player
will cause or observe it in every battle.

### Exact-target facts remain authoritative

Reusable archetype playbooks provide candidate response goals. Exact target
skills, effects, equipment, observations, conflicts, and unavailable facts
decide which transitions and tactical steps are applicable now.

### Complete consideration does not mean unsupported recommendation

The complete learned-skill atlas is considered so the planner can report which
skills are eligible, rejected, or unsupported. A skill enters candidate search
only through a version-matched typed role and exact effect identity followed by
all existing feasibility gates.

### Hard feasibility precedes search and score

Ownership, mastery, direction, breakthrough availability, raw effect identity,
inner-power backlash, active-role compatibility, combat requirements, effective
cost, category capacity, and universal-slot allocation remain filters. A score
cannot compensate for a plan that cannot be equipped or executed.

### Unknown context is not an empty context

Missing weapon, style, distance, stance, breath, resource, or active-role facts
produce an explicit unknown requirement and fallback. They are never replaced
by an empty set, zero, or an assumed satisfied condition.

### Scoring represents supported marginal value

Threat-chain coverage, timing, interaction, layered protection, execution cost,
and damage may affect ranking only through typed version-matched rules. Duplicate
coverage is not counted repeatedly. Unused capacity receives value only when a
documented reserve or marginal-value rule supports that interpretation.

### Search is bounded and accountable

Candidate discovery, pruning, combination exploration, and result selection are
deterministic, cancellable, and bounded. Every result identifies the eligible
universe, removed options, explored combinations, and the first bound that
limited completeness.

### Instructions are conditional information

Every tactical step names its trigger, required state, action to perform
manually, expected verified purpose, evidence, failure or unknown branch, and
relationship to the selected loadout. It never represents a command sent to the
game.

### Game non-interference is permanent

Epic 8 follows
[ADR-0001](../../architecture/ADR-0001-absolute-game-non-interference.md).
All source access is guarded and read-only. Domain, Application, API, UI, and
test contracts describe evidence and manual guidance only.

## Product vocabulary

### Tactical evidence snapshot

One immutable projection of the player, target, observations, catalogue,
installed-data versions, verified rule versions, execution context, warnings,
and fingerprints used by one tactical-planning request.

### Combat state fact

A typed fact relevant to the selected causal chain, such as an exact target
skill phase, mark, resonance state, active role, resource state, threshold,
temporary lockout, or player readiness condition. Every fact retains provenance
and an available, incomplete, unsupported, or conflicting state.

### Tactical transition

A versioned relationship between typed preconditions and typed resulting facts,
with timing, evidence, limitations, and a stable identity. A transition is not a
simulation step or prediction that it will occur.

### Execution context

The exact available facts needed to evaluate a candidate or step, including
equipped and unlocked weapon types, usable combat styles, current or opening
distance, stance and breath, other verified resources, active defense and
agility roles, category budgets, universal-slot allocation, inner-power state,
and legendary-book cost effects.

### Tactical skill role

A versioned typed purpose established for one exact skill direction and effect,
such as suppressing a cast, reducing power or mark duration, preserving mind or
movement, recovering from a lockout, selecting a damage channel, or exploiting
a finish window. It is not inferred from the skill name or category.

### Candidate consideration result

The admitted, rejected, or unsupported result for one learned skill direction.
It retains the exact role, evidence, hard-gate failures, unknown requirements,
dominance or relevance decision, and stable identity.

### Tactical plan

An immutable ordered set of conditional preparation, opening, reaction,
recovery, finish, and fallback steps associated with one feasible loadout and
one evidence snapshot. Each step has a trigger, requirements, manual action,
verified purpose, evidence, and unresolved branch.

### Search coverage diagnostic

The eligible-option count, options removed by each documented pruning rule,
combinations considered, results retained, elapsed budget state, cancellation
state, cache-reuse facts, and any option, exploration, time, or result limit that
affected completeness.

## Initial delivery boundary

E8-000 narrowed the first delivery after verifying:

1. one stable representative magic-sound target identity or synthetic equivalent;
2. the target's core Direct-practice cast, mark, resonance, and defeat-prevention
   or reset relationships that are independently supported;
3. at least one exact suppression or interrupt path and its own execution cost;
4. one evidence-backed recovery or safe fallback after that execution cost;
5. the minimum player execution context required to evaluate the selected path;
6. one typed damage-channel or other finish-window rule, if supported;
7. exact source versions, provenance precedence, unavailable states, and
   standalone runtime safety; and
8. a representative immutable scenario that can be committed without
   proprietary identities or raw game content.

The acceptance invariant is stronger than one exact loadout. For the pinned
historical version, the plan must suppress the verified core cast path,
mitigate the verified mark and resonance path, recover from the selected
counter's verified cost, respect the player's inner-power and
unavailable-direction constraints, fit the exact displayed budgets, and retain
an explicit fallback. No supported finish rule was found, so the initial
vertical is fallback-only. These historical rules do not authorize behavior
for the newer installed runtime.

## Functional scope

### 1. Evidence and representative tactical scenario

Inspect only the minimum permitted save, local catalogue, installed GameData,
and confirmed observation sources needed for the target chain, execution
context, selected skill roles, recovery, and finish claims. Record exact fields,
units, versions, source ownership, precedence, safety, performance, and gaps.

### 2. Product and interaction contract

Define the six plan stages, causal-chain vocabulary, candidate consideration,
search completeness, policy semantics, progressive disclosure, responsive
layout, keyboard behavior, and unavailable/conflict presentation before public
contracts are implemented.

### 3. Immutable causal-chain Domain

Add presentation-neutral contracts for state facts, transitions, requirements,
timing, evidence, limitations, conflicts, tactical roles, plan branches,
diagnostics, canonical ordering, validation, and fingerprints.

### 4. Coherent execution-context projection

Extend the existing immutable combat snapshot with only the evidence E8-000
proves necessary. Reuse one bounded save/catalogue projection per request and
preserve every unavailable or conflicting fact.

### 5. Learned-skill discovery and bounded search

Review the complete learned-skill snapshot, resolve exact typed tactical roles,
apply all hard feasibility gates, remove demonstrably irrelevant or dominated
options, and explore the remaining combinations within explicit deterministic
bounds.

### 6. Evidence-aware scoring and plan compilation

Select feasible candidates using disclosed policy semantics for chains,
interactions, timing, layered protection, execution reliability, supported
damage, and marginal slot value. Compile a conditional plan from the selected
loadout without inventing missing transitions.

### 7. Application and API workflow

Produce one coherent result containing the snapshot summary, target chain,
candidate consideration, search coverage, selected loadout, policy score,
tactical plan, comparison, evidence, and diagnostics. Expose typed contracts
without local paths, raw proprietary payloads, or mutation-capable types.

### 8. Bilingual responsive UI

Add a tactical-plan section to the existing recommendation workflow. Present a
concise manual timeline first, then expose target-chain, candidate, search, and
score evidence through progressive disclosure. Wide and narrow layouts expose
the same facts from one semantic structure.

### 9. Verification and lifecycle

Cover Domain, Application, Infrastructure, API, Presentation, localization,
cancellation, bounds, caching, guarded reads, observation replacement, and
architecture restrictions. Repeated requests prove stable semantic identity;
source hashes prove non-interference.

## User-visible states

- tactical evidence or rule version unsupported;
- target chain confirmed, partial, conflicting, or unsupported;
- execution context complete, incomplete, or conflicting;
- learned skill admitted, rejected, unsupported, or irrelevant;
- no eligible tactical option;
- feasible candidates available with complete search;
- feasible candidates available with option, exploration, time, or result
  truncation;
- selected policy with one or more unavailable score components;
- conditional plan available with explicit unresolved branches;
- finish path supported, fallback-only, or unsupported;
- observation applied or cleared through one atomic replacement;
- request cancelled without a mixed partial result;
- save changed during read and the result was discarded; and
- safe calculation or source failure with retry guidance.

## Epic acceptance criteria

- [x] E8-000 records exact versioned evidence and selects or narrows one
      representative tactical vertical before production rules are added.
- [x] Target mechanics are represented as typed causal facts and transitions,
      not a flat name-derived list or combat simulation.
- [x] Missing, unsupported, or conflicting transitions and execution facts
      never become satisfied requirements or optimistic defaults.
- [x] The complete learned-skill snapshot is considered, while only exact
      verified roles and effects enter candidate search.
- [x] Every candidate continues to pass the existing ownership, mastery,
      direction, breakthrough, effect, backlash, role, inner-power, cost, and
      capacity hard gates.
- [x] Search is deterministic, cancellable, bounded, target-aware, and explicit
      about candidate, pruning, exploration, time, and result coverage.
- [x] Score components preserve unavailable states and do not double-count
      duplicate flat threat coverage or automatically reward unused capacity.
- [x] Safe, Balanced, and Aggressive remain visibly and behaviorally distinct
      under the supported evidence matrix.
- [x] The tactical plan covers supported preparation, opening, trigger,
      recovery, finish, and fallback stages with typed conditions and evidence.
- [x] Every instruction is information-only and agrees with the selected
      feasible loadout, comparison, and manual preparation steps.
- [x] One immutable request snapshot supplies the complete result; observation
      apply/clear replaces every dependent result atomically.
- [x] API and UI preserve every evidence, unavailable, conflict, search, score,
      and plan state without exposing machine paths or proprietary raw content.
- [x] The UI is bilingual, responsive, keyboard accessible, concise, and does
      not rely on color or a wide causal graph.
- [x] Automated and representative manual verification prove determinism,
      bounded behavior, source non-interference, and the selected scenario's
      tactical invariants.
- [x] No result claims a probability of victory or introduces game control.
- [x] The product owner records the Epic 8 completion decision.

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| Desired plan stages encourage invented mechanics | Make E8-000 a blocking evidence gate and permit explicit absent stages |
| A causal model drifts into simulation | Model only verified facts and transitions; forbid clocks, hidden AI, and predicted state advancement |
| The learned-skill universe creates combinatorial growth | Type roles first, reject unsupported skills, prune only with documented proofs, and retain hard bounds |
| Pruning silently removes a useful candidate | Return per-rule counts and candidate diagnostics with deterministic fixtures |
| Missing execution context is treated as satisfied | Use typed unknown requirements and require a fallback or unresolved branch |
| Flat threat scoring rewards duplicate protection | Score marginal chain/timing value and preserve interaction evidence |
| Aggressive policy invents damage | Exclude unavailable damage weight and prohibit win or damage predictions without typed evidence |
| Unused slots are rewarded without combat value | Require a documented reserve or marginal-value rule; otherwise expose neutral unused capacity |
| Plan steps disagree with the selected loadout | Compile from the accepted feasible candidate and assert cross-layer parity |
| Repeated catalogue and snapshot work makes search slow | Reuse one immutable request projection and record cache and performance evidence |
| Tactical wording sounds like automation | Use conditional manual verbs and retain a visible information-only boundary |
| The workflow drifts toward runtime inspection | Enforce ADR-0001 in ports, architecture tests, API verbs, and UI actions |

## Completion decision

Technical, guarded representative, independent, and bilingual responsive
verification are complete. The product owner approved completion on
2026-08-21, as recorded in
[E8-013](../../reviews/E8-013-manual-verification.md). The completed boundary
remains the historical representative tactical vertical; current-version
behavior and the broader anti-magic-sound loadout expansion remain separately
evidence-gated in E8-F01 through E8-F07.

## Delivery reference

Implementation order and item-level evidence are tracked in
[the Epic 8 backlog](./BACKLOG.md). The interaction contract is recorded in
[UI-008](./UI-008-tactical-combat-planner.md) and the accepted
[tactical planning product contract](../../architecture/TACTICAL-COMBAT-PLANNING-CONTRACT.md).

PI-012 was promoted into this epic from
[future product ideas](../FUTURE-PRODUCT-IDEAS.md#pi-012--evidence-backed-tactical-combat-planner)
on 2026-08-20.
