# EPIC-002: Version-aware skill catalogue and character skill atlas

| Field | Value |
|---|---|
| Status | In progress — awaiting product-owner completion decision |
| Milestone | 2 |
| Target release | TBD |
| Last updated | 2026-08-07 |

## Summary

Build a local, bilingual, version-aware catalogue of combat skills from the
player's installed game data, then overlay the current Taiwu character's
save-derived skill progress to create a searchable character skill atlas.

The catalogue describes static skill definitions. The character overlay shows
which skills have been obtained, studied, completed, broken through, mastered,
activated, or equipped, including the exact study details that are complete or
still missing whenever the save and verified game semantics can prove them.

The catalogue is a helper-owned, rebuildable SQLite cache. It is never a copy
of the save and is never authoritative for character progress. The current
read-only save snapshot remains authoritative for character-specific state.
The helper must never modify a save, game file, game configuration, running
game process, runtime memory, or in-game state.

## Problem

Epic 1 can identify learned combat skills and use selected static fields while
building recommendations, but the information is distributed across snapshot
mapping, diagnostic output, local language resources, and verified rule
catalogues. The player cannot currently:

- Browse every locally installed combat skill in one searchable view.
- Search by either Traditional Chinese or English name.
- Distinguish static skill facts from the current character's progress.
- See all obtained, broken-through, mastered, active, or equipped skills.
- Inspect study progress and identify the exact missing study details.
- Tell whether an absent value is genuinely incomplete or merely unavailable.
- Tell whether the local catalogue matches the installed game and language
  resources.
- Open a recommended skill and inspect its full known definition and current
  character state.

The existing structured combat snapshot preserves mastery, active practice
direction, and breakthrough availability, but does not retain grade,
proficiency power, the raw reading state, or individual study-detail state.
The existing breakthrough mapper counts selected reading-state bits without
preserving which individual details were studied. Epic 2 promotes the required
data into verified, typed contracts instead of parsing diagnostic text or
displaying raw bit fields.

## Primary user story

> As a player reviewing my Taiwu's martial arts, I want to browse a bilingual
> catalogue and see my character's exact progress for every combat skill, so I
> can identify what I own, what is mastered or broken through, and which study
> details remain incomplete.

## Supporting user stories

- As a player, I can search for a skill using either its Chinese or English
  name and filter by category, grade, faction, weapon type, and progress.
- As a player, I can distinguish `已取得`, `可突破`, `已突破`, `已大成`, and
  `已裝備` without the helper collapsing them into one ambiguous status.
- As a player, I can inspect every verified study detail and see whether it is
  studied, not studied, or unavailable.
- As a player, I can see when the catalogue was built and whether it matches my
  installed game and language-resource versions.
- As a player, I can open catalogue details from a recommendation without
  rereading raw diagnostic output.

## Goals

1. Build a complete local catalogue of installed combat-skill definitions.
2. Provide Traditional Chinese and English names with deterministic fallback.
3. Record enough source identity to detect stale catalogue data reliably.
4. Rebuild the catalogue deterministically when its schema or sources change.
5. Read character skill progress through the existing non-interfering save
   workflow and keep it separate from static catalogue data.
6. Represent obtained, proficiency, study, breakthrough, direction, mastery,
   activation, and equipment facts independently.
7. Decode individual study details only from verified game semantics.
8. Show unavailable, unsupported, stale, and conflicting data explicitly.
9. Provide local API and UI surfaces for search, filtering, detail inspection,
   and recommendation deep links.
10. Preserve the permanent game non-interference boundary while introducing
    narrowly scoped helper-owned persistence.

## Non-goals

- Modifying saves, game files, game databases, configuration, runtime memory,
  runtime state, or in-game data.
- Injecting into, attaching to, hooking, patching, automating, or controlling
  the game.
- Storing character progress in the static catalogue as authoritative state.
- Building a catalogue of life skills or non-combat skills in this epic.
- Importing, committing, packaging, or distributing proprietary game artwork,
  icons, binaries, complete language resources, or a pre-populated catalogue.
- Inferring trusted combat mechanics from raw effect descriptions.
- Normalizing every direct and reverse effect into a recommendation rule.
- Recommending how to obtain an unlearned skill unless a separate verified
  acquisition rule exists.
