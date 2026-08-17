# Epic 6 backlog: Evidence-aware companion role and candidate finder

This backlog implements [EPIC-006](./EPIC.md) while preserving the permanent
safety boundary in
[ADR-0001](../../architecture/ADR-0001-absolute-game-non-interference.md).

## Conventions

### Priority

- **P0:** Required for the first trustworthy role-to-shortlist vertical.
- **P1:** Required for Epic 6 completion.
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
- use typed, version-matched evidence before a fact can affect eligibility,
  a hard requirement, scoring, ranking, or explanation;
- distinguish candidate-universe membership, eligibility, role requirements,
  and scored suitability;
- preserve immutable collections, stable identities, deterministic ordering,
  ties, unavailable reasons, conflicts, provenance, and diagnostics;
- keep role scores local to one stable role definition and version;
- reject localized names, category labels, and untyped raw descriptions as
  identity or mechanical rules;
- treat missing, stale, unsupported, and conflicting evidence as explicit
  states rather than zero or negative ability;
- read all candidate facts from one coherent save revision and avoid one
  archive open per candidate;
- expose bilingual and accessible states without relying on color alone;
- leave every save, game file, configuration value, running process, runtime
  memory location, and in-game state unchanged;
- introduce no recruitment, training, movement, equipment, assignment,
  persistence, screenshot, upload, process access, automation, or input-control
  capability;
- update architecture, API, UI, testing, and roadmap evidence where the
  contract changes; and
- record the relevant verification command and result.

## Delivery order

| Order | Slice | Outcome |
|---:|---|---|
| 0 | Evidence boundary | Candidate universe, eligibility, role fields, versions, and representative scenarios are verified before coding |
| 1 | Product and interaction contract | Role-local ranking, evidence states, ties, filters, and accessible UX cannot misrepresent results |
| 2 | Candidate and role Domain | Immutable profiles, role definitions, rules, evaluations, and fingerprints are typed |
| 3 | One-pass source projection | All candidate facts come from one bounded read of one save revision |
| 4 | Enrichment and evaluation | Catalogue facts and progress enrich profiles; verified rules produce deterministic evaluations |
| 5 | Shortlist and comparison | Comparable candidates are ordered and every inclusion, exclusion, tie, and tradeoff is explained |
| 6 | Application and API vertical | One immutable result reaches clients through typed contracts |
| 7 | Core UI | Players can select a role, inspect a shortlist, and compare candidates bilingually |
| 8 | Verification and completion | Safety, batching, determinism, evidence fidelity, and product acceptance close the epic |

## Slice 0: Evidence boundary

### E6-000 — Verify the candidate universe and select the initial role matrix

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** Epic 2 character progress and catalogue; existing target
lookup; ADR-0001

Inspect the minimum read-only save and installed GameData sources needed to
identify possible companion candidates, establish eligibility and current
availability, and evaluate useful player-selected roles. Define exact source
semantics before adding Domain contracts or scoring rules.

#### Acceptance criteria

- [x] The inspected Taiwu and GameData versions are recorded.
- [x] Target lookup enumeration is explicitly distinguished from companion-
      candidate eligibility.
- [x] Every candidate-universe and eligibility field records its owning source,
      runtime type, normalized meaning, version, availability, completeness,
      and source-of-truth precedence.
- [x] Candidate discovery evaluates current companions, potential candidates,
      story or generated characters, unavailable characters, and other
      relevant categories without assuming that all are rankable.
- [x] Candidate fields considered for roles record exact units and semantics,
      including attributes, features, skills, progress, relationship or
      membership facts, availability, and location where supported.
- [x] Localized names, feature names, skill names, category labels, and raw
      descriptions are rejected as mechanical evidence by themselves.
- [x] Current ability is separated from future growth, training, acquisition,
      relationship change, and speculative potential.
- [x] At least two genuinely different, evidence-backed initial role presets
      are selected with documented hard requirements and score candidates.
- [x] Combat support is assessed first; teaching or inheritance value is
      delivered only if its mechanics are independently verified.
