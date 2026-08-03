# Epic 2 backlog: Version-aware skill catalogue and character skill atlas

This backlog implements
[EPIC-002](./EPIC.md) while preserving the
permanent safety boundary in
[ADR-0001](../../architecture/ADR-0001-absolute-game-non-interference.md).

Epic 2 work items use `E2-nnn` identifiers so they do not collide with the
provisional `M2-nnn` future candidates recorded before Epic 2 was selected.

## Conventions

### Priority

- **P0:** Required for the first usable catalogue and atlas vertical slice.
- **P1:** Required for Epic 2 completion.
- **P2:** Valuable follow-up that may move to a later epic.

### Estimate

- **S:** Small, normally one focused change.
- **M:** Medium, several related classes and tests.
- **L:** Large, should be split during implementation if it cannot remain one
  reviewable vertical change.

### Status

- **Planned:** Scope is defined but implementation has not started.
- **In progress:** Implementation or verification is underway.
- **Blocked:** A documented external fact or decision is required.
- **Complete:** Acceptance criteria and required evidence are present.

### Definition of done

Every completed backlog item must:

- Preserve Clean Architecture dependency direction.
- Include xUnit v3 tests at the appropriate layer.
- Leave every save, game file, game database, configuration value, running
  game process, runtime memory location, and in-game state unchanged.
- Introduce no endpoint, port, adapter, hook, injection, patch, automation, or
  future extension point capable of modifying or controlling the game.
- Keep generated catalogue data in the exact validated helper-owned storage
  location and outside all game-owned directories.
- Never accept an arbitrary source or destination path through the public API.
- Do not commit or distribute a generated catalogue, proprietary game binary,
  complete extracted resource, icon, or artwork.
- Keep static skill definitions separate from save-derived character progress.
- Give unavailable, unsupported, stale, malformed, or unverified data an
  explicit typed state and player-facing explanation.
- Keep raw localized effect text out of recommendation rules unless a separate
  typed, verified mechanic already exists.
- Update API, architecture, evidence, and roadmap documentation when contracts
  or verified semantics change.
- Record the relevant test command and result in the completed item's evidence.

The game non-interference requirements are absolute product invariants. A work
item that conflicts with them must be rejected rather than postponed or
reclassified.

## Delivery order

| Order | Slice | Outcome |
|---:|---|---|
| 0 | Persistence safety boundary | Helper-owned SQLite cannot become a game-data write path |
| 1 | Evidence and golden progress | Saved fields are mapped to verified player-facing meanings |
| 2 | Domain and Application contracts | Static definitions and dynamic progress are independent typed models |
| 3 | Import and catalogue lifecycle | Installed definitions are imported, versioned, stored, and rebuilt |
| 4 | Character skill progress | The current save produces a detailed immutable skill atlas overlay |
| 5 | Joined queries and API | Search and skill details combine the right sources with provenance |
| 6 | Catalogue and atlas UI | Players can browse, filter, and inspect progress accessibly |
| 7 | Recommendation integration | Existing recommendation cards deep-link without depending on the cache |
| 8 | Verification and completion | Automated and in-game evidence satisfy the epic contract |

## Slice 0: Persistence safety boundary

### E2-000 — Constrain helper-owned catalogue persistence

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** M1-000

Extend the permanent non-interference architecture to permit narrowly scoped
SQLite writes for derived catalogue data without weakening any game-owned file
or runtime protection.

The storage path is chosen by trusted local configuration or a path provider,
not by a request. Create, replace, recovery, and deletion operations must prove
that their exact target is inside the helper-owned catalogue directory and
outside the game installation and configured save directories.

#### Acceptance criteria

- [x] The architecture documentation distinguishes read-only game sources from
      writable helper-owned catalogue storage.
- [x] A single Infrastructure-owned path provider returns the catalogue path.
- [x] Domain and Application contracts contain no filesystem path supplied by
      a player or HTTP request.
- [x] Catalogue write, replace, recovery, and delete operations reject targets
      outside the validated helper-owned directory.
- [x] The configured game installation and save directories are always rejected
      as catalogue destinations, including equivalent normalized paths.
- [x] Presentation contains no direct database or filesystem write API.
- [x] Existing save-reader and process-control prohibitions remain unchanged.
- [x] Architecture tests permit only the named catalogue persistence adapter to
      write helper-owned data; they do not add a global file-write exception.
- [x] Tests cover traversal, relative paths, symlinks or reparse points where
      applicable, case differences, and overlapping-directory configuration.

#### Evidence

- [ADR-0002: Constrain helper-owned catalogue storage](../../architecture/ADR-0002-helper-owned-catalogue-storage.md).
- `CatalogueStoragePathProvider` permits only the fixed database and rebuild
  filenames directly inside its validated helper-owned catalogue directory.
- `CatalogueStoragePathProviderTests`: 18 path-boundary tests cover traversal,
  fixed filenames, protected-directory overlap, case behavior, reparse points,
  and unchanged protected directories.
- `ArchitectureBoundaryTests` reserves persistence APIs for the one named
  future SQLite adapter and rejects catalogue paths in Domain, Application,
  and HTTP contracts.
- `dotnet test TaiWu.slnx --no-restore --verbosity minimal`: 432 total,
  431 passed, 0 failed, and 1 opt-in local-save integration test skipped.

## Slice 1: Evidence and golden progress

### E2-001 — Define the golden catalogue and character-progress scenario

**Status:** Complete

**Priority:** P0

**Estimate:** S

**Dependencies:** E2-000

Select a stable local game version, configured save, and small set of combat
skills that collectively exercise the progress states shown by the intended
atlas.

The set should include, where the save permits:

- A configured skill the character has not obtained.
- An obtained but incomplete skill.
- A skill that is eligible for breakthrough.
- Direct and reverse broken-through skills.
- A mastered skill.
- An equipped or activated skill.
- A skill with partially completed study details.
- At least one missing or unsupported field to verify honest UI behavior.