- Interpreting screenshots automatically or accepting runtime observations.
- Persisting recommendation history, battle outcomes, or player feedback.
- Adding skill-to-skill comparison beyond catalogue filtering and detail
  inspection.
- Expanding verified target or enemy coverage.

## Product principles

### Static definitions and character progress are different models

Installed GameData and language resources describe static skill definitions.
The configured save describes current character progress. The Application
layer may join them by stable skill identifier, but neither source may silently
overwrite the other.

The generated SQLite catalogue may accelerate static queries. It must not
become a stale substitute for reading current character state.

### Progress facts are independent

The UI may present compact badges, but the Domain model must not collapse skill
progress into a single linear enum. Obtained, proficiency, study completion,
breakthrough readiness, completed breakthrough, practice direction, mastery,
activation, and equipment can overlap and must remain independently testable.

For example, `已大成` and `已突破` remain separately named presentation facts,
but the supported version proves they use the same current successful-
breakthrough predicate for the current Taiwu. A skill's direction is
not active merely because some study details are complete. The exact
relationships must follow verified game behavior.

### Evidence before interpretation

Raw save fields and configuration values are not self-explanatory. Epic 2 must
verify the meaning of reading-state bits, proficiency values, breakthrough
flags, mastery checks, and activation state against controlled local evidence
before assigning player-facing labels.

If the helper cannot prove a value, it reports `Unavailable`, `Unsupported`, or
`Unknown`; it does not treat missing data as `false` or `not studied`.

### Rebuildable cache, authoritative sources

The catalogue database is derived helper-owned data. Installed game data and
language resources remain the static source of truth. A schema change or
relevant source-version change invalidates the cache and triggers a controlled,
deterministic rebuild.

### Raw text is display evidence, not a combat rule

Locally imported effect descriptions may be shown to the player with their
source and verification status. The recommendation engine may use only typed,
separately verified mechanics. Merely storing an effect description does not
make it safe to score or interpret.

### Absolute non-interference

The permanent boundary from
[ADR-0001](../../architecture/ADR-0001-absolute-game-non-interference.md)
continues to apply:

- Game-owned files and databases are opened only for permitted reads.
- No game-owned file or directory may be created, updated, deleted, renamed,
  repaired, converted, re-serialized, or overwritten.
- No mutation-capable GameData object crosses an Infrastructure boundary.
- The helper never interacts with the running game process.
- The catalogue database lives in a validated helper-owned application-data
  directory outside the game installation and every save directory.
- Only the named catalogue and derived progress-cache Infrastructure adapters
  may write their respective helper-owned data.
- Presentation and public API contracts expose queries and cache maintenance
  for helper-owned data only; they expose no game command.
- Rebuild, retention, and deletion operations may affect only their exact
  validated helper-owned cache paths.

## Terminology

### Combat-skill definition

Static, locally derived information about a combat skill, keyed by its stable
GameData identifier. A definition can include localized names, category,
grade, faction, element, equipment type, costs, requirements, effect
references, and raw display descriptions.

### Character skill progress

Current save-derived facts for one character and one combat skill. Progress
may include possession, proficiency, study details, breakthrough state,
practice direction, mastery, activation, and equipment state.

### Character skill atlas

A query view that joins installed combat-skill definitions with the current
character's progress. Catalogue entries without progress remain visible as not
obtained only when dictionary membership has been verified to mean possession;
otherwise the UI uses a more precise verified label.

### Study detail

One verified readable or studyable unit represented by the game's skill-study
state. A detail has a stable identity, localized label where available, group,
and state. The implementation must derive the complete set from verified
configuration and reading-state semantics rather than hard-coding labels from
a screenshot.

### Catalogue source manifest

The helper-owned record of schema version, importer version, relevant GameData
identity, language-resource identity, source hashes or versions, build time,
and import diagnostics used to decide whether the catalogue is current.

## Source-of-truth precedence

When values disagree, use and display the following precedence:

1. The latest successfully read save snapshot for character progress.
2. Installed GameData matching the running importer for static definitions.
3. Installed language resources matching the selected language for display
   text.
4. Versioned, verified domain rules for interpreted mechanics.
5. The helper-owned SQLite catalogue only as a rebuildable representation of
   items 2 through 4.

The catalogue cache is never allowed to override a fresher save snapshot.
Conflicting or stale values must retain provenance and produce a visible
warning.

