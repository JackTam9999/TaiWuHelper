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
- Two consecutive structured reads completed in one process. The locally
  checked save fingerprint matched before, during, and after both reads; the
  fingerprint itself is not committed.
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
  equipped skills, generic allocation, optional displayed slot budgets, and
  optional legendary-book slots plus current assignments.
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
  snapshot and two for the merged snapshot. The locally checked save
  fingerprint matched before and after and is not committed.
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
confirmed legendary-book assignments.

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
- `LegendaryBookCostSlot` separates verified ownership and effect identity from
  `LegendaryBookCostAssignment`; empty owned slots remain available for
  proposals without changing current cost.
- `ProposeForSkill` creates a new helper-only `Proposed` assignment with its
  own evidence reference. It does not alter the current snapshot or game.
- Every rule and assignment records its provenance. A slot can have only one
  current assignment, and a skill cannot receive multiple fixed-cost
  assignments.
- `用極`, `專解`, and `絕旨` are excluded because they change power or
  requirements. `大盈` and `大成` are reserved for M1-008 because they change
  category/generic slot contributions, not occupied cost.
- Missing `GridCost` or unconfirmed mastery leaves the ordinary effective cost
  unavailable. A verified `收置` assignment still has exact effective cost
  one, while its derived reduction remains unavailable.
- Boundary tests cover the minimum cost, mastery, fixed-cost composition,
  unassigned slots, category mismatch, duplicate assignments, missing inputs,
  snapshot lookup, and unlearned skills.
- Screenshot evidence and SHA-256 hashes are recorded in
  `docs/scenarios/M1-007-effective-skill-cost-evidence.md`.
- The calculation reads and writes no save, game file, process, input, or live
  game state.
- `dotnet test --no-restore`: 88 tests passed.

### M1-008 — Implement slot-budget calculation

**Status:** Complete

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-003, M1-007

Calculate category capacity from base slots, neigong-specific bonuses, and
generic allocation.

#### Acceptance criteria

- [x] Neigong, attack, agility, defense, and assistance are separate budgets.
- [x] Specific bonuses affect only their category.
- [x] Generic slots cannot be allocated more than once.
- [x] Used and remaining capacity are returned.
- [x] Invalid allocations produce Domain validation errors.

#### Evidence

- `CombatSlotBudgetCalculator` is a pure Domain service over
  `PlayerCombatSnapshot`.
- The verified empty capacities are explicit: Neigong 6 and each outer
  category 2.
- Only equipped Neigong skills add their category-specific
  `SkillSlotContribution`; unequipped and non-Neigong contributions are
  ignored.
- `GenericSlotAllocation` adds only the slots assigned to each outer category
  and continues to reject negative or duplicate allocation.
- Used values sum `CombatSkillCostCalculator` results, including mastery and
  current evidence-backed `收置` fixed costs.
- Missing effective-cost evidence preserves unavailable used and remaining
  values instead of guessing.
- Unknown skills, category mismatches, negative derived capacity, and
  over-budget loadouts are rejected.
- Ten focused xUnit v3 tests cover capacity composition, category isolation,
  generic allocation, 收置 composition, unavailable costs, and invalid inputs.
- The service reads and writes no save, game file, process, input, or live game
  state.
- `dotnet test --no-restore`: 98 tests passed.

### M1-009 — Validate ownership, mastery, and practice direction

**Status:** Complete

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-003

Reject candidates that use unknown skills or unavailable direct/reverse
effects.

#### Acceptance criteria

- [x] Every selected skill exists in the player's learned-skill snapshot.
- [x] Direct, reverse, and neutral directions are distinct.
- [x] Neutral direction cannot activate a direction-specific effect.
- [x] The reason for each rejection is returned.
- [x] All direction cases are unit tested.

#### Evidence

- `CombatSkillCandidate` records a learned-skill ID plus optional mastery and
  direction-specific-effect requirements. It may explicitly permit a manual
  direction-change proposal; strict current-direction validation remains the
  default.
- `CombatSkillCandidateValidator` returns a
  `CombatSkillCandidateValidationResult`; expected rejection never uses an
  exception as control flow.
- Rejections have stable codes and non-blank reasons for unknown ownership,
  unavailable or missing mastery, unavailable or mismatched direction, Neutral
  direction, and unavailable Direct/Reverse effects.
- Direction-independent candidates may use Neutral skills, while Neutral can
  never satisfy a Direct, Reverse, or purported Neutral directional-effect
  requirement in the current snapshot. An explicit proposed direction change
  may target Direct or Reverse only and still requires that exact effect to be
  available.
- Accepted proposed changes expose `RequiredDirectionChange`; the validator
  never performs that change.
- All independently detectable mastery, direction, and effect failures are
  returned together.
- Fourteen focused xUnit v3 tests cover accepted Direct, accepted Reverse,
  direction-independent Neutral, all rejection states, multiple simultaneous
  reasons, and invalid candidate construction.
- The validator reads and writes no save, game file, process, input, or live
  game state.
