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
| 7 | Verification and completion | Determinism and non-interference close the epic |

## Slice 0: Evidence boundary

### E3-000 — Verify observable target-loadout fields

**Status:** Complete

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
- [x] Sparring, hostile, and story-target access semantics are evaluated
      separately; unavailable hostile/story UI never implies an empty loadout.
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

**Status:** Planned

**Priority:** P0

**Estimate:** M

**Dependencies:** E3-001

Extend the existing snapshot/progress provenance vocabulary where necessary so
save and target-observation values can coexist without silent replacement.

#### Acceptance criteria

- [ ] Save, current-screen observation, installed configuration, and verified
      rule sources remain distinguishable.
- [ ] A used observed field records observation time and evidence reference.
- [ ] Conflicting values retain both observations in deterministic order.
- [ ] Conflict and confidence statuses describe evidence, not win
      probability.
- [ ] Public provenance contains no local file path or sensitive exception
      text.
- [ ] Existing player-loadout observation behavior remains compatible.
- [ ] Tests cover available, unavailable, stale, and conflicting values.

## Slice 2: Resolution and merge

### E3-003 — Resolve bilingual target skill selections

**Status:** Planned

**Priority:** P0

**Estimate:** M

**Dependencies:** E3-001, Epic 2 catalogue

Resolve manual skill selections through the current local catalogue and return
stable confirmation candidates.

#### Acceptance criteria

- [ ] Traditional Chinese and English input use the existing normalized search
      behavior.
- [ ] Exact matches rank before partial matches with deterministic ordering.
- [ ] Ambiguous matches require explicit player confirmation.
- [ ] Selected category agrees with the verified static definition.
- [ ] A missing, stale, rebuilding, or unsupported catalogue produces an
      explicit result and no guessed identity.
- [ ] Observed skills absent from the target save remain representable with
      only the static facts required by analysis.
- [ ] Raw descriptions never become typed mechanics during resolution.
- [ ] Tests cover both languages, fallback, ambiguity, missing definitions,
      stale catalogue, and target-snapshot absence.

### E3-004 — Merge target observations into immutable snapshots

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E3-001, E3-002, E3-003

Implement a pure observation merger that returns a new combat snapshot and
stable warnings.

#### Acceptance criteria

- [ ] Observation target ID must match the snapshot target ID.
- [ ] A newer valid observation receives field-level precedence only for its
      declared coverage.
- [ ] An observation older than the save is retained as stale evidence but is
      not applied.
- [ ] Save-time unavailability requires explicit precedence confirmation and
      emits a warning.
- [ ] A partial observation can confirm listed equipped skills but cannot
      remove or negate omitted skills.
- [ ] A complete observation may replace equipped membership only when its
      versioned completeness evidence is valid.
- [ ] Optional observed direction overrides no unrelated skill field.
- [ ] Conflicting save values remain visible with both sources.
- [ ] The original snapshot and observation remain unchanged.
- [ ] Output ordering and warnings are deterministic.
- [ ] Tests cover fresh, stale, partial, complete, conflicting, mismatched
      target, unsupported version, and missing-save-time cases.

#### Evidence when complete

- Observation-merge architecture document.
- Pure Domain/Application test summary.

## Slice 3: API vertical

### E3-005 — Add typed target-observation API contracts

**Status:** Planned

**Priority:** P0

**Estimate:** M

**Dependencies:** E3-003, E3-004

Accept an optional target observation in the existing recommendation workflow
and expose sanitized resolution, provenance, and impact results.

#### Acceptance criteria

- [ ] Recommendation requests accept target ID, observed-at time, coverage,
      evidence reference, and typed selected skills.
- [ ] The contract accepts no save path, game path, screenshot path, process
      ID, raw GameData type, or command-like payload.
- [ ] Invalid and ambiguous observations return stable HTTP 400 problem
      responses without local details.
- [ ] Stale, conflicting, partial, and unsupported evidence remain successful
      typed result states where the request itself is valid.
- [ ] Cancellation propagates through catalogue resolution and recommendation
      generation.
- [ ] Existing requests without a target observation are backward compatible.
- [ ] JSON serialization never evaluates unavailable value getters.
- [ ] Controller and architecture tests enforce the information-only boundary.

## Slice 4: Manual UI

### E3-006 — Build the bilingual target-observation form

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E3-005

Add a manual-first observation surface to the recommendation page.

#### Acceptance criteria

- [ ] The form starts disabled and explains when target observation is useful.
- [ ] Hostile and story targets remain explicitly unavailable for manual
      current-screen loadout observation; the form never requests hidden data.
- [ ] Target identity and save freshness are visible before entry.
- [ ] The player chooses partial or complete coverage with an explanation of
      omission semantics.
- [ ] Skill search follows the active Traditional Chinese or English language.
- [ ] Ambiguous candidates show enough static detail for confirmation.
- [ ] Category is verified rather than freely typed.
- [ ] Direction can be omitted and appears only where E3-000 supports it.
- [ ] The review step shows resolved stable identities, coverage, time, and
      evidence status.
- [ ] Validation errors are field-specific and keyboard accessible.
- [ ] Applying an observation creates a new recommendation request; it does
      not control the game.
