# Epic 8 backlog: Evidence-backed exact-target tactical combat planner

This backlog implements [EPIC-008](./EPIC.md) while preserving the permanent
safety boundary in
[ADR-0001](../../architecture/ADR-0001-absolute-game-non-interference.md).

## Conventions

### Priority

- **P0:** Required for the first trustworthy exact-target tactical vertical.
- **P1:** Required for Epic 8 completion.
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
- use typed, version-matched evidence before a target fact, transition,
  tactical role, requirement, score, candidate, or instruction affects a
  result;
- distinguish observed state, verified transition, expected purpose, manual
  action, and unresolved branch;
- preserve immutable collections, stable identities, canonical ordering,
  deterministic fingerprints, provenance, unavailable reasons, conflicts, and
  diagnostics;
- reject localized names, category labels, untyped raw descriptions, nearby
  identifiers, and apparent similarity as mechanical rules;
- treat missing, stale, unsupported, and conflicting evidence as explicit
  states rather than zero, false, safety, or satisfied conditions;
- retain ownership, mastery, direction, breakthrough, raw-effect, backlash,
  active-role, inner-power, effective-cost, category-capacity, and
  universal-slot hard gates;
- bind every target chain, candidate set, score, comparison, and tactical plan
  to one coherent immutable request snapshot;
- keep candidate discovery and search deterministic, cancellable, bounded,
  and explicit about coverage and truncation;
- expose concise English and Traditional Chinese states without relying on
  color, a wide graph, or repeated evidence;
- leave every save, game file, configuration value, running process, runtime
  memory location, and in-game state unchanged;
- introduce no screenshot, upload, OCR, persistence, network/game-control,
  automation, input-control, or mutation capability;
- update architecture, API, UI, testing, scenario, review, and roadmap evidence
  where the contract changes; and
- record the relevant verification command and result.

## Delivery order

| Order | Slice | Outcome |
|---:|---|---|
| 0 | Evidence boundary | One target chain, execution context, recovery route, finish evidence, and limitations are verified before coding |
| 1 | Product and interaction contract | Conditional-plan stages, search completeness, policy semantics, and accessible presentation are fixed |
| 2 | Causal-chain Domain | Immutable state, transition, role, requirement, branch, and fingerprint contracts are established |
| 3 | Tactical evidence and context | Versioned rules and one coherent execution-context projection support the first vertical |
| 4 | Candidate discovery | The complete learned-skill atlas is considered through typed roles and hard feasibility gates |
| 5 | Search and selection | Target-aware pruning, bounded exploration, and evidence-aware scoring select a feasible candidate |
| 6 | Conditional plan | The accepted loadout becomes a preparation, opening, trigger, recovery, finish, and fallback plan |
| 7 | Application and API vertical | One coherent tactical result reaches clients through typed contracts |
| 8 | Core UI | Players can follow the manual plan and inspect its evidence bilingually |
| 9 | Verification and completion | Safety, bounds, determinism, parity, and the golden tactical invariants close the epic |

## Slice 0: Evidence boundary

### E8-000 — Verify tactical sources and select the golden exact-target vertical

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** Epic 1 combat recommendation foundations; Epic 2 catalogue
and character atlas; Epic 3 observation provenance; Epic 5 target playbooks;
ADR-0001

Inspect the minimum read-only save, local catalogue, installed GameData, and
confirmed observation sources needed to represent one exact target's causal
chain, player execution context, selected suppression or mitigation path,
execution cost, recovery route, and supported finish or fallback. Select the
first vertical only after exact semantics and standalone safety are recorded.

#### Acceptance criteria

- [x] The inspected Taiwu, GameData, language, catalogue schema, observation,
      and verified-rule versions are recorded.
- [x] One stable representative target or synthetic equivalent is selected
      without committing proprietary identity or raw source content.
- [x] Every candidate target state and transition records its exact owning
      source, field or effect identity, timing, meaning, version, precedence,
      completeness, and limitation.
