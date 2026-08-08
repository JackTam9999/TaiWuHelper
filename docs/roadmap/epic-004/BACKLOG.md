# Epic 4 backlog: Side-by-side loadout comparison and change planning

This backlog implements [EPIC-004](./EPIC.md) while preserving the permanent
safety boundary in
[ADR-0001](../../architecture/ADR-0001-absolute-game-non-interference.md).

## Conventions

### Priority

- **P0:** Required for the first trustworthy comparison vertical.
- **P1:** Required for Epic 4 completion.
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
- derive comparison facts from one immutable recommendation result;
- reuse authoritative manual-plan and feasibility semantics;
- preserve unavailable values, diagnostics, provenance, and evidence;
- use stable skill IDs and categories instead of localized names as identity;
- avoid interpreting policy scores as win probability;
- expose bilingual and accessible states without relying on color alone;
- leave every save, game file, configuration value, running process, runtime
  memory location, and in-game state unchanged;
- introduce no game hook, injection, patch, automation, screenshot capture,
  file upload, or input-control capability;
- update architecture, API, UI, testing, and roadmap evidence where the
  contract changes; and
- record the relevant test command and result.

## Delivery order

| Order | Slice | Outcome |
|---:|---|---|
| 0 | Comparison contract | Vocabulary and UX states cannot misrepresent existing results |
| 1 | Domain model | Immutable rows, cells, actions, and diagnostics are typed |
| 2 | Comparison builder | Current and three policy winners are normalized deterministically |
| 3 | API vertical | Typed comparison facts reach clients without UI re-derivation |
| 4 | Core UI | Desktop users can compare all policies and follow exact changes |
| 5 | Responsive and accessible UI | Mobile, keyboard, and bilingual users receive equivalent facts |
| 6 | Tactical explanation | Capacity, threats, requirements, caveats, and evidence explain differences |
| 7 | Verification and completion | Safety, determinism, parity, and product acceptance close the epic |

## Slice 0: Comparison contract

### E4-000 — Define comparison semantics and UI states

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** Epic 1, Epic 2, Epic 3

Document the exact meaning of every column, skill state, capacity value,
diagnostic, filter, responsive mode, and evidence indicator before adding new
public contracts.

#### Acceptance criteria

- [x] Current, Safe, Balanced, and Aggressive columns are defined as one
      recommendation-result boundary.
- [x] Retain, Add, Remove, ChangeDirection, and CompleteBreakthrough map to the
      existing manual-plan semantics.
- [x] Composite actions on one skill are explicitly supported.
- [x] Stable identity is skill ID plus category; localized text is display
      only.
- [x] Current and proposed capacity, remaining slots, effective cost, and 萬用
      allocation have explicit available/unavailable rules.
- [x] Infeasible policies remain diagnostic columns rather than empty
      loadouts.
- [x] Policy-local scores are distinguished from probability or universal
      ranking.
- [x] Desktop and narrow-screen interaction states are specified.
- [x] The design records keyboard order, headings, legends, focus behavior,
      and non-color status cues.
- [x] Screenshot, persistence, export, outcome feedback, and game control are
      confirmed out of scope.

#### Evidence when complete

- `docs/architecture/LOADOUT-COMPARISON-CONTRACT.md`.
- `docs/roadmap/epic-004/UI-004-loadout-comparison.md`.
- Representative English and Traditional Chinese wireframes or semantic
  component snapshots using synthetic data only.
- Verification on 2026-08-08: Markdown link targets and `git diff --check`
  passed.

## Slice 1: Domain model

### E4-001 — Add immutable comparison contracts

**Status:** Planned

**Priority:** P0

**Estimate:** M

**Dependencies:** E4-000

Add presentation-neutral Domain values for comparison identity, columns,
category rows, skill cells, composite actions, capacity summaries, tactical
summaries, provenance, and diagnostics.

#### Acceptance criteria

- [ ] A comparison owns one snapshot reference and exactly one Current column.
- [ ] At most one column exists for each recommendation policy.
- [ ] A category row cannot contain duplicate skill identities or mismatched
      skill categories.
- [ ] Membership state is separate from direction-change and
      breakthrough-required actions.
- [ ] Unavailable numeric or tactical values carry non-blank reasons.
- [ ] Infeasible policies cannot contain a feasible proposed loadout.
- [ ] Public references are stable logical identifiers, not local paths.
- [ ] Collections are immutable and ordering is constructor-validated or
      normalized by the builder.