#### Acceptance criteria

- [x] The evidence document records game, language-resource, and save identities
      without committing proprietary source content.
- [x] The save is identified by hash and helper-safe metadata rather than being
      copied into the repository.
- [x] Stable skill identifiers and bilingual names identify each golden skill.
- [x] Manually observed labels and progress are recorded with observation time
      and source.
- [x] The selected set covers all independent progress facts required by the
      epic or documents why a state is unavailable.
- [x] Source fingerprints before and after evidence collection match.
- [x] The scenario can be repeated after a catalogue rebuild.

#### Evidence

- [Golden skill-atlas scenario](../../scenarios/E2-001-golden-skill-atlas.md).
- [Sanitized machine-readable metadata](../../scenarios/evidence/E2-001-golden-skill-atlas-metadata.json).
- Six bilingual stable skill IDs cover mastered Direct and Reverse skills,
  obtained and partial states, an achievable Direct breakthrough, equipped
  state, and explicit unsupported ownership/detail semantics.
- The configured save and both language packs retained identical lengths,
  timestamps, and SHA-256 fingerprints before and after evidence collection.

### E2-002 — Verify combat-skill progression and study-detail semantics

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E2-001

Map the relevant GameData and save fields to precise player-facing meanings
before those meanings are encoded in Domain models or UI labels.

This work must verify collection membership, proficiency values, the complete
study-detail representation, breakthrough readiness, completed breakthrough,
practice direction, activation, equipment, and mastery. It must extend the
existing reading-state work, which currently counts selected Direct and
Reverse bits for breakthrough eligibility but does not preserve each detail.

#### Acceptance criteria

- [x] Collection membership is given a verified label such as obtained or
      learned; ambiguous terminology is not used as fact.
- [x] The valid range and meaning of proficiency power and maximum power are
      documented.
- [x] The relationship between saved proficiency and the in-game percentage is
      verified or marked unavailable.
- [x] Every study-detail bit or field for the detected version has a stable ID,
      group, ordering, localized label source, and studied-state rule.
- [x] Common, Direct, Reverse, mutually exclusive, and optional details are
      identified only when verified.
- [x] The rule connecting studied details to available breakthrough directions
      is documented and tested against the golden scenario.
- [x] Breakthrough-ready, broken-through, activation, direction, and mastery
      are proven to be separate or explicitly related facts.
- [x] Unknown activation, reading-state, or version values produce unavailable
      results rather than inferred labels.
- [x] The evidence records the inspected game version and the APIs or fields
      used without copying proprietary implementations.
- [x] Existing Epic 1 breakthrough behavior remains valid or receives a
      separately reviewed correction with regression tests.

#### Evidence

- [Combat-skill progress semantics](../../architecture/COMBAT-SKILL-PROGRESS-SEMANTICS.md).
- `CombatSnapshotMappingTests` now cover 15 stable read/active details,
  localization keys, wheel ordering, invalid bitfields, readiness, and
  completed-breakthrough precedence.
- `LocalGameDataIntegrationTests` contains a target-independent raw golden-skill
  assertion guarded by the opt-in variable and the E2-001 save SHA-256.
- The configured save advanced after evidence capture; the fingerprint guard
  correctly skipped stale expectations without exposing its current path or
  hash.
- `dotnet test TaiWu.slnx --no-restore --verbosity minimal`: 444 total,
  442 passed, 0 failed, and 2 opt-in integration assertions skipped.

## Slice 2: Domain and Application contracts

### E2-003 — Define static combat-skill catalogue models

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E2-002

Create immutable Domain models for installed combat-skill definitions and
their provenance without depending on GameData, SQLite, HTTP, or Presentation.

#### Acceptance criteria

- [x] A definition uses the stable combat-skill identifier as its identity.
- [x] Traditional Chinese and English names are independent optional values
      with source provenance and deterministic fallback.
- [x] Category, grade, faction, element, equipment type, base grid cost,
      specific-grid contribution, and generic-grid contribution are typed.
- [x] Requirements, timing, effect IDs, and raw display descriptions distinguish
      verified typed mechanics from unverified text.
- [x] Unsupported and unavailable fields preserve a reason.
- [x] Definitions cannot contain duplicate localized names for the same
      language, invalid grades, invalid costs, or unknown enum values without an
      explicit unsupported representation.
- [x] Source-record identifiers used for diagnostics do not leak
      Infrastructure types.
- [x] Domain tests cover validation, equality, immutability, language fallback,
      and unavailable values.

#### Evidence

- [Combat-skill catalogue Domain model](../../architecture/COMBAT-SKILL-CATALOGUE-DOMAIN.md).
- `TaiWu.Domain.CombatSkills` contains identity-based immutable definitions,
  bilingual name fallback, typed fields, opaque source references, and explicit
  available, unavailable, and unsupported states.
- `CombatSkillDefinitionTests`: 18 cases covering typed construction, stable-ID
  equality, provenance, validation, fallback, immutable copies, and raw-text
  separation; the full Domain suite passes 214/214.
- `ArchitectureBoundaryTests`: 67/67 passed, including the existing inner-layer
  dependency guard that prevents Domain references to Infrastructure, API, or
  GameData assemblies.
- `dotnet test TaiWu.slnx --no-restore --verbosity minimal`: 462 total,
  460 passed, 0 failed, and 2 opt-in integration assertions skipped.

### E2-004 — Define character skill-progress and study-detail models

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E2-002

Create immutable Domain models for character-specific skill progress and its
study details. Avoid a single status enum: each verified fact remains
independent and carries provenance or an unavailability reason.

#### Acceptance criteria

- [x] Progress is keyed by character ID, save-snapshot identity, and skill ID.
- [x] Obtained or learned state uses the exact terminology verified by E2-002.
- [x] Current proficiency, maximum proficiency, and percentage validate their
      ranges and handle unavailable values.
