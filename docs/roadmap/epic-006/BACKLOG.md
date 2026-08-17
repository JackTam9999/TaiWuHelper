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

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E6-000

Document the exact meaning of candidate states, role definitions, hard gates,
score components, total scores, ties, ordering, filters, comparisons, evidence
indicators, and responsive interaction states before adding public contracts.

#### Acceptance criteria

- [x] Every delivered role has a stable identity, purpose, version, supported
      source versions, hard requirements, scored dimensions, normalization
      rules, weights, and tie breakers.
- [x] Scores are explicitly local to one role and are not probabilities,
      grades of intrinsic worth, or comparable across roles.
- [x] Eligible, ineligible, incomplete, unsupported, and conflicting candidate
      states have distinct meanings and presentation.
- [x] Ranked, tied, and unranked evaluation states have exact rules.
- [x] Missing required evidence cannot produce a total score; optional missing
      evidence follows a documented role-specific rule.
- [x] Filtering changes visible rows but not evaluation facts, rank identity,
      or the disclosed unfiltered result count.
- [x] Candidate comparison identifies decisive strengths, weaknesses, hard
      gates, ties, tradeoffs, evidence gaps, and unavailable facts.
- [x] The contract distinguishes current ability from development potential.
- [x] Desktop and narrow-screen states define role selection, shortlist,
      comparison, evidence details, loading, empty, unsupported, conflict, and
      failure behavior.
- [x] Keyboard order, focus behavior, headings, live-region use, table or card
      semantics, and non-color status cues are documented.
- [x] Recruitment instructions, development plans, settlement assignment,
      persistence, export, and game control are confirmed out of scope.

#### Evidence when complete

- `docs/architecture/COMPANION-ROLE-EVALUATION-CONTRACT.md`.
- `docs/roadmap/epic-006/UI-006-companion-candidate-finder.md`.
- English and Traditional Chinese semantic wireframes using synthetic data.

#### Completion evidence

- `COMPANION-ROLE-EVALUATION-CONTRACT.md` defines the two version-1 role
  identities, exact hard-gate order, one identity-normalized component with
  weight 1, role-local total, competition ranking, explicit ties, evidence
  states, canonical order, filters, comparison, and atomic lifecycle.
- Missing, incomplete, unsupported, or conflicting base qualification forbids
  a total score. A confirmed numeric zero remains distinct from missing.
- The first UI design uses a dedicated `/companions` page, explicit read
  action, 960-pixel container boundary, fact-equivalent cards on narrow
  layouts, native controls, deterministic focus order, polite announcements,
  and complete English/Traditional Chinese terminology.
- Synthetic desktop and narrow wireframes contain no real save identity or
  value and show the adjacent limitation that aptitude scores are not current
  attainment, success probability, or universal companion quality.

## Slice 2: Candidate and role Domain

### E6-002 — Add immutable companion-candidate profile contracts

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E6-000, E6-001

Define presentation-neutral Domain values for candidate identity, candidate-
universe state, availability, profile facts, evidence, source versions,
conflicts, unavailable reasons, diagnostics, and deterministic fingerprints.

#### Acceptance criteria

- [x] Candidate identity uses a stable language-independent character identity;
      display text remains separate.
- [x] Candidate-universe and availability states cannot be inferred from name,
      age, location, or target-lookup membership.
- [x] Each profile fact has a stable field identity, typed value, provenance,
      evidence state, source version, and optional unavailable reason.
- [x] Confirmed, incomplete, unsupported, stale, and conflicting evidence have
      explicit invariants.
- [x] Missing evidence cannot construct a confirmed fact or a zero value.
- [x] Source conflicts retain every candidate value and precedence decision.
- [x] Collections copy into immutable values with stable ordering and reject
      nulls, duplicates, invalid enums, blank identities, and incompatible
      evidence.
- [x] A profile fingerprint includes semantic facts and source or rule versions
      but excludes localized text, local paths, and irrelevant timestamps.
- [x] Domain types have no Application, Infrastructure, Presentation,
      persistence, filesystem, process, reflection, or GameData dependencies.
- [x] Unit tests cover valid, empty, duplicate, incomplete, unsupported, stale,
      conflicting, and deterministic-fingerprint cases.

#### Evidence when complete

- `src/TaiWu.Domain/CompanionCandidates/` immutable contracts.
- `tests/TaiWu.Domain.UnitTests/CompanionCandidates/` invariant tests.
- Updated `docs/architecture/COMPANION-CANDIDATE-SOURCES.md`.

