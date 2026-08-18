# Epic 7 backlog: Evidence-aware village workforce and building assignment planner

This backlog implements [EPIC-007](./EPIC.md) while preserving the permanent
safety boundary in
[ADR-0001](../../architecture/ADR-0001-absolute-game-non-interference.md).

## Conventions

### Priority

- **P0:** Required for the first trustworthy assignment-to-worker vertical.
- **P1:** Required for Epic 7 completion.
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
- use typed, version-matched evidence before a fact affects worker membership,
  availability, a hard requirement, an output claim, scoring, or ordering;
- distinguish the worker universe, eligibility, current assignment, proposed
  assignment, requirements, suitability, and manual guidance;
- preserve immutable collections, stable identities, deterministic ordering,
  exact ties, unavailable reasons, conflicts, provenance, and diagnostics;
- keep every score or output local to one stable work-objective definition and
  version;
- reject localized names, category labels, and untyped raw descriptions as
  identity or mechanical rules;
- treat missing, stale, unsupported, and conflicting evidence as explicit
  states rather than zero or negative ability;
- read all required settlement facts from one coherent save revision and avoid
  one archive open per worker or assignment;
- expose concise bilingual and accessible states without relying on color;
- leave every save, game file, configuration value, running process, runtime
  memory location, and in-game state unchanged;
- introduce no recruitment, training, movement, equipment, building,
  assignment, collection, persistence, screenshot, upload, process access,
  automation, or input-control capability;
- update architecture, API, UI, testing, scenario, and roadmap evidence where
  the contract changes; and
- record the relevant verification command and result.

## Delivery order

| Order | Slice | Outcome |
|---:|---|---|
| 0 | Evidence boundary | Worker, building, assignment, resource, output, and version facts are verified before coding |
| 1 | Product and interaction contract | Objective-local comparison, evidence states, ties, manual guidance, and accessible UX cannot misrepresent results |
| 2 | Settlement Domain | Immutable snapshot, assignment, rule, evaluation, comparison, and fingerprint types are established |
| 3 | One-pass source projection | All settlement facts come from one bounded read of one save revision |
| 4 | Work rules and evaluation | Versioned verified rules produce deterministic worker states and components |
| 5 | Shortlist and assignment comparison | Current and alternative workers are compared with an information-only manual plan |
| 6 | Application and API vertical | One immutable result reaches clients through typed contracts |
| 7 | Core UI | Players can select an assignment, inspect alternatives, and compare evidence bilingually |
| 8 | Verification and completion | Safety, batching, determinism, evidence fidelity, and product acceptance close the epic |

## Slice 0: Evidence boundary

### E7-000 — Verify settlement sources and select the first assignment vertical

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** Epic 2 save and catalogue foundations; Epic 6 character
snapshot evidence; ADR-0001

Inspect the minimum read-only save and installed GameData sources needed to
identify settlement workers, relevant buildings or work targets, current
assignments, availability, requirements, resource context, and any output or
suitability formula. Select one representative vertical only after exact
semantics and standalone runtime safety are established.

#### Acceptance criteria

- [x] The inspected Taiwu and GameData versions are recorded.
- [x] The worker candidate universe records its owning source, inclusion and
      exclusion rules, runtime types, completeness, and precedence.
- [x] Village or settlement membership is distinguished from Taiwu-group,
      location, target-lookup, and recruitability facts.
- [x] Candidate availability and current-assignment facts record exact owners,
      types, cardinality, meanings, and unsupported states.
- [x] Building, position, work-slot, or assignment-target identity is stable
      and language independent.
- [x] Every considered attribute, aptitude, feature, skill, resource, or status
      records exact unit, range, version, and mechanical meaning.
- [x] Any proposed productivity or output value has a verified formula and unit
      or is explicitly excluded from the first vertical.
- [x] Context-dependent getters are probed for standalone safety and rejected
      where live special-effect or process context is required.
- [x] Localized labels and raw descriptions are rejected as mechanics by
      themselves.
- [x] The first vertical is narrow enough to compare one current assignment
      with eligible alternatives without solving the whole settlement.
- [x] At least one current-assignment, alternative-worker, tie or incomplete,
      and unsupported scenario is defined with synthetic fixtures.
- [x] One stable local-save probe records cold/warm timing, cancellation, and
      before/after hashes without committing proprietary data or identities.
- [x] Unverified recruitment, development, construction, resource routing, and
      library mechanics are listed as deferred rather than partial rules.

#### Evidence

- `docs/scenarios/E7-000-village-workforce-evidence.md`.
- Source inventory and standalone probe tests under Infrastructure tests.
- Recorded metadata contains versions, hashes, counts, and timing only.