- [x] Study details have stable ID, display order, group, label, and a state of
      studied, not studied, or unavailable.
- [x] Aggregate study completeness is derived from detail state and does not
      count unavailable details as incomplete.
- [x] Breakthrough readiness, available breakthrough directions, completed
      breakthrough, active direction, mastery, activation, and equipment are
      separate properties.
- [x] Impossible combinations proven by E2-002 are rejected, while unproven
      combinations remain representable as unavailable or conflicting.
- [x] Existing `CombatSkillSnapshot` responsibilities are either reused through
      composition or migrated without duplicating contradictory concepts.
- [x] Domain tests cover partial progress, unknown details, direct/reverse
      combinations, mastery, and equipped state.

#### Evidence

- [Character combat-skill progress Domain model](../../architecture/CHARACTER-COMBAT-SKILL-PROGRESS-DOMAIN.md).
- `CharacterCombatSkillProgress` is keyed by character, save fingerprint/read
  time, and stable skill ID; it contains independent learned, proficiency,
  detail, breakthrough, direction, attainment-mastery, simplification,
  activation, and equipment facts.
- `SkillProgressField<T>` preserves available, unavailable, and conflicting
  values with opaque per-field sources; conflicts retain both observations.
- `CharacterCombatSkillProgressTests`: 23 cases; the full Domain suite passes
  237/237.
- `dotnet test TaiWu.slnx --no-restore --verbosity minimal`: 485 total,
  483 passed, 0 failed, and 2 opt-in integration assertions skipped.

#### Evidence when complete

- Domain model and invariant tests.
- A documented mapping between Epic 1 snapshot concepts and Epic 2 progress.

### E2-005 — Add catalogue and atlas Application ports and use cases

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E2-003, E2-004

Define query-oriented Application contracts before implementing GameData,
SQLite, or UI adapters.

#### Acceptance criteria

- [x] A definition-source port reads installed static definitions without
      exposing GameData objects.
- [x] A catalogue repository port queries and replaces helper-owned derived
      definitions without accepting arbitrary paths.
- [x] A progress-reader port returns immutable save-derived progress.
- [x] Use cases exist for ensuring catalogue freshness, searching definitions,
      reading details, reading the character atlas, and reading catalogue
      status.
- [x] Search filters are typed and bounded; paging or result limits are
      deterministic.
- [x] Language selection and fallback are Application policies, not SQL or UI
      string parsing.
- [x] Cancellation and failure results distinguish missing sources, stale
      catalogue, rebuild failure, unsupported version, and save-read failure.
- [x] Application has no dependency on Infrastructure, SQLite, GameData, or
      ASP.NET Core.
- [x] Use-case tests cover orchestration and every failure/status path.

#### Evidence

- [Combat-skill catalogue Application boundary](../../architecture/COMBAT-SKILL-CATALOGUE-APPLICATION.md).
- `CombatSkillCatalogueUseCaseTests` cover freshness, source and repository
  failures, rebuild results, bilingual matching and fallback, deterministic
  paging, details, atlas joining, save failures, cancellation, immutability,
  and path-free contracts.
- `ArchitectureBoundaryTests` proves the three ports expose only Application,
  Domain, and framework types, contain no path parameter, and retain the
  read-only GameData-source marker.
- `dotnet test TaiWu.slnx --no-restore --verbosity minimal`: 517 total,
  515 passed, 0 failed, and 2 opt-in integration assertions skipped.

## Slice 3: Import and catalogue lifecycle

### E2-006 — Implement the read-only bilingual GameData importer

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E2-003, E2-005

Import static combat-skill definitions from the player's installed GameData
and Traditional Chinese and English language resources through a strictly
read-only adapter.

#### Acceptance criteria

- [x] The importer enumerates every configured combat-skill record in stable
      identifier order.
- [x] Every record is imported or produces a deterministic diagnostic with its
      stable source identifier and reason.
- [x] Traditional Chinese and English names are read independently and retain
      their source identity.
- [x] The importer maps all fields approved by E2-003 and leaves unsupported
      fields explicitly unavailable.
- [x] Raw effect or requirement text is labeled display-only unless a typed
      verified rule already exists.
- [x] No runtime-only calculation is invoked merely to populate static data.
- [x] Source files are opened read-only wherever access mode is controlled by
      the helper.
- [x] Source hashes before and after import match.
- [x] Import results contain no mutation-capable GameData object.
- [x] Unit and opt-in local integration tests cover mapping, localization
      fallback, malformed records, determinism, and source preservation.

#### Evidence

- [Read-only bilingual catalogue import](../../architecture/COMBAT-SKILL-CATALOGUE-IMPORT.md),
  including the verified field mapping and golden installed inventory.
- `CombatSkillDefinitionMapperTests`,
  `TaiwuCombatSkillDefinitionSourceTests`, and `TaiwuLanguageCatalogTests`
  cover full mapping, independent languages and fallback, malformed typed
  values and record diagnostics, immutable collection copies, missing sources,
  cancellation, duplicate and dangling language keys, source provenance, and
  fixed source-path derivation.
- `Bilingual_catalogue_import_is_repeatable_and_read_only` imported all 946
  configured records twice in stable order, verified both names for golden
  skill `456`, reported 0 error diagnostics, and proved the three source
  fingerprints unchanged. The binary fingerprint is not committed.
- With the E2-006 catalogue assertion enabled,
  `dotnet test TaiWu.slnx --no-restore --verbosity minimal`: 529 total,
  527 passed, 0 failed, and 2 save-dependent integration assertions skipped.

### E2-007 — Implement the helper-owned SQLite catalogue store

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E2-000, E2-003, E2-005

Implement a SQLite repository for static definitions, localized text, source
manifest data, and import diagnostics at the validated helper-owned path.

