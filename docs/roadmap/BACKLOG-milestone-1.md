# Milestone 1 backlog: Combat-skill recommendation

This backlog implements
[EPIC-001](./EPIC-001-combat-skill-recommendation.md) and its
[combat-recommendation UI specification](./UI-001-combat-recommendation-layout.md).

## Conventions

### Priority

- **P0:** Required for the first usable vertical slice.
- **P1:** Required for Milestone 1 completion.
- **P2:** Valuable follow-up that may move to a later milestone.

### Estimate

- **S:** Small, normally one focused change.
- **M:** Medium, several related classes and tests.
- **L:** Large, should be split during implementation.

### Definition of done

Every completed backlog item must:

- Preserve Clean Architecture dependency direction.
- Include xUnit v3 tests at the appropriate layer.
- Leave every save, game file, game configuration value, running game process,
  runtime memory location, and in-game state unchanged.
- Introduce no method, endpoint, port, adapter, workflow, hook, injection,
  patch, automation, or future extension point capable of modifying the game.
- Keep all helper-owned output outside game-owned storage.
- Do not commit proprietary game binaries.
- Mark unverified or unavailable game data explicitly.
- Update API or architecture documentation when contracts change.

The game non-interference requirements are absolute product invariants. A
backlog item that conflicts with them must be rejected, not postponed or
reclassified.

## Delivery order

| Order | Slice | Outcome |
|---:|---|---|
| 0 | Non-interference boundary | The architecture cannot alter or control the game |
| 1 | Golden scenario | A fixed target and objective define a verifiable result |
| 2 | Typed snapshot | Recommendation code no longer parses diagnostic strings |
| 3 | Feasibility | Impossible loadouts are rejected |
| 4 | Threats and counters | Target mechanics map to verified responses |
| 5 | Recommendation | Feasible loadouts are generated and ranked |
| 6 | API and explanation | The player receives actionable instructions |
| 7 | Automated verification | Domain rules and local reads are verified |
| 8 | Presentation | A local pre-fight briefing presents the result |
| 9 | Manual verification | The player verifies the advice in-game |

## Permanent safety boundary

### M1-000 — Enforce absolute game non-interference

**Status:** Completed

**Priority:** P0  
**Estimate:** M  
**Dependencies:** None

Encode the permanent boundary that TaiWu Helper is an information-only
recommendation system. It can read permitted source data and create immutable,
helper-owned snapshots, but it can never change or control the game.

#### Acceptance criteria

- [x] The architecture decision record lists permitted read operations and
      forbidden mutation or control operations.
- [x] Domain and Application ports expose queries only; no game-data command
      abstraction exists.
- [x] Infrastructure does not expose mutation-capable `GameData` objects to
      other layers.
- [x] Source files are opened read-only wherever access mode is controlled by
      the helper.
- [x] No code writes into the game installation or save directories.
- [x] No code injects, hooks, patches, attaches to, automates input for, or
      writes memory in the running game process.
- [x] API responses contain recommendations only and cannot execute them.
- [x] Architecture and unit tests fail if a game mutation contract or
      command-style endpoint is introduced.

#### Evidence

- [ADR-0001: Absolute game non-interference](../architecture/ADR-0001-absolute-game-non-interference.md).
- `IReadOnlyGameDataSource` marks query-only game-data ports.
- Save reads capture read-only SHA-256 fingerprints before and after loading.
- `TaiWu.Architecture.Tests` enforces the dependency, API, file-access, and
  process-control boundaries.
- `dotnet test TaiWu.slnx --no-restore`: 11 tests passed.

## Slice 1: Golden scenario

### M1-001 — Select the golden target and objective

**Status:** Complete

**Priority:** P0  
**Estimate:** S  
**Dependencies:** M1-000

Choose one target with a clearly identifiable core combat mechanic and define
one initial victory objective, such as safe survival followed by a reliable
defeat.

#### Acceptance criteria