#### Completion evidence

- The Domain contract uses stable numeric candidate identity, typed field and
  discipline identities, a closed typed fact-value union, explicit universe
  and evidence states, versioned provenance, retained conflict candidates,
  unavailable reasons, and typed diagnostics.
- Confirmed, incomplete, unsupported, stale, and conflicting factories enforce
  mutually exclusive state invariants. A missing fact has no value, while a
  confirmed zero remains a real typed value.
- Profiles and nested evidence collections defensively copy, reject null or
  duplicate semantic identities, and sort canonically before fingerprinting.
- The semantic fingerprint includes saved identity, universe state, source and
  mapping versions, facts, conflict decisions, and diagnostics while excluding
  localized text, filesystem paths, free-form detail, and timestamps.
- Fifteen focused contract tests pass inside the 436-test Domain suite and the
  dependency-free `TaiWu.Domain` project builds with zero warnings.

### E6-003 — Define versioned role definitions and evaluation rules

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E6-001, E6-002

Represent player-selectable roles as immutable, versioned definitions over
verified candidate-profile fields. Keep eligibility, hard requirements,
scored dimensions, normalization, weighting, and tie breaking explicit.

#### Acceptance criteria

- [x] Role identity and rule version are stable and non-localized.
- [x] Each role declares supported profile and GameData versions.
- [x] Candidate-universe eligibility is evaluated before role hard
      requirements, and hard requirements are evaluated before scoring.
- [x] Every rule references typed verified fields rather than display text or
      raw descriptions.
- [x] Each scored dimension defines its unit, direction, normalization range,
      weight, missing-evidence behavior, and explanation identity.
- [x] Weights and normalization cannot hide a failed hard requirement.
- [x] Total scores remain role-local and retain all component values and
      evidence references.
- [x] Tie breakers use stable semantic facts and never localized text or source
      enumeration order.
- [x] Unsupported role or source versions fail closed with typed diagnostics.
- [x] At least two verified role definitions demonstrate different hard
      requirements or score tradeoffs over shared candidate profiles.
- [x] Unit tests cover valid definitions, invalid weights, missing fields,
      failed gates, unsupported versions, exact ties, and deterministic rule
      identity.

#### Evidence when complete

- `src/TaiWu.Domain/CompanionRoles/` definitions and rule contracts.
- `tests/TaiWu.Domain.UnitTests/CompanionRoles/` invariant tests.
- `docs/architecture/COMPANION-ROLE-EVALUATION.md`.

#### Completion evidence

- The immutable definition model exposes stable role and rule versions,
  supported source versions, typed discipline bounds, ordered hard gates,
  complete score-dimension semantics, exact tie policy, and a deterministic
  rule fingerprint.
- The verified catalogue resolves unknown identities and unsupported role
  versions as typed fail-closed results. Its martial and life-skill roles use
  different approved typed fields over the same candidate-profile contract.
- The pure single-profile evaluator stops on the first non-passing universe,
  source, discipline, evidence, or provenance gate. Only complete compatible
  facts create components and a role-local total.
- Score components retain raw and normalized values, direction, weight,
  contribution, explanation identity, and evidence. Exact equal totals remain
  ties; candidate ID and localized text never change merit.
- Twenty-two focused test cases (including a four-case evidence-state theory) pass
  inside the 458-test Domain suite with deterministic definition and
  evaluation fingerprints.

## Slice 3: One-pass source projection

### E6-004 — Project a one-pass read-only candidate snapshot

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E6-000, E6-002

Add an Application read port and Infrastructure adapter that project the
verified candidate universe and required raw profile facts from one configured
save revision through one bounded archive session.

#### Acceptance criteria

- [x] The Application owns immutable request and result contracts; the port is
      explicitly read-only.
- [x] Infrastructure reads only the configured trusted save path and accepts no
      caller-provided filesystem path.
- [x] One request opens and projects the archive once instead of invoking a
      single-character archive workflow for each candidate.
- [x] Candidate identity, universe state, eligibility inputs, role inputs,
      availability, and location use only sources approved by E6-000.
- [x] The snapshot records save fingerprint, captured time, GameData version,
      mapping version, load warnings, omissions, and diagnostics.
- [x] A character-level mapping failure cannot fabricate facts or corrupt other
      candidates; its typed omission or incomplete state remains visible.
- [x] Cancellation is observed during large candidate enumeration.
- [x] Enumeration and mapping order do not determine stable output order.
- [x] Save missing, read failure, unsupported version, expected standalone
      runtime boundary, partial result, and changed-revision states are typed.