- [x] Core cast, mark, resonance, threshold, defeat-prevention or reset, and
      temporary-lockout relationships are individually accepted, rejected, or
      marked unsupported rather than assumed as one chain.
- [x] At least one exact suppression, interrupt, or mitigation route is traced
      through its skill direction, raw effect, requirements, timing, and
      expected purpose.
- [x] The selected counter's resource use, self-lock, or other execution cost
      is verified or explicitly excluded.
- [x] At least one recovery route or honest safe fallback is verified for the
      selected execution cost.
- [x] Any proposed finish window or damage-channel choice has typed
      version-matched evidence; otherwise the first vertical is explicitly
      fallback-only.
- [x] Required weapon, combat style, distance, stance, breath, resource,
      active-defense, active-agility, inner-power, capacity, universal-slot,
      and legendary-cost facts are inventoried with unavailable states.
- [x] Existing current-save and observation precedence is tested against the
      selected tactical facts without silently replacing conflicts.
- [x] Context-dependent getters are probed for standalone safety and rejected
      where live process or special-effect context is required.
- [x] Localized names and raw descriptions are retained only as display
      evidence and never serve as identities or rules.
- [x] A synthetic golden scenario defines invariants for suppression,
      mitigation, recovery, feasibility, finish or fallback, and known gaps
      without pinning one brittle exact loadout.
- [x] One stable local probe records cold/warm timing, cancellation, catalogue
      reuse, and before/after source hashes.
- [x] The resulting evidence either authorizes E8-002 through E8-008 or records
      a narrower product boundary before those items begin.

#### Evidence

- `docs/scenarios/E8-000-tactical-combat-evidence.md`.
- `docs/scenarios/evidence/E8-000-golden-tactical-metadata.json` containing
  versions, hashes, counts, supported codes, and timing only.
- Focused read-only source and standalone-safety probes.

## Slice 1: Product and interaction contract

### E8-001 — Define tactical-plan, search, score, and UI semantics

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E8-000

Convert the selected evidence boundary into stable user-visible vocabulary and
interaction rules before implementing public Domain or API contracts.

#### Acceptance criteria

- [x] Preparation, opening, target-state trigger, recovery, finish, and
      fallback stages have distinct definitions and ordering semantics.
- [x] A stage may be unsupported or omitted without fabricating a complete
      timeline.
- [x] Observed state, verified transition, expected purpose, manual action,
      fallback, and unresolved evidence use different labels and semantics.
- [x] Candidate admitted, rejected, unsupported, irrelevant, and dominated
      states are defined without implying character weakness.
- [x] Candidate-universe, relevance, dominance, exploration, time, result,
      cancellation, and cache-reuse diagnostics have exact meanings.
- [x] Safe, Balanced, and Aggressive policy meanings remain distinct when a
      component is unavailable.
- [x] Damage, layered protection, duplicate coverage, execution cost, and
      unused-capacity presentation rules are explicit.
- [x] Shared evidence and limitations appear once per result; plan steps show
      only step-specific conditions and reasons.
- [x] Wide and narrow layouts expose the same facts from one semantic
      structure without requiring a causal graph.
- [x] Keyboard focus, live announcements, disclosures, policy changes,
      observation replacement, and failure recovery are defined.
- [x] English and Traditional Chinese terms are defined before implementation.
- [x] No label, button, or status implies execution, automation, guaranteed
      success, or probability of victory.

#### Evidence

- Completed [UI-008](./UI-008-tactical-combat-planner.md).
- `docs/architecture/TACTICAL-COMBAT-PLANNING-CONTRACT.md`.

## Slice 2: Causal-chain Domain

### E8-002 — Add immutable tactical state, transition, and plan contracts

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E8-000, E8-001

Add presentation-neutral Domain types for one bounded target chain and
conditional manual plan.

