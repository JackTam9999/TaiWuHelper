# Epic 5 backlog: Target archetypes and counter playbooks

This backlog implements [EPIC-005](./EPIC.md) while preserving the permanent
safety boundary in
[ADR-0001](../../architecture/ADR-0001-absolute-game-non-interference.md).

## Conventions

### Priority

- **P0:** Required for the first trustworthy archetype-to-recommendation
  vertical.
- **P1:** Required for Epic 5 completion.
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
- use typed, version-matched evidence before a facet, archetype, threat, or
  counter can affect a recommendation;
- keep weapon or attack-family context separate from damage, defense, control,
  and tempo claims;
- preserve immutable collections, stable identities, deterministic ordering,
  unavailable reasons, conflicts, provenance, and diagnostics;
- reuse authoritative threat, counter, requirement, feasibility, candidate,
  scoring, manual-plan, and comparison semantics where applicable;
- reject inference from localized names, category alone, or untyped raw effect
  descriptions;
- distinguish evidence completeness from win probability;
- expose bilingual and accessible states without relying on color alone;
- leave every save, game file, configuration value, running process, runtime
  memory location, and in-game state unchanged;
- introduce no game hook, injection, patch, automation, screenshot capture,
  file upload, persistence, or input-control capability;
- update architecture, API, UI, testing, and roadmap evidence where the
  contract changes; and
- record the relevant verification command and result.

## Delivery order

| Order | Slice | Outcome |
|---:|---|---|
| 0 | Evidence boundary | Candidate profile fields, thresholds, and representative targets are verified before coding |
| 1 | Profile and match contracts | Multi-label facts, evidence, rules, and result states are immutable and typed |
| 2 | Profile extraction and matching | Targets produce deterministic profile facets and independent archetype matches |
| 3 | Counter playbooks | Reusable response goals reference only verified threats, effects, counters, and requirements |
| 4 | Composition and recommendation | Overlapping playbooks compose and adapt to the exact target and player |
| 5 | API vertical | Typed profile, playbook, adjustment, and diagnostic facts reach clients |
| 6 | Core UI | The recommendation page explains archetypes and reusable strategy compactly |
| 7 | Exact-target explanation | Players can distinguish baseline playbooks from target-specific adjustments and gaps |
| 8 | Verification and completion | Reuse, safety, determinism, representative targets, and product acceptance close the epic |

## Slice 0: Evidence boundary

### E5-000 — Verify target-profile signals and select the representative matrix

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** Epic 1, Epic 2, Epic 3, Epic 4

Inspect the minimum read-only save and version-matched GameData sources needed
to distinguish target attack context, pressure, resilience, control, and tempo.
Document exact semantics before adding Domain profile or matching contracts.

#### Acceptance criteria

- [x] The inspected Taiwu and GameData versions are recorded.
- [x] Every candidate source field records its owning source, type, unit,
      availability, completeness, and source-of-truth precedence.
- [x] Weapon and attack-family facts are documented independently from damage,
      penetration, defense, poison, mind, recovery, and tempo mechanics.
- [x] Every proposed `High` or `Low` classification has a documented threshold,
      comparison population, or exact mechanic definition.
- [x] Skill names, target names, localized labels, and untyped raw descriptions
      are explicitly rejected as mechanical evidence.
- [x] The current magic-sound, distraction, resonance, and defeat-reset target
      is recorded as the baseline playbook verification case.
- [x] Candidate local or synthetic representatives are identified for physical
      offense, physical resilience or attrition, and poison or an evidence-
      approved replacement family.
- [x] The final delivery matrix contains the existing baseline plus three
      newly verifiable families; unsupported candidates remain explicit.
- [x] The evidence matrix distinguishes fields readable from a stable save,
      installed configuration, current-screen observation, and unavailable
      runtime-only state.
- [x] Before/after fingerprints prove all inspected save, GameData, language,
      and other game-owned sources remain byte-for-byte unchanged.
- [x] The product contract is updated if the verified delivery families differ
      from the candidates in EPIC-005.

#### Evidence when complete

- `docs/scenarios/E5-000-target-archetype-evidence.md`.
- `docs/architecture/TARGET-COMBAT-PROFILE.md`.
- A source-field and representative-target matrix with no proprietary data or
  local paths committed.
- Recorded read-only fingerprint results and discovery commands.

#### Completion evidence