- [x] No mutable GameData, reflection, archive, or infrastructure type crosses
      the port.
- [x] Infrastructure tests cover representative states and prove the reader has
      no write, process, network, input, or game-control path.
- [x] Guarded local integration records before and after fingerprints and meets
      the E6-000 performance budget.

#### Evidence when complete

- Candidate snapshot port and contracts under `TaiWu.Application`.
- One-pass adapter and focused mappings under `TaiWu.Infrastructure/SaveGames`.
- Unit, architecture, and opt-in guarded integration tests.
- `docs/architecture/COMPANION-CANDIDATE-SNAPSHOT.md`.

#### Completion evidence

- The path-free Application request and immutable typed result expose complete,
  partial, save-unavailable, unsupported-version, changed-revision, and safe
  read-failure states without an Infrastructure or GameData type.
- The configured-path-only adapter performs exactly one aggregate
  `TaiwuArchiveReadSession.ReadAsync` call and maps each roster candidate
  independently with cancellation and canonical ordering.
- A complete profile contains 101 approved facts: eligibility inputs,
  descriptive saved facts, learned/equipped identities, 30 base aptitude
  values, and 60 explicit unsupported current qualification/attainment facts.
- Unit and architecture suites cover immutable contract invariants, missing,
  partial, ineligible, conflict, invalid-buffer, deterministic, dependency-
  injection, one-pass, and no-mutation behavior.
- The guarded production test returned one complete 101-fact profile with no
  omissions, equivalent repeated fingerprints, one expected standalone
  warning, a 20.487-second cold read, and a 2-millisecond warm read. The save
  and two loaded GameData assemblies retained identical SHA-256, length, and
  last-write time.

## Slice 4: Enrichment and evaluation

### E6-005 — Enrich candidate profiles with verified catalogue and progress facts

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E6-002, E6-003, E6-004, Epic 2

Join the one-pass candidate snapshot to current version-aware catalogue and
character-progress facts required by the approved role matrix. Introduce a
batch projection or join boundary where the existing single-character reader
would otherwise reopen the archive repeatedly.

#### Acceptance criteria

- [x] Enrichment uses stable skill, feature, field, and character identities;
      localized names remain display values.
- [x] Only E6-000-approved fields enter a role-evaluable profile.
- [x] Learned, equipped, mastered, proficiency, study, or other progress states
      retain their existing verified meanings rather than being collapsed.
- [x] Catalogue and progress version compatibility is checked explicitly.
- [x] Missing, stale, rebuilding, partial, unsupported, and failed enrichment
      states remain typed at candidate and result level.
- [x] Missing enrichment cannot become zero progress, no skill, or failed role
      suitability.
- [x] One candidate's unavailable progress does not suppress unrelated
      candidates or fabricate comparative facts.
- [x] The workflow does not call the archive-opening character atlas reader in
      an N+1 loop.
- [x] Enrichment ordering and parallel scheduling cannot affect the result.
- [x] Tests cover version match and mismatch, missing catalogue, partial
      progress, duplicate facts, unsupported fields, and deterministic joins.

#### Evidence when complete

- Candidate profile builder or enrichment service under `TaiWu.Application`.
- Any required batch source contract and Infrastructure mapping.
- Unit and guarded integration tests.
- Updated companion snapshot and role-evaluation architecture documents.

#### Completion evidence

- The Application enrichment service joins exact saved learned/equipped
  martial identities to a current compatible combat-skill catalogue while
  returning the original immutable profiles unchanged.
- Learned, equipped, and learned-life-skill collections retain available,
  incomplete, unsupported, stale, or conflicting evidence. Missing membership
  is nullable and never becomes `false`, an empty collection, zero progress,
  or failed suitability.
- Mastery, proficiency, study, breakthrough, activation, and other detailed
  progress are explicitly not requested because neither approved role uses
  them. The service has no single-character progress-reader dependency and one
  catalogue query call site for the whole snapshot.
- Missing, stale, rebuilding, unsupported, corrupt, query-failed, and partial
  catalogue states remain distinct at result and candidate level. Bilingual
  definition names remain display data and do not affect semantic identity.
- Thirteen focused Application cases and two architecture checks cover the
  deterministic join and safety boundary. The guarded production test retained
  one candidate and 57 saved combat-skill identities while reporting the local
  helper catalogue as stale, accepting no stale definitions, and leaving the
  save plus seven catalogue sources unchanged.

### E6-006 — Evaluate role suitability and rank comparable candidates

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E6-003, E6-005

