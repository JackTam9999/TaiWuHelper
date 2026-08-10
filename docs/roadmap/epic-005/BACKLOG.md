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

**Status:** Planned

**Priority:** P0

**Estimate:** M

**Dependencies:** E5-001

Define archetype rules independently from target identity and model every
match outcome without turning incomplete evidence into `NotMatched`.

#### Acceptance criteria

- [ ] An archetype definition has a stable identity, version, localized-title
      key, required facet predicates, optional supporting predicates, explicit
      exclusions, and evidence references.
- [ ] Definitions contain no target character ID, localized matching string,
      raw GameData object, or fixed recommended loadout.
- [ ] One profile may be evaluated against every applicable definition and may
      return multiple matched archetypes.
- [ ] Matched, partial, not-matched, unsupported, and conflicting results have
      explicit construction rules.
- [ ] `NotMatched` requires sufficient contrary evidence and cannot result
      only from an unavailable facet.
- [ ] Match results retain supporting, missing, excluding, and conflicting
      facet references.
- [ ] Definitions and matches have deterministic stable keys and ordering.
- [ ] Unit tests prove that one profile can multi-match and one archetype can
      match multiple synthetic target profiles.

#### Evidence when complete

- Domain archetype-definition, predicate, match, and diagnostic contracts.
- Domain tests for every match state and multi-label invariant.
- `docs/architecture/TARGET-ARCHETYPE-MATCHING.md`.

## Slice 2: Profile extraction and matching

### E5-003 — Build deterministic profile extraction and archetype matching

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E5-000, E5-001, E5-002

Build the pure services that normalize a combat snapshot and its accepted
observations into a target combat profile, then evaluate the version-compatible
archetype catalogue independently and deterministically.

#### Acceptance criteria

- [ ] Extraction consumes one immutable snapshot, current accepted
      observations, versioned profile rules, and typed existing threat facts.
- [ ] Every emitted facet links to the exact typed evidence that established
      it.
- [ ] Source precedence agrees with Epic 3 and preserves stale or conflicting
      evidence as diagnostics.
- [ ] Weapon or attack-family evidence never emits a damage, defense, poison,
      mind, or tempo facet by implication.
- [ ] Version mismatch produces typed unsupported results and no partial use of
      nearby rules.
- [ ] Arbitrary high/low thresholds, localized string matching, and raw effect
      interpretation are impossible through the production API.
- [ ] Every applicable archetype definition is evaluated; evaluation does not
      stop after the first match.
- [ ] Facets, matches, and diagnostics use stable documented ordering.
- [ ] Repeated identical extraction and matching produce equivalent
      fingerprints and results.
- [ ] Applying the same observation repeatedly is idempotent; clearing it
      reproduces the save-only profile and matches.
- [ ] Tests cover all evidence and match states, multi-match, rule reordering,
      version mismatch, observation apply/clear, and determinism.

#### Evidence when complete

- Application or Domain profile extraction and matcher services at the layer
  selected by the architecture design.
- Focused Domain/Application tests using synthetic source facts.
- `docs/architecture/TARGET-ARCHETYPE-MATCHING.md` implementation notes.

## Slice 3: Counter playbooks

### E5-004 — Define the verified counter-playbook catalogue

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E5-000, E5-002, Epic 1 threat and counter contracts

Represent reusable archetype responses as ordered goals and verified options.
Deliver the baseline plus the three evidence-approved playbook families without
hard-coding a universal loadout.

#### Acceptance criteria

- [ ] A playbook has a stable archetype/version identity, ordered response
      goals, priority, timing, conflict groups, evidence, and known gaps.
- [ ] Every mechanical goal references typed profile facets or existing typed
      threats.
- [ ] Every counter or mitigation option references an existing verified
      effect and `CombatCounterRule` or a separately reviewed typed rule.
- [ ] Raw descriptions and display names may support evidence display but
      cannot create a playable option.
- [ ] A playbook never contains a target character ID or a fixed complete
      loadout.
- [ ] The baseline magic-sound/mind playbook preserves all currently verified
      threat and counter semantics.
- [ ] Three newly evidence-approved playbook families are versioned and tested.
- [ ] Missing or inaccessible response options remain explicit gaps.
- [ ] Playbook goal and option ordering is deterministic and independent of
      source declaration order.
- [ ] Unit tests cover construction invariants, all delivered families,
      unsupported versions, gaps, and deterministic ordering.

#### Evidence when complete

- Domain counter-playbook contracts and versioned catalogue.
- Focused tests linking playbook entries to verified threat/counter/effect
  identities.
- `docs/architecture/TARGET-COUNTER-PLAYBOOKS.md`.

## Slice 4: Composition and recommendation

### E5-005 — Compose overlapping playbooks and apply exact-target adjustments

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E5-003, E5-004

Compose every matched playbook into one reusable strategy, surface real
conflicts, and create typed adjustments from exact target evidence before final
player personalization.