- [x] Target character ID and display name are recorded.
- [x] The target can be found in the configured save.
- [x] The player's preferred weapon and victory objective are recorded.
- [x] The target's expected critical mechanic is manually documented.
- [x] A manually verified baseline loadout is available for comparison.

#### Current evidence

- [Golden-target candidate assessment](../scenarios/M1-001-golden-target-selection.md).
- The player confirmed character `16317` as the 52-year-old 樂器奇書.
- The confirmed setup uses 刀 with a `Safe` objective: survive 失神 and
  心韻激盪, control 正練魔音, then defeat the target reliably.
- The current snapshot has no equipped target skills, so that limitation is
  retained for later snapshot and threat-analysis work.
- The configured save hash remained unchanged during all inspections.
- The local-only `M1-001-current-player-loadout.png` evidence
  is preserved with its capacities, skill costs, and practice directions
  transcribed in the scenario document.
- The empty `6/2/2/2/2` capacities and an individual 內功 capacity tooltip are
  preserved separately, so later calculations distinguish base capacity from
  capacity granted by selected 內功 and allocated 萬用 slots.

### M1-002 — Capture the golden save and evidence metadata

**Status:** Complete

**Priority:** P0  
**Estimate:** S  
**Dependencies:** M1-000, M1-001

Record non-proprietary metadata needed to reproduce the scenario without
committing the save or game binaries.

#### Acceptance criteria

- [x] Save hash, modified time, and snapshot time are recorded.
- [x] Installed `GameData` version is recorded.
- [x] No save or proprietary binary is added to Git.
- [x] Any current-screen observations are documented separately and affect
      analysis only.

#### Evidence

- [Golden-save evidence metadata](../scenarios/M1-002-golden-save-evidence.md).
- [Machine-readable metadata record](../scenarios/evidence/M1-002-golden-save-metadata.json).
- The save SHA-256 was identical before and after metadata capture.
- `.gitignore` excludes `.sav` files and known proprietary game-runtime
  artifacts.
- `TaiWu.Architecture.Tests` rejects proprietary save and GameData runtime
  files in the repository source tree.

## Slice 2: Typed combat snapshot

### M1-003 — Define combat snapshot Domain models

**Status:** Complete

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-001

Create immutable Domain models for snapshot metadata, player, target, learned
skills, equipment, slot budgets, generic allocation, practice direction, and
legendary-book modifiers.

#### Acceptance criteria

- [x] Models contain no `GameData` types.
- [x] Practice direction is a Domain value rather than an unexplained integer.
- [x] Slot categories are represented explicitly.
- [x] Unavailable values have an explicit representation.
- [x] Domain invariants are unit tested.

#### Evidence

- [Combat snapshot Domain model](../architecture/COMBAT-SNAPSHOT-DOMAIN.md).
- `TaiWu.Domain.CombatSnapshots` contains immutable metadata, player, target,
  learned-skill, equipment, slot-budget, generic-allocation, direction, and
  legendary-book modifier models.
- `SnapshotValue<T>` represents available and unavailable values explicitly.
- `TaiWu.Domain.UnitTests` uses xUnit v3 to verify construction invariants and
  collection immutability.
- `dotnet test TaiWu.slnx --no-restore`: 30 tests passed.

### M1-004 — Add structured snapshot reader port

**Status:** Complete

**Priority:** P0  
**Estimate:** S  
**Dependencies:** M1-003

Define an Application port that returns a `CombatSnapshot` independently of the
legacy line report.

#### Acceptance criteria

- [x] Application does not reference `GameData`.
- [x] The port accepts save path and target character ID.
- [x] The port exposes query operations only.
- [x] Cancellation is supported.
- [x] Snapshot warnings and source metadata are returned.

#### Evidence

- `CombatSnapshotReadRequest` requires a non-blank save path and positive
  target character ID.