#### Acceptance criteria

- [x] The schema stores definitions, localized values, typed attributes, raw
      display references, source manifest, and diagnostics without storing
      complete source files.
- [x] Schema constraints enforce unique stable IDs and unique language values
      per skill and language.
- [x] Search indexes support normalized Chinese and English name queries and
      required filters.
- [x] Insert and replacement order is deterministic.
- [x] A reader never observes a partially built catalogue.
- [x] Transactions roll back completely on import or persistence failure.
- [x] Database creation and replacement use only the path from E2-000.
- [x] The generated database and transient files are excluded from Git and
      publish artifacts.
- [x] Repository queries map to Domain models without leaking SQLite types.
- [x] Tests cover round trips, ordering, constraints, rollback, concurrent
      readers, malformed data, and path enforcement.

#### Evidence

- [Helper-owned combat-skill catalogue schema](../../architecture/COMBAT-SKILL-CATALOGUE-SQLITE.md)
  documents the seven strict tables, constraints, indexes, deterministic
  ordering, atomic replacement, and sanitized status behavior.
- `SqliteCombatSkillCatalogueStoreTests`: 12 tests cover missing storage,
  complete Domain/provenance round trips, unavailable and unsupported fields,
  typed filtering, stable limits, unique-key constraints, replacement,
  rollback, concurrent readers, malformed and incomplete databases, and
  pre-write cancellation.
- `ArchitectureBoundaryTests` proves the adapter remains internal,
  Infrastructure-owned, dependent on `CatalogueStoragePathProvider`, and the
  only production source allowed to use persistence APIs. It also checks the
  generated-file exclusions and pinned SQLite packages.
- `CatalogueStoragePathProviderTests`: 18 existing path-boundary tests continue
  to protect fixed filenames, game/save directories, traversal, reparse points,
  and overlapping configuration.
- With the E2-006 catalogue assertion enabled,
  `dotnet test TaiWu.slnx --no-restore --verbosity minimal`: 543 total,
  541 passed, 0 failed, and 2 save-dependent integration assertions skipped.

### E2-008 — Add source manifest, invalidation, and deterministic rebuild

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E2-006, E2-007

Decide whether the local catalogue is current and rebuild it safely when the
schema, importer, GameData, or language resources change.

#### Acceptance criteria

- [x] The manifest records schema version, importer version, relevant GameData
      identity, each imported language-resource identity, build time, and
      import counts.
- [x] Source identity uses stable version metadata and hashes where needed to
      avoid false-current results.
- [x] Identical sources do not trigger unnecessary rebuilds.
- [x] A relevant source or schema change never reports the old catalogue as
      current.
- [x] Rebuild produces an equivalent catalogue and stable query order for
      identical input.
- [x] Build occurs transactionally or in a separate validated helper-owned
      file before the complete result becomes visible.
- [x] Missing, empty, interrupted, and corrupt helper databases recover with a
      clear status and no source-file changes.
- [x] A rebuild failure preserves a previously valid catalogue only when it is
      clearly reported as stale; it never presents it as current.
- [x] Concurrent ensure requests result in one controlled rebuild.
- [x] Tests cover every invalidation input and recovery path.

#### Evidence

- [Combat-skill catalogue lifecycle](../../architecture/COMBAT-SKILL-CATALOGUE-LIFECYCLE.md)
  defines the five-part source identity, schema/importer bump rules, state
  transitions, typed recovery outcomes, concurrency gate, and non-interference
  boundary.
- [Helper-owned combat-skill catalogue schema](../../architecture/COMBAT-SKILL-CATALOGUE-SQLITE.md)
  records schema version 2, importer and diagnostic manifest counts, and the
  validated sibling-file recovery protocol.
- Application lifecycle tests independently invalidate GameData version,
  importer version, GameData fingerprint, Traditional Chinese fingerprint,
  English fingerprint, and definition count; eight concurrent ensure callers
  produce one rebuild and seven current results.
- `SqliteCombatSkillCatalogueStoreTests`: 16 tests now include manifest-count
  reconciliation, no-write current detection, recovery from empty, malformed,
  and old-schema databases, and an interrupted corrupt recovery that preserves
  the original corrupt file with a typed status and no rebuild-file residue.
- `Bilingual_catalogue_import_is_repeatable_and_read_only` imported and stored
  all 946 configured definitions twice in stable order, compared complete
  field-level content identities, and proved the three installed sources
  unchanged. The comparison fingerprint and database are not committed.
- With the catalogue integration assertion enabled,
  `dotnet test TaiWu.slnx --no-restore --verbosity minimal`: 551 total,
  549 passed, 0 failed, and 2 save-dependent integration assertions skipped.

## Slice 4: Character skill progress

### E2-009 — Read the character skill-progress overlay

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E2-004, E2-005, E2-002

Extend the typed save snapshot workflow to capture every verified independent
progress fact required by the atlas. Reuse the existing read-only archive
session and do not parse legacy diagnostic lines.

#### Acceptance criteria

- [x] Progress is read for every character combat-skill entry in stable order.
- [x] The reader captures the exact obtained/learned fact verified by E2-002.
- [x] Proficiency current and maximum values are typed and range-checked.
- [x] Breakthrough readiness, available directions, completed breakthrough,
      active direction, mastery, activation, and equipment are captured
      independently.
- [x] Unknown or invalid source values produce warnings and unavailable fields
      rather than guessed defaults.
- [x] Snapshot metadata includes save hash, read time, game version, and
      warnings.
- [x] Character progress is immutable and is not written into the static
      catalogue database.
- [x] Existing recommendation snapshot behavior remains compatible or is
      migrated with full regression coverage.
- [x] Source fingerprints before and after the read match.
- [x] Unit and opt-in integration tests cover the golden progress states.

#### Evidence when complete