Evaluate candidate-universe eligibility, hard requirements, scored dimensions,
and deterministic tie breakers for one selected role. Rank comparable
evaluations and retain typed reasons for candidates that cannot enter the
shortlist.

#### Acceptance criteria

- [x] Eligibility is evaluated before hard requirements and scoring.
- [x] A failed hard requirement produces an explicit unranked result and cannot
      be overcome by score components.
- [x] Missing required evidence produces incomplete or unsupported evaluation,
      never a numeric penalty.
- [x] Every component retains rule identity, source evidence, normalized value,
      weight, contribution, and explanation identity.
- [x] Total score arithmetic is bounded, deterministic, and validated.
- [x] Equal evaluations remain explicit ties until documented stable tie
      breakers apply.
- [x] Character ID or localized name may stabilize display ordering only after
      semantic rank and tie status are established; neither changes merit.
- [x] Ranked, tied, ineligible, incomplete, unsupported, and conflicting
      results remain distinct.
- [x] Identical candidate profiles, role versions, and rule versions produce
      equivalent components, diagnostics, ordering, and fingerprints.
- [x] Unit tests cover every approved role, hard-gate failure, missing optional
      and required evidence, extremes, ties, unsupported versions, conflicts,
      and deterministic reruns.

#### Evidence when complete

- Pure Domain evaluator, result contracts, and shortlist builder.
- Domain unit tests using synthetic candidate profiles only.
- Updated `docs/architecture/COMPANION-ROLE-EVALUATION.md`.

#### Completion evidence

- `CompanionRoleShortlistBuilder` copies and validates one candidate universe,
  evaluates every unique profile exactly once through the E6-003 evaluator,
  and never reads or scores an enrichment display value.
- Exact decimal totals form descending merit groups. Competition ranks skip
  after ties (`1, 2, 2, 4`), ties remain explicit, and character ID only
  canonicalizes entries after the semantic score group is fixed.
- `CompanionRoleCandidateRanking` retains `Ranked`, `Tied`, `Ineligible`,
  `Incomplete`, `Unsupported`, or `Conflicting` independently of presentation.
  Every exclusion retains the original evaluation, gates, reasons, and evidence
  with no fabricated total or rank.
- `CompanionRoleRanking` validates exact role-definition and discipline
  compatibility, one exact candidate source-version set, unique candidate
  identity, ranking states, score groups, and competition ranks, then
  fingerprints the canonical semantic result.
- Sixteen pure Domain cases cover both verified roles, ordered hard gates,
  required and irrelevant optional evidence, exact components, extremes, ties,
  all exclusions, unsupported inputs, source-version comparability,
  deterministic reruns, semantic changes, duplicate identities, cancellation,
  and an empty candidate universe.

## Slice 5: Shortlist and comparison

### E6-007 — Build evidence-aware shortlist and candidate comparison explanations

**Status:** Complete

**Priority:** P1

**Estimate:** M

**Dependencies:** E6-001, E6-006

Turn role evaluations into an immutable shortlist and comparison model that
explains decisive strengths, weaknesses, hard gates, tradeoffs, ties, and
missing evidence without re-scoring in the UI.

#### Acceptance criteria

- [x] The shortlist retains selected role identity, rule version, source
      identity, unfiltered counts, ranked entries, ties, exclusions, and
      diagnostics.
- [x] Every ranked entry explains its strongest contributions and material
      limitations using existing evaluation components.
- [x] Ineligible or unranked candidates retain exact reasons when exposed.
- [x] Comparing two candidates uses the same immutable role evaluations and
      does not create a second ranking path.
- [x] Comparison rows use stable field or rule identity and show both value and
      evidence state.
- [x] Decisive differences, equal facts, missing evidence, hard-gate outcomes,
      and genuine tradeoffs remain distinguishable.
- [x] Location or availability is displayed only when E6-000-approved evidence
      supports it.
- [x] Filters do not mutate evaluations, scores, ties, or the source shortlist.
- [x] Explanations never recommend unverified recruitment, training, travel,
      equipment, or assignment actions.
- [x] Unit tests cover top results, ties, exclusions, incomplete evidence,
      filtered views, comparisons, and equivalent reruns.

#### Evidence when complete

- Domain or Application shortlist and comparison contracts and builders.
- Focused unit tests.
- `docs/architecture/COMPANION-CANDIDATE-COMPARISON.md`.

#### Completion evidence

- `CompanionRoleShortlist` retains the exact ranking, definition, discipline,
  candidate source versions, canonical entries, ranked and excluded views, all
  six typed state counts, profile and semantic diagnostics, and a deterministic
  fingerprint.