- `dotnet test --no-restore`: 112 tests passed.

### M1-010 — Model activation and combat requirements

**Status:** Complete

**Priority:** P0  
**Estimate:** L  
**Dependencies:** M1-003

Represent weapon, trick, range, Neili, stance, breath, unlock, equipped
passive, active defense, and active agility conditions.

#### Acceptance criteria

- [x] Requirements are typed and evidence-backed.
- [x] Unsatisfied hard requirements reject the candidate.
- [x] Conditional requirements are included in recommendation warnings.
- [x] Multiple defense or agility effects are not modeled as simultaneously
      active.
- [x] At least the golden target's relevant requirements are supported.

#### Evidence

- The Domain has explicit weapon, trick, range, Neili/stance/breath, weapon
  unlock, equipped-passive, active-defense, and active-agility requirement
  types.
- Every requirement records hard/conditional criticality and an evidence
  reference.
- `CombatRequirementContext` is immutable, requires active skills to be
  equipped, and permits only one active defense plus one active agility skill.
- `CombatRequirementEvaluator` reports `Satisfied`, `Unsatisfied`, or
  `Unknown` for every requirement.
- Hard unsatisfied/unknown results reject; conditional unsatisfied/unknown
  results remain visible as warnings.
- The golden anti-magic test covers equipped 老君, active 萬花, 鬼庖丁 weapon
  unlock and trick conditions, and 三部 range.
- Twelve focused xUnit v3 cases cover every requirement type, hard rejection,
  conditional warnings, unavailable facts, active-skill exclusivity, all
  failure collection, and invalid unevidenced construction.
- The model and evaluator have no save, game, process, input, or runtime-control
  dependency.
- `dotnet test --no-restore`: 124 tests passed.

### M1-011 — Implement loadout feasibility validator

**Status:** Complete

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-007, M1-008, M1-009, M1-010

Create a pure Domain service that validates a complete proposed loadout.

#### Acceptance criteria

- [x] Validator has no Infrastructure dependency.
- [x] It returns all validation failures, not only the first.
- [x] Slot totals, generic allocation, ownership, direction, and requirements
      are checked.
- [x] Invalid loadouts cannot enter the scoring stage.
- [x] Unit tests include over-budget and mutually incompatible loadouts.

#### Evidence

- `CombatLoadoutFeasibilityValidator` is a pure Domain service that composes
  candidate eligibility, combat requirements, derived generic-slot totals, and
  complete slot-budget calculation.
- `CombatLoadoutFeasibilityResult` retains every independently detectable
  failure with a stable code, explanation, and skill ID where applicable.
- Candidate coverage detects missing and extra candidate specifications;
  candidate validation covers ownership, mastery, direction, and
  direction-specific effect availability.
- Requirement-context equipped skills must exactly match the proposed
  loadout, and every hard requirement rejection is preserved.
- Generic-slot totals are re-derived from persistent slots plus the proposed
  Neigong contributions before capacity is calculated.
- `FeasibleCombatLoadout` has an internal constructor and is returned only
  when no hard failure exists, giving later scoring a validated-only input.
- Ten focused xUnit v3 tests cover valid output, over-budget and mutually
  incompatible loadouts, aggregated failures, candidate coverage, context
  mismatch, generic totals, unavailable costs, conditional warnings, and
  duplicate specifications.
- The validator reads and writes no save, game file, process, input, or live
  game state.
- `dotnet test --no-restore`: 134 tests passed.

## Slice 4: Threats and counters

### M1-012 — Create a versioned combat-effect catalog

**Status:** Complete

**Priority:** P0  
**Estimate:** L  
**Dependencies:** M1-002, M1-005

Read relevant effect IDs and local configuration text, then map the golden
scenario's effects into typed mechanics.

#### Acceptance criteria

- [x] Catalog records the matching GameData version.
- [x] Direct and reverse effects remain distinct.
- [x] Raw effect ID and source text are preserved as evidence.
- [x] Unrecognized effects remain visible and are not guessed.
- [x] No proprietary configuration data is committed wholesale.

#### Evidence

- `CombatEffectCatalog` requires an exact GameData version and immutable,
  unique skill/direction entries.
- `VerifiedCombatEffectCatalogs.GoldenAntiMagic` matches installed GameData
  product version
  `1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a`.
- Twelve deliberately selected entries cover the Direct and Reverse effects
  of 金猊镇魔刀, 老君拂尘功, 万花听雨式, 墨玉功, 伏龙刀法, and 鬼庖丁刀法.
- Each entry retains the raw effect ID, exact local source text, and individual
  source key alongside a small typed-mechanic set.
- Resolution reports `Unrecognized`, `VersionMismatch`, or
  `EffectIdMismatch` without substituting a likely meaning. An unmapped entry
  can retain source text while exposing no typed mechanics.
- Only the 12 golden-scenario records are committed; the local mapping and
  language files, GameData assemblies, save, and generated inspection output
  are excluded.