- [Read-only character combat-skill progress](../../architecture/CHARACTER-COMBAT-SKILL-PROGRESS-READER.md).
- Mapping, configuration, Application contract, dependency-registration,
  architecture, and typed golden integration tests pass locally.
- The 2026-08-02 golden save fingerprint is
  `77D88A43934E6369F9475AA3742B3161C79A2E9E749BCA6258A2A91391EA0673`.
  Two repeated reads returned the same 501-entry progress overlay, and all
  guarded save and game-source fingerprints matched before and after.
- Golden cases cover learned zero-state, immediate Direct breakthrough,
  completed Direct and Reverse breakthrough, activation, equipment, explicit
  unavailable fields, metadata warnings, stable order, and snapshot identity.
- Focused golden integration: 1 passed, 0 failed, 0 skipped. Full solution with
  local catalogue integration: 561 passed, 0 failed, and 3 intentionally
  save-dependent assertions skipped; the E2-009 golden assertion passed
  separately with the configured current save.

### E2-010 — Decode individual study details and completeness

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E2-002, E2-004, E2-009

Convert the verified version-specific reading state into the complete ordered
set of typed study details and aggregate completeness used by the atlas.

#### Acceptance criteria

- [x] The decoder is selected only for game versions covered by verified
      evidence.
- [x] Every verified detail has stable ID, order, group, localized label source,
      and studied state.
- [x] Unrecognized bits or values are preserved in diagnostics and make the
      affected completeness result partial or unavailable.
- [x] Studied count excludes unavailable details from both numerator and any
      claimed complete denominator.
- [x] Missing-detail output lists exact verified details, not only a percentage.
- [x] Breakthrough-direction availability uses the same decoded source rather
      than an independent contradictory bit-count implementation.
- [x] Unsupported game versions produce a clear warning and no fabricated
      detail map.
- [x] Tests cover none, partial, complete, Direct, Reverse, mixed, unknown-bit,
      malformed, and unsupported-version cases.

#### Evidence when complete

- [Versioned combat-skill study-detail decoder](../../architecture/COMBAT-SKILL-STUDY-DETAIL-DECODER.md),
  including the truth table tied to E2-002 evidence.
- Domain, Application, label-source, mapping, architecture, and opt-in golden
  integration tests cover every verified detail and failure mode.
- The 2026-08-02 golden save fingerprint is
  `9C30C00CF1ABD05973435B14B724A0A41A1B0DCD7847A8CA04D4E60E2B53C916`.
  Two reads produced the same 506-entry overlay. Skill `456` had all 15 details
  read with the five Reverse details active; skill `498` exposed the exact 15
  missing details. All guarded save, GameData, and selected language-resource
  fingerprints matched before and after.
- Focused golden integration: 1 passed, 0 failed, 0 skipped. Full solution with
  local catalogue integration: 571 passed, 0 failed, and 3 intentionally
  save-dependent assertions skipped.

## Slice 5: Joined queries and API

### E2-011 — Build catalogue search and character-atlas queries

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E2-005, E2-008, E2-009, E2-010

Join static definitions with the latest character progress by stable skill ID
and expose deterministic Application results for search, list, and detail
views.

#### Acceptance criteria

- [x] The join never copies save-derived progress into authoritative static
      catalogue state.
- [x] Catalogue entries without progress use the exact verified negative or
      unknown possession label from E2-002.
- [x] Progress entries without a static definition remain visible with a
      diagnostic instead of disappearing.
- [x] Search matches Traditional Chinese and English names using deterministic
      normalization and fallback.
- [x] Filters cover category, grade, faction, equipment type, element, and each
      independent progress fact required by the epic.
- [x] Base grid cost and current character-effective cost remain distinct.
- [x] List and detail results carry catalogue freshness, save freshness,
      provenance, completeness, and warnings.
- [x] Paging or virtualization keys are stable across identical queries.
- [x] Catalogue rebuild, missing save, stale catalogue, partial localization,
      and unsupported study mapping have explicit result states.
- [x] Application tests cover joins, filters, language matching, ordering, and
      all partial-data paths.

#### Evidence

- [Combat-skill catalogue and character-atlas query contracts](../../architecture/COMBAT-SKILL-ATLAS-QUERIES.md).
- `ReadCharacterCombatSkillAtlas` builds a deterministic in-memory union by
  stable skill ID, preserves immutable definition/progress values, retains
  progress-only entries with diagnostics, and derives exact unlearned state
  from complete learned-collection absence.
- Search and atlas requests apply bilingual NFKC/case/whitespace normalization,
  typed static and independent progress filters, deterministic fallback,
  bounded paging, and stable `combat-skill:{id}` keys.
- Atlas and details expose separate base/effective costs, catalogue and save
  freshness, provenance, warnings, partial-data flags, and explicit rebuild or
  failure states.
- `CombatSkillCatalogueUseCaseTests`: 77/77 passed, including join, every
  filter, unavailable-versus-false semantics, normalization, ordering, paging,
  detail, rebuild, and partial/failure paths.
- `dotnet test TaiWu.slnx --no-restore --verbosity minimal` with installed
  catalogue verification enabled: 585 total, 582 passed, 0 failed, and 3
  opt-in save assertions skipped because no save path was supplied.

### E2-012 — Add information-only catalogue and atlas API endpoints

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E2-011

Expose local HTTP query endpoints for catalogue status, search, skill details,
and the current character atlas.

#### Acceptance criteria

- [x] `GET /api/combat-skills` supports bounded search, filter, sort, and paging
      parameters.
- [x] `GET /api/combat-skills/{skillId}` returns joined definition and current
      character progress when available.
- [x] `GET /api/character-skill-atlas` returns the current atlas and snapshot
      metadata.
- [x] Catalogue status is returned directly or embedded consistently in query
      responses.
- [x] Validation failures use stable problem responses and do not expose local
      filesystem paths.