- [ ] Domain contracts reference no GameData, ASP.NET Core, SQLite, files,
      screenshots, or processes.
- [ ] Tests cover validation, equality, immutability, composite states, missing
      policies, and unavailable values.

#### Evidence when complete

- `docs/architecture/LOADOUT-COMPARISON-DOMAIN.md`.
- Focused Domain test summary.

## Slice 2: Comparison builder

### E4-002 — Build deterministic comparisons from recommendation results

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E4-001

Create a pure builder that normalizes the current `PlayerCombatSnapshot` and
each policy's selected result without reading Infrastructure or recalculating
recommendations.

#### Acceptance criteria

- [ ] The builder accepts one `CombatLoadoutRecommendation` and performs no
      save, catalogue, database, network, or clock read.
- [ ] Current membership comes from the player snapshot used by that result.
- [ ] Proposed membership, capacity, scoring, threat links, conditions,
      caveats, and diagnostics come from the matching style result.
- [ ] Every manual change has a corresponding comparison action and every
      comparison action traces to a manual change.
- [ ] Add plus direction change and Add plus breakthrough remain composite.
- [ ] Current and proposed 萬用 allocations use the existing validated
      snapshots and proposals.
- [ ] Infeasible or missing styles produce typed diagnostics.
- [ ] Skill and row ordering is category-first and stable-ID based.
- [ ] Repeated identical input produces structurally equal output.
- [ ] Tests cover all-retained, mixed changes, capacity changes, composite
      actions, infeasible policies, unavailable values, and deterministic
      ordering.

#### Evidence when complete

- `docs/architecture/LOADOUT-COMPARISON-BUILDER.md`.
- Manual-plan parity test matrix.

## Slice 3: API vertical

### E4-003 — Expose typed loadout comparison contracts

**Status:** Planned

**Priority:** P0

**Estimate:** M

**Dependencies:** E4-002

Project the comparison through the combat-recommendation API so clients do not
need to rediscover comparison rules from raw style data.

#### Acceptance criteria

- [ ] The API exposes snapshot identity, Current provenance, policy status,
      category rows, skill states, composite actions, capacity, tactical
      summaries, evidence references, and diagnostics.
- [ ] Existing recommendation facts retain their existing meaning and stable
      references.
- [ ] Unavailable values remain nullable or explicitly unavailable with a
      reason; zero is never used as a substitute.
- [ ] Infeasible policies cannot serialize a fake empty proposal.
- [ ] Public contracts expose no save path, game path, screenshot path,
      process identifier, or exception detail.
- [ ] Serialization order is deterministic.
- [ ] Controller tests cover feasible, mixed, infeasible, unavailable, and
      observed-baseline responses.
- [ ] API documentation includes backward-compatibility and versioning notes.

#### Evidence when complete

- `docs/architecture/LOADOUT-COMPARISON-API.md`.
- Updated combat-recommendation API documentation.
- Focused API test summary.

## Slice 4: Core UI

### E4-004 — Build the desktop comparison matrix

**Status:** Planned

**Priority:** P0

**Estimate:** L

**Dependencies:** E4-003

Add a comparison section to the combat recommendation page that lets desktop
users inspect Current, Safe, Balanced, and Aggressive results together.

#### Acceptance criteria

- [ ] The matrix shows all five skill categories in stable order.
- [ ] Every skill cell displays localized name, category, membership state,
      and any direction or breakthrough action.
- [ ] Capacity summaries show used, total, remaining, effective cost where
      available, and 萬用 allocation.
- [ ] Current baseline provenance is visible.
- [ ] Infeasible policies show their diagnostic in place of loadout rows.
- [ ] A legend explains statuses and does not rely on color alone.
- [ ] All rows and differences-only modes preserve required manual actions.
- [ ] Choosing a policy links to or selects the existing setup checklist and
      battle plan for that same policy.
- [ ] Loading, failure, stale, and observation-cleared states replace the
      matrix atomically.
- [ ] Component tests cover feasible, partially infeasible, unchanged,
      changed, and unavailable renderings.

#### Evidence when complete

- UI component implementation and rendering tests.
- Updated recommendation-page architecture note.

## Slice 5: Responsive and accessible UI

### E4-005 — Add narrow-screen, bilingual, and keyboard interaction

**Status:** Planned

