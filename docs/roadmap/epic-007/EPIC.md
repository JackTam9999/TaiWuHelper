# EPIC-007: Evidence-aware village workforce and building assignment planner

| Field | Value |
|---|---|
| Status | Active — awaiting closure decisions |
| Milestone | 7 |
| Target release | TBD |
| Last updated | 2026-08-18 |

## Summary

Help the player compare villagers for a selected settlement work objective or
assignment target without claiming that aptitude alone proves productivity.
Epic 7 builds one version-aware, read-only settlement snapshot from the
configured save and permitted installed GameData, applies explicit verified
work rules, and presents eligibility, suitability, tradeoffs, missing facts,
and a manual reassignment checklist.

The first delivery follows this information flow:

```text
Configured save and installed GameData
    -> verified villager, building, and assignment sources
    -> immutable settlement snapshot
    -> versioned work-objective and requirement rules
    -> deterministic worker shortlist and comparison
    -> information-only manual reassignment checklist
```

Epic 7 reuses the stable character, provenance, unavailable-state,
deterministic comparison, localization, accessibility, guarded archive-read,
and non-interference foundations delivered by Epics 1 through 6. It introduces
a separate settlement-work domain. Companion-role suitability from Epic 6 is
not settlement suitability, and village membership is not evidence that a
character is recruitable as a companion.

The feature never assigns a worker, constructs or upgrades a building,
collects resources, recruits a character, changes the party, writes a save, or
controls the game.

## Context

Epic 6 deliberately limited its candidate universe to the saved non-Taiwu
group roster and deferred settlement workers. The helper can read stable base
attributes and martial- and life-skill aptitudes, but those facts do not by
themselves establish villager availability, building compatibility, work
output, assignment constraints, or resource effects.

Players need a different decision model for settlement work. A useful answer
must start with the selected building or work objective, establish which
villagers may actually be considered, distinguish current assignment from a
proposed manual change, and explain every rule that affects ordering. A global
character score or reuse of the companion breadth index would hide the
mechanics that make an assignment valid.

Epic 7 therefore begins with an evidence gate. E7-000 inspects the supported
installed version and one stable configured-save revision, identifies the
candidate universe and exact building, assignment, resource, availability, and
worker facts, and selects one representative vertical. No production ranking
rule is implemented until that gate records standalone-safe semantics.

## Primary user story

> As a player reviewing village work, I want to compare the current worker
> with verified eligible alternatives for one assignment so I can understand
> the tradeoffs and make any change manually in the game.

## Supporting user stories

- As a player, I select an objective or assignment target before workers are
  evaluated.
- As a player, I can see the current assignment and whether its source is
  confirmed, incomplete, unsupported, or conflicting.
- As a player, I can distinguish an eligible worker from a person whose
  presence, availability, or assignment eligibility is unknown.
- As a player, I can see hard requirements before any suitability score.
- As a player, I can trace every scored component to a version-matched source
  and rule.
- As a player, I can compare the current worker with alternatives without
  treating the result as universal character quality.
- As a player, I can see six base attributes and martial/life aptitudes as
  descriptive context without allowing unrelated values to change the work
  result.
- As a player, I receive a concise summary first and expand evidence only when
  needed.
- As a player, I receive a manual checklist that never claims the helper made
  an in-game assignment.
- As an API consumer, I receive typed settlement, worker, assignment,
  evaluation, evidence, and unavailable-state semantics.
- As a bilingual, keyboard, or mobile user, I receive the same facts and
  decision path without relying on color or a wide table.

## Goals

1. Verify the supported villager universe and exact meaning of settlement-work
   eligibility and availability.
2. Verify building, assignment, resource, output, and work-requirement facts
   before selecting the first delivery vertical.
3. Define one immutable settlement snapshot bound to one save fingerprint and
   installed-data version.
