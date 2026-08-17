# EPIC-006: Evidence-aware companion role and candidate finder

| Field | Value |
|---|---|
| Status | In progress — technical verification complete; product decision pending |
| Milestone | 6 |
| Target release | TBD |
| Last updated | 2026-08-17 |

## Summary

Help the player choose a suitable 同道 for a selected role without claiming
that one character is universally best. Epic 6 builds a version-aware,
read-only candidate profile from the configured save and permitted installed
GameData, applies explicit role-specific evaluation rules, and presents a
deterministic shortlist with evidence, tradeoffs, missing facts, and reasons.

The first delivery follows this information flow:

```text
Configured save and installed GameData
    -> verified candidate universe and eligibility
    -> immutable candidate profiles
    -> versioned role-specific evaluation
    -> deterministic shortlist
    -> evidence-aware comparison and explanation
```

Epic 6 reuses the stable character lookup, version-aware catalogue, arbitrary-
character combat-skill progress, provenance, unavailable-state, comparison,
localization, and accessibility foundations delivered by Epics 1 through 5.
It introduces a separate companion-selection domain instead of treating
combat-target suitability as companion suitability.

The feature remains information-only. It never recruits, dismisses, trains,
moves, equips, assigns, or otherwise changes a character, party, save, game
file, process, runtime value, or in-game state.

## Context

The helper can currently enumerate non-player characters for target lookup and
can read combat-skill progress for an explicitly selected character. Those
capabilities do not establish that every enumerated character is a companion
candidate, currently available, recruitable, or suitable for a player-selected
role. Name, age, location, combat skills, and localized labels are useful
facts, but none is a substitute for verified candidate eligibility or a
documented role model.

Players make different companion choices for different purposes. A useful
combat supporter, martial-art teacher, inheritance candidate, life-skill
specialist, or settlement worker may be judged by different evidence and hard
requirements. Combining those purposes into one opaque score would hide
tradeoffs and create a misleading universal ranking.

Epic 6 therefore starts with an evidence gate. It verifies which candidate,
eligibility, availability, attribute, feature, skill, relationship, and
location fields can be read safely from the supported version. It then selects
at least two genuinely different role presets whose mechanics can be evaluated
without guessing. Unsupported roles and fields remain explicit.

## Primary user story

> As a player choosing a 同道 for a particular role, I want a trustworthy
> shortlist that explains eligibility, strengths, weaknesses, tradeoffs, and
> missing evidence so I can make the final choice manually in the game.

## Supporting user stories

- As a player, I choose an objective before candidates are evaluated.
- As a player, I can distinguish a confirmed candidate from a character whose
  eligibility or availability is unknown.
- As a player, I can see why a candidate passed or failed each hard role
  requirement.
- As a player, I can trace every scored dimension to version-matched evidence.
- As a player, I can compare shortlisted candidates without treating a score
  as a universal measure of character quality.
- As a player, I can see important weaknesses, conflicts, and missing fields
  instead of having them silently converted to zero.
- As a player, I can locate a candidate when a verified save location is
  available and see an unavailable reason when it is not.
- As a player, I receive stable ordering when the same save, role, and rule
  version are evaluated again.
- As an API consumer, I receive typed eligibility, profile, evaluation,
  provenance, ranking, tradeoff, and unavailable-state semantics.
- As a bilingual, keyboard, or mobile user, I receive the same facts and
  decision path without relying on color or a wide-only table.

## Goals

1. Verify the supported character universe and the exact meaning of companion
   eligibility and availability before ranking anyone.
2. Select at least two evidence-backed initial role presets with genuinely
   different requirements or tradeoffs.
3. Define immutable candidate profiles with stable identity, source versions,
   provenance, completeness, conflicts, and unavailable reasons.
4. Define explicit, versioned role requirements and evaluation rules.
5. Separate hard eligibility and role requirements from scored suitability.
6. Rank only comparable candidates and retain excluded or unranked characters
   with honest reasons where useful to the player.