- [ ] Clearing returns to the save-only form and result.
- [ ] Initial, editing, ambiguous, stale, conflicting, unsupported, loading,
      applied, and cleared states work in both languages.
- [ ] Rendering tests cover semantic labels, focusable controls, status text,
      and no color-only meaning.

## Slice 5: Recommendation integration

### E3-007 — Use confirmed target loadout evidence in threat analysis

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E3-004, E3-005

Feed the merged target snapshot into the existing versioned threat analyzer
without weakening rule verification.

#### Acceptance criteria

- [ ] Confirmed equipped skills are analyzed before learned-but-unconfirmed
      skills.
- [ ] A partial observation cannot suppress possible learned threats.
- [ ] A valid complete observation can remove stale equipped membership from
      current analysis while preserving conflict evidence.
- [ ] Observed direction affects a rule only when available and version-matched.
- [ ] Unknown effects remain warnings and cannot acquire severity or score.
- [ ] Every threat records whether its source is save, observed equipped,
      learned-unconfirmed, or verified rule evidence.
- [ ] Recommendations without observations remain byte-for-byte equivalent at
      the contract level, excluding newly added empty metadata fields.
- [ ] Deterministic tests cover added, removed, unchanged, and unsupported
      threats.

### E3-008 — Recalculate recommendations from observed threats

**Status:** Planned

**Priority:** P1

**Estimate:** M

**Dependencies:** E3-007

Run feasibility, counter selection, scoring, and explanation against the
observation-enhanced threat set.

#### Acceptance criteria

- [ ] Hard constraints are still evaluated before scoring.
- [ ] Only typed verified threats and counters affect recommendation choice.
- [ ] Added or removed recommendations are deterministic for the same evidence.
- [ ] A target observation cannot make an unavailable player skill feasible.
- [ ] All recommendation styles preserve their documented policy weights.
- [ ] Unresolved target evidence remains visible and cannot be converted into
      a favorable assumption.
- [ ] Tests cover a recommendation that changes and one that correctly remains
      unchanged.

## Slice 6: Impact explanation

### E3-009 — Explain observation impact

**Status:** Planned

**Priority:** P1

**Estimate:** L

**Dependencies:** E3-007, E3-008

Present the difference between save-only and observation-enhanced analysis.

#### Acceptance criteria

- [ ] The result lists threats confirmed, added, demoted, removed, unchanged,
      and still unsupported.
- [ ] It lists recommended skills/counters added or removed and explains the
      evidence chain for each change.
- [ ] Feasibility changes are separated from scoring changes.
- [ ] Partial coverage displays an explicit remaining-unknown warning.
- [ ] Conflicts show both sources, timestamps, and the applied precedence rule.
- [ ] Evidence confidence is never phrased as win probability.
- [ ] Save-only and observed results can be compared without raw diagnostic
      text.
- [ ] API and bilingual rendering tests cover all impact categories.

## Slice 7: Verification and completion

### E3-010 — Verify observation safety and determinism

**Status:** Planned

**Priority:** P1

**Estimate:** L

**Dependencies:** E3-005, E3-006, E3-007, E3-008, E3-009

Run the full cross-layer and local read-only verification matrix.

#### Acceptance criteria

- [ ] Domain, Application, Infrastructure, API, Presentation, integration, and
      architecture tests pass.
- [ ] Repeating the same save/catalogue/observation input produces equivalent
      snapshots, threats, recommendations, impacts, and ordering.
- [ ] Clearing the observation reproduces the save-only result.
- [ ] Current catalogue rebuild/cache behavior remains independent from
      session observation state.
- [ ] Architecture tests reject file paths, screenshot capture, process access,
      game control, mutation APIs, and observation-history persistence.
- [ ] Before/after SHA-256 fingerprints prove every inspected save and game
      source is unchanged.
- [ ] Manual bilingual verification matches the E3-000 evidence scenario.
- [ ] Test commands, results, expected opt-in skips, and source versions are
      documented.

#### Evidence when complete

- `docs/reviews/E3-010-automated-verification.md`.
- Current-save observation vertical result.
- Final automated suite summary.

### E3-011 — Validate the workflow and close Epic 3

**Status:** Planned

**Priority:** P1

**Estimate:** M

**Dependencies:** E3-000 through E3-010

Compare the completed workflow with the recorded target UI, audit every Epic 3
criterion, and record the product-owner decision.

#### Acceptance criteria

- [ ] The manual form can reproduce the representative E3-000 observation.
- [ ] Resolved bilingual skill identities match the recorded target UI.
- [ ] Partial/complete semantics agree with the verified screen behavior.
- [ ] Threat and recommendation changes are evidence-backed and explainable.
- [ ] Clearing returns to the expected save-only result.
- [ ] All Epic 3 acceptance criteria have linked implementation or evidence.
- [ ] Deferred screenshot assistance and observation history remain explicit
      future work rather than hidden partial implementations.
- [ ] The product owner records the Epic 3 completion decision.

#### Evidence when complete

- `docs/reviews/E3-011-manual-verification.md`.
- Updated status and completion decision in [EPIC-003](./EPIC.md).

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