- The evidence gate selected exact outer-damage presence, positive channel-
  resistance asymmetry, and poison-application presence alongside the existing
  magic-sound/reset baseline. No first-delivery rule uses `High` or `Low`.
- The source matrix records saved positive membership, installed static
  configuration, E3-000 current-screen evidence, base-only character values,
  unsafe live-runtime paths, field types, raw-unit boundaries, completeness,
  and precedence.
- A rejected learned-skill scan proved why learned membership cannot mean
  active. The corrected equipped-only discovery found opaque local and
  synthetic representatives without committing character identity or save
  content.
- The current-save guarded vertical passed 1/1 with save, runtime, and installed
  language-source fingerprints unchanged. The equipped-only discovery also
  reported the save fingerprint unchanged across 8,775 candidates.
- `EPIC.md` now records the corrected final delivery boundary; unsupported
  high/low, repeated-hit, penetration, recovery, avoidance, and tempo claims
  remain explicit.

## Slice 1: Profile and match contracts

### E5-001 — Add immutable target combat-profile contracts

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E5-000

Define presentation-neutral Domain values for profile dimensions, facets,
evidence, unavailable states, conflicts, diagnostics, and deterministic profile
identity.

#### Acceptance criteria

- [x] Profile dimensions separately represent attack family, pressure,
      resilience, control, and tempo.
- [x] Each facet has a stable non-localized identity, typed value, evidence
      state, provenance, source version, and optional unavailable reason.
- [x] Confirmed, incomplete, unsupported, and conflicting evidence states have
      explicit invariants.
- [x] Missing evidence cannot construct a confirmed facet or zero value.
- [x] Collections are copied into immutable values with stable ordering.
- [x] Blank codes, duplicate facets, invalid enum values, incompatible values,
      blank evidence, and invalid versions fail construction.
- [x] A profile fingerprint is derived from stable facts and excludes display
      text, local paths, timestamps that do not affect semantics, and mutable
      references.
- [x] Domain types have no Application, Infrastructure, Presentation,
      persistence, filesystem, process, or GameData dependencies.
- [x] Unit tests cover valid, empty, duplicate, unavailable, conflicting, and
      deterministic-fingerprint cases.

#### Evidence when complete

- `src/TaiWu.Domain/TargetProfiles/` immutable contracts.
- `tests/TaiWu.Domain.UnitTests/TargetProfiles/` invariant tests.
- Updated `docs/architecture/TARGET-COMBAT-PROFILE.md`.

#### Completion evidence

- `TargetCombatProfile` owns canonical immutable facets and diagnostics for one
  target and profile-rule version. The five independent dimensions are part of
  the Domain vocabulary.
- Confirmed facets require a compatible typed presence or positive-measurement
  value plus evidence. Incomplete and unsupported facets have no authoritative
  value; conflicting facets retain at least two distinct typed candidates with
  their own evidence.
- Stable tokens reject blanks, localized/path-shaped identity values, and
  invalid versions. Duplicate or incompatible values fail construction.
- The length-prefixed canonical fingerprint includes stable semantic facts and
  excludes optional unavailable detail, display text, paths, timestamps, and
  caller-owned mutable collections.
- Domain unit tests: **337 passed, 0 failed, 0 skipped**. Architecture tests:
  **79 passed, 0 failed, 0 skipped**.
- `dotnet build TaiWu.slnx -c Release --no-restore` completed with zero
  warnings and zero errors. `dotnet test TaiWu.slnx -c Release --no-build
  --no-restore` passed **952 total: 943 passed, 0 failed, 9 expected opt-in
  integration skips**.

### E5-002 — Define versioned multi-label archetype rules and match states

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E5-001

Define archetype rules independently from target identity and model every
match outcome without turning incomplete evidence into `NotMatched`.

#### Acceptance criteria

- [x] An archetype definition has a stable identity, version, localized-title
      key, required facet predicates, optional supporting predicates, explicit
      exclusions, and evidence references.
- [x] Definitions contain no target character ID, localized matching string,
      raw GameData object, or fixed recommended loadout.
- [x] One profile may be evaluated against every applicable definition and may
      return multiple matched archetypes.
- [x] Matched, partial, not-matched, unsupported, and conflicting results have
      explicit construction rules.
- [x] `NotMatched` requires sufficient contrary evidence and cannot result
      only from an unavailable facet.
- [x] Match results retain supporting, missing, excluding, and conflicting
      facet references.
- [x] Definitions and matches have deterministic stable keys and ordering.
- [x] Unit tests prove that one profile can multi-match and one archetype can
      match multiple synthetic target profiles.