- Ranked explanations point to existing score-component objects for strongest
  contribution, declared score scope, and exact ties. Excluded explanations
  point to the exact existing non-passing gate and outcome identity.
- `CompanionRoleComparison` selects two exact shortlist entries and creates one
  stable row per existing dimension. Confirmed values and evidence states remain
  visible, while comparisons involving unranked or conflicting evidence never
  invent a score difference.
- Comparison advantage uses existing direction-aware component contributions,
  so normalization, weighting, totals, and ranking are not recalculated.
  Conflict, unavailable, tradeoff, advantage, and equality remain distinct.
- Status filters return views over the original entry objects and retain all
  unfiltered counts. Confirmed current-save location evidence is separated from
  stale or unavailable location facts without affecting merit.
- Seventeen focused Domain cases cover counts, ties, exclusions, explanations,
  direction-aware comparison outcomes, filters, location evidence, invalid
  selections, deterministic reruns, and an empty shortlist.

## Slice 6: Application and API vertical

### E6-008 — Orchestrate one coherent companion-finder result

**Status:** Complete

**Priority:** P1

**Estimate:** M

**Dependencies:** E6-004, E6-005, E6-006, E6-007

Compose snapshot reading, catalogue and progress enrichment, role resolution,
evaluation, shortlist construction, filters, and comparison from one request
and one immutable source revision.

#### Acceptance criteria

- [x] Requests accept a stable role identity and bounded product filters but no
      filesystem path, raw rule definition, arbitrary expression, or game
      command.
- [x] Unknown roles, unsupported role versions, invalid filters, and invalid
      comparison selections fail with typed results.
- [x] One workflow result binds save fingerprint, GameData version, catalogue
      version, profile mapping version, role version, and evaluation version.
- [x] A save revision change triggers a complete new result rather than mixing
      candidate facts from two revisions.
- [x] Catalogue and progress failures preserve candidate-source evidence and
      return honest partial or unavailable states where permitted.
- [x] Filters are applied after authoritative evaluation and retain original
      counts and result identity.
- [x] Cancellation reaches source projection and expensive evaluation loops.
- [x] Repeated equivalent requests return semantically equivalent immutable
      results.
- [x] Tests cover success, empty, partial, unsupported, stale, conflict,
      changed-revision, cancellation, filter, and comparison states.

#### Evidence when complete

- Application use case and request/result contracts.
- Application unit tests with substituted read-only ports.
- `docs/architecture/COMPANION-FINDER-APPLICATION.md`.

#### Completion evidence

- The immutable request accepts one stable role/version, typed discipline,
  bounded status filter, and optional pair of positive distinct comparison IDs.
  It exposes no path, raw definition, expression, sorting policy, or command.
- Validation and exact role resolution precede one path-free snapshot call.
  The workflow then enriches the returned snapshot, evaluates its unchanged
  profiles once, constructs one shortlist, and only then applies view and
  comparison selections.
- Authoritative results require one reference-identical snapshot, enrichment,
  ranking, shortlist, view, and optional comparison chain. Source identity binds
  save, GameData, catalogue, mapping, role, evaluation, and discipline versions.
- Missing, stale, rebuilding, unsupported, corrupt, and failed catalogue states
  retain snapshot evidence in a `Partial` result because version-1 role scores
  do not depend on catalogue display enrichment. No progress fact is invented.
- The authoritative fingerprint excludes capture time, filter, comparison, and
  localization state. A changed save revision rebuilds the entire result and
  changes the fingerprint without mixing profile revisions.
- Cancellation reaches the source and per-candidate loop. Nineteen focused
  Application cases and two architecture checks cover the request, state map,
  source chain, partial evidence, filters, comparisons, deterministic reruns,
  cancellation, and changed-revision rebuild.

### E6-009 — Expose typed companion-finder API contracts

**Status:** Complete

**Priority:** P1

**Estimate:** M

**Dependencies:** E6-008

Expose the available role presets and complete companion-finder result through
localhost-only HTTP endpoints and pure response mappers.

#### Acceptance criteria

- [x] Role discovery exposes stable identity, version, localized purpose,
      supported state, and limitations.
- [x] Finder responses expose source identity, role identity, candidate states,
      hard gates, score components, shortlist order, ties, tradeoffs,
      provenance, conflicts, unavailable reasons, and diagnostics.
- [x] API types do not expose local paths, proprietary raw content, internal
      GameData types, reflection objects, or mutation-capable handles.
