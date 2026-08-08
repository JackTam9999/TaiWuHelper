# Epic 3 backlog: Verified target observations and evidence-aware recommendations

This backlog implements [EPIC-003](./EPIC.md) while preserving the permanent
safety boundary in
[ADR-0001](../../architecture/ADR-0001-absolute-game-non-interference.md).

## Conventions

### Priority

- **P0:** Required for the first trustworthy target-observation vertical.
- **P1:** Required for Epic 3 completion.
- **P2:** Valuable follow-up that may move to a later epic.

### Estimate

- **S:** One focused change.
- **M:** Several related classes and tests.
- **L:** A cross-layer slice that should be split if it cannot remain
  reviewable.

### Status

- **Planned:** Scope is defined but implementation has not started.
- **In progress:** Implementation or evidence collection is underway.
- **Blocked:** A documented external fact or product decision is required.
- **Complete:** Acceptance criteria and required evidence are present.

### Definition of done

Every completed item must:

- preserve Clean Architecture dependency direction;
- include xUnit v3 tests at the appropriate layers;
- leave every save, game file, configuration value, running process, runtime
  memory location, and in-game state unchanged;
- introduce no game hook, injection, patch, automation, screenshot capture,
  or input-control capability;
- use immutable typed observations with explicit provenance;
- distinguish partial, complete, stale, conflicting, and unsupported evidence;
- resolve skills through stable catalogue identities rather than raw text;
- prevent unverified raw descriptions from entering recommendation rules;
- expose bilingual and accessible failure states;
- update architecture, API, scenario, testing, and roadmap evidence where the
  contract changes;
- record the relevant test command and result.

## Delivery order

| Order | Slice | Outcome |
|---:|---|---|
| 0 | Observation evidence | Only reliably visible target facts enter the contract |
| 1 | Domain model | Coverage, provenance, conflicts, and invariants are typed |
| 2 | Resolution and merge | Catalogue identities become a safe immutable target overlay |
| 3 | API vertical | Typed observations reach the recommendation use case safely |
| 4 | Manual UI | Players can enter and confirm bilingual target observations |
| 5 | Recommendation integration | Confirmed equipped evidence changes threat analysis correctly |
| 6 | Impact explanation | Players can see what changed and why |
| 7 | Verification and completion | Determinism and non-interference close the original vertical |
| 8 | Scope correction | Hostile/story battle-visible evidence is modeled without inventing a complete loadout |

## Slice 0: Evidence boundary

### E3-000 — Verify observable target-loadout fields

**Status:** In progress

**Priority:** P0

**Estimate:** L

**Dependencies:** None

Record the current supported target-inspection UI and establish the exact
meaning and completeness of every field proposed for manual entry.

#### Acceptance criteria

- [x] The evidence identifies the inspected game and language-resource
      versions without committing proprietary assets.
- [x] Target identity, skill name, category, slot visibility, empty-slot
      behavior, paging, and direction visibility are evaluated separately.
- [x] Sparring complete-loadout access, hostile/story full-page unavailability,
      and hostile/story partial combat-tooltip evidence are evaluated
      separately; an unavailable full page never implies an empty loadout or
      no observable evidence.
- [x] The evidence proves whether a screen can support `CompleteLoadout` or
      only `PartialLoadout`.
- [x] Any category or direction not reliably visible remains unsupported.
- [x] At least one representative complete or partial target observation is
      recorded with timestamp and opaque evidence reference.
- [x] Before/after fingerprints prove the inspected save and game sources are
      unchanged.
- [x] A version change invalidates the completeness rule rather than silently
      reusing it.

#### Evidence when complete

- `docs/scenarios/E3-000-target-observation-evidence.md`.
- Minimal metadata for any local-only captures, including SHA-256 and capture
  time, without committing the image unless distribution is approved.
- A versioned observable-field table consumed by later items.

## Slice 1: Domain model

### E3-001 — Define target observation and coverage models

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E3-000

Add immutable Domain values for target identity, evidence, observed skills,
optional direction, and partial/complete coverage.

#### Acceptance criteria

- [x] `TargetLoadoutObservation` carries target ID, UTC observation time,
      evidence reference, coverage, and immutable observed skills.