#### Evidence when complete

- Domain archetype-definition, predicate, match, and diagnostic contracts.
- Domain tests for every match state and multi-label invariant.
- `docs/architecture/TARGET-ARCHETYPE-MATCHING.md`.

#### Completion evidence

- Versioned definitions own a stable archetype identity, exact applicable
  profile-rule version, localized-title resource key, required and optional
  predicates, explicit exclusions, and evidence references. Their contract has
  no target identity, localized match text, GameData object, or loadout.
- `FacetConfirmed` and typed `ValueEquals` predicates evaluate immutable profile
  facets. Missing, incomplete, unsupported, conflicting, and contradicted facts
  remain distinct.
- Every supplied definition is evaluated independently. Results retain
  canonical supporting, missing, excluding, and conflicting facet references
  plus typed diagnostics.
- Confirmed required-value contradiction or a confirmed explicit exclusion is
  required for `NotMatched`. Unknown exclusions remain partial; unavailable
  requirements remain unsupported or partial.
- Domain tests cover every match state, multi-match, one rule across multiple
  targets, exclusions, version mismatch, deterministic ordering, and stable
  keys.
- Domain unit tests passed **357/357**. `dotnet build TaiWu.slnx -c Release
  --no-restore` completed with zero warnings and zero errors; the full no-build
  solution run passed **972 total: 963 passed, 0 failed, 9 expected opt-in
  integration skips**.

## Slice 2: Profile extraction and matching

### E5-003 — Build deterministic profile extraction and archetype matching

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E5-000, E5-001, E5-002

Build the pure services that normalize a combat snapshot and its accepted
observations into a target combat profile, then evaluate the version-compatible
archetype catalogue independently and deterministically.

#### Acceptance criteria

- [x] Extraction consumes one immutable snapshot, current accepted
      observations, versioned profile rules, and typed existing threat facts.
- [x] Every emitted facet links to the exact typed evidence that established
      it.
- [x] Source precedence agrees with Epic 3 and preserves stale or conflicting
      evidence as diagnostics.
- [x] Weapon or attack-family evidence never emits a damage, defense, poison,
      mind, or tempo facet by implication.
- [x] Version mismatch produces typed unsupported results and no partial use of
      nearby rules.
- [x] Arbitrary high/low thresholds, localized string matching, and raw effect
      interpretation are impossible through the production API.
- [x] Every applicable archetype definition is evaluated; evaluation does not
      stop after the first match.
- [x] Facets, matches, and diagnostics use stable documented ordering.
- [x] Repeated identical extraction and matching produce equivalent
      fingerprints and results.
- [x] Applying the same observation repeatedly is idempotent; clearing it
      reproduces the save-only profile and matches.
- [x] Tests cover all evidence and match states, multi-match, rule reordering,
      version mismatch, observation apply/clear, and determinism.

#### Evidence when complete

- Application or Domain profile extraction and matcher services at the layer
  selected by the architecture design.
- Focused Domain/Application tests using synthetic source facts.
- `docs/architecture/TARGET-ARCHETYPE-MATCHING.md` implementation notes.

#### Completion evidence

- The snapshot now carries optional version-matched configured outer-damage
  and poison-presence flags, positive weapon subtype, and positive base channel
  resistance. Missing source data remains unavailable, never false or zero.
- `E5.PROFILE.1` extracts only the E5-000 exact facets. Current-screen evidence
  binds active skills before positive saved membership; learned-only skills and
  learned-only threat sources cannot confirm a facet.
- Existing typed mind-damage, distraction, resonance, and reset threats map to
  independent facets without reading names or raw descriptions. Weapon subtype
  emits only `AttackFamily` context.
- The extractor consumes the already merged Epic 3 snapshot and retains stale,
  partial, unsupported, precedence, and save-conflict warnings as profile
  diagnostics. Reapply and clear tests prove deterministic replacement.
- `TargetCombatProfileAnalyzer` performs threat analysis, extraction, and every
  supplied archetype match in one pure immutable flow. Version mismatch emits
  an empty diagnostic profile and no nearby-rule facts.
- Domain unit tests passed **374/374**. The solution build completed with zero
  warnings and errors; the full synthetic/default suite passed **989 total:
  980 passed, 0 failed, 9 expected opt-in integration skips**.
