# EPIC-003: Verified target observations and evidence-aware recommendations

| Field | Value |
|---|---|
| Status | In progress — scope corrected by new UI evidence |
| Milestone | 3 |
| Target release | TBD |
| Last updated | 2026-08-08 |

## Summary

Allow the player to manually report target combat information that is visible
in the current game UI but absent or stale in the configured save.
Resolve the reported skills through the local bilingual catalogue, retain
observation time and provenance, merge only verified fields into an immutable
analysis snapshot, and show exactly how the confirmed evidence changes threat
analysis and loadout recommendations. Hostile and story targets do not expose
their complete opponent `運功` page in the supported UI version, but later
evidence proves that their combat information panels may expose partial active
skill names, power, and effect text. The helper must accept only those visible
facts without treating them as a complete loadout or an absence claim.

Epic 3 remains an information-only workflow. The helper does not inspect game
memory, capture the screen, automate input, attach to the game, or modify any
game-owned state. The player observes the game normally and chooses what to
report.

## Context

[Epic 1](../epic-001/EPIC.md) produces deterministic, feasible combat-skill
recommendations from a read-only combat snapshot. It already supports a
current-screen observation of the player's own loadout, but the target's
equipped combat-skill list may be absent from the disk save. In that case the
threat analyzer must conservatively consider learned-but-unequipped skills and
warn that the target's actual loadout is unknown.

[Epic 2](../epic-002/EPIC.md) adds a version-aware bilingual combat-skill
catalogue, stable skill identities, field provenance, and explicit
unavailable/conflicting progress values. Those capabilities make a safe manual
target-observation workflow practical without parsing free-form text or
guessing skill identities.

The remaining product gap is not a lack of recommendation output. It is the
lack of trustworthy current evidence about which target mechanics are actually
equipped and active, together with the need to state honestly when the game UI
does not make that evidence observable.

## Primary user story

> As a player inspecting an accessible sparring opponent in the current game
> UI, I want to report the opponent's visible equipped combat skills so the
> helper can distinguish actual threats from possible learned skills and
> recalculate an evidence-backed recommendation.

## Supporting user stories

- As a player, I can find an observed target skill by either its Traditional
  Chinese or English name.
- As a player, I can state whether I observed the complete loadout or only a
  partial set, so omitted skills are never misinterpreted.
- As a player, I can report a practice direction only when it is actually
  visible and leave it unavailable otherwise.
- As a player, I can review the resolved stable skill IDs before the
  observation affects analysis.
- As a player, I can see whether each threat came from the save, installed
  GameData, a verified rule, or my current-screen observation.
- As a player, I can see which threats, counters, and recommended skills
  changed after applying the observation.
- As a player, I can clear the observation and reproduce the original
  save-only recommendation.
- As a player facing a hostile or story target, I am told that current-screen
  loadout observation is unavailable instead of being asked for hidden data.

## Goals

1. Define exactly which target fields can be reliably observed in the current
   supported game UI.
2. Restrict current-screen loadout observations to UI-visible sparring
   opponents and preserve an unavailable state for hostile/story targets.
3. Capture target observations through explicit manual user input.
4. Resolve observed skill names to stable catalogue identities with visible
   confirmation for ambiguous matches.
5. Distinguish complete-loadout observations from partial observations.
6. Retain observation time, evidence reference, field provenance, and source
   conflicts without silently overwriting evidence.
7. Merge observations into a new immutable combat snapshot through
   deterministic, version-aware rules.
8. Re-run threat analysis and recommendations using only confirmed
   observations and existing verified mechanics.
9. Explain the recommendation impact of the observation.
10. Keep observations session-bound and explicitly clearable in the first
   release.
11. Preserve the absolute game non-interference boundary.

## Non-goals

- Capturing screenshots automatically.
- OCR, computer vision, or automatic screenshot interpretation.
- Reading the game process, runtime memory, network traffic, logs, or hidden UI
  state.
- Attaching to, injecting into, hooking, patching, automating, or controlling
  the game.
- Reloading a save or navigating the game for the player.
- Treating free-form player text as a verified combat mechanic.
- Inferring a complete loadout from a partial observation.
- Predicting an exact win probability.
- Persisting observation history, battle outcomes, or player feedback.
- Sharing observations between users or devices.
- General screenshot/file management.
- Expanding the skill catalogue to life skills.
- Normalizing every raw direct/reverse effect description into a typed rule.

## Product principles

### Observation is evidence, not authority over all fields

An observation may provide better evidence for a specific current field, such
as the target's equipped loadout. It does not replace the save, catalogue, or
verified rules wholesale. Fields not explicitly observed retain their original
state and provenance.

When sources disagree, the helper retains the disagreement and explains which
value the current analysis used. It never silently deletes the older value.

### Partial and complete observations are different claims

The observation must explicitly declare its coverage:

- `CompleteLoadout`: the player confirms that every visible equipped
  combat-skill slot was inspected;