## Skill progress model

The following concepts must remain separate even when the UI displays them as
badges:

| Fact | Example UI | Expected source |
|---|---|---|
| Definition exists | Listed in catalogue | Installed GameData |
| Obtained or learned | `已取得` | Verified save collection semantics |
| Proficiency | Persisted current and storage maximum | Save skill state |
| Displayed power | Current final power and requirements-layer maximum | Live GameData display calculation; unavailable in standalone mode |
| Study details | Complete and missing detail list | Verified reading-state mapping |
| Breakthrough readiness | `可突破` | Save state plus verified rule |
| Breakthrough completed | `已突破` | Save activation/breakthrough state |
| Practice direction | `正`, `逆`, or neutral | Active save state |
| Mastery | `已大成` | Verified mastery API or rule |
| Activated | Active/inactive | Save skill state |
| Equipped | `已裝備` | Current save loadout |

A value unavailable from the current game version or save must carry a reason.
No unavailable boolean may default to `false`.

## Functional scope

### 1. Evidence mapping

Create versioned evidence for the exact semantics used by the atlas, including:

- Meaning of membership in the character combat-skill collection.
- Meaning and valid range of proficiency, current power, and maximum power.
- The fact that the skill screen's displayed percentage is final power rather
  than a percentage derived from saved proficiency.
- Complete reading-state bit or field mapping for every study detail.
- Stable identity and grouping of common, direct, and reverse study details.
- Relationship between studied details and available breakthrough directions.
- Difference between breakthrough-ready, broken-through, active, and mastered.
- Direction and neutral-state behavior before and after breakthrough.
- Behavior for incomplete, malformed, unknown-version, and target-character
  skill data.

Evidence should use controlled local saves, read-only fingerprints, configured
game data, and manually recorded observations. Proprietary screenshots or
extracted assets are not committed unless their use and distribution are
explicitly permitted; written observations and minimal metadata are preferred.

### 2. Version-aware bilingual catalogue

Import, where locally available and permitted:

- Stable skill identifier.
- Traditional Chinese and English display names.
- Category, grade, faction, five-element classification, and equipment type.
- Base grid cost, specific-grid contribution, and generic-grid contribution.
- Activation timing and requirements that already have typed representations.
- Direct, reverse, and neutral effect identifiers.
- Raw localized effect or requirement text for display, clearly marked as raw
  and unverified where no typed rule exists.
- Source record identifiers required to diagnose or rebuild an entry.

The importer reports fields it cannot map. It must not silently skip malformed
records or replace unsupported values with plausible defaults.

### 3. Character progress overlay

Read the current configured save through the existing read-only archive
session and produce typed progress for every learned or obtained combat skill:

- Character and save-snapshot identity.
- Proficiency current value and maximum value.
- Current final power and maximum power as distinct typed fields, unavailable
  when the live calculation context is absent.
- Individual study-detail states and aggregate completeness.
- Breakthrough readiness and available directions.
- Completed breakthrough and active practice direction.
- Mastery, activation, and equipped state.
- Per-field provenance and unavailability reasons.

The overlay is immutable and belongs to the save snapshot. Epic 2 does not
persist it as authoritative catalogue state.

### 4. Catalogue storage and lifecycle

Store static definitions and the source manifest in a helper-owned SQLite
database. The Infrastructure implementation must provide:

- A schema version independent of the installed game version.
- Deterministic import ordering and stable query results.
- Transactions that never expose a partially rebuilt catalogue.
- Exact path validation before create, replace, or delete operations.
- Rebuild on incompatible schema or relevant source identity change.
- Recovery from a missing, empty, interrupted, or corrupt helper database.
- Import counts and diagnostics visible to Application and Presentation.
- No pre-populated database in source control or release artifacts.

Character progress may be held in memory or in a bounded helper-owned cache for
the active/current source snapshot. The cache is an accelerator only: it keeps
at most eight recent save-path snapshots, replaces stale revisions, can be
cleared explicitly, and never becomes authoritative progress history.
Historical character progress remains outside this epic.

### 5. Catalogue and atlas queries

Provide Application use cases for:

- Ensuring a current local catalogue exists.
- Searching and filtering combat-skill definitions.
- Reading one skill definition and its field provenance.
- Reading the current character skill atlas.
- Reading one skill's joined definition and character progress.
- Reporting catalogue freshness, rebuild status, and diagnostics.