- The focused guarded current-save vertical passed **1/1** in about 29 seconds,
  confirmed mapped configured mechanics and save-only profile repeatability,
  and left every inspected save, runtime, and language fingerprint unchanged.

## Slice 3: Counter playbooks

### E5-004 — Define the verified counter-playbook catalogue

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E5-000, E5-002, Epic 1 threat and counter contracts

Represent reusable archetype responses as ordered goals and verified options.
Deliver the baseline plus the three evidence-approved playbook families without
hard-coding a universal loadout.

#### Acceptance criteria

- [x] A playbook has a stable archetype/version identity, ordered response
      goals, priority, timing, conflict groups, evidence, and known gaps.
- [x] Every mechanical goal references typed profile facets or existing typed
      threats.
- [x] Every counter or mitigation option references an existing verified
      effect and `CombatCounterRule` or a separately reviewed typed rule.
- [x] Raw descriptions and display names may support evidence display but
      cannot create a playable option.
- [x] A playbook never contains a target character ID or a fixed complete
      loadout.
- [x] The baseline magic-sound/mind playbook preserves all currently verified
      threat and counter semantics.
- [x] Three newly evidence-approved playbook families are versioned and tested.
- [x] Missing or inaccessible response options remain explicit gaps.
- [x] Playbook goal and option ordering is deterministic and independent of
      source declaration order.
- [x] Unit tests cover construction invariants, all delivered families,
      unsupported versions, gaps, and deterministic ordering.

#### Evidence when complete

- Domain counter-playbook contracts and versioned catalogue.
- Focused tests linking playbook entries to verified threat/counter/effect
  identities.
- `docs/architecture/TARGET-COUNTER-PLAYBOOKS.md`.

#### Completion evidence

- `TaiWu.Domain.TargetPlaybooks` provides immutable identities, goals,
  verified options, typed gaps, exact-version resolution, deterministic
  ordering, and the initial versioned catalogue.
- The catalogue registers the reusable mind/resonance baseline, an independent
  defeat-mark reset overlay, and the three E5-000 families. E5-011 superseded
  the initial gap-only delivery with exact reviewed options for configured
  outer damage, channel resistance asymmetry, and configured poison.
- The mind/resonance baseline retains the verified mind, distraction, and
  resonance semantics. The separate reset overlay retains the existing
  strength, direction, activation timing, effect identity, requirements,
  source evidence, and non-guaranteed reset-lockout caveat without preventing
  broader reuse of the mind counters.
- Focused Domain verification on 2026-08-10: 385 passed, 0 failed, 0 skipped.
- Full release verification on 2026-08-10: 1,000 total, 991 passed,
  0 failed, and 9 expected opt-in integration skips.
- Architecture and catalogue rationale are recorded in
  [TARGET-COUNTER-PLAYBOOKS.md](../../architecture/TARGET-COUNTER-PLAYBOOKS.md).

## Slice 4: Composition and recommendation

### E5-005 — Compose overlapping playbooks and apply exact-target adjustments

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E5-003, E5-004

Compose every matched playbook into one reusable strategy, surface real
conflicts, and create typed adjustments from exact target evidence before final
player personalization.

#### Acceptance criteria

- [x] Shared response goals, threat references, and identical counter options
      deduplicate by stable identity.
- [x] Priority and activation timing resolve only according to documented
      composition rules.
- [x] Incompatible active roles, requirements, timing, or capacity demands
      remain explicit composition conflicts.
- [x] Partial, unsupported, and conflicting archetype matches cannot silently
      contribute a confirmed mechanical goal.
- [x] Exact target threats, skills, effects, equipment, observations, and gaps
      can retain, elevate, reduce, add, replace, or leave unresolved a response
      with a typed reason.
- [x] A broad archetype cannot override contrary exact-target evidence.
- [x] Composition order is deterministic for reordered equivalent inputs.
- [x] Tests cover overlapping coverage, priority, timing, true conflicts,
      exact-target overrides, unsupported matches, and stable diagnostics.

#### Evidence when complete

- Pure playbook composer and target-adjustment contracts/services.
- Domain/Application tests for the full composition state matrix.
- `docs/architecture/TARGET-PLAYBOOK-COMPOSITION.md`.

#### Completion evidence

- `TargetPlaybookComposer` admits only `Matched` archetypes, resolves exact-
  version playbooks, merges shared goals, and globally deduplicates threats and
  verified counter options while retaining source references.
- Strongest priority and earliest response timing use one documented ordering.
  Reviewed conflict groups preserve active-role, timing, requirement, and
  capacity conflicts; the composer does not choose a side.