- [x] Observation access distinguishes UI-visible sparring opponents from
      hostile/story targets whose loadout page is unavailable.
- [x] Every observed skill has a stable non-negative ID and verified category.
- [x] Direction is optional and uses the existing `PracticeDirection` value.
- [x] Duplicate skills, duplicate slots where applicable, blank evidence,
      invalid IDs, invalid categories, and unsupported direction values are
      rejected.
- [x] `PartialLoadout` cannot express absence for omitted skills.
- [x] `CompleteLoadout` is constructible only with E3-000 completeness
      provenance for the detected version.
- [x] No Domain type references GameData, ASP.NET Core, SQLite, files,
      screenshots, or processes.
- [x] Tests cover immutability, equality, validation, partial coverage,
      complete coverage, and optional direction.

#### Evidence when complete

- Domain-model architecture note.
- Focused Domain test summary.

### E3-002 — Define observation provenance and conflict results

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E3-001

Extend the existing snapshot/progress provenance vocabulary where necessary so
save and target-observation values can coexist without silent replacement.

#### Acceptance criteria

- [x] Save, current-screen observation, installed configuration, and verified
      rule sources remain distinguishable.
- [x] A used observed field records observation time and evidence reference.
- [x] Conflicting values retain both observations in deterministic order.
- [x] Conflict and confidence statuses describe evidence, not win
      probability.
- [x] Public provenance contains no local file path or sensitive exception
      text.
- [x] Existing player-loadout observation behavior remains compatible.
- [x] Tests cover available, unavailable, stale, and conflicting values.

## Slice 2: Resolution and merge

### E3-003 — Resolve bilingual target skill selections

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E3-001, Epic 2 catalogue

Resolve manual skill selections through the current local catalogue and return
stable confirmation candidates.

#### Acceptance criteria

- [x] Traditional Chinese and English input use the existing normalized search
      behavior.
- [x] Exact matches rank before partial matches with deterministic ordering.
- [x] Ambiguous matches require explicit player confirmation.
- [x] Selected category agrees with the verified static definition.
- [x] A missing, stale, rebuilding, or unsupported catalogue produces an
      explicit result and no guessed identity.
- [x] Observed skills absent from the target save remain representable with
      only the static facts required by analysis.
- [x] Raw descriptions never become typed mechanics during resolution.
- [x] Tests cover both languages, fallback, ambiguity, missing definitions,
      stale catalogue, and target-snapshot absence.

### E3-004 — Merge target observations into immutable snapshots

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E3-001, E3-002, E3-003

Implement a pure observation merger that returns a new combat snapshot and
stable warnings.

#### Acceptance criteria

- [x] Observation target ID must match the snapshot target ID.
- [x] A newer valid observation receives field-level precedence only for its
      declared coverage.
- [x] An observation older than the save is retained as stale evidence but is
      not applied.
- [x] Save-time unavailability requires explicit precedence confirmation and
      emits a warning.
- [x] A partial observation can confirm listed equipped skills but cannot
      remove or negate omitted skills.
- [x] A complete observation may replace equipped membership only when its
      versioned completeness evidence is valid.
- [x] Optional observed direction overrides no unrelated skill field.
- [x] Conflicting save values remain visible with both sources.
- [x] The original snapshot and observation remain unchanged.
- [x] Output ordering and warnings are deterministic.
- [x] Tests cover fresh, stale, partial, complete, conflicting, mismatched
      target, unsupported version, and missing-save-time cases.

#### Evidence when complete

- Observation-merge architecture document.
- Pure Domain/Application test summary.

## Slice 3: API vertical

### E3-005 — Add typed target-observation API contracts

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E3-003, E3-004

Accept an optional target observation in the existing recommendation workflow
and expose sanitized resolution, provenance, and impact results.

#### Acceptance criteria

- [x] Recommendation requests accept target ID, observed-at time, coverage,
      evidence reference, and typed selected skills.
- [x] The contract accepts no save path, game path, screenshot path, process
      ID, raw GameData type, or command-like payload.
- [x] Invalid and ambiguous observations return stable HTTP 400 problem
      responses without local details.
- [x] Stale, conflicting, partial, and unsupported evidence remain successful
      typed result states where the request itself is valid.
- [x] Cancellation propagates through catalogue resolution and recommendation
      generation.