- [x] Request validation bounds filters and comparison selections.
- [x] HTTP behavior distinguishes invalid request, missing save, unsupported
      version, partial result, conflict, cancellation, and internal failure.
- [x] Mapping is pure and cannot recompute eligibility, scores, or ordering.
- [x] Traditional Chinese and English display values map from the same stable
      identities and facts.
- [x] OpenAPI documents score limitations and evidence states without
      universal-best or probability language.
- [x] Controller, contract, mapper, serialization, localization, and
      architecture tests cover all response states.

#### Evidence when complete

- Companion-finder controller and response contracts under `TaiWuAPI`.
- API and architecture tests.
- `docs/api/COMPANION-CANDIDATES.md`.

#### Completion evidence

- `GET /api/companion-candidates/roles` exposes both verified presets with
  stable identity/version, typed discipline range, supported state, bilingual
  purpose, and explicit role-local score limitation.
- `POST /api/companion-candidates/find` validates bounded transport input and
  maps one Application execution into source, role, count, candidate, gate,
  component, fact, conflict, location, enrichment, comparison, and diagnostic
  response contracts.
- Missing, incomplete, unsupported, stale, and conflicting score evidence is
  explicit. Current numeric values exist only for confirmed facts, and API
  contracts contain no local path, raw content, internal GameData object,
  reflection type, general object payload, or mutation handle.
- HTTP `200`, `206`, `400`, `404`, `409`, `422`, `499`, and `500` distinguish
  complete/empty, partial, invalid, missing, changed-revision, unsupported,
  cancelled, and failed states. Candidate conflict remains typed response
  evidence rather than an HTTP revision conflict.
- The pure mapper copies existing order, rank, ties, outcomes, components, and
  evidence. Architecture checks forbid evaluator, ranking-builder, merit-
  comparer, filesystem, or process use in the controller and mapper.
- English and Traditional Chinese change only mapped display text. OpenAPI
  response metadata and property descriptions state that totals are role-local,
  not universal rankings, probabilities, or action recommendations.
- Twenty-two focused API cases and two architecture checks cover role
  discovery, controller behavior, mapping, localization parity, serialization,
  public types, every response state, and no re-ranking.

## Slice 7: Core UI

### E6-010 — Deliver the bilingual accessible companion-finder UI

**Status:** Complete

**Priority:** P1

**Estimate:** L

**Dependencies:** E6-001, E6-009

Add a focused page where the player selects a role, reviews the evidence-aware
shortlist, filters visible candidates, compares candidates, and inspects
reasons without introducing a second evaluation path in Presentation.

#### Acceptance criteria

- [x] The page follows `UI-006-companion-candidate-finder.md` and the shared UI
      presentation guidelines.
- [x] Role selection precedes ranking and clearly states the objective and
      limitations.
- [x] The summary distinguishes total considered, eligible, ranked, tied,
      ineligible, incomplete, unsupported, and conflicting candidates.
- [x] Shortlist entries show rank or tie, role-local score when available,
      decisive strengths, material limitations, evidence state, and verified
      availability or location.
- [x] The comparison view exposes the same immutable evaluation components and
      hard gates without recalculation.
- [x] Filters disclose the unfiltered result count and do not change score or
      rank facts.
- [x] Missing, stale, rebuilding, partial, unsupported, conflicting, empty,
      loading, cancelled, and failed states have actionable information-only
      messages.
- [x] Score labels state that values are role-local and are neither universal
      rankings nor success probabilities.
- [x] Traditional Chinese and English copy is complete and uses stable
      identity-backed localization.
- [x] Narrow layouts preserve fact parity without horizontal dependence; wide
      layouts do not force assistive technology through duplicated content.
- [x] Headings, landmarks, table or list semantics, labels, focus order,
      expanded states, status announcements, and non-color cues are tested.
- [x] Presentation types cannot recruit, train, move, equip, assign, persist,
      upload, automate input, or mutate game state.

#### Evidence when complete

- Razor components, presentation view models, pure mappers, and localization.
- Presentation and rendered semantic tests.
- Completed `docs/roadmap/epic-006/UI-006-companion-candidate-finder.md`.
- English and Traditional Chinese desktop and narrow-screen review captures
  using synthetic or redacted data.

#### Completion evidence

- `/companions` Razor page, reusable candidate-result component, pure
  Presentation mapper/view models, helper-session interaction state, and the
  enum-backed 122-key bilingual copy catalogue.