- `TargetSpecificPlaybookAdjuster` requires the exact profile fingerprint and
  match-set key, extracts typed facet/threat/skill/effect/equipment/
  observation/gap/match evidence, and emits retained, elevated, reduced,
  added, replaced, or unresolved decisions with typed reasons.
- Missing, wrong-state, missing-response, and shadowed reviewed rules remain
  deterministic diagnostics. Explicit contrary evidence overrides the broad
  automatic decision; absence alone never reduces it.
- Focused Domain verification on 2026-08-10: 412 passed, 0 failed, 0 skipped.
- Full release verification on 2026-08-10: 1,027 total, 1,018 passed,
  0 failed, and 9 expected opt-in integration skips.
- Composition and adjustment rules are recorded in
  [TARGET-PLAYBOOK-COMPOSITION.md](../../architecture/TARGET-PLAYBOOK-COMPOSITION.md).

### E5-006 — Personalize playbooks through the existing recommendation engine

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E5-005, Epic 1 recommendation pipeline

Supply composed, target-adjusted response options to the existing candidate,
feasibility, scoring, explanation, manual-plan, and comparison pipeline without
creating a parallel loadout engine.

#### Acceptance criteria

- [x] Only matched and exact-target-confirmed mechanical goals can affect
      candidate construction or scoring.
- [x] Final options still pass ownership, direction, raw-effect, requirement,
      capacity, generic-slot, backlash, and active-role hard filters.
- [x] Existing bounded search limits and truncation diagnostics remain intact.
- [x] Inaccessible counters produce an unresolved gap and are not replaced by
      a name-similar or lower-ranked unverified 功法.
- [x] Existing policy-score meanings and deterministic tie-breakers remain
      unchanged unless a separately documented rule change is approved.
- [x] The manual plan, tactical explanation, and Epic 4 comparison agree with
      the selected feasible loadout.
- [x] Applying or clearing an observation atomically replaces the profile,
      matches, playbooks, adjustments, recommendation, and comparison.
- [x] The save-only result is reproducible after clearing observations.
- [x] Tests prove feasibility rejection, accessible and inaccessible counters,
      target adjustment, deterministic ranking, manual-plan parity, and
      observation lifecycle behavior.

#### Evidence when complete

- Updated recommendation orchestration and option-building integration.
- Application and Domain regression tests for all delivered playbooks.
- Updated candidate-generation and recommendation architecture documentation.

#### Completion evidence

- `TargetPlaybookRecommendationPersonalizer` derives the profile, match set,
  matched-playbook composition, exact-target adjustments, eligible verified
  options, and player-access evaluations from the recommendation snapshot.
- `CombatLoadoutRecommendation.TargetPlaybook` retains eligible goals, exact
  counter availability, catalogue gaps, and player-specific inaccessible or
  infeasible gaps with the recommendation that they produced.
- The existing generator, feasibility validator, policy scorer, explanation,
  manual plan, and Epic 4 comparison remain the only recommendation path;
  their bounds, score meanings, and deterministic ordering are unchanged.
- Observation tests prove apply/repeat/clear replacement across profile,
  composition, adjustments, recommendation, and comparison fingerprints.
- Release build on 2026-08-10: succeeded with 0 warnings and 0 errors.
- Full release verification on 2026-08-10: 1,030 total, 1,021 passed,
  0 failed, and 9 expected opt-in integration skips.
- Design and safety boundaries are recorded in
  [TARGET-PLAYBOOK-PERSONALIZATION.md](../../architecture/TARGET-PLAYBOOK-PERSONALIZATION.md).

## Slice 5: API vertical

### E5-007 — Expose typed target-profile and playbook contracts

**Status:** Complete

**Priority:** P1

**Estimate:** M

**Dependencies:** E5-003, E5-005, E5-006

Map the immutable Epic 5 result into public response contracts without asking
clients to reclassify targets or compose playbooks from display strings.

#### Acceptance criteria

- [x] The API exposes profile dimensions, typed values, evidence states,
      provenance, unavailable reasons, and diagnostics.
- [x] Archetype results expose stable identities, match state, supporting,
      missing, excluding, and conflicting facet references.
- [x] Playbooks expose response goals, threat/counter references, timing,
      requirements, known gaps, and composition conflicts.
- [x] Target-specific adjustments expose stable kinds and reasons.
- [x] Response ordering matches Domain/Application ordering.
- [x] Localized text is display-only and stable identities remain
      language-neutral.