**Priority:** P1

**Estimate:** L

**Dependencies:** E4-004

Make the same comparison facts usable without a four-column viewport and
complete the English/Traditional Chinese accessibility pass.

#### Acceptance criteria

- [ ] Narrow screens show Current plus one explicitly selected policy.
- [ ] Switching policies does not lose filters, category position, warnings,
      or the relationship to the setup checklist.
- [ ] Desktop and narrow-screen modes expose equivalent comparison facts.
- [ ] Keyboard users can reach the policy selector, category navigation,
      differences filter, legend, warnings, and evidence details in logical
      order.
- [ ] Focus remains stable after policy or filter changes.
- [ ] Screen readers receive meaningful column, category, skill, and status
      labels.
- [ ] Status is conveyed by text/icon semantics in addition to color.
- [ ] All new user-facing text has complete English and Traditional Chinese
      localization.
- [ ] Long bilingual skill names and unavailable reasons do not overflow or
      hide required actions.
- [ ] Rendering tests cover both languages and representative desktop/narrow
      states.

#### Evidence when complete

- `docs/roadmap/epic-004/UI-004-loadout-comparison.md`.
- Bilingual and accessibility rendering test summary.
- Manual keyboard and responsive verification record.

## Slice 6: Tactical explanation

### E4-006 — Compare threat coverage, requirements, and unresolved risks

**Status:** Planned

**Priority:** P1

**Estimate:** M

**Dependencies:** E4-002, E4-004

Add compact factual summaries that explain why policy proposals differ without
introducing new combat rules or probability language.

#### Acceptance criteria

- [ ] Each policy shows verified covered threat codes and titles.
- [ ] Unresolved threats, unmet/manual conditions, caveats, and unsupported
      mechanics remain visible.
- [ ] Evidence references link tactical summaries to existing typed facts.
- [ ] Policy score components retain their weights and explanations.
- [ ] Cross-policy totals are not styled or labelled as win probability or a
      universal best score.
- [ ] Differences-only mode cannot hide an unresolved critical risk.
- [ ] Raw effect prose or evidence-only power cannot become new coverage.
- [ ] Tests cover distinct coverage, identical coverage, unsupported evidence,
      critical caveats, and policy-local scoring.

#### Evidence when complete

- `docs/architecture/LOADOUT-COMPARISON-EXPLANATION.md`.
- Focused Application and Presentation test summary.

## Slice 7: Verification and completion

### E4-007 — Verify comparison safety, parity, and determinism

**Status:** Planned

**Priority:** P1

**Estimate:** M

**Dependencies:** E4-003, E4-005, E4-006

Run the full automated and local read-only verification matrix, audit every
Epic 4 criterion, and record the product-owner decision.

#### Acceptance criteria

- [ ] Domain tests cover all comparison invariants and composite actions.
- [ ] Application tests prove manual-plan parity and deterministic ordering for
      all three policies.
- [ ] API tests prove typed unavailable and infeasible states survive mapping.
- [ ] Presentation tests cover bilingual desktop and narrow-screen workflows.
- [ ] Architecture tests prevent file, process, screenshot, persistence,
      game-control, and mutation-capable dependencies.
- [ ] Applying the same player/target observation repeatedly produces an
      equivalent comparison.
- [ ] Clearing observations reproduces the save-only comparison.
- [ ] A guarded current-save vertical verifies Current plus all available
      policy columns while preserving save, GameData, and language-resource
      fingerprints.
- [ ] Release build, default test matrix, formatting, and diff checks pass.
- [ ] Every Epic 4 acceptance criterion links to implementation or evidence.
- [ ] Deferred export, persistence, screenshot assistance, lower-ranked
      candidate exploration, and outcome feedback remain explicit future work.
- [ ] The product owner records the Epic 4 completion decision.

#### Evidence when complete

- `docs/reviews/E4-007-automated-verification.md`.
- `docs/reviews/E4-007-manual-verification.md`.
- Updated completion decision in [EPIC-004](./EPIC.md).

## Future work outside Epic 4

- Shareable bilingual recommendation cards built from the stable comparison
  projection.
- Persisted comparison or recommendation history with retention and deletion
  controls.
- Screenshot-assisted input after privacy and accuracy review.
- Arbitrary lower-ranked candidate exploration beyond policy winners.
- Player-reported battle outcomes and feedback.
- Cross-save or cross-target historical comparison.