- [x] Existing requests without a target observation are backward compatible.
- [x] JSON serialization never evaluates unavailable value getters.
- [x] Controller and architecture tests enforce the information-only boundary.

## Slice 4: Manual UI

### E3-006 — Build the bilingual target-observation form

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E3-005

Add a manual-first observation surface to the recommendation page.

#### Acceptance criteria

- [x] The form starts disabled and explains when target observation is useful.
- [x] Hostile and story targets remain explicitly unavailable for manual
      current-screen loadout observation; the form never requests hidden data.
- [x] Target identity and save freshness are visible before entry.
- [x] The player chooses partial or complete coverage with an explanation of
      omission semantics.
- [x] Skill search follows the active Traditional Chinese or English language.
- [x] Ambiguous candidates show enough static detail for confirmation.
- [x] Category is verified rather than freely typed.
- [x] Direction can be omitted and appears only where E3-000 supports it.
- [x] The review step shows resolved stable identities, coverage, time, and
      evidence status.
- [x] Validation errors are field-specific and keyboard accessible.
- [x] Applying an observation creates a new recommendation request; it does
      not control the game.
- [x] Clearing returns to the save-only form and result.
- [x] Initial, editing, ambiguous, stale, conflicting, unsupported, loading,
      applied, and cleared states work in both languages.
- [x] Rendering tests cover semantic labels, focusable controls, status text,
      and no color-only meaning.

## Slice 5: Recommendation integration

### E3-007 — Use confirmed target loadout evidence in threat analysis

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E3-004, E3-005

Feed the merged target snapshot into the existing versioned threat analyzer
without weakening rule verification.

#### Acceptance criteria

- [x] Confirmed equipped skills are analyzed before learned-but-unconfirmed
      skills.
- [x] A partial observation cannot suppress possible learned threats.
- [x] A valid complete observation can remove stale equipped membership from
      current analysis while preserving conflict evidence.
- [x] Observed direction affects a rule only when available and version-matched.
- [x] Unknown effects remain warnings and cannot acquire severity or score.
- [x] Every threat records whether its source is save, observed equipped,
      learned-unconfirmed, or verified rule evidence.
- [x] Recommendations without observations remain byte-for-byte equivalent at
      the contract level, excluding newly added empty metadata fields.
- [x] Deterministic tests cover added, removed, unchanged, and unsupported
      threats.

### E3-008 — Recalculate recommendations from observed threats

**Status:** Complete

**Priority:** P1

**Estimate:** M

**Dependencies:** E3-007

Run feasibility, counter selection, scoring, and explanation against the
observation-enhanced threat set.

#### Acceptance criteria

- [x] Hard constraints are still evaluated before scoring.
- [x] Only typed verified threats and counters affect recommendation choice.
- [x] Added or removed recommendations are deterministic for the same evidence.
- [x] A target observation cannot make an unavailable player skill feasible.
- [x] All recommendation styles preserve their documented policy weights.
- [x] Unresolved target evidence remains visible and cannot be converted into
      a favorable assumption.
- [x] Tests cover a recommendation that changes and one that correctly remains
      unchanged.

## Slice 6: Impact explanation

### E3-009 — Explain observation impact

**Status:** Complete

**Priority:** P1

**Estimate:** L

**Dependencies:** E3-007, E3-008

Present the difference between save-only and observation-enhanced analysis.

#### Acceptance criteria

- [x] The result lists threats confirmed, added, demoted, removed, unchanged,
      and still unsupported.
- [x] It lists recommended skills/counters added or removed and explains the
      evidence chain for each change.
- [x] Feasibility changes are separated from scoring changes.
- [x] Partial coverage displays an explicit remaining-unknown warning.
- [x] Conflicts show both sources, timestamps, and the applied precedence rule.
- [x] Evidence confidence is never phrased as win probability.
- [x] Save-only and observed results can be compared without raw diagnostic
      text.
- [x] API and bilingual rendering tests cover all impact categories.

#### Evidence when complete

- `docs/architecture/TARGET-OBSERVATION-IMPACT-EXPLANATION.md`.

## Slice 7: Verification and completion

### E3-010 — Verify observation safety and determinism

**Status:** Complete

**Priority:** P1

**Estimate:** L