- [x] Contracts expose no save path, game path, screenshot path, raw
      proprietary payload, process identifier, persistence command, or
      mutation-capable game type.
- [x] Mapper tests cover every evidence, match, conflict, gap, and adjustment
      state in Traditional Chinese and English where text is projected.
- [x] API documentation includes complete, partial, unsupported, conflicting,
      multi-match, and adjusted examples.

#### Evidence when complete

- Epic 5 response contracts and pure mappers under `TaiWuAPI/Contracts`.
- API and mapper tests.
- Updated `docs/api/COMBAT-RECOMMENDATIONS.md` and an Epic 5 API design note.

#### Completion evidence

- `CombatRecommendationResponse.TargetStrategy` additively projects the
  immutable profile, all multi-label match states, deterministic composition,
  exact-target adjustments, and player-specific counter availability.
- Typed contracts retain facet values and provenance, match support/missing/
  exclusion/conflict references, all six combat requirement shapes, playbook
  gaps/conflicts, all six adjustment actions, and feasibility/access states.
- English and Traditional Chinese mappers change display text only; stable
  codes, versions, fingerprints, enums, references, and ordering are shared.
- Mapper tests cover confirmed/incomplete/unsupported/conflicting profiles,
  matched/not-matched/partial/unsupported/conflicting archetypes, multi-match
  composition, player gaps, every adjustment action, and every adjustment
  evidence kind/state.
- Architecture verification rejects path, screenshot, process, payload,
  persistence-command, GameData, snapshot, infrastructure, and mutation-
  capable signatures in target response contracts.
- Release build on 2026-08-10: succeeded with 0 warnings and 0 errors.
- Full release verification on 2026-08-10: 1,035 total, 1,026 passed,
  0 failed, and 9 expected opt-in integration skips.
- Contract and state examples are recorded in
  [TARGET-STRATEGY-API.md](../../architecture/TARGET-STRATEGY-API.md) and
  [COMBAT-RECOMMENDATIONS.md](../../api/COMBAT-RECOMMENDATIONS.md).

## Slice 6: Core UI

### E5-008 — Add a compact bilingual archetype and strategy section

**Status:** Complete

**Priority:** P1

**Estimate:** L

**Dependencies:** E5-007

Add one progressive-disclosure section to the existing recommendation page so
the player can understand the matched archetypes and reusable strategy without
duplicating target threats, loadout cards, manual changes, or the comparison
matrix.

#### Acceptance criteria

- [x] Dominant matched archetypes appear before partial, unsupported, or
      conflicting results.
- [x] Multiple matches are grouped as one target profile rather than separate
      recommendation results.
- [x] Attack-family context is visually distinct from verified pressure,
      resilience, control, and tempo mechanics.
- [x] Every match exposes concise evidence and freshness without dumping raw
      diagnostics into the primary view.
- [x] Reusable response goals link to existing threat, counter, requirement,
      and evidence detail where available.
- [x] Inaccessible counters and unresolved goals remain visible.
- [x] The section does not add a new recommendation-policy control or repeat
      the complete loadout comparison.
- [x] Loading, no-match, multi-match, partial, unsupported, conflicting,
      available, inaccessible-counter, and failure states are rendered.
- [x] Traditional Chinese and English copy is complete and stable identities
      are never shown as untranslated raw codes when display text exists.
- [x] Desktop and narrow layouts expose equivalent facts with no horizontal
      overflow.
- [x] Native headings, lists, buttons, and disclosures provide logical
      keyboard and screen-reader navigation; state never relies on color alone.
- [x] Component tests cover the complete state matrix and duplicate-element
      regression guards.

#### Evidence when complete

- Epic 5 Presentation view models, mapper, localization, Razor components, and
  styles.
- Component-rendering and localization tests.
- `docs/roadmap/epic-005/UI-005-target-archetype-strategy.md`.
- Release verification: build succeeded with zero warnings and errors; 1,043
  tests total, 1,034 passed, and 9 guarded local-integration tests skipped as
  expected.

## Slice 7: Exact-target explanation

### E5-009 — Explain target-specific adjustments and unresolved gaps

**Status:** Complete

**Priority:** P1

**Estimate:** M

**Dependencies:** E5-005, E5-006, E5-008

Explain how the final selected target changed the reusable playbook and why the
player's accessible 功法 produced the final recommendation.

#### Acceptance criteria