- The read-only inspection verified the save fingerprint was unchanged; the
  fingerprint itself is not committed.
- Ten focused xUnit v3 tests cover version binding, Direct/Reverse separation,
  raw evidence, unknown and unmapped effects, mismatches, uniqueness, and
  invalid observations.
- `dotnet test --no-restore`: 144 tests passed.

### M1-013 — Define target-threat taxonomy

**Status:** Complete

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-012

Define typed threats needed by the golden target, including severity and
evidence.

#### Acceptance criteria

- [x] Threat types are Domain concepts.
- [x] Severity has a documented scale.
- [x] Every threat contains source evidence.
- [x] The taxonomy supports the golden target's critical mechanic.
- [x] Unknown mechanics generate warnings.

#### Evidence

- `TargetThreat` represents stable code, typed kind, severity, title,
  explanation, activation timing, and immutable evidence entirely in Domain.
- The documented ascending severity scale is Informational, Moderate, High,
  and Critical; enum values preserve that ordering for later deterministic
  analysis.
- Construction rejects a threat with no source evidence. Evidence retains its
  reference, summary, confidence, and optional raw skill/effect identity.
- `TargetThreatTaxonomy.Normalize` preserves recognized threats, rejects
  duplicate stable codes, and converts every `UnknownTargetMechanic` into an
  `UNRECOGNIZED_TARGET_MECHANIC` warning without guessing a threat type.
- `VerifiedTargetThreatTaxonomies.GoldenMagicSound` represents the verified
  positive-practice mind-damage pressure, critical distraction-mark
  accumulation, and critical mind-resonance cascade.
- The observed 36-defeat-mark reset remains a warning rather than a recognized
  threat because the golden target's equipped reverse 九色玉蟬法 source effect
  is still unconfirmed.
- Nine focused xUnit v3 tests cover typed fields, severity ordering, mandatory
  evidence, unknown warnings, the golden critical chain and hypothesis,
  uniqueness, immutability, and invalid construction.
- `dotnet test --no-restore`: 153 tests passed.

### M1-014 — Implement target threat analyzer

**Status:** Complete

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-005, M1-013

Analyze the target snapshot and return ordered threats.

#### Acceptance criteria

- [x] Equipped target skills are analyzed before unequipped learned skills.
- [x] Combat-start and always-equipped effects are distinguished from active
      effects.
- [x] Critical threat ranking is deterministic.
- [x] Golden-target output matches manual analysis.

#### Evidence

- `TargetThreatAnalyzer` accepts only the immutable `CombatSnapshot` and a
  versioned `TargetThreatRuleSet`; it has no Infrastructure or GameData
  dependency.
- Candidate traversal records equipped sources first and then learned,
  unequipped sources in skill-ID order. Multiple sources for one threat remain
  visible in that same order.
- Activation timing remains typed as Always, CombatStart, OnSkillUse, OnHit,
  OnMarkApplied, Threshold, or Unknown, so passive/opening effects are not
  collapsed into active effects.
- Threats sort by descending severity, equipped-before-learned source scope,
  and stable ordinal threat code.
- GameData version, skill ID, practice direction, and raw effect ID must all
  match the verified rule signature. Missing or changed facts become warnings.
- The golden rules cover type-13 skills `718`–`733`. The current snapshot
  matches eight Direct-practice skills: `719`, `721`, `722`, `724`, `725`,
  `727`, `731`, and `733`.
- Golden output is deterministically `DISTRACTION_MARK_ACCUMULATION`,
  `MIND_RESONANCE_CASCADE`, then
  `POSITIVE_MAGIC_SOUND_MIND_DAMAGE`, matching the manual analysis.
- Because target `16317` has no equipped-skill list in the disk snapshot, all
  matched sources are explicitly `LearnedUnequipped` and a
  `TARGET_EQUIPPED_SKILLS_UNAVAILABLE` warning is returned. The unconfirmed
  36-mark reset warning is also retained.
- The fingerprint-checked inspection reported the save unchanged; no save,
  report output, path, or fingerprint is committed.
- Nine focused xUnit v3 tests cover source precedence, activation timing,
  deterministic ranking, missing loadout fallback, unknown and Neutral
  effects, version invalidation, golden output, and invalid rules.
- `dotnet test --no-restore`: 162 tests passed.

### M1-015 — Define counter rules for the golden target

**Status:** Complete

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-012, M1-013

Create verified mappings between the golden target's threats and player
skills, equipment, or tactical responses.

#### Acceptance criteria

- [x] Every counter cites an effect, configuration entry, or verified rule.
- [x] Required direction and activation timing are represented.
- [x] Hard counters are distinguished from mitigation.
- [x] Missing player access to a counter is reported.
- [x] Counter rules are unit tested.

#### Evidence

- `CombatCounterRule` links stable threat codes to one typed, recognized
  `CombatEffectCatalogEntry`; raw effect ID, text, source key, skill, and
  required direction remain available through that entry.