- [x] Settlement work, building assignment, and library roles remain outside
      the selected matrix.
- [x] Representative synthetic fixtures and privacy-safe local verification
      cases cover eligible, ineligible, incomplete, unsupported, and
      conflicting candidates.
- [x] A measurable cold and warm performance budget is recorded for projecting
      and evaluating the verified candidate universe.
- [x] Before and after fingerprints prove every inspected save, GameData,
      language, and other game-owned source remains unchanged.
- [x] EPIC-006 is updated if verified fields, role presets, or candidate scope
      differ from the proposed boundary.

#### Evidence when complete

- `docs/scenarios/E6-000-companion-candidate-evidence.md`.
- `docs/architecture/COMPANION-CANDIDATE-SOURCES.md`.
- A source-field, role-rule, representative-scenario, and performance matrix
  containing no proprietary data, character identities, or local paths.
- Recorded guarded read commands and fingerprint results.

#### Completion evidence

- The exact installed GameData version exposes a 14-entry saved base martial-
  qualification buffer and a 16-entry saved base life-skill-qualification
  buffer. Every entry was readable in the privacy-safe local representative.
- Current modified qualification and attainment are unsupported: every martial
  and life-skill getter entered unavailable `SpecialEffectDomain.ModifyData`.
- The authoritative first universe is the saved Taiwu group excluding Taiwu,
  with object existence, Domain membership, character membership, and living-
  state agreement required. Broad character and target lookup cannot create a
  candidate.
- `MARTIAL_DISCIPLINE_APTITUDE` and
  `LIFE_SKILL_DISCIPLINE_APTITUDE` are the two initial role families. General
  combat support, teaching, inheritance, recruitment, and settlement work are
  unsupported or deferred.
- The local save contained one privacy-safe eligible companion after excluding
  Taiwu. Synthetic representatives cover ineligible, incomplete, unsupported,
  conflicting, multi-candidate ordering, and tie cases.
- Cold projection completed in 21.598 seconds against a 30-second budget; warm
  projection completed in 4 milliseconds against a 2-second budget. Repeated
  aggregate results were equivalent and used one archive session per request.
- Five metadata/XML sources and three local archive sources retained identical
  length, last-write time, and SHA-256 before and after inspection. An earlier
  changing-save attempt was rejected without accepting a mixed revision.

## Slice 1: Product and interaction contract

### E6-001 — Define role evaluation, shortlist, and UI semantics

**Status:** Planned

**Priority:** P0

**Estimate:** M

**Dependencies:** E6-000

Document the exact meaning of candidate states, role definitions, hard gates,
score components, total scores, ties, ordering, filters, comparisons, evidence
indicators, and responsive interaction states before adding public contracts.

#### Acceptance criteria

- [ ] Every delivered role has a stable identity, purpose, version, supported
      source versions, hard requirements, scored dimensions, normalization
      rules, weights, and tie breakers.
- [ ] Scores are explicitly local to one role and are not probabilities,
      grades of intrinsic worth, or comparable across roles.
- [ ] Eligible, ineligible, incomplete, unsupported, and conflicting candidate
      states have distinct meanings and presentation.
- [ ] Ranked, tied, and unranked evaluation states have exact rules.
- [ ] Missing required evidence cannot produce a total score; optional missing
      evidence follows a documented role-specific rule.
- [ ] Filtering changes visible rows but not evaluation facts, rank identity,
      or the disclosed unfiltered result count.
- [ ] Candidate comparison identifies decisive strengths, weaknesses, hard
      gates, ties, tradeoffs, evidence gaps, and unavailable facts.
- [ ] The contract distinguishes current ability from development potential.
- [ ] Desktop and narrow-screen states define role selection, shortlist,
      comparison, evidence details, loading, empty, unsupported, conflict, and
      failure behavior.
- [ ] Keyboard order, focus behavior, headings, live-region use, table or card
      semantics, and non-color status cues are documented.
- [ ] Recruitment instructions, development plans, settlement assignment,
      persistence, export, and game control are confirmed out of scope.