- [x] API responses distinguish missing, stale, rebuilding, partial,
      unsupported, and failed states.
- [x] No endpoint accepts a game/save path or changes any game-owned state.
- [x] Any explicit catalogue rebuild operation can affect only the trusted
      helper-owned cache and is clearly named as cache maintenance.
- [x] Controller tests cover success, validation, partial data, rebuild status,
      unsupported versions, and failure mapping.
- [x] API documentation includes source precedence and raw-text limitations.

#### Evidence

- [Combat-skill catalogue and character-atlas API](../../api/COMBAT-SKILLS.md).
- `CombatSkillsController` exposes status, bounded definition search, joined
  detail, and explicitly named `catalogue-cache/rebuild`; the separate
  `CharacterSkillAtlasController` exposes the read-only atlas.
- API response contracts project available/unavailable/conflicting values
  safely, retain catalogue/save provenance and warnings, and replace local
  exception or path details with stable public reasons.
- Search supports deterministic `DisplayName`, `SkillId`, and `Grade` sorting;
  full-width/case/whitespace normalization and stable ID tie-breaks remain in
  Application.
- Production DI now resolves the guarded singleton SQLite catalogue repository
  using the fixed helper-owned path provider and protected game/save roots.
- `CombatSkillsControllerTests` cover success, filters, sort, paging,
  validation, joined/partial serialization, safe missing-save and unsupported
  states, all catalogue statuses, rebuild, and route/mutation boundaries.
- `ArchitectureBoundaryTests`: 73/73 passed, including the API path/body/verb
  boundary; `CatalogueDependencyInjectionTests` verifies production repository
  wiring without creating a database.
- `dotnet test TaiWu.slnx --no-restore --verbosity minimal` with installed
  catalogue verification enabled: 605 total, 602 passed, 0 failed, and 3
  opt-in save assertions skipped because no save path was supplied.

## Slice 6: Catalogue and atlas UI

### E2-013 — Build the searchable catalogue and character-atlas page

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E2-011, E2-012

Add a local page that presents installed combat skills and current character
progress in a faction-oriented, searchable layout inspired by the information
hierarchy of the game without copying proprietary artwork.

#### Acceptance criteria

- [x] The page can be reached directly and through local navigation.
- [x] Search accepts Traditional Chinese or English names.
- [x] Filters cover category, grade, faction, equipment type, element, and
      independent progress facts including breakthrough and mastery.
- [x] Skills can be filtered and grouped by familiar named factions, with
      category available as an additional filter.
- [x] The faction filter presents a compact circular mark using the first
      character of the faction name in the active language, without requiring
      proprietary artwork.
- [x] Faction-name and monogram color comes from the installed faction's main
      inner-power element; the outer ring comes from its primary alignment,
      with both meanings also written as localized text.
- [x] Each collapsed skill card shows the active-language name, faction, grade,
      category, and a primary current status before exposing the full set of
      independent progress badges.
- [x] Each collapsed skill card uses a circular active-language name initial
      instead of game artwork, while an expanded card spans the result row for
      readable facts and navigation.
- [x] `已取得`, `可突破`, `已突破`, `正`, `逆`, `已大成`, and `已裝備` labels
      appear only when supported by the corresponding typed fact.
- [x] A completed breakthrough places one circled `正` or `逆` marker before
      the skill name. A ready breakthrough places `突破` plus only its verified
      available direction markers before the name, ordered `正` then `逆`.
- [x] Catalogue freshness, build version, save read time, and warnings are
      visible without opening developer tools.
- [x] Loading, rebuilding, empty, partial, stale, unsupported, and failure
      states are usable and translated.
- [x] Large catalogues use bounded paging or virtualization and remain
      responsive.
- [x] Keyboard users can search, filter, move through results, and open a skill.
- [x] Status is never communicated by color alone.
- [x] No game icon or artwork is required or redistributed.

#### Evidence

- `SkillCatalogueRenderingTests` covers the current catalogue/current-Taiwu
  page, freshness, filters, positive-only progress labels, missing-cache rebuild
  action, unsupported state, and accessible expandable cards.
- `ArchitectureBoundaryTests` protects the read-only page boundary, automatic
  current-Taiwu selection, semantic list/details markup, responsive grid, and
  reduced-motion treatment.
- Product-owner review replaced the faction dropdown with a responsive
  circular picker that forms two rows at desktop width and reflows without
  horizontal scrolling on narrow screens. Each mark uses the active-language
  faction initial, installed `Organization.FiveElementsType` for its text
  color, and installed `Organization.MainMorality` for its outer ring;
  localized element and alignment labels preserve the same information without
  relying on color.
- Results remain grouped by faction, `品階` is relabeled as `品級`, and grade
  plus the primary current status remain visible in each collapsed card.
- Product-owner review replaced repeated long-form direction text beside the
  primary status with compact, accessible circled `正`/`逆` markers before the
  skill name; ready skills show `突破` and their verified available directions.
- Product-owner review changed the collapsed results into a compact circular
  skill atlas. Each mark uses the first character of the active-language skill
  name; opening a mark expands that card across the row without removing its
  grade, faction, category, progress status, or detail link.
- Live validation used the current save and the explicitly rebuilt helper-owned
  GameData `1.0.0` catalogue: 946 definitions matched the character overlay.
  Traditional Chinese search for `黑血蠱降` returned the English-localized
  `Corruptive Gu Infection` card; switching language displayed `黑血蠱降` and
  the translated category without horizontal overflow.
- Responsive browser verification measured `1280x720` desktop and `390x844`
  mobile viewports with no horizontal overflow. Local-only screenshots are
  `E2-013-atlas-desktop.png` (SHA-256
  `F72EB68536BCBECAF88EB299E31AA540D5DD6183D9948A54F88D998E7EBCE3BD`)
  and `E2-013-atlas-mobile.png` (SHA-256
  `78030A1BF148F53E2C470CB7100896B200E8B09C10742E50B90A76A701B90778`).