- `ICombatSnapshotReader.ReadAsync` returns the immutable `CombatSnapshot`,
  which includes `CombatSnapshotMetadata` and snapshot warnings.
- The port inherits `IReadOnlyGameDataSource` and accepts a
  `CancellationToken`.
- Application and architecture xUnit v3 tests verify the contract and
  query-only boundary.
- `dotnet test TaiWu.slnx --no-restore`: 36 tests passed.

### M1-005 — Implement the GameData snapshot adapter

**Status:** Complete

**Priority:** P0  
**Estimate:** L  
**Dependencies:** M1-004

Map the already loaded save and configuration data into the typed snapshot.
Retain the existing line report as a separate diagnostic capability.

#### Acceptance criteria

- [x] Player identity and learned skills are mapped.
- [x] Current equipped skills are mapped by category.
- [x] Actual `GridCost`, mastery, direction, and grid bonuses are mapped.
- [x] Target features, equipped skills, relevant learned skills, and equipment
      are mapped.
- [x] Saves and all other game-owned files remain byte-for-byte unchanged.
- [x] No mutation-capable `GameData` object or operation crosses the adapter
      boundary.
- [x] Repeated reads do not duplicate GameData handlers.
- [x] Unsupported runtime calculations are not invoked.

#### Evidence

- `TaiwuCombatSnapshotReader` returns only immutable
  `TaiWu.Domain.CombatSnapshots` types through `ICombatSnapshotReader`.
- Player `21396` and target `16317` were mapped from the golden save with 411
  player skills, 50 relevant target skills, and 12 target features.
- The target's absent disk loadout remains an explicit unavailable value and
  warning; it is not replaced by the newer screenshot until M1-006.
- `TaiwuArchiveReadSession` shares one lock and one-time configuration
  initialization between the structured and diagnostic readers, and clears
  monitored handlers before every load.
- Two consecutive structured reads completed in one process. The save SHA-256
  remained
  `B9E86B80B564035CBE7D15F2C5F297AF3ACDE5470509B0550D930ED91DDF1930`
  before, during, and after both reads.
- The adapter does not call `Character.GetCombatSkillGridCost` or
  `SpecialEffectDomain.ModifyData`. Configured cost and mastery remain
  separate; effective used capacity is explicitly unavailable where the
  standalone runtime cannot safely establish it.
- Architecture tests reject save-write/game-control APIs and any public
  `GameData` type exposure.
- `dotnet test TaiWu.slnx --no-restore`: 59 tests passed.

### M1-006 — Add snapshot freshness and observation handling

**Status:** Complete

**Priority:** P1  
**Estimate:** M  
**Dependencies:** M1-005

Allow a request to report current-screen loadout information when it is newer
than the disk save. This information changes the helper's analysis snapshot
only; it is never written to the game or save.

#### Acceptance criteria

- [x] Response includes save hash and read time.
- [x] Request can report observed equipped skills and generic-grid allocation.
- [x] Observations are validated before use.
- [x] Response identifies each field sourced from an observation.
- [x] Observations exist only in helper-owned request/snapshot data.
- [x] No disk data or live game state is changed.

#### Evidence

- `PlayerLoadoutObservation` is an immutable Domain value accepted optionally
  by `CombatSnapshotReadRequest`.
- Observations carry a UTC observation time, evidence reference, categorized
  equipped skills, generic allocation, and optional displayed slot budgets.
- `CombatSnapshotObservationMerger.Merge` verifies that observed skills are
  learned and assigned to their configured categories before returning a new
  snapshot.
- An observation at or before the save modified time is not applied and emits
  `CURRENT_SCREEN_OBSERVATION_NOT_NEWER`.
- `SnapshotFieldSource` identifies each replaced field as
  `CurrentScreenObservation` with its evidence and capture time.
- Save hash, save modified time, and snapshot read time remain unchanged
  metadata from the disk read; observation provenance cannot masquerade as save
  data.