4. Model current assignments separately from helper-side proposals.
5. Define explicit, versioned work-objective and assignment rules.
6. Evaluate hard gates before suitability components and ordering.
7. Rank only comparable workers and retain unranked states with honest reasons.
8. Explain decisive strengths, limitations, missing evidence, ties, and the
   difference from the current assignment.
9. Read all required settlement facts through one bounded archive operation,
   not one archive open per villager or building.
10. Deliver typed API contracts and a bilingual, responsive, accessible UI.
11. Preserve deterministic behavior and absolute game non-interference.

## Non-goals

- Declaring one villager universally best for the whole settlement.
- Reusing Epic 6 companion scores as worker productivity.
- Ranking all characters merely because they can be enumerated.
- Treating a life-skill aptitude, main attribute, feature name, building name,
  or raw description as a verified production formula.
- Predicting growth, training, teaching, recruitment, or future potential.
- Identifying recruitable companion prospects without separately verified
  recruitment evidence.
- Optimizing every building, worker, resource, and dependency simultaneously
  in the first delivery.
- Planning construction, upgrades, demolition, collection, or resource routing.
- Library, book inventory, repair, study, or acquisition planning unless a
  later product decision promotes a separately verified slice.
- Persisting assignment proposals, preferences, histories, or outcomes in the
  first delivery.
- Learning rules from player choices or reported outcomes.
- Statistical optimization, machine learning, or success-probability claims.
- Assigning workers, changing buildings, recruiting characters, simulating
  input, attaching to the game, reading process memory, or modifying any
  game-owned state.

## Product principles

### The assignment objective comes before the comparison

There is no global workforce score. Every evaluation names one stable work
objective or assignment definition and version. Results from different
objectives are not comparable and must not be combined into a universal
leaderboard.

### Worker eligibility precedes suitability

A person must first have evidence-backed settlement presence, availability,
and assignment eligibility. A display name, location, aptitude, group status,
or readable character object cannot prove that the person may work in the
selected assignment.

### Current and proposed assignments are different facts

The current assignment is save-derived evidence. A proposed assignment is an
immutable helper-side comparison artifact. The proposal never replaces the
snapshot value and is never sent to the game.

### Aptitude is not productivity without a verified rule

Six base attributes, martial aptitudes, and life-skill aptitudes may be shown as
descriptive saved facts. A value affects eligibility, output, or ordering only
when E7-000 proves the exact versioned mechanic connecting it to the selected
work objective.

### Unknown is not zero

Missing, unavailable, unsupported, stale, or conflicting evidence does not
mean a weak worker or empty output. The evaluation records the exact state and
omits any numeric result that cannot be supported.

### Rules are typed, versioned, and explainable

Every requirement, scored component, normalization rule, weight, output unit,
and tie breaker has a stable identity and supported source version. Localized
labels and untyped descriptions never become mechanics.

### One coherent snapshot owns one result

Villager identity, building state, current assignments, resource facts, worker
profiles, and comparison results come from one immutable snapshot boundary.
The workflow never mixes save revisions or refreshes one row inside an older
result.

### Determinism remains mandatory

Identical save fingerprint, installed-data versions, objective definition,
rule version, filters, and language-independent facts produce identical worker
states, components, ordering, ties, diagnostics, and result identity.

### Game non-interference is permanent

Epic 7 follows
[ADR-0001](../../architecture/ADR-0001-absolute-game-non-interference.md).
Every source read is guarded and read-only. Domain, API, UI, and test contracts
describe information and helper-side proposals only; none can become a game
command.

## Product vocabulary

### Settlement snapshot

One immutable, fingerprinted projection of the supported villager universe,
relevant buildings or work targets, current assignments, required resource
facts, source versions, warnings, and capture time from one stable save read.

### Worker candidate universe

The version-matched set of people E7-000 proves relevant to the first work
vertical. It is not automatically the Taiwu group, every character, everyone
at one location, or every person labelled as a villager. Each member retains an
explicit eligibility state such as eligible, ineligible, incomplete,
unsupported, or conflicting.