#### Evidence when complete

- `docs/architecture/COMPANION-ROLE-EVALUATION-CONTRACT.md`.
- `docs/roadmap/epic-006/UI-006-companion-candidate-finder.md`.
- English and Traditional Chinese semantic wireframes using synthetic data.

## Slice 2: Candidate and role Domain

### E6-002 — Add immutable companion-candidate profile contracts

**Status:** Planned

**Priority:** P0

**Estimate:** M

**Dependencies:** E6-000, E6-001

Define presentation-neutral Domain values for candidate identity, candidate-
universe state, availability, profile facts, evidence, source versions,
conflicts, unavailable reasons, diagnostics, and deterministic fingerprints.

#### Acceptance criteria

- [ ] Candidate identity uses a stable language-independent character identity;
      display text remains separate.
- [ ] Candidate-universe and availability states cannot be inferred from name,
      age, location, or target-lookup membership.
- [ ] Each profile fact has a stable field identity, typed value, provenance,
      evidence state, source version, and optional unavailable reason.
- [ ] Confirmed, incomplete, unsupported, stale, and conflicting evidence have
      explicit invariants.
- [ ] Missing evidence cannot construct a confirmed fact or a zero value.
- [ ] Source conflicts retain every candidate value and precedence decision.
- [ ] Collections copy into immutable values with stable ordering and reject
      nulls, duplicates, invalid enums, blank identities, and incompatible
      evidence.
- [ ] A profile fingerprint includes semantic facts and source or rule versions
      but excludes localized text, local paths, and irrelevant timestamps.
- [ ] Domain types have no Application, Infrastructure, Presentation,
      persistence, filesystem, process, reflection, or GameData dependencies.
- [ ] Unit tests cover valid, empty, duplicate, incomplete, unsupported, stale,
      conflicting, and deterministic-fingerprint cases.

#### Evidence when complete

- `src/TaiWu.Domain/CompanionCandidates/` immutable contracts.
- `tests/TaiWu.Domain.UnitTests/CompanionCandidates/` invariant tests.
- Updated `docs/architecture/COMPANION-CANDIDATE-SOURCES.md`.

### E6-003 — Define versioned role definitions and evaluation rules

**Status:** Planned

**Priority:** P0

**Estimate:** M

**Dependencies:** E6-001, E6-002

Represent player-selectable roles as immutable, versioned definitions over
verified candidate-profile fields. Keep eligibility, hard requirements,
scored dimensions, normalization, weighting, and tie breaking explicit.

#### Acceptance criteria

- [ ] Role identity and rule version are stable and non-localized.
- [ ] Each role declares supported profile and GameData versions.
- [ ] Candidate-universe eligibility is evaluated before role hard
      requirements, and hard requirements are evaluated before scoring.
- [ ] Every rule references typed verified fields rather than display text or
      raw descriptions.
- [ ] Each scored dimension defines its unit, direction, normalization range,
      weight, missing-evidence behavior, and explanation identity.
- [ ] Weights and normalization cannot hide a failed hard requirement.
- [ ] Total scores remain role-local and retain all component values and
      evidence references.
- [ ] Tie breakers use stable semantic facts and never localized text or source
      enumeration order.
- [ ] Unsupported role or source versions fail closed with typed diagnostics.
- [ ] At least two verified role definitions demonstrate different hard
      requirements or score tradeoffs over shared candidate profiles.
- [ ] Unit tests cover valid definitions, invalid weights, missing fields,
      failed gates, unsupported versions, exact ties, and deterministic rule
      identity.

#### Evidence when complete

- `src/TaiWu.Domain/CompanionRoles/` definitions and rule contracts.
- `tests/TaiWu.Domain.UnitTests/CompanionRoles/` invariant tests.
- `docs/architecture/COMPANION-ROLE-EVALUATION.md`.

## Slice 3: One-pass source projection

### E6-004 — Project a one-pass read-only candidate snapshot

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E6-000, E6-002