- Bilingual candidate and location descriptors captured inside the existing
  one-pass save projection, plus a path-free installed discipline-label source;
  all display values remain outside evaluation profiles and fingerprints.
- Mapper, rendering, API, Infrastructure, and architecture coverage for every
  visible state, immutable comparison, native semantics, single-DOM responsive
  rendering, no raw-ID fallback, and no Presentation mutation path.
- Candidate evidence uses a closed native disclosure with an exact passed/total
  summary; full typed gates remain available on demand and the role-wide score
  limitation is not repeated inside every candidate.
- [Presentation architecture](../../architecture/COMPANION-FINDER-PRESENTATION.md),
  updated [API contract](../../api/COMPANION-CANDIDATES.md), and
  [browser review](../../reviews/E6-010-companion-finder-ui.md).
- Synthetic [English desktop](../../reviews/assets/epic-006/companion-finder-en-desktop.png),
  [Traditional Chinese narrow result](../../reviews/assets/epic-006/companion-finder-zh-narrow.png),
  and [narrow candidate-card](../../reviews/assets/epic-006/companion-finder-zh-narrow-candidates.png)
  captures; no real save value, identity, fingerprint, or local path is stored.
  These PNGs are retained as historical responsive-layout evidence; the
  corrected fixture and executable Razor rendering tests own current eligible-
  count and typed-state semantics.

## Slice 8: Verification and completion

### E6-011 — Verify safety, batching, determinism, and cross-layer parity

**Status:** Complete

**Priority:** P1

**Estimate:** L

**Dependencies:** E6-002 through E6-010

Audit the complete vertical with synthetic matrices, automated suites,
architecture rules, performance checks, and guarded read-only local scenarios.

#### Acceptance criteria

- [x] Domain tests cover every candidate, eligibility, evidence, hard-gate,
      score, tie, exclusion, comparison, and fingerprint invariant.
- [x] Each delivered role has synthetic positive, negative, incomplete,
      unsupported, conflicting, and tied cases.
- [x] Application tests prove one coherent revision and deterministic rebuild
      after a revision change.
- [x] Infrastructure tests prove one bounded archive projection rather than an
      archive open per candidate.
- [x] API and Presentation tests prove parity with the immutable Application
      result and no re-ranking.
- [x] English and Traditional Chinese states contain equivalent facts and
      accessible names.
- [x] Performance verification meets the cold and warm budgets selected by
      E6-000 for the representative candidate universe.
- [x] Repeated and reordered synthetic inputs produce equivalent evaluations,
      ties, shortlist ordering, comparisons, diagnostics, and fingerprints.
- [x] Architecture tests forbid mutation-capable GameData dependencies,
      persistence, process access, screenshot handling, automation, or input
      control in the Epic 6 vertical.
- [x] Guarded local verification records save and installed-source fingerprints
      before and after every read scenario and reports no changes.
- [x] Release build, default test matrix, formatter verification, Markdown link
      validation, and `git diff --check` pass.
- [x] Every Epic 6 acceptance criterion maps to implementation or evidence.

#### Evidence when complete

- `docs/reviews/E6-011-automated-verification.md`.
- Automated test suites and guarded integration results.
- Performance and non-interference evidence without proprietary data or local
  paths.

#### Completion evidence

- [Automated verification and Epic traceability](../../reviews/E6-011-automated-verification.md).
- Two-role Domain state matrix covering positive, ineligible, incomplete,
  unsupported, conflicting, and tied candidates under reordered inputs.
- Three guarded Release integration scenarios, including two repeated complete
  role workflows and all installed discipline-language sources; every guarded
  save/runtime/source file remained byte-for-byte unchanged.
- Release build with zero warnings/errors, 1,248 default non-integration tests,
  formatter and link checks, architecture capability scans, and clean diff
  validation.

### E6-013 — Add a transparent companion capability overview

**Status:** Complete

**Priority:** P1

**Estimate:** M

**Dependencies:** E6-004, E6-008 through E6-010

Add a compact companion-to-companion summary over all verified saved-base
attributes and aptitudes while preserving the selected role as the only
ranking and recommendation path.

#### Acceptance criteria

- [x] Each candidate profile contains six typed saved base main attributes in
      the verified Strength, Dexterity, Concentration, Vitality, Energy, and
      Intelligence order.
- [x] The summary exposes separate arithmetic averages for all six main
      attributes, 14 martial aptitudes, and 16 life-skill aptitudes only when
      every expected component is confirmed.
- [x] A versioned breadth index uses the disclosed equal-weight mean of the
      three category averages, rounded deterministically to two decimals.