7. Explain every decisive strength, weakness, tradeoff, and missing field.
8. Read the configured archive through one bounded snapshot operation rather
   than reopening it once per candidate.
9. Reuse verified catalogue and character-progress semantics without treating
   raw text or localized labels as mechanics.
10. Deliver typed API contracts and a bilingual, responsive, accessible UI.
11. Preserve deterministic behavior and absolute game non-interference.

## Non-goals

- Declaring one character universally best across all objectives.
- Ranking every character merely because target lookup can enumerate them.
- Guessing recruitment, relationship, teaching, inheritance, life-skill,
  settlement, or availability mechanics from names, categories, or raw text.
- Building a companion development or training plan from PI-009.
- Assigning companions or villagers to buildings, jobs, or settlement work.
- Planning village construction, resources, libraries, books, or study.
- Combining current ability with speculative future potential in one score.
- Persisting shortlists, preferences, observations, recommendations, or
  outcomes in the first delivery.
- Learning weights from player choices or reported outcomes.
- Statistical optimization, machine learning, or probability claims.
- Automating recruitment, dialogue, travel, training, equipment, party
  changes, village assignments, or any other game action.
- Reading process memory, attaching to the game, injecting code, capturing
  screenshots, simulating input, or modifying game-owned state.

## Product principles

### The objective comes before the ranking

There is no global candidate score. Every evaluation names one stable role
definition and version. Scores from different roles are not comparable, and
the UI must not combine them into a universal leaderboard.

There is one explicit comprehensive saved-base objective. Its breadth index is
the transparent equal-weight mean of three complete category averages and is
the role-local score only when that objective is selected. It never changes a
martial- or life-skill objective, and it is not future potential, universal
suitability, success probability, or an action recommendation.

### Eligibility precedes suitability

A character must first have an evidence-backed eligibility state. Target
lookup membership, a display name, proximity, or a learned skill cannot prove
that a character is a current or possible 同道. A candidate with incomplete,
unsupported, or conflicting eligibility is not silently ranked as eligible.

### Hard requirements and scored dimensions are different claims

A hard requirement determines whether a candidate can be evaluated for a
role. A scored dimension orders candidates who remain comparable. Failing a
hard requirement cannot be disguised as a small score penalty, and a high
score cannot override a failed gate.

### Unknown is not zero

Missing, unavailable, unsupported, stale, or conflicting evidence does not
mean poor ability. Each role definition states which missing fields make the
evaluation incomplete or unrankable and which optional fields may be omitted
without changing the result.

### Current ability is separate from development potential

Epic 6 evaluates verified current facts. It may display directly supported
progress, mastery, or age facts, but it does not predict future training,
acquisition, growth, relationship changes, or an ideal development path. That
work belongs to PI-009 after separate evidence and product decisions.

### Rules are typed, versioned, and explainable

Every role requirement, scored dimension, normalization rule, weight, and tie
breaker has a stable identity and supported source version. Localized names,
raw descriptions, category labels, and undocumented calculations may be shown
as evidence but cannot affect eligibility or ranking.

### One coherent snapshot owns one result

Candidate identity, eligibility, profile facts, role evaluation, shortlist,
and comparison come from one immutable snapshot boundary. The workflow does
not mix facts from different save revisions or silently refresh one candidate
inside an existing result.

### Determinism remains mandatory

Identical save fingerprint, installed data versions, role definition, rule
version, filters, and language-independent facts produce identical candidate
states, score components, ordering, ties, diagnostics, and shortlist identity.
Localized display text does not affect ranking.

### Game non-interference is permanent

Epic 6 follows
[ADR-0001](../../architecture/ADR-0001-absolute-game-non-interference.md).
Every read remains guarded and read-only. Candidate, shortlist, API, and UI
types describe information only and can never become commands for the game.

## Product vocabulary

### Candidate universe

The candidate universe is the version-matched set of characters considered by
the finder after E6-000 verifies the owning sources and inclusion rules. It is
not automatically equal to all non-player characters returned by target
lookup. Each member has one of these evidence states:

- `Eligible`: verified rules establish that the character may be considered
  for the selected role.
- `Ineligible`: sufficient verified evidence proves that an explicit
  eligibility rule fails.
- `Incomplete`: some relevant evidence exists, but a required eligibility fact
  is missing.
- `Unsupported`: the current source or GameData version cannot evaluate the
  eligibility rule.
- `Conflicting`: applicable sources disagree and precedence cannot resolve the
  character's eligibility silently.

The final stable names may be refined by E6-000 and E6-001 before Domain types
are implemented, but the distinctions must remain.

### Candidate profile

A candidate profile contains only verified or explicitly unavailable facts
needed by the delivered roles. Candidate dimensions may include:

- stable character identity and evidence-backed candidate status;
- current availability and location;
- base attributes or features whose semantics are verified;
- learned, equipped, mastered, or progressed skills;
- role-specific relationship or membership facts;
- source fingerprint, GameData version, catalogue version, and rule version;
- completeness, conflicts, warnings, and unavailable reasons.

This list is a discovery boundary, not permission to score every readable
field.

### Role definition

A role definition contains:

- a stable, non-localized identity and version;
- player-facing purpose and supported source versions;
- candidate-universe and eligibility requirements;
- hard role requirements;
- ordered scored dimensions and normalization rules;
- weights and deterministic tie breakers;
- required and optional evidence fields;
- explanation templates and known limitations.

### Role evaluation

A role evaluation records eligibility, hard-requirement outcomes, score
components, total role-local score when available, strengths, weaknesses,
tradeoffs, missing evidence, diagnostics, and a stable result identity. It
describes evidence-backed suitability for one role, not intrinsic character
quality or probability of success.

### Shortlist

A shortlist is the deterministic ordered set of comparable role evaluations.
It may also retain separate ineligible, incomplete, unsupported, and
conflicting candidates with reasons. Filters may narrow display but must not
silently alter evaluation facts or score semantics.

## Initial delivery boundary

E6-000 inspected the current installed metadata and a stable configured-save
revision and selected this candidate and role boundary:

1. the current saved Taiwu group roster excluding the Taiwu player, with
   character-object, Domain membership, character membership, and living-state
   agreement required for eligibility;
2. `MARTIAL_DISCIPLINE_APTITUDE`, comparing exact saved base qualification for
   one player-selected martial discipline; and
3. `LIFE_SKILL_DISCIPLINE_APTITUDE`, comparing exact saved base qualification
   for one player-selected life-skill discipline; and
4. an opt-in comparison overview over the six saved base main attributes, all
   14 martial aptitudes, and all 16 life-skill aptitudes, with complete-
   evidence coverage and a disclosed equal-category formula.

The stable archive probe confirmed that both base-qualification buffers are
standalone-safe, deterministic, and inside the cold and warm performance
budgets. Current modified qualification and attainment are explicitly
unsupported because their getters require unavailable live special-effect
context. The detailed evidence is recorded in
[E6-000-companion-candidate-evidence.md](../../scenarios/E6-000-companion-candidate-evidence.md).

General combat support is too broad because qualification does not prove party
synergy or battle contribution. Teaching and inheritance require unverified
interaction or future-development rules. Settlement work remains part of
PI-010 because it requires assignment, building, and resource models.

The first delivery considers only characters whose inclusion and eligibility
can be established from the configured save and supported installed sources.
It does not promise coverage of every visible, historical, generated, story,
hostile, deceased, unavailable, or otherwise enumerated character.

## Functional scope

### 1. Evidence and representative-scenario matrix

Inspect the minimum permitted save and installed GameData sources needed to
establish candidate membership, eligibility, availability, role facts, and
location. Record field ownership, type, unit, version, completeness,
precedence, runtime safety, and limitations. Select local and synthetic
representative scenarios without committing proprietary data or identities.

### 2. Role and interaction contract