#### Acceptance criteria

- [ ] Shared response goals, threat references, and identical counter options
      deduplicate by stable identity.
- [ ] Priority and activation timing resolve only according to documented
      composition rules.
- [ ] Incompatible active roles, requirements, timing, or capacity demands
      remain explicit composition conflicts.
- [ ] Partial, unsupported, and conflicting archetype matches cannot silently
      contribute a confirmed mechanical goal.
- [ ] Exact target threats, skills, effects, equipment, observations, and gaps
      can retain, elevate, reduce, add, replace, or leave unresolved a response
      with a typed reason.
- [ ] A broad archetype cannot override contrary exact-target evidence.
- [ ] Composition order is deterministic for reordered equivalent inputs.
- [ ] Tests cover overlapping coverage, priority, timing, true conflicts,
      exact-target overrides, unsupported matches, and stable diagnostics.

#### Evidence when complete

- Pure playbook composer and target-adjustment contracts/services.
- Domain/Application tests for the full composition state matrix.
- `docs/architecture/TARGET-PLAYBOOK-COMPOSITION.md`.

### E5-006 — Personalize playbooks through the existing recommendation engine

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E5-005, Epic 1 recommendation pipeline

Supply composed, target-adjusted response options to the existing candidate,
feasibility, scoring, explanation, manual-plan, and comparison pipeline without
creating a parallel loadout engine.

#### Acceptance criteria

- [ ] Only matched and exact-target-confirmed mechanical goals can affect
      candidate construction or scoring.
- [ ] Final options still pass ownership, direction, raw-effect, requirement,
      capacity, generic-slot, backlash, and active-role hard filters.
- [ ] Existing bounded search limits and truncation diagnostics remain intact.
- [ ] Inaccessible counters produce an unresolved gap and are not replaced by
      a name-similar or lower-ranked unverified 功法.
- [ ] Existing policy-score meanings and deterministic tie-breakers remain
      unchanged unless a separately documented rule change is approved.
- [ ] The manual plan, tactical explanation, and Epic 4 comparison agree with
      the selected feasible loadout.
- [ ] Applying or clearing an observation atomically replaces the profile,
      matches, playbooks, adjustments, recommendation, and comparison.
- [ ] The save-only result is reproducible after clearing observations.
- [ ] Tests prove feasibility rejection, accessible and inaccessible counters,
      target adjustment, deterministic ranking, manual-plan parity, and
      observation lifecycle behavior.

#### Evidence when complete

- Updated recommendation orchestration and option-building integration.
- Application and Domain regression tests for all delivered playbooks.
- Updated candidate-generation and recommendation architecture documentation.

## Slice 5: API vertical

### E5-007 — Expose typed target-profile and playbook contracts

**Status:** Planned

**Priority:** P1

**Estimate:** M

**Dependencies:** E5-003, E5-005, E5-006

Map the immutable Epic 5 result into public response contracts without asking
clients to reclassify targets or compose playbooks from display strings.

#### Acceptance criteria

- [ ] The API exposes profile dimensions, typed values, evidence states,
      provenance, unavailable reasons, and diagnostics.
- [ ] Archetype results expose stable identities, match state, supporting,
      missing, excluding, and conflicting facet references.
- [ ] Playbooks expose response goals, threat/counter references, timing,
      requirements, known gaps, and composition conflicts.
- [ ] Target-specific adjustments expose stable kinds and reasons.
- [ ] Response ordering matches Domain/Application ordering.
- [ ] Localized text is display-only and stable identities remain
      language-neutral.
- [ ] Contracts expose no save path, game path, screenshot path, raw
      proprietary payload, process identifier, persistence command, or
      mutation-capable game type.
- [ ] Mapper tests cover every evidence, match, conflict, gap, and adjustment
      state in Traditional Chinese and English where text is projected.
- [ ] API documentation includes complete, partial, unsupported, conflicting,
      multi-match, and adjusted examples.

#### Evidence when complete

- Epic 5 response contracts and pure mappers under `TaiWuAPI/Contracts`.
- API and mapper tests.
- Updated `docs/api/COMBAT-RECOMMENDATIONS.md` and an Epic 5 API design note.

## Slice 6: Core UI

### E5-008 — Add a compact bilingual archetype and strategy section

**Status:** Planned

**Priority:** P1

**Estimate:** L

**Dependencies:** E5-007

Add one progressive-disclosure section to the existing recommendation page so
the player can understand the matched archetypes and reusable strategy without
duplicating target threats, loadout cards, manual changes, or the comparison
matrix.

#### Acceptance criteria

- [ ] Dominant matched archetypes appear before partial, unsupported, or
      conflicting results.
- [ ] Multiple matches are grouped as one target profile rather than separate
      recommendation results.
- [ ] Attack-family context is visually distinct from verified pressure,
      resilience, control, and tempo mechanics.