- A real two-read smoke test returned zero observation sources for the disk
  snapshot and two for the merged snapshot. The save SHA-256 remained
  `B9E86B80B564035CBE7D15F2C5F297AF3ACDE5470509B0550D930ED91DDF1930`
  before and after.
- Architecture tests verify that the observation contract is an immutable
  helper value and exposes only the pure `Merge` operation.
- `dotnet test TaiWu.slnx --no-restore`: 68 tests passed.

## Slice 3: Feasibility

### M1-007 — Implement effective skill-cost calculation

**Status:** Complete

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-003

Calculate effective cost from actual `GridCost`, confirmed mastery, and
confirmed legendary-book modifiers.

#### Acceptance criteria

- [x] Actual `GridCost` is the base value.
- [x] Mastery reduction applies only when confirmed.
- [x] Cost never falls below the verified minimum.
- [x] Legendary-book reductions require explicit snapshot evidence.
- [x] Boundary and combination cases are unit tested.

#### Evidence

- `CombatSkillCostCalculator` is a pure Domain service returning a
  `CombatSkillCostBreakdown`; Infrastructure no longer owns a partial mastery
  cost calculation.
- Configured `GridCost` is retained as the base. Confirmed mastery reduces it
  by one, with a minimum effective cost of one.
- The supplied `內功·浮心無字訣` screenshots establish that `收置` fixes a
  placed skill's occupied cost at one. The implementation models that fixed
  cost instead of inventing an additive reduction.
- Independent `身法·白衣行化笈` evidence confirms the same `收置` fixed-cost
  rule for an agility skill, and a cross-category unit test covers it.
- A third assistance-book screenshot confirms the same rule for `玲瓏九竅`.
  Displayed `生效功法` values are treated as current replaceable selections,
  not permanent effect-to-skill bindings.
- The user confirmed that the four supplied books are the complete currently
  owned set. `刀法·十余魔羅錄` supplies a fourth `收置` confirmation and also
  shows an empty assignment, which produces no current cost modifier.
- Effects from unowned books remain unverified and unavailable; the helper
  neither invents them nor blocks current-player recommendations waiting for
  unobtainable screenshots.
- `LegendaryBookModifier.ForSkill` creates a new immutable helper-side value
  for proposed assignments. It does not alter the current snapshot or game.
- Every legendary-book fixed-cost modifier records its source and evidence
  reference. A skill cannot receive multiple fixed-cost modifiers.
- `用極`, `專解`, and `絕旨` are excluded because they change power or
  requirements. `大盈` and `大成` are reserved for M1-008 because they change
  category/generic slot contributions, not occupied cost.
- Missing `GridCost` or unconfirmed mastery leaves effective cost unavailable;
  an applicable derived `收置` reduction is unavailable for the same reason.
- Boundary tests cover the minimum cost, mastery, fixed-cost composition,
  unrelated skills, category mismatch, duplicate modifiers, missing inputs,
  snapshot lookup, and unlearned skills.
- Screenshot evidence and SHA-256 hashes are recorded in
  `docs/scenarios/M1-007-effective-skill-cost-evidence.md`.
- The calculation reads and writes no save, game file, process, input, or live
  game state.
- `dotnet test TaiWu.slnx --no-restore`: 84 tests passed.

### M1-008 — Implement slot-budget calculation

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-003, M1-007

Calculate category capacity from base slots, neigong-specific bonuses, and
generic allocation.

#### Acceptance criteria

- [ ] Neigong, attack, agility, defense, and assistance are separate budgets.
- [ ] Specific bonuses affect only their category.
- [ ] Generic slots cannot be allocated more than once.
- [ ] Used and remaining capacity are returned.
- [ ] Invalid allocations produce Domain validation errors.

### M1-009 — Validate ownership, mastery, and practice direction

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-003

Reject candidates that use unknown skills or unavailable direct/reverse
effects.

#### Acceptance criteria