**Dependencies:** E3-005, E3-006, E3-007, E3-008, E3-009

Run the full cross-layer and local read-only verification matrix.

#### Acceptance criteria

- [x] Domain, Application, Infrastructure, API, Presentation, integration, and
      architecture tests pass.
- [x] Repeating the same save/catalogue/observation input produces equivalent
      snapshots, threats, recommendations, impacts, and ordering.
- [x] Clearing the observation reproduces the save-only result.
- [x] Current catalogue rebuild/cache behavior remains independent from
      session observation state.
- [x] Architecture tests reject file paths, screenshot capture, process access,
      game control, mutation APIs, and observation-history persistence.
- [x] Before/after SHA-256 fingerprints prove every inspected save and game
      source is unchanged.
- [x] Manual bilingual verification matches the E3-000 evidence scenario.
- [x] Test commands, results, expected opt-in skips, and source versions are
      documented.

#### Evidence when complete

- `docs/reviews/E3-010-automated-verification.md`.
- Current-save observation vertical result.
- Final automated suite summary.

### E3-011 — Validate the workflow and close Epic 3

**Status:** In progress

**Priority:** P1

**Estimate:** M

**Dependencies:** E3-000 through E3-010

Compare the completed workflow with the recorded target UI, audit every Epic 3
criterion, and record the product-owner decision.

#### Acceptance criteria

- [x] The manual form can reproduce the representative E3-000 observation.
- [x] Resolved bilingual skill identities match the recorded target UI.
- [ ] Partial/complete semantics agree with the expanded verified screen
      behavior, including hostile/story combat tooltips.
- [ ] Threat and recommendation changes from battle-visible active effects are
      evidence-backed and explainable.
- [x] Clearing returns to the expected save-only result.
- [ ] All Epic 3 acceptance criteria have linked implementation or evidence.
- [x] Deferred screenshot assistance and observation history remain explicit
      future work rather than hidden partial implementations.
- [ ] The product owner records the Epic 3 completion decision.

#### Evidence when complete

- `docs/reviews/E3-011-manual-verification.md`.
- Updated status and completion decision in [EPIC-003](./EPIC.md).

### E3-012 — Support hostile/story battle-visible observations

**Status:** In progress

**Priority:** P0

**Estimate:** L

**Dependencies:** E3-000, E3-003, E3-005, E3-007, E3-009

Replace the overly broad hostile/story rejection with a separate partial
observation path for skill effects that the normal combat UI visibly exposes,
without claiming that the hidden `運功` page or full equipped loadout is known.

#### Acceptance criteria

- [x] Versioned evidence records the visible panel heading, skill name, power,
      effect text, stable bilingual identity, and unresolved indicators.
- [ ] Observation provenance distinguishes a complete sparring `運功` screen
      from a hostile/story battle-visible active-effect panel.
- [ ] Hostile/story observations are always partial and can never establish
      omitted-skill absence or complete loadout coverage.
- [ ] Visible names resolve through the guarded catalogue; exact versioned
      effect text may confirm direction/effect ID without accepting a free-form
      mechanic claim.
- [ ] Current power may be retained as evidence but cannot influence legality
      or scoring until a separate typed power rule exists.
- [ ] Unlabeled colored values and status icons remain explicitly unsupported.
- [ ] Threat analysis can use a verified visible active effect without
      silently relabeling it as complete equipped membership.
- [ ] The bilingual UI explains that the full loadout is unavailable while
      allowing only the partial facts actually visible in combat.
- [ ] Applying and clearing the observation remain deterministic,
      session-bound, and information-only.
- [ ] Domain, API, Presentation, architecture, and local read-only tests pass.

#### Evidence when complete

- Revised [E3-000 evidence](../../scenarios/E3-000-target-observation-evidence.md).
- Updated target-observation provenance, API, threat-analysis, and UI design.
- E3-012 automated and local read-only verification summary.

## Future work outside Epic 3

- Automatic or assisted screenshot interpretation after a separate privacy and
  accuracy review.
- Persisted observation history with retention, deletion, and freshness
  governance.
- Side-by-side current/recommended/alternative loadout comparison.
- Shareable recommendation cards.
- Player-reported battle outcomes and feedback.
- Additional typed effect normalization.
- Life-skill catalogue support.