### Assignment target

The stable building, position, work slot, or other supported target to which
one worker evaluation applies. E7-000 determines its exact identity and
cardinality; the UI does not invent generic slots where the source has none.

### Work objective definition

A versioned definition containing its stable identity, supported target kind,
eligibility and hard requirements, evidence fields, scored components,
normalization and output rules, tie breakers, limitations, and explanation
identities.

### Worker evaluation

The eligibility, hard-gate outcomes, verified components, work-local score or
output measure when available, strengths, limitations, missing evidence, and
stable result identity for one worker and one assignment objective. It is not
intrinsic character quality or a success probability.

### Assignment comparison

A neutral comparison of the current worker and one or more eligible
alternatives from the same settlement snapshot and objective. It retains exact
ties and does not silently select a winner.

### Manual reassignment checklist

An information-only list describing the current assignment, the proposed
helper-side alternative, prerequisites, reassignment cautions, and facts
to verify manually. It never marks an in-game change complete.

## Initial delivery boundary

E7-000 selected the first vertical after verifying:

1. the worker candidate universe and availability facts;
2. one stable assignment-target identity;
3. the current-assignment representation;
4. every worker input used by the target's requirements or comparison;
5. the exact meaning and unit of any output, capacity, or suitability value;
6. source precedence, version support, and standalone runtime safety; and
7. at least one representative current-assignment and alternative-worker
   scenario that can be described without committing proprietary data.

The first vertical compares one existing shop manager slot with alternatives
from the public work-candidate result. Its only ordering component is the saved
base life-skill qualification selected by the shop's typed required-discipline
field. It does not claim current modified attainment, efficiency, production,
capacity, or success probability. Current manager assignments remain factual
save evidence even when a manager is outside the alternative universe.

Complete village membership is not exposed through the supported public source
surface and remains unsupported. The exact evidence boundary and deferred
mechanics are recorded in
[E7-000-village-workforce-evidence](../../scenarios/E7-000-village-workforce-evidence.md).

## Functional scope

### 1. Evidence and representative scenario

Inspect only the minimum permitted save and installed GameData sources needed
for worker membership, availability, buildings or work targets, current
assignments, required resources, requirements, and output semantics. Record
ownership, runtime type, unit, completeness, precedence, version, safety, and
limitations.

### 2. Product and interaction contract

Define objective selection, assignment identity, eligibility states, hard
gates, component meaning, ties, filters, comparison, manual-plan wording,
responsive layouts, evidence disclosure, and unsupported states before public
contracts are implemented.

### 3. Immutable settlement Domain

Add presentation-neutral Domain contracts for settlement identity, worker
facts, assignment targets, current and proposed assignments, evidence,
conflicts, unavailable reasons, rules, evaluations, comparisons, and stable
fingerprints.

### 4. One-pass read-only source projection

Project all required settlement facts through one bounded configured-archive
read. Preserve cancellation, load warnings, source fingerprint, captured time,
installed versions, and byte-for-byte non-interference evidence.

### 5. Versioned work rules and evaluation

Represent the selected work objective as explicit rules. Evaluate membership,
availability, and hard requirements before any numeric component. Normalize
and combine only verified comparable facts and retain every component.

### 6. Shortlist, comparison, and manual plan

Build a stable shortlist of comparable workers plus separate ineligible and
unranked states. Compare the current assignment with alternatives and describe
manual considerations without claiming that any action occurred.

### 7. Application and API workflow

Compose snapshot reading, rule selection, evaluation, filtering, comparison,
and manual-plan generation into one immutable result. Expose typed contracts
without paths, proprietary raw content, arbitrary reflection values, or
mutation-capable GameData types.

### 8. Bilingual responsive UI