- [ ] Every selected skill exists in the player's learned-skill snapshot.
- [ ] Direct, reverse, and neutral directions are distinct.
- [ ] Neutral direction cannot activate a direction-specific effect.
- [ ] The reason for each rejection is returned.
- [ ] All direction cases are unit tested.

### M1-010 — Model activation and combat requirements

**Priority:** P0  
**Estimate:** L  
**Dependencies:** M1-003

Represent weapon, trick, range, Neili, stance, breath, unlock, equipped
passive, active defense, and active agility conditions.

#### Acceptance criteria

- [ ] Requirements are typed and evidence-backed.
- [ ] Unsatisfied hard requirements reject the candidate.
- [ ] Conditional requirements are included in recommendation warnings.
- [ ] Multiple defense or agility effects are not modeled as simultaneously
      active.
- [ ] At least the golden target's relevant requirements are supported.

### M1-011 — Implement loadout feasibility validator

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-007, M1-008, M1-009, M1-010

Create a pure Domain service that validates a complete proposed loadout.

#### Acceptance criteria

- [ ] Validator has no Infrastructure dependency.
- [ ] It returns all validation failures, not only the first.
- [ ] Slot totals, generic allocation, ownership, direction, and requirements
      are checked.
- [ ] Invalid loadouts cannot enter the scoring stage.
- [ ] Unit tests include over-budget and mutually incompatible loadouts.

## Slice 4: Threats and counters

### M1-012 — Create a versioned combat-effect catalog

**Priority:** P0  
**Estimate:** L  
**Dependencies:** M1-002, M1-005

Read relevant effect IDs and local configuration text, then map the golden
scenario's effects into typed mechanics.

#### Acceptance criteria

- [ ] Catalog records the matching GameData version.
- [ ] Direct and reverse effects remain distinct.
- [ ] Raw effect ID and source text are preserved as evidence.
- [ ] Unrecognized effects remain visible and are not guessed.
- [ ] No proprietary configuration data is committed wholesale.

### M1-013 — Define target-threat taxonomy

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-012

Define typed threats needed by the golden target, including severity and
evidence.

#### Acceptance criteria

- [ ] Threat types are Domain concepts.
- [ ] Severity has a documented scale.
- [ ] Every threat contains source evidence.
- [ ] The taxonomy supports the golden target's critical mechanic.
- [ ] Unknown mechanics generate warnings.

### M1-014 — Implement target threat analyzer

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-005, M1-013

Analyze the target snapshot and return ordered threats.

#### Acceptance criteria

- [ ] Equipped target skills are analyzed before unequipped learned skills.
- [ ] Combat-start and always-equipped effects are distinguished from active
      effects.
- [ ] Critical threat ranking is deterministic.
- [ ] Golden-target output matches manual analysis.

### M1-015 — Define counter rules for the golden target

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-012, M1-013

Create verified mappings between the golden target's threats and player
skills, equipment, or tactical responses.

#### Acceptance criteria

- [ ] Every counter cites an effect, configuration entry, or verified rule.
- [ ] Required direction and activation timing are represented.
- [ ] Hard counters are distinguished from mitigation.
- [ ] Missing player access to a counter is reported.
- [ ] Counter rules are unit tested.

## Slice 5: Recommendation

### M1-016 — Generate feasible candidate loadouts

**Priority:** P0  
**Estimate:** L  
**Dependencies:** M1-011, M1-015

Generate candidate loadouts using hard filters before exploring combinations.

#### Acceptance criteria

- [ ] Every emitted candidate passes the feasibility validator.
- [ ] Required combat-start counters are considered first.
- [ ] Search is bounded and deterministic.
- [ ] Existing equipped skills are retained when equally suitable.
- [ ] Candidate-generation diagnostics can explain exclusions.

### M1-017 — Implement recommendation scoring

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-014, M1-016

Score candidates by threat coverage, survival, reliability, compatibility,
damage, opportunity cost, and conditional risk.