Add an Application read port and Infrastructure adapter that project the
verified candidate universe and required raw profile facts from one configured
save revision through one bounded archive session.

#### Acceptance criteria

- [ ] The Application owns immutable request and result contracts; the port is
      explicitly read-only.
- [ ] Infrastructure reads only the configured trusted save path and accepts no
      caller-provided filesystem path.
- [ ] One request opens and projects the archive once instead of invoking a
      single-character archive workflow for each candidate.
- [ ] Candidate identity, universe state, eligibility inputs, role inputs,
      availability, and location use only sources approved by E6-000.
- [ ] The snapshot records save fingerprint, captured time, GameData version,
      mapping version, load warnings, omissions, and diagnostics.
- [ ] A character-level mapping failure cannot fabricate facts or corrupt other
      candidates; its typed omission or incomplete state remains visible.
- [ ] Cancellation is observed during large candidate enumeration.
- [ ] Enumeration and mapping order do not determine stable output order.
- [ ] Save missing, read failure, unsupported version, expected standalone
      runtime boundary, partial result, and changed-revision states are typed.
- [ ] No mutable GameData, reflection, archive, or infrastructure type crosses
      the port.
- [ ] Infrastructure tests cover representative states and prove the reader has
      no write, process, network, input, or game-control path.
- [ ] Guarded local integration records before and after fingerprints and meets
      the E6-000 performance budget.

#### Evidence when complete

- Candidate snapshot port and contracts under `TaiWu.Application`.
- One-pass adapter and focused mappings under `TaiWu.Infrastructure/SaveGames`.
- Unit, architecture, and opt-in guarded integration tests.
- `docs/architecture/COMPANION-CANDIDATE-SNAPSHOT.md`.

## Slice 4: Enrichment and evaluation

### E6-005 — Enrich candidate profiles with verified catalogue and progress facts

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E6-002, E6-003, E6-004, Epic 2

Join the one-pass candidate snapshot to current version-aware catalogue and
character-progress facts required by the approved role matrix. Introduce a
batch projection or join boundary where the existing single-character reader
would otherwise reopen the archive repeatedly.

#### Acceptance criteria

- [ ] Enrichment uses stable skill, feature, field, and character identities;
      localized names remain display values.
- [ ] Only E6-000-approved fields enter a role-evaluable profile.
- [ ] Learned, equipped, mastered, proficiency, study, or other progress states
      retain their existing verified meanings rather than being collapsed.
- [ ] Catalogue and progress version compatibility is checked explicitly.
- [ ] Missing, stale, rebuilding, partial, unsupported, and failed enrichment
      states remain typed at candidate and result level.
- [ ] Missing enrichment cannot become zero progress, no skill, or failed role
      suitability.
- [ ] One candidate's unavailable progress does not suppress unrelated
      candidates or fabricate comparative facts.
- [ ] The workflow does not call the archive-opening character atlas reader in
      an N+1 loop.
- [ ] Enrichment ordering and parallel scheduling cannot affect the result.
- [ ] Tests cover version match and mismatch, missing catalogue, partial
      progress, duplicate facts, unsupported fields, and deterministic joins.

#### Evidence when complete

- Candidate profile builder or enrichment service under `TaiWu.Application`.
- Any required batch source contract and Infrastructure mapping.
- Unit and guarded integration tests.
- Updated companion snapshot and role-evaluation architecture documents.

### E6-006 — Evaluate role suitability and rank comparable candidates

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E6-003, E6-005

Evaluate candidate-universe eligibility, hard requirements, scored dimensions,
and deterministic tie breakers for one selected role. Rank comparable
evaluations and retain typed reasons for candidates that cannot enter the
shortlist.

#### Acceptance criteria

- [ ] Eligibility is evaluated before hard requirements and scoring.
- [ ] A failed hard requirement produces an explicit unranked result and cannot
      be overcome by score components.
- [ ] Missing required evidence produces incomplete or unsupported evaluation,
      never a numeric penalty.
- [ ] Every component retains rule identity, source evidence, normalized value,
      weight, contribution, and explanation identity.