#### Acceptance criteria

- [x] Stable typed identities represent combat facts, transition definitions,
      requirements, timing, roles, plan stages, branches, evidence, and
      diagnostics.
- [x] State facts preserve available, incomplete, unsupported, and conflicting
      states with provenance.
- [x] A transition separates preconditions, resulting facts, timing, expected
      purpose, limitation, and evidence.
- [x] Transitions cannot advance a simulated clock, select hidden AI behavior,
      or claim an outcome beyond their verified effect.
- [x] Requirement results distinguish satisfied, unsatisfied, unknown,
      unsupported, and conflicting states.
- [x] Tactical steps distinguish manual action from expected verified purpose
      and from an unresolved or fallback branch.
- [x] Candidate consideration retains admitted, rejected, unsupported,
      irrelevant, and dominated decisions with typed reasons.
- [x] Search coverage retains counts and bounds with explicit units and cannot
      claim completeness after truncation.
- [x] Collections are immutable, validated, deduplicated, and canonically
      ordered.
- [x] Invalid cycles, dangling references, duplicate identities, incompatible
      evidence versions, and impossible stage order fail before planning.
- [x] Fingerprints exclude localized display text and capture time while
      including every semantic fact that can affect a result.
- [x] Domain projects reference no Application, Infrastructure, ASP.NET, UI,
      filesystem, process, reflection, or GameData type.
- [x] Unit tests cover validation, unavailable states, conflicts, ordering,
      equality, branching, and fingerprints.

#### Evidence

- `docs/architecture/TACTICAL-COMBAT-DOMAIN.md`.
- Focused Domain unit tests.

## Slice 3: Tactical evidence and context

### E8-003 — Define versioned causal-transition and tactical-role rules

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E8-000, E8-002

Encode only the target transitions and exact skill roles authorized by the
evidence gate. Extend the existing effect and counter vocabulary instead of
creating a name-derived parallel catalogue.

#### Acceptance criteria

- [x] Every transition and role rule has a stable identity, semantic version,
      supported source versions, evidence requirements, timing, and limitation.
- [x] Rules can represent only E8-000-approved purposes such as cast
      suppression, power or duration reduction, resource preservation,
      recovery, damage-channel choice, or finish-window support.
- [x] Exact skill ID, practice direction, raw effect ID, GameData version, and
      typed mechanics are required for every skill role.
- [x] A shared counter exposes only roles and target transitions relevant to
      the selected exact-target goals.
- [x] Several evidence prerequisites require the requested state on every
      identity rather than accepting one partial match.
- [x] Contrary exact-target evidence overrides a broad playbook rule; absence
      alone does not prove a transition false.
- [x] Unsupported versions return typed unsupported results rather than nearest
      rules or stale fallback behavior.
- [x] Raw descriptions remain display evidence and cannot be parsed in Domain
      matching, scoring, or planning.
- [x] Duplicate definitions, unknown references, invalid timing, and
      inconsistent source versions are rejected.
- [x] Unit tests pin every delivered rule, interaction, and version boundary.

#### Evidence

- `docs/architecture/TACTICAL-COMBAT-RULES.md`.
- Updated effect, counter, playbook, and tactical-rule tests.

### E8-004 — Project one coherent tactical execution context

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E8-000, E8-002, E8-003

Extend the existing immutable combat snapshot with exactly the player and
target facts required by the selected tactical vertical. Read and resolve the
facts once per request.

#### Acceptance criteria

- [ ] One tactical request uses one stable save revision, one observation set,
      one catalogue projection, and one compatible rule set.
- [ ] Equipped and unlocked weapon types, usable combat styles, current or
      opening distance, stance, breath, required resources, active defense and
      agility, inner power, category budgets, universal slots, and legendary
      costs are available only where E8-000 verified them.
- [ ] Missing source fields create typed unknown or unsupported facts rather
      than empty sets, zero values, or satisfied requirements.