#### Acceptance criteria

- [ ] Score components are individually visible.
- [ ] Hard constraints are not represented merely as score penalties.
- [ ] Stable tie-breaking produces deterministic results.
- [ ] Safe, balanced, and aggressive policies use documented weight sets.
- [ ] Golden-target ranking is manually reviewed.

### M1-018 — Produce suggested manual loadout changes and battle plan

**Priority:** P1  
**Estimate:** M  
**Dependencies:** M1-016, M1-017

Compare the selected candidate with the current loadout and produce
informational instructions for the player to carry out manually.

#### Acceptance criteria

- [ ] Manual add, remove, retain, and change-direction suggestions are returned.
- [ ] Primary and alternative defense/agility choices are identified.
- [ ] Opening actions and switching conditions are included.
- [ ] Every instruction references its recommendation reason.

### M1-019 — Add evidence-backed recommendation explanations

**Priority:** P1  
**Estimate:** M  
**Dependencies:** M1-014, M1-017, M1-018

Create structured explanations suitable for both API clients and later natural
language presentation.

#### Acceptance criteria

- [ ] Every selected skill has at least one reason.
- [ ] Threat, counter, direction, cost, and conditions are linked.
- [ ] Assumptions and unavailable data are explicit.
- [ ] Explanations do not depend on an LLM.

## Slice 6: Application and API

### M1-020 — Implement `RecommendCombatLoadout` use case

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-005, M1-014, M1-016, M1-017

Orchestrate snapshot creation, threat analysis, candidate generation,
validation, scoring, and explanation.

#### Acceptance criteria

- [ ] Application depends on ports and Domain services only.
- [ ] Cancellation is propagated.
- [ ] Snapshot warnings are preserved.
- [ ] NSubstitute tests verify orchestration and failure paths.

### M1-021 — Add combat-recommendation endpoint

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-020

Add `POST /api/combat-recommendations`.

#### Acceptance criteria

- [ ] Target character ID is required.
- [ ] Objective supports safe, balanced, and aggressive.
- [ ] Configured save path is used by default.
- [ ] Current-screen observations are optional and affect analysis only.
- [ ] Validation errors return appropriate problem responses.
- [ ] Response is typed JSON rather than line-oriented text.
- [ ] Available styles are returned from the same immutable snapshot.
- [ ] Threats, skill reasons, manual changes, and battle-plan steps have stable
      references.
- [ ] The endpoint cannot execute a recommendation or mutate game state.

### M1-022 — Add target lookup endpoint

**Priority:** P1  
**Estimate:** M  
**Dependencies:** M1-005

Allow clients to find valid target IDs by name and snapshot context.

#### Acceptance criteria

- [ ] Search does not require parsing diagnostic lines.
- [ ] Results include ID, name, location, and enough context to disambiguate.
- [ ] Missing and ambiguous targets are handled explicitly.

## Slice 7: Automated verification

### M1-023 — Add Domain rule test suite

**Priority:** P0  
**Estimate:** L  
**Dependencies:** M1-007 through M1-019

Create comprehensive unit tests for costs, budgets, direction, compatibility,
threats, candidate validation, and scoring.

#### Acceptance criteria

- [ ] Every hard constraint has positive and negative tests.
- [ ] Boundary conditions cover exact capacity and one-over-capacity.
- [ ] Determinism is tested.
- [ ] No test requires the installed game unless explicitly categorized as an
      integration test.

### M1-024 — Add opt-in local GameData integration tests

**Priority:** P1  
**Estimate:** M  
**Dependencies:** M1-005, M1-020

Verify the adapter against the locally installed game and configured save.

#### Acceptance criteria

- [ ] Tests skip clearly when local prerequisites are absent.
- [ ] Hashes of all game-owned files touched by the read path are unchanged
      before and after.