Search must support both installed languages without requiring the UI to know
which language produced a match.

### 6. Local API

Expose information-only endpoints such as:

```http
GET /api/combat-skills?search=金剛&category=defense&progress=mastered
GET /api/combat-skills/{skillId}
GET /api/character-skill-atlas
```

High-level list result:

```json
{
  "catalogue": {
    "status": "current",
    "gameDataVersion": "...",
    "languageVersions": {},
    "builtAt": "2026-08-02T00:00:00Z",
    "warnings": []
  },
  "character": {
    "characterId": 12345,
    "saveHash": "...",
    "readAt": "2026-08-02T00:00:00Z"
  },
  "skills": []
}
```

Cache maintenance endpoints, if required, operate only on the validated
helper-owned catalogue and must not accept an arbitrary filesystem path.

### 7. Local UI

Add a martial-art catalogue and character-atlas page that:

- Searches by Traditional Chinese or English name.
- Filters by category, grade, faction, weapon or equipment type, element, and
  independent character-progress facts.
- Groups skills by familiar combat-skill categories.
- Shows localized names and compact progress badges.
- Distinguishes base cost from current effective character cost.
- Shows study progress as text and an accessible visual detail map.
- Identifies exact studied, missing, unavailable, and unsupported details.
- Shows catalogue version, freshness, save snapshot time, and warnings.
- Supports loading, rebuilding, empty, partial, stale, unsupported, and failure
  states.
- Uses no extracted game artwork required for the information to be usable.
- Remains keyboard accessible and never relies on color alone.

The visual structure may take inspiration from the game's category grid and
study wheel, but it must use helper-owned presentation assets and must not
claim to be an exact reproduction of the game UI.

### 8. Recommendation integration

Recommendation skill cards may link to the corresponding catalogue detail.
The catalogue may supply verified static definitions through typed query
contracts. Raw imported text may be displayed as supporting information, but
it cannot create a new threat, counter, feasibility rule, or score component.

The recommendation workflow must remain usable when the catalogue is missing
or rebuilding. Catalogue availability must not weaken Epic 1 feasibility or
non-interference guarantees.

## Clean Architecture placement

### Domain

- `CombatSkillDefinition` and localized-name value objects.
- Catalogue source identity and field-provenance values.
- Character combat-skill progress and independent progress facts.
- Study-detail identity, group, and state.
- Catalogue freshness and completeness concepts without SQLite dependencies.

### Application

- `EnsureCombatSkillCatalogue`.
- `SearchCombatSkillDefinitions`.
- `ReadCombatSkillDetails`.
- `ReadCharacterCombatSkillAtlas`.
- `ClearCharacterCombatSkillProgressCache`.
- Query-only ports for installed definitions and save-derived progress.
- A helper-owned catalogue repository port with no arbitrary-path parameter.
- Join and language-fallback policies.

### Infrastructure

- Strictly read-only GameData and language-resource importer.
- SQLite schema, repository, and transactional rebuild implementation.
- Validated helper-owned catalogue-path provider.
- Source identity, hashing, and invalidation adapter.
- Save-derived character-progress adapter using the existing read-only session.
- Bounded, path-safe current-progress snapshot cache with explicit clearing.

### Presentation

- Catalogue status, search, filters, and paging or virtualization.
- Character skill atlas and detail view.
- Accessible study-detail visualization.
- Explicit stale, unavailable, unsupported, and partial-data states.
- Deep links from recommendation skill cards.
- Information-only contracts with no game mutation or control action.
- Primary ownership filtering, grouped ownership counts, and a numeric
  low-to-high grade legend.

## Milestone acceptance criteria

- [x] A clean local installation can build the catalogue from permitted local
      GameData and language resources without a pre-populated database.
- [x] Every configured combat skill is imported or appears in a deterministic
      diagnostic explaining why it was rejected.
- [x] Stable identifiers are unique and query ordering is deterministic.
- [x] Traditional Chinese and English names are searchable with documented
      fallback when one language value is unavailable.
- [x] The source manifest records schema, importer, GameData, and language
      identities sufficient to determine catalogue freshness.
- [x] Relevant source or schema changes invalidate and rebuild the catalogue.
- [x] Interrupted, corrupt, or missing helper databases recover without
      modifying any source file.