- [x] Retained, elevated, reduced, added, replaced, and unresolved adjustment
      kinds have concise bilingual explanations.
- [x] Every adjustment links the relevant archetype goal, exact target fact,
      threat, counter candidate, feasibility result, or missing evidence.
- [x] The explanation distinguishes target customization from player
      feasibility filtering.
- [x] A missing counter does not read as a completed mitigation.
- [x] A reduced broad risk does not erase exact evidence or historical source
      conflicts.
- [x] The UI identifies when the final recommendation is unchanged because the
      current loadout already satisfies the composed response.
- [x] Observation apply/clear updates the explanation together with every
      other Epic 5 result.
- [x] Presentation does not restate full skill cards, warning lists, manual
      checklist items, or comparison rows.
- [x] Mapper and component tests cover each adjustment kind, unchanged result,
      missing counter, and observation lifecycle state.

#### Evidence when complete

- Adjustment explanation mapping and compact Presentation component updates.
- Focused mapper and rendering tests.
- Updated UI and playbook-composition documentation.
- Release verification: build succeeded with zero warnings and errors; 1,047
  tests total, 1,038 passed, and 9 guarded local-integration tests skipped as
  expected.

## Slice 8: Verification and completion

### E5-010 — Verify archetype reuse, safety, and determinism

**Status:** Remediated — awaiting product-owner decision

**Priority:** P1

**Estimate:** L

**Dependencies:** E5-000 through E5-009

Run the full automated and guarded local verification matrix, audit every Epic
5 criterion, and record the product-owner completion decision.

#### Acceptance criteria

- [x] Domain tests cover profile, evidence, definition, match, playbook,
      composition, adjustment, conflict, and deterministic-order invariants.
- [x] Synthetic cases prove one target can multi-match and one playbook family
      can apply to multiple targets without target-ID rules.
- [x] Application tests prove exact-target adjustment, player feasibility,
      recommendation parity, and observation apply/clear behavior.
- [x] API tests prove typed unavailable, partial, unsupported, conflicting,
      multi-match, playbook-gap, and adjustment states survive mapping.
- [x] Presentation tests cover bilingual desktop and narrow workflows,
      keyboard semantics, non-color states, and duplicate-element guards.
- [x] Architecture tests prevent localized or raw-text mechanical matching,
      unbounded alternative engines, file/process/screenshot access,
      persistence, game control, and mutation-capable dependencies.
- [x] The mind/resonance baseline, reset overlay, and all three newly verified
      playbook families pass their documented synthetic verification matrix.
- [x] Guarded local verification exercises every representative family
      available in the current save and records unsupported local cases
      honestly.
- [x] Repeated identical runs produce equivalent profile, match, playbook,
      adjustment, recommendation, comparison, and diagnostic fingerprints.
- [x] Applying the same observation repeatedly is idempotent and clearing it
      reproduces the save-only result.
- [x] All inspected save, GameData, language, and other game-owned source
      fingerprints remain unchanged.
- [x] Release build, default test matrix, formatting, and diff checks pass.
- [x] Every Epic 5 acceptance criterion links to implementation or evidence.
- [x] Deferred clustering, persistence, screenshot assistance, outcome
      learning, broader target coverage, companions, village, and library work
      remain explicit future work.
- [ ] The product owner records the Epic 5 completion decision.

#### Evidence when complete

- `docs/reviews/E5-010-automated-verification.md`.
- `docs/reviews/E5-010-manual-verification.md`.
- Updated completion decision in [EPIC-005](./EPIC.md).

#### Completion evidence

- Completion-audit Release build: zero warnings and zero errors.
- Default Release matrix: 1,058 total; 1,049 passed; 0 failed; 9 expected
  opt-in integration skips.
- Focused layers: Domain 421/421, Application 139/139, Infrastructure unit
  132/132, API/Presentation 276/276, and Architecture 80/80.
- Guarded current-save vertical: 1 passed, 0 failed, 0 skipped; every
  registered family evaluated and every inspected source unchanged.
- The rebuilt Traditional Chinese desktop workflow exposed the verified outer
  counter in one compact strategy panel with no document overflow. The earlier
  English/Traditional Chinese 390 by 844 layout matrix remains valid.
- The original technical completion claim was reopened by independent review
  because the three new families were gap-only. E5-011 records the completed
  remediation. E5-012 records the completion refactor and final code audit;
  the product-owner decision remains open.