- [ ] Total score arithmetic is bounded, deterministic, and validated.
- [ ] Equal evaluations remain explicit ties until documented stable tie
      breakers apply.
- [ ] Character ID or localized name may stabilize display ordering only after
      semantic rank and tie status are established; neither changes merit.
- [ ] Ranked, tied, ineligible, incomplete, unsupported, and conflicting
      results remain distinct.
- [ ] Identical candidate profiles, role versions, and rule versions produce
      equivalent components, diagnostics, ordering, and fingerprints.
- [ ] Unit tests cover every approved role, hard-gate failure, missing optional
      and required evidence, extremes, ties, unsupported versions, conflicts,
      and deterministic reruns.

#### Evidence when complete

- Pure Domain evaluator, result contracts, and shortlist builder.
- Domain unit tests using synthetic candidate profiles only.
- Updated `docs/architecture/COMPANION-ROLE-EVALUATION.md`.

## Slice 5: Shortlist and comparison

### E6-007 — Build evidence-aware shortlist and candidate comparison explanations

**Status:** Planned

**Priority:** P1

**Estimate:** M

**Dependencies:** E6-001, E6-006

Turn role evaluations into an immutable shortlist and comparison model that
explains decisive strengths, weaknesses, hard gates, tradeoffs, ties, and
missing evidence without re-scoring in the UI.

#### Acceptance criteria

- [ ] The shortlist retains selected role identity, rule version, source
      identity, unfiltered counts, ranked entries, ties, exclusions, and
      diagnostics.
- [ ] Every ranked entry explains its strongest contributions and material
      limitations using existing evaluation components.
- [ ] Ineligible or unranked candidates retain exact reasons when exposed.
- [ ] Comparing two candidates uses the same immutable role evaluations and
      does not create a second ranking path.
- [ ] Comparison rows use stable field or rule identity and show both value and
      evidence state.
- [ ] Decisive differences, equal facts, missing evidence, hard-gate outcomes,
      and genuine tradeoffs remain distinguishable.
- [ ] Location or availability is displayed only when E6-000-approved evidence
      supports it.
- [ ] Filters do not mutate evaluations, scores, ties, or the source shortlist.
- [ ] Explanations never recommend unverified recruitment, training, travel,
      equipment, or assignment actions.
- [ ] Unit tests cover top results, ties, exclusions, incomplete evidence,
      filtered views, comparisons, and equivalent reruns.

#### Evidence when complete

- Domain or Application shortlist and comparison contracts and builders.
- Focused unit tests.
- `docs/architecture/COMPANION-CANDIDATE-COMPARISON.md`.

## Slice 6: Application and API vertical

### E6-008 — Orchestrate one coherent companion-finder result

**Status:** Planned

**Priority:** P1

**Estimate:** M

**Dependencies:** E6-004, E6-005, E6-006, E6-007

Compose snapshot reading, catalogue and progress enrichment, role resolution,
evaluation, shortlist construction, filters, and comparison from one request
and one immutable source revision.

#### Acceptance criteria

- [ ] Requests accept a stable role identity and bounded product filters but no
      filesystem path, raw rule definition, arbitrary expression, or game
      command.
- [ ] Unknown roles, unsupported role versions, invalid filters, and invalid
      comparison selections fail with typed results.
- [ ] One workflow result binds save fingerprint, GameData version, catalogue
      version, profile mapping version, role version, and evaluation version.
- [ ] A save revision change triggers a complete new result rather than mixing
      candidate facts from two revisions.
- [ ] Catalogue and progress failures preserve candidate-source evidence and
      return honest partial or unavailable states where permitted.
- [ ] Filters are applied after authoritative evaluation and retain original
      counts and result identity.
- [ ] Cancellation reaches source projection and expensive evaluation loops.
- [ ] Repeated equivalent requests return semantically equivalent immutable
      results.
- [ ] Tests cover success, empty, partial, unsupported, stale, conflict,
      changed-revision, cancellation, filter, and comparison states.