Define stable role identities, eligibility states, evaluation states, hard
requirements, score meaning, tie behavior, filters, comparison semantics,
responsive layouts, and evidence presentation before public contracts are
implemented.

### 3. Immutable candidate-profile domain

Add presentation-neutral Domain contracts for candidate identity, profile
facts, evidence, conflicts, unavailable reasons, diagnostics, source identity,
and deterministic fingerprints. Collections are immutable and ordered by
stable, language-independent keys.

### 4. Versioned role rules and evaluation

Represent role definitions as explicit versioned rules. Evaluate eligibility
and hard requirements before scoring. Normalize and combine only verified
comparable facts, retain every component, and apply documented stable tie
breakers.

### 5. One-pass read-only candidate snapshot

Project all required candidates and role facts through one bounded configured-
archive read. Avoid calling an archive-opening single-character workflow in a
loop. Preserve cancellation, load warnings, save fingerprint, captured time,
source versions, and byte-for-byte non-interference evidence.

### 6. Catalogue and progress enrichment

Join candidate facts to stable catalogue identities and verified character
progress where a delivered role requires them. Preserve missing, stale,
rebuilding, unsupported, partial, and conflicting states. Never allow raw
descriptions or localized labels to become role rules.

### 7. Shortlist, comparison, and explanation

Build a stable shortlist from comparable evaluations and a separate set of
excluded or unranked results with reasons. Explain decisive strengths,
weaknesses, hard gates, tradeoffs, missing evidence, ties, and location or
availability facts without suggesting unverified recruitment or development
steps. When two candidates are selected, show the descriptive capability
overview separately from the selected-role comparison.

### 8. Application and API workflow

Compose snapshot reading, enrichment, role evaluation, filtering, shortlist,
and comparison into one immutable request result. Expose typed response
contracts without leaking local paths, proprietary raw content, arbitrary
reflection objects, or mutation-capable GameData types.

### 9. Bilingual responsive UI

Add a companion finder with role selection, result summary, shortlist cards or
rows, candidate comparison, evidence details, filters, and explicit empty,
incomplete, unsupported, conflict, loading, and failure states. English and
Traditional Chinese layouts remain keyboard accessible and do not rely on
color alone.

### 10. Verification and lifecycle

Cover Domain, Application, Infrastructure, API, Presentation, architecture
boundaries, localization, batching, and guarded local reads. Repeated requests
prove stable ordering and fingerprint identity. Save, GameData, language, and
other game-owned sources remain unchanged.

## User-visible states

The workflow must present these states explicitly:

- roles loading, available, unsupported, or failed;
- candidate snapshot loading, available, unsupported, or failed;
- no verified candidates for the selected role;
- eligible and ranked candidate;
- eligible candidates tied under the documented rules;
- confirmed ineligible candidate with reasons;
- incomplete candidate eligibility or role evidence;
- unsupported source or rule version;
- conflicting eligibility or role evidence;
- candidate location available or unavailable;
- shortlist filtered with the original result count retained;
- candidate comparison available or unavailable;
- catalogue or progress missing, stale, rebuilding, partial, or failed; and
- save revision changed, requiring a complete new result rather than a partial
  refresh.

## Epic acceptance criteria

- [x] The supported candidate universe and every eligibility rule are defined
      by version-matched evidence rather than target-lookup membership.
- [x] At least two genuinely different role presets are selected through the
      evidence gate and approved before implementation.
- [x] Every delivered role has stable identity, version, hard requirements,
      score semantics, required evidence, weights, and tie breakers.
- [x] Candidate identity, eligibility, profile facts, source versions,
      conflicts, diagnostics, and fingerprints are immutable and typed.
- [x] Eligibility and hard requirements are evaluated before suitability
      scoring.
- [x] Missing, stale, unsupported, or conflicting evidence never becomes zero,
      a negative trait, or confirmed ineligibility.
- [x] Only comparable candidates enter the ranked shortlist; every omitted or
      unranked candidate has a typed reason when retained.