Add a village workforce page with a compact objective and assignment summary,
current assignment, alternative shortlist, optional comparison, expandable
evidence, and explicit loading, empty, incomplete, unsupported, conflict, and
failure states. Shared limitations appear once rather than repeating in every
row.

### 9. Verification and lifecycle

Cover Domain, Application, Infrastructure, API, Presentation, localization,
batching, guarded local reads, and architecture boundaries. Repeated requests
prove stable identity and ordering; source hashes prove non-interference.

## User-visible states

- source discovery or objective unsupported;
- configured save missing, unreadable, unstable, or unsupported;
- settlement snapshot loading, available, partial, unsupported, or failed;
- assignment target available, missing, incomplete, or conflicting;
- current assignment confirmed, incomplete, conflicting, or unsupported;
- worker eligible and comparable;
- worker ineligible with hard-gate reasons;
- worker incomplete, unsupported, or conflicting with no zero fallback;
- explicit tie under the documented rules;
- no eligible alternative for the selected assignment;
- filtered shortlist with original counts retained;
- comparison and manual checklist available or unavailable; and
- save revision changed, requiring a complete new result.

## Epic acceptance criteria

- [x] The supported worker universe and every eligibility rule are backed by
      version-matched evidence.
- [x] One representative assignment vertical is selected with exact source,
      identity, unit, and rule semantics.
- [x] Current assignment and helper-side proposal are separate immutable facts.
- [x] Missing or conflicting evidence never becomes zero, false, or an implied
      productivity penalty.
- [x] Only verified requirements and components affect ordering.
- [x] Six attributes and martial/life aptitudes are descriptive unless a typed
      work rule explicitly uses them.
- [x] Identical inputs produce identical evaluations, ties, ordering,
      diagnostics, and result fingerprints.
- [x] All required settlement facts are read through one bounded save session.
- [x] The API exposes typed states without local paths, raw proprietary data,
      or mutation-capable types.
- [ ] The UI is bilingual, responsive, keyboard accessible, and concise, with
      evidence available through progressive disclosure.
- [x] A manual checklist never claims or triggers an in-game change.
- [x] Automated and representative manual verification prove non-interference.
- [x] Deferred recruitment, development, whole-village optimization, library,
      persistence, and game-control work remains explicitly outside the epic.
- [ ] The product owner records the Epic 7 completion decision.

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| Aptitude is mistaken for production | Require an exact versioned work rule before a value affects ordering; otherwise show it as descriptive only |
| The character universe includes nonworkers | Define source-backed membership and availability gates before evaluation |
| Current assignment and proposal are confused | Give them different types, origins, labels, and API fields |
| Missing data makes a worker appear weak | Preserve incomplete and unsupported states and omit unsupported numeric comparisons |
| One readable getter depends on live runtime context | Probe standalone safety and mark context-dependent mechanics unsupported |
| Per-worker archive reads make the page unusable | Project the full bounded settlement snapshot once per request |
| Facts from different save revisions are mixed | Bind the entire result to one fingerprint and rebuild atomically |
| A whole-village optimizer makes hidden tradeoffs | Limit the first vertical to one selected assignment target |
| Comparison is mistaken for recruitment advice | Use workforce vocabulary and keep recruitability outside the candidate contract |
| The workflow drifts toward game control | Enforce ADR-0001 in ports, architecture tests, API verbs, and UI actions |

## Completion decision

Technical and guarded representative verification are complete. Final
wide/narrow visual confirmation and explicit product-owner approval remain
pending in [E7-011](../../reviews/E7-011-manual-verification.md).

## Delivery reference

Implementation order and item-level evidence are tracked in
[the Epic 7 backlog](./BACKLOG.md). The interaction contract is recorded in
[UI-007](./UI-007-village-workforce-planner.md).

PI-010 was promoted into this epic from
[future product ideas](../FUTURE-PRODUCT-IDEAS.md#pi-010--village-workforce-and-building-management)
on 2026-08-18.