- [ ] Every match exposes concise evidence and freshness without dumping raw
      diagnostics into the primary view.
- [ ] Reusable response goals link to existing threat, counter, requirement,
      and evidence detail where available.
- [ ] Inaccessible counters and unresolved goals remain visible.
- [ ] The section does not add a new recommendation-policy control or repeat
      the complete loadout comparison.
- [ ] Loading, no-match, multi-match, partial, unsupported, conflicting,
      available, inaccessible-counter, and failure states are rendered.
- [ ] Traditional Chinese and English copy is complete and stable identities
      are never shown as untranslated raw codes when display text exists.
- [ ] Desktop and narrow layouts expose equivalent facts with no horizontal
      overflow.
- [ ] Native headings, lists, buttons, and disclosures provide logical
      keyboard and screen-reader navigation; state never relies on color alone.
- [ ] Component tests cover the complete state matrix and duplicate-element
      regression guards.

#### Evidence when complete

- Epic 5 Presentation view models, mapper, localization, Razor components, and
  styles.
- Component-rendering and localization tests.
- `docs/roadmap/epic-005/UI-005-target-archetype-strategy.md`.

## Slice 7: Exact-target explanation

### E5-009 — Explain target-specific adjustments and unresolved gaps

**Status:** Planned

**Priority:** P1

**Estimate:** M

**Dependencies:** E5-005, E5-006, E5-008

Explain how the final selected target changed the reusable playbook and why the
player's accessible 功法 produced the final recommendation.

#### Acceptance criteria

- [ ] Retained, elevated, reduced, added, replaced, and unresolved adjustment
      kinds have concise bilingual explanations.
- [ ] Every adjustment links the relevant archetype goal, exact target fact,
      threat, counter candidate, feasibility result, or missing evidence.
- [ ] The explanation distinguishes target customization from player
      feasibility filtering.
- [ ] A missing counter does not read as a completed mitigation.
- [ ] A reduced broad risk does not erase exact evidence or historical source
      conflicts.
- [ ] The UI identifies when the final recommendation is unchanged because the
      current loadout already satisfies the composed response.
- [ ] Observation apply/clear updates the explanation together with every
      other Epic 5 result.
- [ ] Presentation does not restate full skill cards, warning lists, manual
      checklist items, or comparison rows.
- [ ] Mapper and component tests cover each adjustment kind, unchanged result,
      missing counter, and observation lifecycle state.

#### Evidence when complete

- Adjustment explanation mapping and compact Presentation component updates.
- Focused mapper and rendering tests.
- Updated UI and playbook-composition documentation.

## Slice 8: Verification and completion

### E5-010 — Verify archetype reuse, safety, and determinism

**Status:** Planned

**Priority:** P1

**Estimate:** L

**Dependencies:** E5-000 through E5-009

Run the full automated and guarded local verification matrix, audit every Epic
5 criterion, and record the product-owner completion decision.

#### Acceptance criteria

- [ ] Domain tests cover profile, evidence, definition, match, playbook,
      composition, adjustment, conflict, and deterministic-order invariants.
- [ ] Synthetic cases prove one target can multi-match and one playbook family
      can apply to multiple targets without target-ID rules.
- [ ] Application tests prove exact-target adjustment, player feasibility,
      recommendation parity, and observation apply/clear behavior.
- [ ] API tests prove typed unavailable, partial, unsupported, conflicting,
      multi-match, playbook-gap, and adjustment states survive mapping.
- [ ] Presentation tests cover bilingual desktop and narrow workflows,
      keyboard semantics, non-color states, and duplicate-element guards.
- [ ] Architecture tests prevent localized or raw-text mechanical matching,
      unbounded alternative engines, file/process/screenshot access,
      persistence, game control, and mutation-capable dependencies.
- [ ] The baseline and all three newly verified playbook families pass their
      documented synthetic verification matrix.
- [ ] Guarded local verification exercises every representative family
      available in the current save and records unsupported local cases
      honestly.
- [ ] Repeated identical runs produce equivalent profile, match, playbook,
      adjustment, recommendation, comparison, and diagnostic fingerprints.
- [ ] Applying the same observation repeatedly is idempotent and clearing it
      reproduces the save-only result.
- [ ] All inspected save, GameData, language, and other game-owned source
      fingerprints remain unchanged.
- [ ] Release build, default test matrix, formatting, and diff checks pass.
- [ ] Every Epic 5 acceptance criterion links to implementation or evidence.
- [ ] Deferred clustering, persistence, screenshot assistance, outcome
      learning, broader target coverage, companions, village, and library work
      remain explicit future work.
- [ ] The product owner records the Epic 5 completion decision.

#### Evidence when complete

- `docs/reviews/E5-010-automated-verification.md`.
- `docs/reviews/E5-010-manual-verification.md`.
- Updated completion decision in [EPIC-005](./EPIC.md).

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