- [ ] Current facts and proposed-loadout facts have distinct types and origins.
- [ ] The context identifies which facts are pre-combat configurable, manually
      observable, fixed for the request, or unavailable at runtime-independent
      planning time.
- [ ] Observation precedence retains conflicts and atomically replaces the
      complete tactical snapshot.
- [ ] Repeated catalogue and source projection within one request is reused and
      exact call counts are testable.
- [ ] Cancellation is honored before and during bounded source and mapping
      loops.
- [ ] No raw GameData object, save path, proprietary payload, or process state
      escapes Infrastructure.
- [ ] Repeated stable reads produce identical semantic snapshots while capture
      time remains separate observation metadata.
- [ ] Integration tests prove inspected source hashes and timestamps remain
      unchanged.
- [ ] Architecture tests forbid process, input, network/game-control, mutation,
      and unbounded source-enumeration capabilities.

#### Planned evidence

- `docs/architecture/TACTICAL-EXECUTION-CONTEXT.md`.
- Infrastructure unit and guarded local integration tests.

## Slice 4: Candidate discovery

### E8-005 — Discover verified tactical candidates from all learned skills

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E8-002 through E8-004; Epic 2 character skill atlas

Consider the complete learned-skill snapshot for exact tactical roles while
admitting only version-matched, typed, feasible options to combination search.

#### Acceptance criteria

- [ ] Every learned combat skill and available practice direction receives one
      canonical consideration result.
- [ ] A skill is admitted only when its exact version, direction, raw effect,
      typed role, timing, and requirements are verified.
- [ ] Unsupported catalogue entries remain visible as unsupported consideration
      results and cannot become generic candidates.
- [ ] Ownership, mastery, direction, immediate breakthrough availability, raw
      effect identity, backlash-on-use, active-role, inner-power, effective
      cost, category capacity, and universal-slot gates run before search.
- [ ] Unknown execution requirements prevent unconditional admission and retain
      the exact missing context.
- [ ] Current-loadout retention remains distinct from evidence that a skill has
      tactical value for the selected target.
- [ ] Opposite directions of one skill may be considered separately, but a
      resulting loadout selects at most one direction.
- [ ] Localized name, faction, category, weapon label, and raw effect text do
      not select a tactical role.
- [ ] Consideration is deterministic regardless of atlas enumeration order.
- [ ] Candidate counts, supported-role coverage, rejection reasons, and
      unsupported counts are bounded and aggregated without losing examples.
- [ ] Tests cover admitted, retained-only, infeasible, unknown-context,
      unsupported-effect, wrong-version, wrong-direction, breakthrough,
      backlash, and duplicate-direction cases.

#### Planned evidence

- `docs/architecture/TACTICAL-CANDIDATE-DISCOVERY.md`.
- Domain and Application candidate-discovery tests.

## Slice 5: Search and selection

### E8-006 — Add deterministic target-aware pruning and bounded search

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E8-005; existing feasible-loadout generator and validator

Reduce the verified eligible set using only documented target-aware proofs,
then explore feasible combinations within explicit request bounds.

#### Acceptance criteria

- [ ] Pruning runs only after evidence and hard-feasibility admission.
- [ ] Irrelevance requires no applicable verified target role or transition;
      absence of evidence alone cannot prove irrelevance.
- [ ] Dominance requires an explicit same-context proof across role value,
      timing, requirements, effective cost, conflicts, and execution risk.
- [ ] Every removed option retains one typed pruning rule and evidence set.
- [ ] Target-aware option ordering and combination traversal are deterministic.
- [ ] Search remains cancellable and bounded by option, combination,
      elapsed-time, and result limits.
- [ ] The result reports initial learned directions, role-supported options,
      hard-gate rejections, each pruning count, explored combinations, feasible
      results, and the first limiting bound.
- [ ] A time-limited search never uses wall-clock timing as a semantic
      fingerprint input; diagnostics report the budget state honestly.