- `dotnet test TaiWu.slnx --no-restore --verbosity minimal` with installed
  catalogue verification enabled: 609 total, 606 passed, 0 failed, and 3
  opt-in historical save assertions skipped because no pinned save path was
  supplied.

### E2-014 — Build the skill detail and accessible study-detail view

**Status:** Complete

**Priority:** P1

**Estimate:** L

**Dependencies:** E2-010, E2-011, E2-013

Present one skill's static definition, current character state, provenance, and
complete study-detail progress. The visual design may echo a wheel or map, but
the semantic representation must remain understandable as text and to
assistive technology.

#### Acceptance criteria

- [x] The view follows the active language tab and never presents Chinese and
      English names or descriptions together; fallback remains explicit when
      the selected language is unavailable.
- [x] Static category, grade, faction, element, equipment type, costs,
      requirements, and effect references are separated from character state.
- [x] Base cost and current effective cost have distinct labels and provenance.
- [x] Proficiency current, maximum, and percentage appear only when valid.
- [x] Every study detail is identified as studied, not studied, or unavailable.
- [x] Exact missing verified details are listed in text.
- [x] Any wheel/map visualization has equivalent ordered semantic markup and
      does not rely on color alone.
- [x] Common, Direct, and Reverse groups appear only when verified.
- [x] Breakthrough readiness, completed breakthrough, direction, mastery,
      activation, and equipment are displayed independently.
- [x] Raw effect descriptions carry a display-only or verified-mechanic label.
- [x] Field-level source and unavailability explanations are accessible on
      demand.
- [x] The view supports initial, loading, partial, unsupported, and failure
      states in both languages.

#### Evidence

- `SkillDetailRenderingTests` covers static/current-state separation, valid-only
  proficiency, distinct costs, active-language isolation and explicit fallback, exact
  missing and unavailable detail states, raw-text trust labels, source
  disclosures, unsupported Chinese state, and automatic current-Taiwu reads.
- `ArchitectureBoundaryTests` protects the direct route, streamed loading state,
  read-only Application boundary, ordered-list study semantics, non-color
  statuses, and the no-artwork requirement. Application and API tests confirm
  that omitting `characterId` selects the current Taiwu.
- Keyboard order follows breadcrumb, page identity, the active-language name, static
  facts, character facts, ordered Common/Direct/Reverse lists, then raw text.
  Native links and `details`/`summary` disclosures remain keyboard operable;
  every colored state also has a symbol and written label.
- Live validation used the current save and skill `456` (`黑血蠱降` /
  `Corruptive Gu Infection`). The current overlay reported all 15 available
  verified details studied, the three verified groups, the active Reverse
  details, explicit partial-data warnings, and separate base/current costs.
  The initial page streamed immediately while the save read completed; language
  changes showed a translated loading state instead of mixing old/new labels.
- Product-owner review removed the simultaneous bilingual-name panel and
  filters raw descriptions to the active language tab.
- Browser verification measured the default `1280x720` desktop and `390x844`
  mobile viewports with no horizontal overflow. Local-only screenshots are
  `E2-014-detail-desktop.png` (SHA-256
  `A7DA796B352CC58B0CA43F9B708C823EED618968A6AE7D9E59000F479BA024C3`),
  `E2-014-study-map-desktop.png` (SHA-256
  `B212B0305E19B35EA379D343083E6BC227181D9A312BF582F30D40EE32D0FB43`),
  and `E2-014-detail-mobile-zh.png` (SHA-256
  `11291780A2B999EAE5AF783289309CED8CDCFFAFD9D238E5E50307E735C0E33B`).
- `dotnet test TaiWu.slnx --no-restore --verbosity minimal` with installed
  catalogue verification enabled: 612 total, 609 passed, 0 failed, and 3
  opt-in historical save assertions skipped because no pinned save path was
  supplied.

## Slice 7: Recommendation integration

### E2-015 — Link recommendations to catalogue details

**Status:** Complete

**Priority:** P1

**Estimate:** S

**Dependencies:** E2-014

Let a player open a recommended skill's catalogue detail without making the
recommendation workflow depend on catalogue availability.

#### Acceptance criteria

- [x] Recommendation skill cards link by stable skill ID.
- [x] The detail view identifies the recommendation context when supplied but
      remains usable as a standalone route.
- [x] Missing, stale, or rebuilding catalogue state does not prevent Epic 1
      recommendations from being created or displayed.
- [x] Raw catalogue descriptions do not create or modify recommendation rules,
      feasibility, threats, counters, or scores.
- [x] Existing recommendation API and UI contracts remain backward compatible
      unless a separately documented additive change is required.
- [x] Tests cover successful navigation, missing definitions, stale catalogue,
      and recommendation independence.

#### Evidence

- Recommendation component rendering verifies every recommended skill exposes
  `/skills/{skillId}?context=recommendation` without requiring catalogue
  services, while preserving the existing view-model and API contracts.
- Detail rendering verifies the recommendation-context note and return link,
  standalone behavior, missing catalogue, stale catalogue, and missing static
  definition states. Catalogue failures never suppress the already-produced
  recommendation card.
- `ArchitectureBoundaryTests` proves the recommendation card and Domain/
  Application recommendation logic do not reference catalogue repositories or
  `RawCombatSkillDescription`; the detail page alone consumes the optional
  presentation context.
- `dotnet test TaiWu.slnx --no-restore --verbosity minimal` with installed
  catalogue verification enabled: 615 total, 612 passed, 0 failed, and 3
  opt-in historical save assertions skipped because no pinned save path was
  supplied.

## Slice 8: Verification and completion

### E2-016 — Add end-to-end automated catalogue and atlas verification

**Status:** Complete