### E5-011 — Deliver playable family counters and reusable overlays

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E5-004, E5-005, E5-010 independent review

Close the product-value gaps found by the independent playbook-family review:
give each new family at least one exact, typed counter path; separate the
defeat-reset requirement from the reusable mind/resonance response; and make
exact channel-resistance evidence affect production recommendations.

#### Acceptance criteria

- [x] Configured outer damage references the reviewed reverse 伏龍刀法
      all-摧破 power reduction effect.
- [x] Configured poison references both reviewed 五黃辟毒術 directions with
      their active-defense requirement.
- [x] Channel-resistance asymmetry references both reviewed
      錯倒陰陽拂塵 directions and selects the route that attacks the lower
      resistance channel.
- [x] Mind/resonance counters do not require defeat-mark reset evidence; reset
      is an independent composable overlay.
- [x] Production uses reviewed exact-target replacement rules.
- [x] Rules requiring several evidence identities require the requested state
      on every identity.
- [x] Opposite directions of one skill remain visible to feasibility while the
      loadout generator deterministically selects one direction per skill.
- [x] The recommendation threat inventory includes every eligible playbook
      threat, so comparison/API/UI mapping cannot lose a newly covered family.
- [x] A current-player test proves an owned reverse 五黃辟毒術 path flows from
      poison evidence through matching, playbook composition, feasibility, and
      bounded recommendation generation.
- [x] Catalogue, composition, adjustment, Application, API, guarded read-only,
      and compact UI regression checks pass.

#### Completion evidence

- Exact effect and counter entries are version-gated in the Domain catalogue;
  names and raw descriptions remain display evidence only.
- Read-only catalogue inspection confirmed the reviewed mechanics and costs.
  Read-only current-save inspection confirmed reverse 五黃辟毒術 is learned
  and breakthrough-active; machine paths, fingerprints, and save content are
  not committed.
- [E5-011 remediation review](../../reviews/E5-011-playbook-remediation.md).
- Remediated Release verification: 1,053 total; 1,044 passed; 0 failed;
  9 expected opt-in skips; guarded current-save vertical 1/1.
- A screenshot-reported real-save poison-family crash was reproduced and
  traced to a missing typed comparison threat. The complete threat inventory
  and a poison-family Presentation regression test close that path without
  recording the target identity.

### E5-012 — Scope shared counters to the selected target threats

**Status:** Complete

**Priority:** P0

**Estimate:** S

**Dependencies:** E5-006, E5-007, E5-011 completion review

Close the cross-family correctness gap found during the final Epic 5 audit. A
verified counter rule may address several threats, but one matched family must
not claim threats that exist only in another family.

#### Acceptance criteria

- [x] A composed counter derives contextual threat coverage from the selected
      source goals and the rule's verified threat capabilities.
- [x] Recommendation candidates carry only threats from currently eligible
      source goals.
- [x] A verified-rule option rejects empty contextual coverage and threats not
      owned by that rule.
- [x] Goal-level API options reference only threats exposed by their containing
      goal.
- [x] An outer-only target using shared reverse 伏龍刀法 claims only configured
      outer-damage coverage and emits no caveat for absent mind threats.
- [x] Domain, Application, API, formatting, build, and full Release tests pass.

#### Completion evidence

- `ComposedTargetCounterOption.ApplicableThreatCodes` is the shared Domain
  projection used by recommendation generation and API mapping.
- Domain tests cover selected-goal intersection plus invalid and empty verified
  scopes. Application and API regressions cover the outer-only candidate and
  goal-reference integrity.
- [E5-012 completion refactor review](../../reviews/E5-012-contextual-counter-scope.md).
- Release build: zero warnings and zero errors. Default matrix: 1,058 total;
  1,049 passed; 0 failed; 9 expected opt-in integration skips.
- `dotnet format TaiWu.slnx --no-restore --verify-no-changes` and
  `git diff --check` pass on the final worktree.

## Future work outside Epic 5

- Additional archetype families and verified target coverage beyond the first
  baseline plus three new playbooks.
- Statistical clustering, learned archetypes, win-probability models, or
  outcome-trained rules after separate evidence and governance decisions.
- Persisted profile, recommendation, or battle-outcome history.
- Screenshot-assisted target profiling after privacy and accuracy review.
- Arbitrary lower-ranked or constraint-driven alternative loadout exploration.
- Companion role selection and development from PI-008 and PI-009.
- Village workforce, building, library, and book planning from PI-010 and
  PI-011.