- [ ] Hitting a bound cannot be labelled complete or optimal.
- [ ] Existing feasibility validation remains the sole authority for complete
      loadout acceptance.
- [ ] Per-request snapshot, catalogue, role-resolution, and capacity work is
      reused with bounded caches keyed by semantic identity.
- [ ] Tests prove deterministic results and diagnostics under shuffled inputs,
      every limit, cancellation, dominance ties, no-candidate, and cache-reuse
      scenarios.

#### Planned evidence

- `docs/architecture/TACTICAL-LOADOUT-SEARCH.md`.
- Domain and Application search, bound, cancellation, and cache tests.

### E8-007 — Score causal value, execution reliability, and supported finish paths

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E8-003, E8-006

Refine policy scoring so a feasible candidate is valued for supported marginal
tactical contribution rather than duplicate flat threat codes or automatically
unused capacity.

#### Acceptance criteria

- [ ] Threat value is derived from applicable causal-chain states and
      transitions, not merely the count of repeated threat codes.
- [ ] Duplicate coverage of one transition receives no duplicate full reward.
- [ ] Independently useful layered protection may receive marginal value only
      through a documented interaction or fallback rule.
- [ ] Timing and execution reliability account for preparation, trigger
      observability, resource requirements, self-lock or recovery cost, and
      unresolved context.
- [ ] Damage or finish-path value is available only from E8-000-approved typed
      attack, hit/cast reliability, target defense/resistance, and applicable
      condition evidence.
- [ ] Missing damage or finish evidence remains unavailable and its weight is
      excluded rather than becoming zero or an inferred average.
- [ ] Unused capacity is neutral unless a documented reserve or marginal-value
      rule makes it useful in the exact plan.
- [ ] Safe, Balanced, and Aggressive publish stable weights and retain distinct
      ranking behavior under the supported fixture matrix.
- [ ] Safe does not mean guaranteed survival, and Aggressive does not claim
      probability of victory or predicted damage without evidence.
- [ ] Score components expose raw inputs, normalization, weight, contribution,
      evidence, limitations, and unavailable state.
- [ ] Ranking remains deterministic and cannot override hard feasibility.
- [ ] Tests cover duplicate coverage, useful layering, unknown timing,
      recovery cost, unavailable damage, supported channel choice, unused
      capacity, policy distinction, ties, and input shuffling.

#### Planned evidence

- `docs/architecture/TACTICAL-RECOMMENDATION-SCORING.md`.
- Updated scorer and policy regression tests.

## Slice 6: Conditional plan

### E8-008 — Compile a conditional preparation-to-fallback battle plan

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E8-002, E8-004, E8-007

Compile the selected feasible loadout and verified target chain into one
conditional manual plan. Keep the first compiler limited to the E8-000 vertical
so it remains reviewable.

#### Acceptance criteria

- [ ] The plan is built only from the selected accepted feasible loadout,
      tactical snapshot, applicable transitions, and score evidence.
- [ ] Preparation covers supported manual direction, breakthrough, equipment,
      capacity, universal-slot, weapon, and context checks.
- [ ] Opening covers supported active defense, agility, resource, distance, and
      first-use choices.
- [ ] Trigger steps name observable or manually confirmable target states,
      required player state, manual response, expected purpose, and evidence.
- [ ] Recovery steps address the selected counter's verified self-lock,
      resource depletion, unmet trigger, or failed condition where supported.
- [ ] Finish steps require an exact supported window and feasible damage route;
      otherwise the plan exposes fallback-only or unsupported finish status.
- [ ] Fallback branches state which condition failed or remained unknown and
      select only separately verified feasible actions.
- [ ] Every step retains plan stage, order, condition, requirement state,
      manual action, expected purpose, reason, evidence, limitations, and stable
      identity.