## Slice 1: Product and interaction contract

### E7-001 — Define workforce evaluation, comparison, and UI semantics

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E7-000

Convert the selected evidence boundary into stable vocabulary and interaction
semantics before implementing public Domain or API contracts.

#### Acceptance criteria

- [x] Stable identities exist for the work objective, target kind, eligibility
      state, requirement kind, component kind, evaluation state, and result.
- [x] Current and proposed assignments have different origins and lifecycle.
- [x] Hard gates are visibly evaluated before any suitability component.
- [x] Score, output, or comparison units and limitations are defined exactly.
- [x] Missing, unsupported, partial, stale, and conflicting states have honest
      UI behavior with no zero fallback.
- [x] Tie rank and deterministic order inside ties are defined separately.
- [x] Filters and name queries never alter immutable evaluation facts.
- [x] The manual checklist is information-only and cannot claim completion.
- [x] Shared limitations appear once per result; per-worker disclosures contain
      only worker-specific evidence to avoid repeated information.
- [x] Wide and narrow layouts expose identical facts in one DOM.
- [x] English and Traditional Chinese terms are defined before implementation.

#### Evidence

- Completed [UI-007](./UI-007-village-workforce-planner.md).
- `docs/architecture/VILLAGE-WORKFORCE-EVALUATION-CONTRACT.md`.

## Slice 2: Settlement Domain

### E7-002 — Add immutable settlement, worker, and assignment contracts

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E7-000, E7-001

Add presentation-neutral Domain types for one coherent settlement snapshot and
the exact selected assignment vertical.

#### Acceptance criteria

- [x] Stable typed identities represent settlement, worker, assignment target,
      objective, rule version, source, and result.
- [x] Worker facts preserve available, unavailable, unsupported, and conflict
      states with provenance.
- [x] Current assignment is immutable save evidence; proposed assignment is an
      immutable helper artifact and cannot enter a current snapshot.
- [x] Requirement, component, output, evaluation, comparison, and diagnostic
      contracts use typed identities and units.
- [x] Collections are immutable, validated, and canonically ordered.
- [x] Invalid duplicate workers, targets, assignments, components, or evidence
      fail before evaluation.
- [x] Fingerprints exclude localized display text and include every fact that
      can affect a result.
- [x] Domain projects reference no Application, Infrastructure, ASP.NET, UI,
      GameData, reflection, filesystem, or process type.
- [x] Unit tests cover validation, unavailable states, equality, ordering,
      current/proposed separation, and fingerprints.

#### Evidence

- `docs/architecture/VILLAGE-WORKFORCE-DOMAIN.md`.
- `tests/TaiWu.Domain.UnitTests/VillageWorkforce/`.

## Slice 3: One-pass source projection

### E7-003 — Project a one-pass read-only settlement snapshot

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E7-000, E7-002

Read the configured archive once and project every required worker, target,
assignment, and resource fact for the selected vertical into immutable
Application/Domain-owned contracts.

#### Acceptance criteria

- [x] One request opens one bounded archive session rather than one per worker
      or assignment.
- [x] The configured save path uses the existing trusted-path and stable-read
      guard.
- [x] Snapshot identity includes SHA-256, captured time, installed-data version,
      and rule-compatible source identity without exposing the path.
- [x] Worker and target enumeration is stable and language independent.
- [x] Exact source states map to typed available, missing, unsupported,
      conflicting, unstable, cancelled, and failed results.
- [x] Cancellation is honored before and during bounded projection loops.
- [x] No GameData object escapes Infrastructure.
- [x] Repeated stable reads produce identical normalized snapshots.
- [x] Integration tests prove game-owned source hashes and timestamps remain
      unchanged.
- [x] Architecture tests forbid persistence and mutation capabilities in the
      settlement reader.

#### Evidence

- `docs/architecture/VILLAGE-WORKFORCE-SNAPSHOT.md`.
- Infrastructure unit and opt-in local integration tests.

## Slice 4: Work rules and evaluation

### E7-004 — Define versioned assignment and work-objective rules

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E7-000, E7-002

Encode only the work requirements and components selected by the evidence
gate. Raw source labels remain display evidence and cannot select a rule.

#### Acceptance criteria

- [x] Every rule has a stable identity, semantic version, supported source
      version, target kind, evidence requirements, and limitation.
- [x] Membership, availability, vacancy, and target compatibility are distinct
      hard requirements where the evidence supports them.
- [x] Every numeric component names its source fact, normalization rule, unit,
      direction, and weight.