- [ ] The helper opens source files read-only wherever it controls access.
- [ ] Two consecutive reads succeed in one process.
- [ ] Snapshot contains the expected golden player and target.
- [ ] Proprietary data is not stored in test artifacts.

## Slice 8: Presentation

### M1-026 — Define recommendation presentation view models

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-019, M1-021

Map the structured recommendation response into presentation-specific view
models used by the local UI.

#### Acceptance criteria

- [ ] Safe, balanced, and aggressive recommendations are returned from the
      same immutable snapshot.
- [ ] The requested style identifies the initially selected recommendation.
- [ ] Threats, skill reasons, manual changes, and battle-plan steps have stable
      references.
- [ ] Direction, actual and effective cost, capacity, generic allocation,
      timing, conditions, evidence, and warnings are represented explicitly.
- [ ] Presentation view models contain no `GameData` types.
- [ ] No response or view-model operation can execute a recommendation.
- [ ] Contract and mapping behavior is covered by xUnit v3 tests.

### M1-027 — Add the local Blazor shell and recommendation controls

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-021, M1-022, M1-026

Host a Blazor Interactive Server page in the existing ASP.NET Core application
and implement the recommendation input workflow.

#### Acceptance criteria

- [ ] The UI runs in the existing local .NET 10 process.
- [ ] The player can search for and select a target.
- [ ] The player can choose a preferred style and optional weapon.
- [ ] Current-screen observations are clearly identified as analysis input
      only.
- [ ] Snapshot read time, freshness, and game version are visible.
- [ ] A persistent `Information only` badge is visible.
- [ ] Request cancellation and repeated requests are handled safely.
- [ ] No separate Node-based frontend toolchain is required.

### M1-028 — Build the threat and recommended-loadout layout

**Priority:** P0  
**Estimate:** L  
**Dependencies:** M1-026, M1-027

Implement the primary two-column pre-fight briefing described by
[UI-001](./UI-001-combat-recommendation-layout.md).

#### Acceptance criteria

- [ ] Critical and moderate target threats are ordered by severity.
- [ ] Selecting a threat highlights its countering skills and plan steps.
- [ ] Skills are grouped as 內功, 摧破, 輕靈, 護體, and 奇竅.
- [ ] Each category shows used capacity, available capacity, and generic-slot
      allocation.
- [ ] Every skill card shows its Chinese in-game name, direction, effective
      cost, manual-change status, reason, activation timing, and requirements.
- [ ] Safe, balanced, and aggressive tabs switch between results from the same
      snapshot.
- [ ] Known-constraint validation is not presented as a win probability.

### M1-029 — Add the manual setup checklist and battle plan

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-018, M1-028

Present the proposed loadout differences as manual player instructions and
show the evidence-backed combat sequence.

#### Acceptance criteria

- [ ] The checklist includes manual add, remove, retain, direction, generic
      allocation, weapon, and Neili steps where relevant.
- [ ] Checklist completion changes helper UI state only.
- [ ] The battle plan covers before-combat, opening, normal execution,
      trigger-based reactions, and switching conditions when available.
- [ ] Every checklist and plan item links to its reason or evidence.
- [ ] The section states that TaiWu Helper cannot perform the instructions.
- [ ] Copy and print operations contain recommendations only.

### M1-030 — Add warnings, alternatives, assumptions, and evidence

**Priority:** P1  
**Estimate:** M  
**Dependencies:** M1-019, M1-028

Make uncertainty and supporting detail available without overwhelming the
primary recommendation.

#### Acceptance criteria

- [ ] Critical warnings appear above the loadout and are not collapsed.
- [ ] Stale data, observation differences, unavailable values, unverified
      mechanics, and conditional requirements are distinguishable.
- [ ] Alternatives, assumptions, score contributions, and detailed evidence
      use accessible supporting panels.
- [ ] Each warning explains its effect on the recommendation.
- [ ] Unknown values are never silently replaced with estimates.

### M1-031 — Implement responsive, accessible, and failure states