- [ ] Unsupported stages remain explicit without placeholder actions.
- [ ] The plan cannot introduce a skill, direction, role, allocation, or context
      absent from the selected candidate and snapshot.
- [ ] The existing manual preparation plan and Epic 4 comparison agree with
      every proposed loadout change.
- [ ] Applying or clearing an observation atomically replaces chain, candidate,
      score, plan, comparison, and fingerprints.
- [ ] Tests cover the golden path, unavailable trigger, recovery branch,
      fallback-only finish, unsupported stage, comparison parity, observation
      lifecycle, and deterministic ordering.

#### Planned evidence

- `docs/architecture/CONDITIONAL-TACTICAL-PLAN.md`.
- Domain and Application plan-compilation tests.

## Slice 7: Application and API vertical

### E8-009 — Orchestrate one coherent tactical recommendation result

**Status:** Planned

**Priority:** P1

**Estimate:** M

**Dependencies:** E8-008; existing recommendation and observation orchestration

Compose snapshot reading, chain evaluation, candidate discovery, pruning,
search, scoring, plan compilation, comparison, and diagnostics behind one
Application request/result boundary.

#### Acceptance criteria

- [ ] One request names stable player, target, observation, policy, and bound
      inputs rather than localized labels.
- [ ] One coherent tactical snapshot supplies the entire result.
- [ ] Exact call-count tests prove source, catalogue, atlas, role, and capacity
      work is not repeated for each candidate or plan step.
- [ ] Source, evidence, context, rule, search, score, and planning failures map
      to typed result states.
- [ ] Cancellation propagates through discovery and search without returning a
      mixed partial result.
- [ ] Unexpected programmer faults reach host logging while client-facing
      results remain safe.
- [ ] Result identity includes snapshot, observation, target-chain, rule,
      candidate, bound, policy, selected-loadout, and plan fingerprints.
- [ ] Capture time and elapsed measurements remain diagnostics rather than
      semantic identity inputs.
- [ ] Observation apply, repeat, replace, and clear are atomic and idempotent.
- [ ] Existing recommendation, comparison, and manual-plan consumers remain
      compatible when tactical planning is unsupported.
- [ ] Application tests cover success, partial evidence, unsupported chain,
      no candidate, truncation, cancellation, observation lifecycle, and exact
      call counts.

#### Planned evidence

- `docs/architecture/TACTICAL-COMBAT-APPLICATION.md`.
- Application orchestration and regression tests.

### E8-010 — Expose typed tactical-planning API contracts

**Status:** Planned

**Priority:** P1

**Estimate:** M

**Dependencies:** E8-009

Add tactical planning to the read-only loopback recommendation surface without
asking clients to infer mechanics or branch logic from display text.

#### Acceptance criteria

- [ ] Requests expose only stable target, policy, observation, and bounded-search
      controls supported by the Application contract.
- [ ] Responses preserve snapshot summary, chain state, transitions, execution
      context, candidate consideration, pruning, search coverage, score,
      selected loadout, tactical stages, branches, evidence, and diagnostics.
- [ ] Every available, incomplete, unsupported, conflicting, rejected,
      truncated, cancelled, and fallback-only state survives mapping.
- [ ] Numeric enum values, unknown stable identities, and invalid bounds are
      rejected with safe validation responses.
- [ ] Response ordering matches Domain and Application canonical ordering.
- [ ] Localized text is display-only and stable identities remain language
      neutral.
- [ ] Local paths, raw saves, proprietary configuration objects, exceptions,
      process identifiers, screenshots, uploads, persistence commands, and
      mutation-capable types never enter contracts.
- [ ] No route equips, applies, executes, acknowledges completion, writes
      outcomes, or controls the game.
- [ ] Mapper and controller tests cover the complete tactical state matrix and
      public-token fixtures.
- [ ] API documentation includes complete, partial, unsupported, truncated,
      fallback-only, and observation-replaced examples.

#### Planned evidence