- Activation is explicitly CombatStartPassive, EquippedPassive, ActiveAttack,
  ActiveDefense, or ActiveAgility.
- `REVERSE_JINNI_SUPPRESSION` is the initial hard counter. Reverse 老君,
  reverse 萬花, direct 墨玉, and reverse 伏龍 are modeled as mitigations.
- Passive and active-agility rules carry evidence-backed
  `SkillActivationRequirement` instances. No unverified legendary-book or
  unowned effect is used.
- `CombatCounterAccessEvaluator` composes candidate eligibility and combat
  requirements, checks the observed raw effect ID, returns every rule, and
  separates accessible counters from missing access.
- Missing ownership, unavailable or wrong direction, unavailable or changed
  effect identity, and unmet hard activation requirements remain explicit
  issues. Conditional requirement failures remain warnings.
- The current confirmed directions make reverse 老君, reverse 萬花, and
  reverse 伏龍 eligible under the intended activation context. Neutral 金猊
  and reverse 墨玉 remain missing-access results because their required
  directions are Reverse and Direct respectively.
- Nine focused xUnit v3 tests cover effect citations, strength and timing,
  successful access, missing ownership, direction/effect mismatch, activation,
  the current direction profile, and invalid rule construction.
- The rules and evaluator are pure Domain code and cannot equip a skill,
  change direction, write a save, or control the game.
- `dotnet test --no-restore`: 171 tests passed.

## Slice 5: Recommendation

### M1-016 — Generate feasible candidate loadouts

**Status:** Complete

**Priority:** P0  
**Estimate:** L  
**Dependencies:** M1-011, M1-015

Generate candidate loadouts using hard filters before exploring combinations.

#### Acceptance criteria

- [x] Every emitted candidate passes the feasibility validator.
- [x] Required combat-start counters are considered first.
- [x] Search is bounded and deterministic.
- [x] Existing equipped skills are retained when equally suitable.
- [x] Candidate-generation diagnostics can explain exclusions.

#### Evidence

- `CombatLoadoutGenerator` hard-filters option ownership, mastery, required
  direction/effect availability, and expected raw effect identity before
  exploring combinations.
- Options are ordered by combat-start counter, hard-counter strength, threat
  coverage, current equipment, and skill ID. Include-first traversal therefore
  considers required opening counters first.
- Every combination builds a complete `ProposedCombatLoadout` and is emitted
  only from `CombatLoadoutFeasibilityValidator.FeasibleLoadout`.
- Multiple active agility or defense choices are rejected before feasibility;
  all selected passive, active, and combat requirements are evaluated in the
  proposed context.
- Search is capped at 40 options, 65,536 explored combinations, and 256
  results, with lower per-request limits supported. Exploration and result
  truncation are visible diagnostics.
- Pre-scoring order is deterministic: combat-start counters, hard counters,
  distinct threat coverage, fewer selected skills, more retained current
  skills, then stable categorized skill key.
- Ineligible options, active-role conflicts, every infeasible combination,
  exploration truncation, and result truncation retain explicit diagnostics;
  feasibility diagnostics include all underlying failures.
- Explicit helper-side direction changes allow neutral 金猊 to be proposed as
  Reverse and reverse 墨玉 as Direct. The exact target effect must exist and
  match verified evidence; the result only records a manual change.
- Ten focused generator xUnit v3 tests cover feasibility, over-budget
  exclusion, opening priority, bounds, determinism, retention, option/effect
  diagnostics, active-role conflicts, and manual direction changes. Three
  candidate-validation cases cover opt-in direction changes and missing
  effects.
- Generation is pure Domain work and cannot equip skills, change directions,
  write a save, or control the game.
- `dotnet test --no-restore`: 184 tests passed.

### M1-017 — Implement recommendation scoring

**Status:** Complete

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-014, M1-016

Score candidates by threat coverage, survival, reliability, compatibility,
damage, opportunity cost, and conditional risk.

#### Acceptance criteria

- [x] Score components are individually visible.
- [x] Hard constraints are not represented merely as score penalties.
- [x] Stable tie-breaking produces deterministic results.
- [x] Safe, balanced, and aggressive policies use documented weight sets.
- [x] Golden-target ranking is manually reviewed.

#### Evidence

- `CombatRecommendationScorer` returns seven individually visible components:
  threat coverage, survival, execution reliability, current-loadout
  compatibility, damage potential, opportunity cost, and conditional risk.
  Every component includes its policy weight, explanation, and evidence
  reference.
- The scorer only accepts `GeneratedCombatLoadout` values. Their internal
  construction is owned by `CombatLoadoutGenerator` and wraps an accepted-only
  `FeasibleCombatLoadout`, so invalid ownership, effect, direction, slot, and
  requirement combinations never enter scoring as low-scored alternatives.
- Missing damage evidence remains an explicit unavailable component and is
  excluded from normalization. The scorer does not invent a damage estimate.
- Safe, Balanced, and Aggressive weights are fixed, sum to 100, and are
  documented in `docs/architecture/RECOMMENDATION-SCORING.md`.