- [x] The database exists only in a validated helper-owned directory and is
      excluded from Git and release artifacts.
- [x] Character progress is read from the current save snapshot and is not
      treated as static catalogue data.
- [x] Obtained, proficiency, study, breakthrough, direction, mastery,
      activation, and equipment facts remain independent.
- [x] The exact reading-state-to-study-detail mapping is documented with
      versioned evidence.
- [x] Every study detail is shown as studied, not studied, or unavailable with
      a reason; unknown is never silently shown as incomplete.
- [x] Breakthrough availability and practice direction agree with verified
      save evidence for the golden character skills.
- [x] The UI can filter learned, breakthrough-ready, broken-through, mastered,
      and equipped skills independently.
- [x] The UI shows both catalogue freshness and save-snapshot freshness.
- [x] A player can search in either installed language and open a skill detail
      page from both the atlas and a recommendation.
- [x] Base definition values and current character-effective values are
      visually and semantically distinct.
- [x] Raw descriptions are labeled as display evidence and never become
      recommendation mechanics without a typed verified rule.
- [x] Catalogue absence or rebuild does not break the Epic 1 recommendation
      workflow.
- [x] No generated database, proprietary binary, complete extracted resource,
      or game artwork is committed or distributed.
- [x] Before-and-after fingerprints prove every inspected game-owned source is
      unchanged by import, rebuild, query, and character-atlas operations.
- [x] Architecture tests keep file-write permission narrowly limited to the
      named, validated helper-owned cache Infrastructure adapters.
- [x] Domain, Application, Infrastructure, API, Presentation, integration, and
      architecture behavior is covered by xUnit v3 tests.
- [x] Manual comparison against the agreed in-game skill list and study-detail
      screens confirms the atlas labels and progress for the golden save.
- [x] The current-progress cache is bounded, path-safe, explicitly clearable,
      and cannot become authoritative character-progress history.
- [x] Ownership filtering and per-category counts are primary UI information;
      low-to-high grade order is explained by numeric labels as well as color.

## Success measures

- 100% of installed combat-skill records are imported or diagnosed.
- 100% of displayed character progress facts carry save-derived provenance or
  an explicit unavailability reason.
- 100% of displayed study details use a verified mapping for the detected game
  version.
- Identical source versions produce an equivalent catalogue and stable query
  order.
- A relevant source change is never reported as a current catalogue.
- Search finds the same skill by its available Chinese or English name.
- No catalogue operation changes any game-owned byte or runtime state.
- No raw effect text influences recommendation legality or scoring.

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| Reading-state bits are misunderstood | Begin with controlled evidence and preserve unknown values |
| `已大成` is confused with `功法精解` | Preserve named attainment and breakthrough fields with their verified equality, and keep simplification independent |
| Game updates change configuration shape | Record source identity and fail or rebuild explicitly |
| Character progress becomes stale | Read it from the active save snapshot and show read time/hash |
| SQLite creates a broad file-write exception | Use one path-guarded Infrastructure adapter and architecture tests |
| A partial rebuild is queried | Build transactionally and publish only a complete manifest |
| Raw effect text is treated as verified | Carry verification status and gate recommendation use on typed rules |
| Missing localization looks like missing skill data | Store language values independently and apply visible fallback |
| Extracted game artwork creates distribution risk | Use helper-owned UI assets and text-first presentation |
| Catalogue growth makes the UI slow | Index normalized names/filters and virtualize or page results |

## Evidence decisions

E2-001 through E2-017 established the versioned membership, 15-detail reading
map, Direct/Reverse activation states, importable static fields, display-only
raw-text boundary, and source-identity rules used by the atlas. Those decisions
are recorded in the linked architecture and review evidence rather than
inferred at runtime.

E2-F06 verified the remaining display semantics. For the current Taiwu,
`已大成` is the successful-breakthrough predicate
`(ActivationState & 0x001F) != 0`; it is not `功法精解`. The study-screen centre
percentage is current final `Power`, not proficiency or a current/maximum
ratio. Current and maximum power remain typed unavailable in standalone-save
reads because their installed calculation requires live special-effect state.

## Delivery reference

Implementation and completion evidence are tracked in
[the Epic 2 backlog](./BACKLOG.md).