- Updated `docs/api/COMBAT-RECOMMENDATIONS.md`.
- Tactical contract, mapper, controller, and architecture tests.

## Slice 8: Core UI

### E8-011 — Deliver the bilingual accessible tactical-plan UI

**Status:** Planned

**Priority:** P1

**Estimate:** L

**Dependencies:** E8-001, E8-010

Deliver [UI-008](./UI-008-tactical-combat-planner.md) as a concise extension of
the existing recommendation workflow, using progressive disclosure rather
than duplicating the loadout, target strategy, or comparison surfaces.

#### Acceptance criteria

- [ ] The selected target, policy, evidence freshness, plan availability, and
      information-only scope are visible before plan steps.
- [ ] Preparation, opening, trigger, recovery, finish, and fallback stages have
      semantic headings and deterministic order.
- [ ] Each visible step shows a concise condition, manual action, expected
      purpose, and state; detailed evidence remains in one disclosure.
- [ ] Missing or conflicting conditions do not look satisfied, completed, or
      safe.
- [ ] Unsupported stages and fallback-only finish status are visible without
      empty placeholder instructions.
- [ ] Candidate and search summaries show considered, admitted, pruned,
      explored, retained, and limiting-bound facts without dumping every
      diagnostic into the primary timeline.
- [ ] Policy components expose available and unavailable states and never show
      win probability.
- [ ] The page does not duplicate full skill cards, threat lists, archetype
      details, manual loadout changes, or the Epic 4 comparison matrix.
- [ ] Applying, replacing, or clearing an observation replaces the complete
      active result; draft changes cannot relabel stale plan data.
- [ ] Wide and narrow layouts expose identical facts from one DOM with no
      horizontal overflow.
- [ ] Native headings, lists, buttons, and disclosures support keyboard and
      assistive-technology navigation; status never relies on color alone.
- [ ] English and Traditional Chinese copy is complete and raw identifiers are
      hidden where display text exists.
- [ ] Loading, complete, partial, unsupported, no-candidate, truncated,
      cancelled, fallback-only, stale-draft, and failure states are tested.
- [ ] The UI exposes no execute, equip, apply, automate, capture, upload,
      outcome-recording, or game-control action.

#### Planned evidence

- Epic 8 Presentation view models, mapper, localization, components, and styles.
- Component-rendering, localization, accessibility, and responsive tests.
- Browser verification assets under `docs/reviews/assets/epic-008/`.

## Slice 9: Verification and completion

### E8-012 — Verify evidence fidelity, safety, bounds, determinism, and parity

**Status:** Planned

**Priority:** P1

**Estimate:** L

**Dependencies:** E8-002 through E8-011

Prove that the complete tactical vertical preserves its evidence, feasibility,
search, scoring, plan, source, performance, and presentation contracts.

#### Acceptance criteria

- [ ] Domain tests cover state, transition, role, requirement, candidate,
      pruning, score, plan, branch, conflict, ordering, and fingerprint
      invariants.
- [ ] Application tests prove one coherent snapshot, bounded reuse, exact call
      counts, cancellation, observation replacement, and plan/comparison parity.
- [ ] Infrastructure tests prove guarded reads, standalone safety, source
      non-interference, stable projection, and unsupported runtime-dependent
      facts.
- [ ] API and Presentation retain every unavailable, conflict, rejection,
      pruning, truncation, score, and plan state.
- [ ] Localization coverage is exhaustive for typed Epic 8 keys and raw stable
      identities are not leaked as copy.
- [ ] Semantic architecture tests forbid localized mechanical matching,
      unbounded search, filesystem paths in contracts, process access,
      screenshots, uploads, persistence, network/game control, and mutation.
- [ ] Repeated identical requests produce identical chain, candidate, pruning,
      search, score, loadout, plan, comparison, ordering, and diagnostic
      fingerprints.
- [ ] Shuffled equivalent inputs produce the same result and every configured
      bound has deterministic fixture coverage.