- Ranking is deterministic by total score, threat-coverage score, retained
  current-skill count, and the candidate stable key.
- The golden threat fixture was manually reviewed under Safe policy: verified
  hard coverage for the critical mind-resonance threat ranks above mitigation
  alone. This is a structural review of the verified threat/counter model, not
  a claim of simulated win probability.
- Eight focused xUnit v3 tests cover component visibility, unknown damage,
  policy weights and priorities, deterministic ties, conditional risk,
  golden hard-counter ranking, and damage-evidence validation.
- Scoring is pure Domain work and cannot equip skills, change directions,
  write a save, or control the game.
- `dotnet test --no-restore`: 192 tests passed.

### M1-018 — Produce suggested manual loadout changes and battle plan

**Status:** Complete

**Priority:** P1  
**Estimate:** M  
**Dependencies:** M1-016, M1-017

Compare the selected candidate with the current loadout and produce
informational instructions for the player to carry out manually.

#### Acceptance criteria

- [x] Manual add, remove, retain, and change-direction suggestions are returned.
- [x] Primary and alternative defense/agility choices are identified.
- [x] Opening actions and switching conditions are included.
- [x] Every instruction references its recommendation reason.

#### Evidence

- `ManualCombatPlanBuilder` compares the selected feasible proposal with the
  current snapshot and returns explicit `Add`, `Remove`, `Retain`, and
  `ChangeDirection` steps.
- Required direction changes come from accepted candidate validation and state
  the exact Direct or Reverse direction for the player to select manually.
- The highest-ranked active defense and agility are returned as primary
  choices. Up to three distinct choices from lower-ranked feasible candidates
  are retained as alternatives.
- Counter activation timing produces ordered opening instructions for
  combat-start passives, equipped passives, active defense, active agility,
  and active attacks.
- Alternative active-role choices produce explicit switch-before-combat
  conditions. The plan never implies that the helper changes a selection
  during combat.
- Every loadout change, role choice, opening action, and switch condition owns
  a structured recommendation reason with a code, summary, evidence
  references, and relevant threat codes.
- An empty ranking returns a diagnostic rather than an invented plan.
- Seven focused xUnit v3 tests cover all change kinds, reason references,
  defense/agility alternatives, activation order, switching conditions, empty
  rankings, and deterministic output.
- Planning is pure Domain work. It cannot equip a skill, change a direction,
  write a save, or control the game.
- `dotnet test --no-restore`: 199 tests passed.

### M1-019 — Add evidence-backed recommendation explanations

**Status:** Complete

**Priority:** P1  
**Estimate:** M  
**Dependencies:** M1-014, M1-017, M1-018

Create structured explanations suitable for both API clients and later natural
language presentation.

#### Acceptance criteria

- [x] Every selected skill has at least one reason.
- [x] Threat, counter, direction, cost, and conditions are linked.
- [x] Assumptions and unavailable data are explicit.
- [x] Explanations do not depend on an LLM.

#### Evidence

- `CombatRecommendationExplanationBuilder` creates one structured explanation
  per selected skill and carries forward the plan's reason codes, summaries,
  evidence references, and threat codes.
- Each skill explanation links matched `TargetThreat` records, counter strength
  and activation timing, current and required direction, expected effect ID,
  effective slot-cost breakdown, category budget, and evaluated combat
  requirements.
- Threat references without supplied structured details are reported as
  unavailable instead of being invented. Compatibility-only selections
  explicitly state that no verified counter mapping is attached.
- Current-screen observations, player observations, and hypotheses are
  surfaced as assumptions with their source references.
- Missing damage evidence, skill fields, cost fields, and unknown requirement
  evaluations use typed unavailable-data caveats with stable codes.
- Explanations are produced by a static, deterministic Domain builder with no
  model service, prompt, network, or natural-language-generation dependency.
- Eight focused xUnit v3 tests cover per-skill reasons, all evidence links,
  assumptions, unavailable data, unmatched threats, compatibility-only
  selections, input validation, and absence of model dependencies.
- Explanation building is pure Domain work and cannot equip a skill, change a
  direction, write a save, or control the game.
- `dotnet test --no-restore`: 207 tests passed.

## Slice 6: Application and API

### M1-020 — Implement `RecommendCombatLoadout` use case

**Status:** Complete

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-005, M1-014, M1-016, M1-017

Orchestrate snapshot creation, threat analysis, candidate generation,
validation, scoring, and explanation.

#### Acceptance criteria

- [x] Application depends on ports and Domain services only.
- [x] Cancellation is propagated.
- [x] Snapshot warnings are preserved.
- [x] NSubstitute tests verify orchestration and failure paths.

#### Evidence

- `RecommendCombatLoadout` depends only on `ICombatSnapshotReader` and invokes
  the Domain threat analyzer, bounded candidate generator, feasibility
  validation, scorer, manual-plan builder, and explanation builder in order.