- [x] Descriptive six-attribute and martial/life summaries remain outside the
      work result unless an explicit rule references one exact field.
- [x] Unsupported versions produce a typed unsupported result, not a fallback
      rule.
- [x] Duplicate identities, invalid weights, mismatched units, or unknown
      source fields are rejected.
- [x] Unit tests pin every delivered rule and supported-version boundary.

#### Evidence

- `docs/architecture/VILLAGE-WORKFORCE-RULES.md`.
- Domain rule tests.

### E7-005 — Evaluate worker eligibility and deterministic suitability

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E7-003, E7-004

Evaluate each worker against one target and objective, preserving every gate,
component, tie, missing fact, conflict, and limitation.

#### Acceptance criteria

- [x] Candidate-universe eligibility and assignment hard gates run before
      numeric components.
- [x] A failed gate cannot be hidden as a score penalty or overridden by a high
      aptitude.
- [x] Missing required evidence makes the evaluation unrankable without a zero
      component.
- [x] Every available component retains raw value, normalized value, weight,
      contribution, unit, evidence reference, and explanation identity.
- [x] Exact ties remain ties; a stable identity orders rendering only.
- [x] Current worker status does not automatically confer rank or advantage.
- [x] Identical language-independent inputs produce identical evaluations and
      fingerprints.
- [x] Tests cover ranked, tied, ineligible, incomplete, unsupported,
      conflicting, and current-worker cases.

#### Evidence

- Domain evaluation tests and architecture documentation.

## Slice 5: Shortlist and assignment comparison

### E7-006 — Build the worker shortlist, comparison, and manual checklist

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E7-005

Build one canonical result that keeps the current assignment visible, orders
comparable alternatives, retains unranked states, and explains any proposed
manual reassignment.

#### Acceptance criteria

- [ ] Ranked, tied, ineligible, incomplete, unsupported, and conflicting
      workers remain distinct result groups with stable counts.
- [ ] The current worker is identifiable without being forced to rank first.
- [ ] Comparison uses two workers from the same snapshot, target, objective,
      and rule version.
- [ ] Every difference has an exact outcome: higher, lower, equal,
      unavailable, incompatible, or not comparable.
- [ ] Shared scope and score limitations appear once at result level.
- [ ] The manual checklist identifies current and proposed assignments,
      prerequisites, cautions, and facts to verify manually.
- [ ] No checklist item represents a command or stores completion state.
- [ ] Filters do not change result identity, scores, ties, or unfiltered counts.
- [ ] Unit tests cover empty, one-worker, current-best, alternative-best, tie,
      no-vacancy, missing-current, and incomplete-evidence scenarios.

#### Evidence

- `docs/architecture/VILLAGE-WORKFORCE-COMPARISON.md`.
- Domain/Application shortlist tests.

## Slice 6: Application and API vertical

### E7-007 — Orchestrate one coherent village-workforce result

**Status:** Planned

**Priority:** P1

**Estimate:** M

**Dependencies:** E7-003, E7-006

Compose snapshot reading, rule resolution, evaluation, shortlist, comparison,
and manual guidance behind one Application request/result boundary.

#### Acceptance criteria

- [ ] The request names stable target and objective identities rather than
      localized labels.
- [ ] One snapshot read supplies the complete result.
- [ ] Source, rule, and evaluation failures map to typed result states.
- [ ] Cancellation propagates; unexpected programmer faults reach host logging.
- [ ] No partial result mixes an old snapshot with new controls or rules.
- [ ] Result identity includes snapshot, target, objective, rule, and canonical
      evaluation fingerprints.
- [ ] Application tests cover every orchestration state and exact call counts.

#### Evidence

- `docs/architecture/VILLAGE-WORKFORCE-APPLICATION.md`.
- Application unit tests.

### E7-008 — Expose typed village-workforce API contracts

**Status:** Planned

**Priority:** P1

**Estimate:** M

**Dependencies:** E7-007

Expose read-only loopback endpoints for supported targets/objectives and one
workforce result without leaking implementation or source details.

#### Acceptance criteria

- [ ] Routes use `GET` or information-only request semantics; no assignment,
      building, collection, or mutation route exists.
- [ ] Request validation rejects unknown stable identities and invalid
      comparison selections with safe `400` responses.
- [ ] Typed responses preserve snapshot, target, current assignment, worker,
      evaluation, shortlist, comparison, evidence, and diagnostic states.
- [ ] Numeric enum values are rejected and public tokens are fixture-tested.
- [ ] Local paths, raw save content, proprietary configuration objects,
      exceptions, and mutation-capable types never enter responses.