**Priority:** P1

**Estimate:** L

**Dependencies:** E2-008, E2-010, E2-012, E2-014, E2-015

Create the automated evidence needed to trust import, persistence, progress,
API, UI, and non-interference behavior as one vertical slice.

#### Acceptance criteria

- [x] Domain tests cover definition, provenance, progress, study-detail, and
      completeness invariants.
- [x] Application tests cover catalogue lifecycle, joins, filters, language
      fallback, status propagation, and failures.
- [x] Infrastructure tests cover import mapping, source preservation, SQLite
      transactions, path guards, invalidation, corruption recovery, and
      deterministic rebuild.
- [x] API tests cover every endpoint and status mapping.
- [x] Presentation tests cover filters, progress badges, detail states,
      accessibility semantics, and recommendation deep links.
- [x] Architecture tests prevent SQLite, GameData, or filesystem dependencies
      from crossing inward.
- [x] Architecture tests keep game-owned writes and process-control APIs
      forbidden.
- [x] Opt-in local integration tests compare two identical imports and verify
      stable content and ordering.
- [x] Opt-in local integration tests fingerprint all inspected game and save
      sources before and after import and atlas reads.
- [x] The full default suite passes without requiring a proprietary save in CI.

#### Evidence when complete

- Updated testing documentation with test counts and commands.
- Local integration result recording versions, counts, hashes, and skipped
  conditions without proprietary content.

#### Completion evidence

- `docs/reviews/E2-016-automated-verification.md` maps every acceptance area to
  its primary test classes and records reproducible commands.
- The default suite passed 616 total tests: 611 passed, 0 failed, and 5 opt-in
  local-data tests skipped.
- Installed-catalogue verification passed 612 tests with 4 save-dependent
  skips. It deterministically imported all 946 GameData `1.0.0` definitions.
- The new current-save vertical check passed independently: it imported and
  persisted the catalogue, produced two identical 946-match atlas views, and
  verified every inspected game/save fingerprint was unchanged.
- The complete local integration project passed 3 of 6 tests against the
  current save. Three pinned historical assertions skipped cleanly because the
  current save no longer matches their golden fingerprints.

### E2-017 — Validate the atlas against the game and close Epic 2

**Status:** In progress

**Priority:** P1

**Estimate:** M

**Dependencies:** E2-001, E2-002, E2-016

Compare the completed local catalogue and character atlas with the golden
in-game skill list and study-detail screens, record discrepancies, and make the
final completion decision.

#### Acceptance criteria

- [x] Catalogue counts and representative definitions match the installed
      game's visible or configured data for the recorded version.
- [x] Golden character skills show the correct obtained/learned, proficiency,
      breakthrough, direction, mastery, activation, and equipment facts.
- [x] Every visible golden study detail agrees with the verified decoded state.
- [x] Exact missing details and aggregate completion agree with the game UI or
      any difference is explained by documented source freshness.
- [x] Chinese and English searches resolve the agreed representative skills.
- [x] Catalogue version and save freshness warnings behave correctly after a
      controlled source or save change.
- [x] Rebuild and recovery affect only helper-owned catalogue files.
- [x] Epic 1 recommendations still work when the catalogue is current, missing,
      stale, and rebuilding.
- [x] All Epic 2 milestone acceptance criteria are checked against evidence.
- [x] Remaining unsupported semantics become explicit future backlog items and
      are not silently accepted as complete.
- [ ] The product owner records the Epic 2 completion decision.

#### Evidence when complete

- `docs/reviews/E2-017-manual-verification.md`.
- Final automated test summary.
- Updated status and completion decision in
  [EPIC-002](./EPIC.md).

#### Verification evidence

- [E2-017 manual verification](../../reviews/E2-017-manual-verification.md)
  matches both original in-game captures by SHA-256 and compares every visible
  list/detail observation with the versioned decoder and helper UI.
- The current-save vertical check passed with 946 joined definitions and
  unchanged source fingerprints. After product-owner UI revisions including
  the installed faction-profile picker, the default solution suite passed 645
  tests: 640 passed, 0 failed, and 5 documented opt-in checks skipped.
- Installed-catalogue verification passed 641 tests with 4 save-dependent
  skips and verified deterministic faction element/alignment profiles without
  changing any inspected source.
- Source/save freshness, transactional rebuild/recovery, recommendation
  independence, and all Epic 2 milestone criteria are mapped to automated or
  recorded evidence in the review.
- The completion decision remains pending product-owner approval. Unsupported
  attainment, percentage, and runtime-power semantics are captured by E2-F06.

## Deferred backlog

The following ideas are related but are not required for Epic 2 completion:

### E2-F01 — Add life-skill catalogue support

Extend the static catalogue and character overlay to non-combat life skills
only after their data shape and progress semantics receive separate evidence.

### E2-F02 — Compare multiple skill definitions side by side

Add a comparison surface for costs, requirements, directions, progress, and
verified effects without turning raw text differences into mechanical claims.

### E2-F03 — Persist historical character skill progress

Store helper-owned, hash-keyed progress history only after retention, deletion,
freshness, and privacy behavior is separately approved. Current save data must
remain authoritative.

### E2-F04 — Add verified acquisition guidance

Explain how an unlearned skill can be obtained only after acquisition sources
and prerequisites are represented by typed, versioned evidence.

### E2-F05 — Normalize additional effect mechanics

Promote selected raw direct or reverse descriptions into verified typed rules
one mechanic at a time, with recommendation integration reviewed separately.

### E2-F06 — Verify attainment, displayed percentage, and runtime power

Identify the version-specific source for the visible `已大成` attainment label,
the study-screen centre percentage, and calculated runtime power/maximum power.
Keep these independent from completed breakthrough, page reading, page
activation, and martial-art simplification. Do not expose save-derived values
until a live-safe or persisted source is verified with controlled evidence.