- The use case selects only verified counter rules that address analyzed
  threats, retains currently equipped non-counter skills as candidate options,
  and never converts names or guesses into combat rules.
- The exact `CancellationToken` is passed to the snapshot reader. Cancellation
  is checked before the read and between the synchronous Domain stages.
- `CombatLoadoutRecommendation` returns the immutable source snapshot and
  exposes its original warning collection unchanged alongside threat,
  generation, scoring, plan, and explanation results.
- No-threat/no-option input returns generation diagnostics and an empty manual
  plan instead of inventing a recommendation.
- The curated-option bound is now 40, while exploration and result bounds stay
  at 65,536 and 256. This accommodates the observed full loadout plus the
  verified counter set without removing the deterministic search guard.
- Five NSubstitute/xUnit v3 use-case tests cover successful orchestration,
  observation and policy forwarding, cancellation, reader failure, empty
  results, and request validation. A Domain boundary test covers the revised
  option limit.
- The Application layer has no Infrastructure or GameData dependency and
  exposes no operation capable of changing a save or controlling the game.
- `dotnet test --no-restore`: 213 tests passed.

### M1-021 — Add combat-recommendation endpoint

**Status:** Complete

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-020

Add `POST /api/combat-recommendations`.

#### Acceptance criteria

- [x] Target character ID is required.
- [x] Objective supports safe, balanced, and aggressive.
- [x] Configured save path is used by default.
- [x] Current-screen observations are optional and affect analysis only.
- [x] Validation errors return appropriate problem responses.
- [x] Response is typed JSON rather than line-oriented text.
- [x] Available styles are returned from the same immutable snapshot.
- [x] Threats, skill reasons, manual changes, and battle-plan steps have stable
      references.
- [x] The endpoint cannot execute a recommendation or mutate game state.

#### Evidence

- `POST /api/combat-recommendations` accepts a required positive
  `targetCharacterId`, a string-enum Safe/Balanced/Aggressive objective, and an
  optional helper-owned current-screen loadout observation.
- The request has no save-path field. `SaveGames:DefaultSaveFilePath` is always
  supplied to the Application use case from validated configuration.
- One snapshot read is shared by all three policy scores and plans.
  `snapshotReference` is repeated on every style result, and `requestedStyle`
  identifies the initially selected result.
- The response uses typed records for threats, component scores, selected
  skills, reasons, manual changes, plan steps, caveats, and warnings. It does
  not return line-oriented diagnostic text as the primary contract.
- Threat, candidate, skill, reason, change, plan-step, caveat, and warning
  objects receive deterministic references.
- Missing/invalid targets, invalid objectives or observations, missing save
  files, and invalid save data return RFC problem responses with status 400.
- JSON enum serialization uses names, so clients send and receive `Safe`,
  `Balanced`, and `Aggressive` instead of implementation-specific integers.
- Eight API xUnit v3 cases cover the typed success response, one-read
  multi-style behavior, configured path, optional observations, target and
  objective validation, expected reader failures, stable references, and the
  single POST-only surface.
- The controller depends on the Application input port and configuration only.
  It exposes no execute/apply/equip/write operation and cannot mutate the game
  or save.
- `dotnet test --no-restore`: 221 tests passed.

### M1-022 — Add target lookup endpoint

**Status:** Complete

**Priority:** P1  
**Estimate:** M  
**Dependencies:** M1-005

Allow clients to find valid target IDs by name and snapshot context.

#### Acceptance criteria

- [x] Search does not require parsing diagnostic lines.
- [x] Results include ID, name, location, and enough context to disambiguate.
- [x] Missing and ambiguous targets are handled explicitly.

#### Evidence

- `ITargetLookupReader` is a query-only Application port returning immutable
  `TargetLookupEntry` values rather than legacy report text.
- `TaiwuTargetLookupReader` reuses `TaiwuArchiveReadSession`, enumerates the
  loaded read-only `CharacterDomain.Characters` view, excludes Taiwu, and maps
  character ID, display name, age, area ID, and block ID.
- The archive session retains its before/after read-only file fingerprint and
  serialized reader lock. Target lookup has no separate archive load path.
- `FindTargets` supports exact positive character IDs and
  case-insensitive name fragments. Exact-name matches sort first, followed by
  deterministic name, location, and ID ordering.
- Results explicitly use `Found`, `NotFound`, or `Ambiguous`; `totalMatches`
  remains accurate when the returned list is limited.
- `GET /api/targets?query=...` always uses the configured save path and returns
  typed match, location, warning, capture-time, and GameData-version fields
  with stable target/location references.
- Eight Application xUnit v3 cases cover ID and name matching, ambiguity,
  missing targets, result limits, validation, cancellation, reader failures,
  and the query-only port. Seven API cases cover structured results,
  configured-path use, validation/failure problems, stable references, and
  the GET-only surface.
- No result can select a target in the game, start combat, write a save, or
  control the runtime.
- `dotnet test --no-restore`: 236 tests passed.

## Slice 7: Automated verification

### M1-023 — Add Domain rule test suite