- [ ] Controller tests cover success, unsupported, missing save, unstable save,
      invalid target, comparison, cancellation, and unexpected failure.
- [ ] Architecture tests inventory nested cross-layer contract types.

#### Evidence

- `docs/api/VILLAGE-WORKFORCE.md`.
- API and architecture tests.

## Slice 7: Core UI

### E7-009 — Deliver the bilingual accessible village-workforce UI

**Status:** Planned

**Priority:** P1

**Estimate:** L

**Dependencies:** E7-001, E7-008

Deliver the [UI-007](./UI-007-village-workforce-planner.md) page using one
coherent result and progressive disclosure.

#### Acceptance criteria

- [ ] Navigation and route names clearly describe village work, not companion
      recruitment or automatic optimization.
- [ ] The objective and assignment target are selected before evaluation.
- [ ] The current assignment, result summary, and top alternatives are visible
      without expanding repeated evidence.
- [ ] Shared scope, formula, and information-only limitations render once.
- [ ] Per-worker details contain only worker-specific gates, components,
      provenance, and limitations.
- [ ] Wide semantic tables and narrow cards expose identical facts in one DOM.
- [ ] Native controls, focus movement, live announcements, and comparison limits
      are keyboard and assistive-technology accessible.
- [ ] Every state and status has English and Traditional Chinese text and a
      non-color cue.
- [ ] Filters and comparison never reread the save.
- [ ] The page exposes no assign, build, collect, recruit, upload, process,
      automation, or input-control action.
- [ ] Rendering and architecture tests cover all states and raw-ID hiding.

#### Evidence

- Presentation mapper, rendering, localization, architecture, and browser
  review evidence.

## Slice 8: Verification and completion

### E7-010 — Verify safety, batching, determinism, and cross-layer parity

**Status:** Planned

**Priority:** P1

**Estimate:** L

**Dependencies:** E7-002 through E7-009

Prove the complete Epic 7 vertical preserves its source, evidence, performance,
and presentation contracts.

#### Acceptance criteria

- [ ] Domain rule and state coverage maps every delivered acceptance claim.
- [ ] Application tests prove one snapshot read and stable orchestration.
- [ ] Infrastructure tests prove one bounded archive session and source
      non-interference.
- [ ] API and Presentation retain every typed unavailable and conflict state.
- [ ] Localization coverage is exhaustive for typed Epic 7 keys.
- [ ] Semantic architecture tests forbid write, process, network/game-control,
      persistence, upload, and input capabilities.
- [ ] Repeated identical requests produce identical fingerprints, evaluations,
      ties, ordering, comparisons, and manual-plan identities.
- [ ] Cold and warm local performance budgets are recorded from E7-000 evidence.
- [ ] Release build has zero warnings and the full non-opt-in suite passes.
- [ ] Opt-in local tests record skips explicitly when sources are unavailable.

#### Evidence

- `docs/reviews/E7-010-automated-verification.md`.

### E7-011 — Validate the representative assignment and close Epic 7

**Status:** Planned

**Priority:** P1

**Estimate:** M

**Dependencies:** E7-010

Perform representative manual review, reconcile every epic criterion, record
deferred work, and request the product-owner completion decision.

#### Acceptance criteria

- [ ] The selected local assignment scenario matches documented source facts
      without exposing proprietary identities or data.
- [ ] English and Traditional Chinese wide/narrow states expose the same facts.
- [ ] Empty, tied, ineligible, incomplete, unsupported, conflict, and failure
      states are reviewed with synthetic data.
- [ ] Repeated runs retain stable result identity and source hashes.
- [ ] Every Epic 7 acceptance criterion links to implementation or evidence.
- [ ] Unsupported settlement, recruitment, development, library, persistence,
      and game-control mechanics remain explicit future work.
- [ ] Independent review findings are resolved or consciously deferred.
- [ ] The product owner records the Epic 7 completion decision.

#### Evidence

- `docs/reviews/E7-011-manual-verification.md`.
- Completion decision in `EPIC.md` and the roadmap index.

## Future work outside Epic 7

- Companion development and future-potential planning from PI-009.
- Recruitable-prospect comparison until recruitment availability, relationship,
  dialogue, travel, and party-capacity rules are verified.
- Whole-village multi-building optimization and resource routing.
- Construction, upgrades, demolition, collection, and other game actions.
- Library, book inventory, repair, study, and acquisition planning from PI-011.
- Persisted assignment proposals, preferences, observations, histories, or
  reported outcomes.
- User-authored weights, arbitrary formulas, statistical models, or learned
  productivity estimates.
- Any save writing, game-file changes, process access, automation, or input
  control.