**Priority:** P1  
**Estimate:** M  
**Dependencies:** M1-027, M1-028, M1-029, M1-030

Complete the page states and ensure the briefing remains usable at common
desktop and narrow-window sizes.

#### Acceptance criteria

- [ ] Initial, loading, success, success-with-warning, empty, ambiguous-target,
      invalid-configuration, unsupported-version, and failure states exist.
- [ ] Threat and loadout panels are side by side at 1280 pixels and stack
      below that width.
- [ ] All interactive elements support keyboard navigation and visible focus.
- [ ] Severity, direction, and status are not communicated by colour alone.
- [ ] Conditions and evidence do not require hover.
- [ ] Error recovery never offers to repair or modify game data.

### M1-032 — Add Presentation test coverage

**Priority:** P1  
**Estimate:** M  
**Dependencies:** M1-027, M1-028, M1-029, M1-030, M1-031

Test presentation mapping, state, important rendered information, and failure
handling with xUnit v3 and NSubstitute where a use case substitute is needed.

#### Acceptance criteria

- [ ] Tests cover all three recommendation styles.
- [ ] Tests cover capacity, direction, cost, timing, conditions, and manual
      change rendering.
- [ ] Tests cover loading, warning, empty, ambiguous, and failure states.
- [ ] Tests verify that checklist interaction remains helper-local.
- [ ] Tests verify the persistent information-only message.
- [ ] Tests do not require the installed game.

### M1-033 — Perform the presentation non-interference review

**Priority:** P0  
**Estimate:** S  
**Dependencies:** M1-000, M1-027, M1-028, M1-029, M1-030, M1-031, M1-032

Audit the completed UI against the permanent game non-interference boundary.

#### Acceptance criteria

- [ ] No `Apply`, `Equip`, `Execute`, repair, patch, or game-control action
      exists.
- [ ] No UI event calls a game-data mutation or process-control operation.
- [ ] Copy, print, and export operations write only helper-owned
      recommendation content outside game-owned storage.
- [ ] Refresh performs a read-only snapshot request.
- [ ] Manual instructions cannot be confused with automated actions.
- [ ] The review result is recorded before manual in-game verification.

## Slice 9: Manual verification and release

### M1-025 — Verify the recommendation in-game

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-023, M1-024, M1-033

The player equips the returned loadout manually and compares its feasibility
and tactical claims with the game. The helper does not attach to or control the
game during verification.

#### Acceptance criteria

- [ ] Returned slot totals match the game UI.
- [ ] Every returned skill and direction can be equipped.
- [ ] Required weapon and execution conditions are accurate.
- [ ] The battle plan addresses the documented critical threat.
- [ ] Differences are recorded as rule corrections, not silently ignored.
- [ ] Every save, game-owned file, and observed runtime state remains unchanged
      by the helper.

## Later backlog

### M2-001 — Persist recommendation history and outcomes

**Priority:** P2  
**Estimate:** M  
**Dependencies:** Milestone 1

Use SQLite to store recommendation metadata, user feedback, and battle outcomes.
The database is helper-owned, resides outside game-owned storage, and is never
used to write anything back to the game. Do not store proprietary game content
or complete saves.

### M2-002 — Add rule and snapshot caching

**Priority:** P2  
**Estimate:** M  
**Dependencies:** Milestone 1

Use SQLite only if profiling shows that versioned rule or snapshot caching
materially improves the playing workflow. Cached data is helper-owned and can
never become a game-data write path.

### M2-003 — Add natural-language presentation

**Priority:** P2  
**Estimate:** M  
**Dependencies:** M1-019

Optionally use an LLM to phrase already structured evidence and instructions.
The deterministic result remains authoritative.

### M2-004 — Expand target coverage

**Priority:** P2  
**Estimate:** L  
**Dependencies:** M1-025

Add verified threat and counter rules incrementally by mechanic and target
archetype.