#### Evidence when complete

- Application use case and request/result contracts.
- Application unit tests with substituted read-only ports.
- `docs/architecture/COMPANION-FINDER-APPLICATION.md`.

### E6-009 — Expose typed companion-finder API contracts

**Status:** Planned

**Priority:** P1

**Estimate:** M

**Dependencies:** E6-008

Expose the available role presets and complete companion-finder result through
localhost-only HTTP endpoints and pure response mappers.

#### Acceptance criteria

- [ ] Role discovery exposes stable identity, version, localized purpose,
      supported state, and limitations.
- [ ] Finder responses expose source identity, role identity, candidate states,
      hard gates, score components, shortlist order, ties, tradeoffs,
      provenance, conflicts, unavailable reasons, and diagnostics.
- [ ] API types do not expose local paths, proprietary raw content, internal
      GameData types, reflection objects, or mutation-capable handles.
- [ ] Request validation bounds filters and comparison selections.
- [ ] HTTP behavior distinguishes invalid request, missing save, unsupported
      version, partial result, conflict, cancellation, and internal failure.
- [ ] Mapping is pure and cannot recompute eligibility, scores, or ordering.
- [ ] Traditional Chinese and English display values map from the same stable
      identities and facts.
- [ ] OpenAPI documents score limitations and evidence states without
      universal-best or probability language.
- [ ] Controller, contract, mapper, serialization, localization, and
      architecture tests cover all response states.

#### Evidence when complete

- Companion-finder controller and response contracts under `TaiWuAPI`.
- API and architecture tests.
- `docs/api/COMPANION-CANDIDATES.md`.

## Slice 7: Core UI

### E6-010 — Deliver the bilingual accessible companion-finder UI

**Status:** Planned

**Priority:** P1

**Estimate:** L

**Dependencies:** E6-001, E6-009

Add a focused page where the player selects a role, reviews the evidence-aware
shortlist, filters visible candidates, compares candidates, and inspects
reasons without introducing a second evaluation path in Presentation.

#### Acceptance criteria

- [ ] The page follows `UI-006-companion-candidate-finder.md` and the shared UI
      presentation guidelines.
- [ ] Role selection precedes ranking and clearly states the objective and
      limitations.
- [ ] The summary distinguishes total considered, eligible, ranked, tied,
      ineligible, incomplete, unsupported, and conflicting candidates.
- [ ] Shortlist entries show rank or tie, role-local score when available,
      decisive strengths, material limitations, evidence state, and verified
      availability or location.
- [ ] The comparison view exposes the same immutable evaluation components and
      hard gates without recalculation.
- [ ] Filters disclose the unfiltered result count and do not change score or
      rank facts.
- [ ] Missing, stale, rebuilding, partial, unsupported, conflicting, empty,
      loading, cancelled, and failed states have actionable information-only
      messages.
- [ ] Score labels state that values are role-local and are neither universal
      rankings nor success probabilities.
- [ ] Traditional Chinese and English copy is complete and uses stable
      identity-backed localization.
- [ ] Narrow layouts preserve fact parity without horizontal dependence; wide
      layouts do not force assistive technology through duplicated content.
- [ ] Headings, landmarks, table or list semantics, labels, focus order,
      expanded states, status announcements, and non-color cues are tested.
- [ ] Presentation types cannot recruit, train, move, equip, assign, persist,
      upload, automate input, or mutate game state.

#### Evidence when complete

- Razor components, presentation view models, pure mappers, and localization.
- Presentation and rendered semantic tests.
- Completed `docs/roadmap/epic-006/UI-006-companion-candidate-finder.md`.
- English and Traditional Chinese desktop and narrow-screen review captures
  using synthetic or redacted data.

## Slice 8: Verification and completion

### E6-011 — Verify safety, batching, determinism, and cross-layer parity

**Status:** Planned

**Priority:** P1

**Estimate:** L

**Dependencies:** E6-002 through E6-010

Audit the complete vertical with synthetic matrices, automated suites,
architecture rules, performance checks, and guarded read-only local scenarios.