**Status:** Complete

**Priority:** P0  
**Estimate:** L  
**Dependencies:** M1-007 through M1-019

Create comprehensive unit tests for costs, budgets, direction, compatibility,
threats, candidate validation, and scoring.

#### Acceptance criteria

- [x] Every hard constraint has positive and negative tests.
- [x] Boundary conditions cover exact capacity and one-over-capacity.
- [x] Determinism is tested.
- [x] No test requires the installed game unless explicitly categorized as an
      integration test.

#### Evidence

- `docs/testing/DOMAIN-RULE-COVERAGE.md` traces ownership, mastery, direction,
  exact effect, requirement, role, proposal, slot, legendary-book, version,
  signature, and determinism rules to xUnit v3 cases.
- Each supported hard requirement type has a satisfied case and an explicit
  rejection case.
- A five-case boundary theory accepts exact capacity and rejects one over for
  Neigong 6 and Attack/Agility/Defense/Assistance 2.
- Duplicate generation options and duplicate equipped skills are rejected
  before recommendation scoring.
- Generator input order, score tie-breaking, threat order, explanations, and
  manual plans retain deterministic output.
- An architecture test verifies the Domain test project has no Infrastructure
  or GameData dependency and no installed-game path or namespace dependency.
- Domain tests operate on immutable in-memory snapshots only.
- `dotnet test --no-restore`: 247 tests passed.

### M1-024 — Add opt-in local GameData integration tests

**Status:** Complete

**Priority:** P1  
**Estimate:** M  
**Dependencies:** M1-005, M1-020

Verify the adapter against the locally installed game and configured save.

#### Acceptance criteria

- [x] Tests skip clearly when local prerequisites are absent.
- [x] Hashes of all game-owned files touched by the read path are unchanged
      before and after.
- [x] The helper opens source files read-only wherever it controls access.
- [x] Two consecutive reads succeed in one process.
- [x] Snapshot contains the expected golden player and target.
- [x] Proprietary data is not stored in test artifacts.

#### Evidence

- `TaiWu.Infrastructure.IntegrationTests` is an xUnit v3 project with one
  prerequisite-independent contract test and one opt-in local read test.
- The local read test requires only `TAIWU_INTEGRATION_SAVE_PATH`. An absent or
  invalid value, or absent runtime dependencies, produces an explicit skip.
- [Local integration instructions](../testing/LOCAL-GAMEDATA-INTEGRATION-TESTS.md)
  contain no machine-specific path, save hash, or proprietary fixture.
- The test fingerprints the source save and every recognized GameData runtime
  dependency in the test process before reading, then compares length, SHA-256,
  and last-write time in a `finally` block.
- Fingerprint streams specify `FileMode.Open`, `FileAccess.Read`, and shared
  read access. The production adapter retains its read-only fingerprint guard
  and architecture-level save-write prohibition.
- Two consecutive reads through `ICombatSnapshotReader` returned player
  `21396`, target `16317`, and the expected target age of 52 from the golden
  save in one process.
- The contract test verifies no save or GameData source is embedded in the
  integration-test assembly. Build-time local runtime copies remain ignored
  and excluded from publication.
- Opt-in local run: 2 tests passed; no source fingerprint changed.
- Default `dotnet test --no-restore`: 249 tests discovered, 248 passed, and the
  one local GameData read test skipped explicitly.

## Slice 8: Presentation

### M1-026 — Define recommendation presentation view models

**Status:** Complete

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-019, M1-021

Map the structured recommendation response into presentation-specific view
models used by the local UI.

#### Acceptance criteria

- [x] Safe, balanced, and aggressive recommendations are returned from the
      same immutable snapshot.
- [x] The requested style identifies the initially selected recommendation.
- [x] Threats, skill reasons, manual changes, and battle-plan steps have stable
      references.
- [x] Direction, actual and effective cost, capacity, generic allocation,
      timing, conditions, evidence, and warnings are represented explicitly.
- [x] Presentation view models contain no `GameData` types.
- [x] No response or view-model operation can execute a recommendation.
- [x] Contract and mapping behavior is covered by xUnit v3 tests.

#### Evidence

- `CombatRecommendationViewModelMapper` maps all Safe, Balanced, and Aggressive
  results from one `CombatLoadoutRecommendation` and repeats the source
  snapshot reference on every style.
- The requested policy produces exactly one `IsInitiallySelected` style and a
  stable `InitiallySelectedStyleReference`.
- Presentation records explicitly model threats, scores, five Chinese-named
  skill categories, capacity, remaining and generic slots, skill direction,
  actual/effective cost, reductions, counter timing, conditions, evidence,
  manual changes, battle-plan steps, caveats, and warnings.
- Stable references cover styles, threats, categories, skills, reasons,
  conditions, score rows, manual changes, plan steps, caveats, and warnings.
- Every model carries the persistent information-only notice that the helper
  cannot apply, equip, or execute a recommendation.