- [ ] Safe, Balanced, and Aggressive remain distinct under complete and
      unavailable-damage fixtures.
- [ ] Cold and warm local performance budgets and cache-reuse counts are
      recorded from E8-000 evidence.
- [ ] English and Traditional Chinese wide/narrow rendering expose the same
      facts and keyboard path.
- [ ] Release build has zero warnings and the full non-opt-in suite passes.
- [ ] Guarded local tests record explicit skips when source evidence is
      unavailable.
- [ ] Every Epic 8 acceptance criterion links to implementation or evidence.

#### Planned evidence

- `docs/reviews/E8-012-automated-verification.md`.
- Updated domain-rule coverage and local-integration documentation.

### E8-013 — Validate the golden tactical plan and close Epic 8

**Status:** Planned

**Priority:** P1

**Estimate:** M

**Dependencies:** E8-012

Perform representative manual review, reconcile the complete Epic contract,
record deferred work, complete an independent closure review, and request the
product-owner completion decision.

#### Acceptance criteria

- [ ] The representative target and player facts match the E8-000 evidence
      without exposing proprietary identities, machine paths, or raw content.
- [ ] The plan suppresses or mitigates every E8-000-required core transition
      using only exact verified skill roles.
- [ ] The mark and resonance path retains its supported mitigation and explicit
      unresolved gaps.
- [ ] The selected suppression counter's verified execution cost has a feasible
      recovery step or an honest fallback branch.
- [ ] Inner-power backlash, unavailable directions, mastery, breakthrough,
      weapon/style, active-role, and resource constraints remain hard gates.
- [ ] The displayed effective costs, category capacities, universal-slot
      allocation, and manual changes exactly match the accepted loadout.
- [ ] A supported finish path uses typed evidence, or the plan is visibly
      fallback-only with no invented damage claim.
- [ ] Candidate and search coverage diagnostics explain every active bound and
      make no optimality claim after truncation.
- [ ] Safe, Balanced, and Aggressive differences remain understandable and do
      not change feasibility facts.
- [ ] English and Traditional Chinese wide/narrow states expose the same
      conditions, actions, purposes, evidence, gaps, and information-only
      boundary.
- [ ] Complete, partial, unsupported, no-candidate, truncated, cancelled,
      fallback-only, observation-replaced, and failure states are reviewed with
      synthetic data.
- [ ] Repeated runs retain stable semantic results and source fingerprints;
      capture time and elapsed diagnostics remain honest metadata.
- [ ] All inspected save, GameData, language, and catalogue source hashes and
      timestamps remain unchanged.
- [ ] Additional targets, broader skill roles, persistence, outcome learning,
      screenshots, simulation, probabilities, and game control remain explicit
      future work.
- [ ] Independent Epic 8 closure review is complete and every actionable
      finding is corrected and reverified.
- [ ] The product owner records the Epic 8 completion decision.

#### Planned evidence

- `docs/reviews/E8-013-manual-verification.md`.
- Completion decision in `EPIC.md` and the roadmap index.

## Future work outside Epic 8

- Additional exact targets, archetype families, causal chains, tactical roles,
  and verified effects beyond the first vertical.
- A general combat simulator, hidden-state inference, predicted turn sequence,
  damage distribution, target difficulty, or probability of victory.
- Persisted plans, observations, preferences, recommendations, reported
  outcomes, or regression histories.
- Automatic rule generation, statistical learning, or causal claims from wins
  and losses.
- Screenshot capture, upload, OCR, or automated image interpretation.
- Arbitrary user-authored weights, formulas, pruning rules, or unbounded search.
- Mid-combat equipment changes or other operations not independently verified
  as manual planning facts.
- Companion development, village expansion, library and book planning, or
  shareable recommendation exports.
- Any save writing, game-file changes, process access, automation, input
  control, or game-state mutation.