#### Acceptance criteria

- [ ] Domain tests cover every candidate, eligibility, evidence, hard-gate,
      score, tie, exclusion, comparison, and fingerprint invariant.
- [ ] Each delivered role has synthetic positive, negative, incomplete,
      unsupported, conflicting, and tied cases.
- [ ] Application tests prove one coherent revision and deterministic rebuild
      after a revision change.
- [ ] Infrastructure tests prove one bounded archive projection rather than an
      archive open per candidate.
- [ ] API and Presentation tests prove parity with the immutable Application
      result and no re-ranking.
- [ ] English and Traditional Chinese states contain equivalent facts and
      accessible names.
- [ ] Performance verification meets the cold and warm budgets selected by
      E6-000 for the representative candidate universe.
- [ ] Repeated and reordered synthetic inputs produce equivalent evaluations,
      ties, shortlist ordering, comparisons, diagnostics, and fingerprints.
- [ ] Architecture tests forbid mutation-capable GameData dependencies,
      persistence, process access, screenshot handling, automation, or input
      control in the Epic 6 vertical.
- [ ] Guarded local verification records save and installed-source fingerprints
      before and after every read scenario and reports no changes.
- [ ] Release build, default test matrix, formatter verification, Markdown link
      validation, and `git diff --check` pass.
- [ ] Every Epic 6 acceptance criterion maps to implementation or evidence.

#### Evidence when complete

- `docs/reviews/E6-011-automated-verification.md`.
- Automated test suites and guarded integration results.
- Performance and non-interference evidence without proprietary data or local
  paths.

### E6-012 — Validate representative roles and close Epic 6

**Status:** Planned

**Priority:** P1

**Estimate:** M

**Dependencies:** E6-011

Compare each delivered role, candidate state, shortlist explanation, and
responsive UI against the verified game sources and representative scenarios.
Record remaining limitations and obtain the explicit completion decision.

#### Acceptance criteria

- [ ] Every delivered role is manually checked against at least one privacy-
      safe local or redacted representative scenario.
- [ ] Candidate-universe membership and eligibility are checked independently
      from target lookup.
- [ ] Hard requirements, score components, ties, strengths, weaknesses,
      tradeoffs, and missing evidence agree across source facts, Domain,
      Application, API, and UI.
- [ ] At least one incomplete, unsupported, or conflicting candidate remains
      visibly unranked rather than receiving an invented low score.
- [ ] English and Traditional Chinese desktop and narrow layouts preserve fact
      parity, readable order, keyboard operation, and non-color status cues.
- [ ] Performance remains within the E6-000 budget on the representative save.
- [ ] Save, GameData, language, and other inspected fingerprints remain
      unchanged.
- [ ] Companion development, village assignment, library planning,
      persistence, export, automation, and game control are confirmed absent.
- [ ] All Epic 6 acceptance criteria have linked implementation or evidence.
- [ ] Remaining unsupported roles or mechanics become explicit future backlog
      candidates rather than partially implemented rules.
- [ ] The product owner records the Epic 6 completion decision.

#### Evidence when complete

- `docs/reviews/E6-012-manual-verification.md`.
- Reviewed English and Traditional Chinese captures using synthetic or
  redacted content.
- Product-owner completion decision in `EPIC.md` and the roadmap index.

## Future work outside Epic 6

- Companion development, staged training, equipment, and opportunity planning
  from PI-009.
- Village workforce, building assignment, resource optimization, libraries,
  and book planning from PI-010 and PI-011.
- Additional role presets whose eligibility or scoring evidence is not
  verified by E6-000.
- Persisted candidate shortlists, preferences, observations, recommendation
  history, or reported outcomes.
- Shareable companion comparison cards or export artifacts.
- User-authored weights, arbitrary formulas, cross-role aggregate scores, or
  universal rankings.
- Statistical, learned, or outcome-trained suitability models.
- Recruitment, dialogue, travel, party, training, equipment, assignment, or
  any other game-control workflow.