- Three API-layer xUnit v3 cases cover style selection, shared snapshot
  identity, non-zero generic allocation, costs, direction, timing, conditions,
  evidence, warnings, stable mapping, and the non-interference notice.
- An architecture test reflects every public Presentation signature and
  rejects GameData types or game-mutation operation names.
- Default `dotnet test --no-restore`: 253 tests discovered, 252 passed, and the
  opt-in local GameData read skipped explicitly.

### M1-027 — Add the local Blazor shell and recommendation controls

**Status:** Complete

**Priority:** P0  
**Estimate:** M  
**Dependencies:** M1-021, M1-022, M1-026

Host a Blazor Interactive Server page in the existing ASP.NET Core application
and implement the recommendation input workflow.

#### Acceptance criteria

- [x] The UI runs in the existing local .NET 10 process.
- [x] The player can search for and select a target.
- [x] The player can choose a preferred style and optional weapon.
- [x] Current-screen observations are clearly identified as analysis input
      only.
- [x] Snapshot read time, freshness, and game version are visible.
- [x] A persistent `Information only` badge is visible.
- [x] Request cancellation and repeated requests are handled safely.
- [x] No separate Node-based frontend toolchain is required.

#### Evidence

- The existing ASP.NET Core host now registers Blazor Interactive Server
  components and maps the page alongside the unchanged API controllers.
- `/` provides target search and selection using `IFindTargets`, including
  character name, age, ID, area, and block context.
- Controls expose Safe/Balanced/Aggressive style selection and an optional
  visible 刀 preference. The preference is explicitly described as UI context;
  it does not bypass verified feasibility rules.
- The optional observation panel accepts equipped skill IDs for all five
  categories plus 萬用 allocation and maps them to `PlayerLoadoutObservation`.
  It is labelled analysis input only and cannot write to the game.
- Successful results display snapshot read time, source age at capture,
  GameData version, warnings, and all three styles without rereading when the
  visible style tab changes.
- The sticky application header and result area both display
  `Information only`; the page states that the helper cannot change the save,
  equip skills, or control the game.
- Search and recommendation operations use separate cancellation sources and
  monotonic request versions. Repeated requests cancel and supersede stale
  work, and component disposal cancels outstanding reads.
- Responsive local CSS uses the existing Web SDK only. No package manifest,
  Node dependency, frontend build, or deployment configuration was added.
- A local smoke run returned HTTP 200 for `/` with the app title, information
  boundary, and Blazor boot script.
- An architecture test verifies local hosting, Interactive Server wiring,
  query use cases, cancellation, analysis-only copy, and absence of mutation
  controls or Node manifests.
- Default `dotnet test --no-restore`: 254 tests discovered, 253 passed, and the
  opt-in local GameData read skipped explicitly.

### M1-028 — Build the threat and recommended-loadout layout

**Status:** Complete

**Priority:** P0  
**Estimate:** L  
**Dependencies:** M1-026, M1-027

Implement the primary two-column pre-fight briefing described by
[UI-001](./UI-001-combat-recommendation-layout.md).

#### Acceptance criteria

- [x] Critical and moderate target threats are ordered by severity.
- [x] Selecting a threat highlights its countering skills and plan steps.
- [x] Skills are grouped as 內功, 摧破, 輕靈, 護體, and 奇竅.
- [x] Each category shows used capacity, available capacity, and generic-slot
      allocation.
- [x] Every skill card shows its Chinese in-game name, direction, effective
      cost, manual-change status, reason, activation timing, and requirements.
- [x] Safe, balanced, and aggressive tabs switch between results from the same
      snapshot.
- [x] Known-constraint validation is not presented as a win probability.

#### Evidence

- `ThreatPanel` orders groups by descending `TargetThreatSeverity`, preserves
  deterministic code order within a group, and exposes keyboard-accessible
  pressed-state buttons.
- `RecommendationSelectionState` retains one immutable recommendation while
  switching policies and toggles one validated threat reference.
- Selecting a threat highlights only skill cards and opening/switch cues whose
  structured threat references match the selection.
- The loadout renders all five presentation categories using the Chinese
  in-game labels 內功, 摧破, 輕靈, 護體, and 奇竅.
- `CapacityBar` shows used/capacity values, remaining availability through its
  accessible progress state, and non-zero 萬用 allocation.
- Each skill card presents Chinese name where available, current/required
  direction, actual/effective cost, add/retain/direction status, counter
  timing, reasons, conditions, linked threats, and expandable evidence.
- Policy tabs use the existing three style results and never issue another
  save read. A visible disclaimer states that the known-constraint score is
  not a win probability.
- Three xUnit v3 cases cover same-snapshot style switching, threat-linked
  highlighting and toggle behavior, and rejection of unknown selections.
- An architecture test verifies the severity ordering, linked components,
  cost/direction/timing/condition/evidence fields, generic slots, and
  non-probability wording.
- Default `dotnet test --no-restore`: 258 tests discovered, 257 passed, and the
  opt-in local GameData read skipped explicitly.

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