- [x] API and bilingual Presentation expose exact confirmed/expected coverage,
      category averages, breadth index, and the top three localized values.
- [x] Missing, incomplete, unsupported, stale, or conflicting evidence remains
      explicit and never becomes zero or a fabricated partial average.
- [x] The overview is visibly labelled as saved-base descriptive evidence and
      does not change role eligibility, score, rank, tie, shortlist order,
      explanation, recommendation, or finder fingerprint.
- [x] Comparison uses a separate semantic table with responsive single-DOM
      behavior and no raw character or discipline identity fallback.
- [x] The archive adapter copies the fixed six-value buffer once per candidate
      inside the existing guarded one-pass session and introduces no mutation
      or current-modified getter.
- [x] Domain, Infrastructure, API, Presentation, rendering, and architecture
      tests cover complete and unavailable summaries, formula transparency,
      localization, and non-interference with role ranking.

#### Completion evidence

- `CompanionCapabilitySummaryBuilder` and typed main-attribute profile facts.
- Updated one-pass snapshot mapping, API contracts, bilingual comparison view,
  responsive styling, and executable Razor rendering coverage.
- [E6-000 source evidence](../../scenarios/E6-000-companion-candidate-evidence.md),
  [snapshot architecture](../../architecture/COMPANION-CANDIDATE-SNAPSHOT.md),
  [API contract](../../api/COMPANION-CANDIDATES.md), and
  [UI contract](./UI-006-companion-candidate-finder.md).
- Release verification and updated browser review recorded under E6-010 and
  E6-011.

### E6-012 — Validate representative roles and close Epic 6

**Status:** Awaiting product-owner decision

**Priority:** P1

**Estimate:** M

**Dependencies:** E6-011, E6-013

Compare each delivered role, candidate state, shortlist explanation, and
responsive UI against the verified game sources and representative scenarios.
Record remaining limitations and obtain the explicit completion decision.

#### Acceptance criteria

- [x] Every delivered role is manually checked against at least one privacy-
      safe local or redacted representative scenario.
- [x] Candidate-universe membership and eligibility are checked independently
      from target lookup.
- [x] Hard requirements, score components, ties, strengths, weaknesses,
      tradeoffs, and missing evidence agree across source facts, Domain,
      Application, API, and UI.
- [x] At least one incomplete, unsupported, or conflicting candidate remains
      visibly unranked rather than receiving an invented low score.
- [x] English and Traditional Chinese desktop and narrow layouts preserve fact
      parity, readable order, keyboard operation, and non-color status cues.
- [x] Performance remains within the E6-000 budget on the representative save.
- [x] Save, GameData, language, and other inspected fingerprints remain
      unchanged.
- [x] Companion development, village assignment, library planning,
      persistence, export, automation, and game control are confirmed absent.
- [x] All Epic 6 acceptance criteria have linked implementation or evidence.
- [x] Remaining unsupported roles or mechanics become explicit future backlog
      candidates rather than partially implemented rules.
- [ ] The product owner records the Epic 6 completion decision.

#### Evidence when complete

- `docs/reviews/E6-012-manual-verification.md`.
- Reviewed English and Traditional Chinese captures using synthetic or
  redacted content.
- Product-owner completion decision in `EPIC.md` and the roadmap index.

#### Evidence ready for decision

- [Representative manual verification](../../reviews/E6-012-manual-verification.md)
  covering both live role families and both languages without recording local
  identity or value data.
- Four authoritative representative API responses retained exact semantic
  parity across localization and one stable save revision.
- Observed cold `27.929` seconds and warm `3`/`1`/`1` milliseconds, within the
  E6-000 budgets; two subsequent expanded E6-011 integration runs each
  completed all three guarded scenarios in about 24 seconds with no changes,
  and the fresh saved-installation-path correction run passed 3 of 3 in
  `27.846` seconds.
- Candidate-universe source inspection, synthetic unavailable/tie UI review,
  cross-layer traceability, scope audit, and explicit deferred-mechanics list.
- Independent closure review findings were corrected: exact universe-eligible
  counts replace rankability counts, Presentation retains specific snapshot,
  enrichment, and catalogue states, and the read-only guard includes all eight
  bilingual name/map packs inspected during candidate display projection.
- Fresh re-review corrections make the visible boundary match the saved
  non-Taiwu roster universe, derive those eight guard paths from the same
  configured-save production locator as display reads, and retain exact
  evaluation plus identifiable ordered gate semantics through Presentation and
  comparison.

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