- `PartialLoadout`: the player confirms only the listed skills.

For a partial observation, an omitted skill remains unknown. For a complete
observation, omission may establish that a previously saved skill is not in
the current equipped loadout, but only after the observation passes the
versioned completeness rules established by E3-000.

### Observation access depends on encounter context

E3-000 establishes that the supported UI exposes an opponent's `運功` page in
`切磋武功`, but not for hostile or story targets. Complete and partial
current-screen observations are therefore valid only for a confirmed sparring
context. An inaccessible target produces an unavailable state, never an empty
or partial loadout, and the helper must not prompt the player to reconstruct
hidden data.

### Manual first

The first release uses catalogue-assisted manual selection. This keeps the
evidence claim visible and reviewable, avoids unreliable OCR, and works in both
supported languages. Any future screenshot-assistance feature requires its
own evidence, privacy, retention, error-correction, and distribution review.

### Evidence completeness is not win probability

The helper may label evidence as confirmed, partial, conflicting, stale, or
unsupported. These statuses describe the quality and completeness of inputs;
they are not probabilities that a recommendation will win.

### Determinism remains mandatory

The same save fingerprint, catalogue identity, verified-rule version, and
confirmed observation must produce the same merged snapshot, threats,
recommendations, explanations, and ordering.

### Game non-interference is permanent

Epic 3 follows
[ADR-0001](../../architecture/ADR-0001-absolute-game-non-interference.md).
The feature accepts helper-owned information from the player and returns
advice. It creates no path that can write to or control the game.

## Source-of-truth and conflict policy

For a field explicitly covered by a valid current observation, analysis uses:

1. A manually confirmed current-screen target observation that is newer than
   the save snapshot.
2. The latest successfully read save snapshot.
3. Installed configuration matching the detected GameData version.
4. A versioned verified Domain rule.

This is field-level precedence, not aggregate replacement. Every used value
retains provenance. A conflicting value remains available as an observation
record and produces a stable warning.

An observation older than the save does not override the save. When save
modified time is unavailable, the helper requires explicit confirmation before
using current-screen precedence and emits a warning.

## Functional scope

### 1. Observable-field evidence

Before implementation, record the supported target screen and determine:

- which encounter contexts expose the opponent `運功` page;
- whether the displayed list is a complete equipped loadout or a partial view;
- which skill categories are visible;
- whether empty slots are distinguishable from hidden or paged slots;
- whether practice direction is visible for each skill;
- whether target identity can be confirmed independently;
- which values change during battle and therefore require a capture time;
- which language labels can be resolved through the Epic 2 catalogue.

Unsupported fields remain unavailable and are not added to the input form.

### 2. Domain observation model

The Domain model should include immutable equivalents of:

- target character identity;
- observation access context, distinguishing visible sparring evidence from
  unavailable hostile/story targets;
- UTC observation time;
- a short opaque evidence reference;
- observation coverage (`CompleteLoadout` or `PartialLoadout`);
- observed equipped skills grouped by verified category;
- optional observed practice direction per skill;
- stable field sources and any conflicting observations.

Collections are copied into immutable values. Duplicate skills, invalid IDs,
unknown categories, invalid directions, blank evidence, and impossible
coverage combinations are rejected at construction.

### 3. Catalogue-assisted resolution

The Application layer resolves user selections through the current Epic 2
catalogue. Search follows the existing bilingual normalization and fallback
rules. The player confirms a stable match before it enters the observation.

An observed skill absent from a stale target snapshot is not silently rejected
or silently treated as fully known. The resolver joins only the static facts
required by target analysis, records that current-screen evidence established
equipped membership, and leaves unrelated character progress unavailable.

### 4. Immutable observation merge

A pure Domain service merges a valid target observation into a copy of the
combat snapshot. It must:

- require matching target identity;
- apply freshness and coverage rules;
- validate category and skill identity;
- replace only fields covered by the observation;
- retain conflicting save values and sources;
- preserve all unobserved target and player fields;
- return stable warnings for partial, stale, conflicting, or unsupported
  evidence;
- never mutate the original snapshot or observation.

### 5. Recommendation integration

Threat analysis prioritizes confirmed equipped target skills over
learned-but-unconfirmed skills. A complete observation may remove saved
equipped membership from the current analysis; a partial observation may only
add confirmed equipped evidence and cannot prove absence.

Only existing typed, version-matched threat and counter rules can change
recommendation feasibility or scoring. A reported skill with an unrecognized
effect remains visible as unsupported evidence and cannot invent a threat.

### 6. Recommendation-impact explanation

The result should distinguish:

- threats confirmed by the observation;
- threats that remain possible but unconfirmed;
- threats removed from the current analysis by a complete newer observation;
- counters or recommended skills added or removed;
- feasibility changes;
- unchanged assumptions and unresolved risks.

The player can compare save-only and observation-enhanced results without
interpreting raw diagnostics.

### 7. Manual observation UI

The recommendation page provides a target-observation workflow:

1. Confirm encounter context, target identity, and source freshness.
2. Stop with an explicit unavailable state for hostile or story targets;
   otherwise select complete or partial coverage for a sparring opponent.
3. Search and add skills in the active UI language.
4. Confirm category and optional visible direction.
5. Review resolved stable identities and evidence status.
6. Apply the observation to a new recommendation request.
7. Inspect impact and warnings.
8. Clear the observation to return to save-only analysis.

Status is never communicated by color alone. The workflow is keyboard
accessible, exposes validation errors next to the affected field, and retains
Traditional Chinese/English fallback labeling.

### 8. API and session lifecycle

The API accepts typed observation data as part of the recommendation request.
It does not accept a screenshot path, arbitrary save path, game-process
identifier, or raw mechanic claim.

The first release keeps observation state in the current UI/request only. It
does not add a history table or authoritative observation database. Refreshing
or explicitly clearing the form removes the observation.

### 9. Verification

Verification must cover Domain, Application, API, Presentation,
Infrastructure boundaries, and a local read-only vertical test. The test must
prove that applying and clearing an observation changes only helper-owned
immutable values and leaves every inspected game/save fingerprint unchanged.

## User-visible states

The workflow must present these states explicitly:

- no observation;
- observation unavailable for hostile/story context;
- editing;
- invalid or ambiguous selection;
- confirmed partial observation;
- confirmed complete observation;
- observation older than save;
- full loadout unavailable with partial battle-visible evidence;
- save time unavailable;
- observation/save conflict;
- unsupported catalogue or GameData version;
- recommendation completed with observation;
- observation cleared.

## Epic acceptance criteria

- [x] The supported target UI fields and completeness rules are documented
      with versioned evidence.
- [ ] Hostile/story complete loadouts remain explicitly unavailable while
      separately visible battle-effect evidence is accepted only as partial
      and never becomes an empty-loadout claim.
- [x] Target observations use stable bilingual catalogue identities.
- [ ] Partial battle-visible effects and complete sparring loadout coverage
      cannot be confused.
- [x] Observation time, evidence reference, and field provenance are retained.
- [x] Stale and conflicting observations remain visible and deterministic.
- [x] Only explicitly covered fields receive current-screen precedence.
- [x] An observed skill missing from a stale target snapshot is represented
      without fabricating unrelated progress.
- [ ] Threat analysis distinguishes battle-visible active effects, confirmed
      equipped sources, and possible learned sources.
- [ ] Recommendation impact is explained in terms of changed evidence,
      threats, counters, feasibility, and unresolved risks.
- [x] Save-only behavior remains reproducible after clearing the observation.
- [x] Unknown raw effects cannot influence legality or scoring.
- [x] Observation state is session-bound and is not persisted as history.
- [x] No endpoint accepts a screenshot path, game path, process identifier, or
      mutation-capable game type.
- [ ] The UI is bilingual, accessible, and explicit about full-loadout
      unavailability versus partial battle-visible evidence.
- [ ] Automated tests cover valid, battle-visible partial, complete, stale,
      conflicting, ambiguous, unsupported, and cleared states.
- [ ] Local vertical verification proves all inspected game and save sources
      are byte-for-byte unchanged.
- [ ] The product owner records the Epic 3 completion decision.

## Success measures

- A player can report a representative sparring-target loadout without
  entering a raw skill ID.
- A hostile/story target keeps the complete loadout unavailable while allowing
  the player to report only skill effects visibly exposed by the combat UI.
- Every observation-used threat identifies current-screen provenance.
- A partial observation never removes an unobserved possibility.
- A complete observation changes absence claims only when the verified
  completeness rule is satisfied.
- Identical evidence produces identical recommendations.
- Clearing an observation reproduces the save-only result.
- No observation operation changes game-owned bytes or runtime state.

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| A partial screen is mistaken for a complete loadout | Require explicit coverage and verify completeness in E3-000 |
| Sparring evidence is applied to an unobservable hostile/story target | Require encounter context, reject current-screen observation for hostile/story targets, and retain save-only uncertainty |
| Similar bilingual names resolve to the wrong skill | Show stable match details and require confirmation on ambiguity |
| Observation silently overwrites fresher save data | Apply timestamp rules, retain both sources, and emit conflicts |
| An observed skill is absent from a stale target snapshot | Join only required static facts and leave unrelated progress unavailable |
| Free-form text becomes an invented mechanic | Accept typed catalogue selections only; display notes cannot enter rules |
| Evidence confidence is read as win probability | Label it as evidence completeness and prohibit probability language |
| Observation state becomes ungoverned history | Keep the first release session-bound and explicitly clearable |
| Screenshot support expands privacy/distribution scope | Keep capture, upload, OCR, and storage outside Epic 3 |
| Target observations create a game-control path | Enforce ADR-0001 with architecture tests and information-only APIs |

## Delivery reference

Implementation order and item-level evidence are tracked in
[the Epic 3 backlog](./BACKLOG.md).