- [x] Every score component and decisive explanation links to verified source
      evidence and the owning rule version.
- [x] Localized names, raw descriptions, and category labels never become
      identity, eligibility rules, or scored mechanics.
- [x] Current ability remains distinct from speculative future development.
- [x] The candidate source is projected through one bounded archive read and
      does not reopen the archive for each candidate.
- [x] Save revision, catalogue version, GameData version, and rule version form
      one coherent immutable result boundary.
- [x] Filters and localization do not change score facts, ordering, ties, or
      stable result identity.
- [x] API and UI expose equivalent eligibility, evaluation, evidence,
      tradeoff, conflict, and unavailable-state semantics.
- [x] The capability overview exposes exact 6/14/16 coverage, formula version,
      category averages, breadth index, and top values without changing a
      martial- or life-skill evaluation; the same complete breadth is the
      score only for the explicitly selected comprehensive objective.
- [x] Traditional Chinese and English layouts are complete, responsive,
      keyboard accessible, and do not rely on color alone.
- [x] Automated tests cover eligible, ineligible, incomplete, unsupported,
      conflicting, tied, filtered, stale, and changed-revision states.
- [x] Guarded local verification proves every inspected save, GameData,
      language, and other game-owned source remains byte-for-byte unchanged.
- [x] No recruitment, training, movement, equipment, assignment, persistence,
      screenshot, process access, input automation, or game-control capability
      is introduced.
- [x] Every acceptance criterion links to implementation or verification
      evidence.
- [ ] The product owner records the Epic 6 completion decision.

## Success measures

- A player can choose a role and understand why the shortlist is specific to
  that objective.
- A player can explain the decisive difference between any two shortlisted
  candidates without relying on an opaque total score.
- At least two role presets demonstrate different requirements or tradeoffs
  over the same candidate-profile foundation.
- Unsupported candidates and missing evidence remain visible and honest rather
  than being ranked poorly.
- The full candidate set is projected in one bounded archive operation within
  the performance budget established by E6-000.
- Repeated identical inputs produce equivalent profiles, evaluation
  components, ties, ordering, diagnostics, and result fingerprints.
- No Epic 6 operation changes game-owned bytes or runtime state.

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| Target lookup is mistaken for a recruitable-candidate list | Establish a separate verified candidate universe and eligibility contract in E6-000 |
| One opaque score becomes a universal character ranking | Require one selected role, role-local scores, component explanations, and no cross-role comparison |
| A descriptive breadth index is mistaken for a universal recommendation | Label the equal-category formula and limitation, expose all category coverage, and permit it to affect evaluation and order only for the explicitly selected comprehensive objective |
| Missing data makes a candidate look weak | Preserve incomplete and unsupported states; never normalize missing evidence to zero |
| Readable fields are assigned guessed mechanics | Require typed, version-matched rules and reject raw text or label inference |
| A hard requirement is hidden inside weighting | Evaluate and display hard gates before any score |
| Current ability is confused with future potential | Keep development prediction and planning in PI-009 |
| Per-character archive reads make the workflow unusable | Project the candidate snapshot in one bounded archive read and add a performance budget |
| Character facts come from different save revisions | Bind the complete result to one save fingerprint and rebuild atomically on change |
| Story or unavailable characters leak into the shortlist | Use explicit universe, availability, and eligibility states with representative tests |
| Role presets encode subjective bias without disclosure | Version every dimension, weight, normalization rule, and tie breaker and show components |
| Scope expands into settlement management | Keep PI-010 and PI-011 outside the epic even if character primitives are reusable |
| Feature drifts toward game control | Enforce ADR-0001 in ports, architecture tests, API verbs, and UI language |

## Delivery reference

Implementation order and item-level evidence are tracked in
[the Epic 6 backlog](./BACKLOG.md).

PI-008 was promoted into this epic from
[future product ideas](../FUTURE-PRODUCT-IDEAS.md#pi-008--companion-role-and-candidate-finder)
on 2026-08-17.
