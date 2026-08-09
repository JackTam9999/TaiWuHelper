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
| 4 | Core UI | Desktop users can compare the two review policies and follow exact changes |
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

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E4-000

Add presentation-neutral Domain values for comparison identity, columns,
category rows, skill cells, composite actions, capacity summaries, tactical
summaries, provenance, and diagnostics.

#### Acceptance criteria

- [x] A comparison owns one snapshot reference and exactly one Current column.
- [x] At most one column exists for each recommendation policy.
- [x] A category row cannot contain duplicate skill identities or mismatched
      skill categories.
- [x] Membership state is separate from direction-change and
      breakthrough-required actions.
- [x] Unavailable numeric or tactical values carry non-blank reasons.
- [x] Infeasible policies cannot contain a feasible proposed loadout.
- [x] Public references are stable logical identifiers, not local paths.
- [x] Collections are immutable and ordering is constructor-validated or
      normalized by the builder.
- [x] Domain contracts reference no GameData, ASP.NET Core, SQLite, files,
      screenshots, or processes.
- [x] Tests cover validation, equality, immutability, composite states, missing
      policies, and unavailable values.

#### Evidence when complete

- `docs/architecture/LOADOUT-COMPARISON-DOMAIN.md`.
- Focused Domain test summary.
- Verification on 2026-08-08: Domain unit tests passed 308/308, architecture
  tests passed 78/78, and `dotnet format TaiWu.slnx --verify-no-changes
  --no-restore` passed.

## Slice 2: Comparison builder

### E4-002 — Build deterministic comparisons from recommendation results

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E4-001

Create a pure builder that normalizes the current `PlayerCombatSnapshot` and
each policy's selected result without reading Infrastructure or recalculating
recommendations.

#### Acceptance criteria

- [x] The builder accepts one `CombatLoadoutRecommendation` and performs no
      save, catalogue, database, network, or clock read.
- [x] Current membership comes from the player snapshot used by that result.
- [x] Proposed membership, capacity, scoring, threat links, conditions,
      caveats, and diagnostics come from the matching style result.
- [x] Every manual change has a corresponding comparison action and every
      comparison action traces to a manual change.
- [x] Add plus direction change and Add plus breakthrough remain composite.
- [x] Current and proposed 萬用 allocations use the existing validated
      snapshots and proposals.
- [x] Infeasible or missing styles produce typed diagnostics.
- [x] Skill and row ordering is category-first and stable-ID based.
- [x] Repeated identical input produces structurally equal output.
- [x] Tests cover all-retained, mixed changes, capacity changes, composite
      actions, infeasible policies, unavailable values, and deterministic
      ordering.

#### Evidence when complete

- `docs/architecture/LOADOUT-COMPARISON-BUILDER.md`.
- Manual-plan parity test matrix.
- Verification on 2026-08-08: Application unit tests passed 135/135,
  architecture tests passed 78/78, and solution formatting verification
  passed.

## Slice 3: API vertical

### E4-003 — Expose typed loadout comparison contracts

**Status:** Complete

**Priority:** P0

**Estimate:** M

**Dependencies:** E4-002

Project the comparison through the combat-recommendation API so clients do not
need to rediscover comparison rules from raw style data.

#### Acceptance criteria

- [x] The API exposes snapshot identity, Current provenance, policy status,
      category rows, skill states, composite actions, capacity, tactical
      summaries, evidence references, and diagnostics.
- [x] Existing recommendation facts retain their existing meaning and stable
      references.
- [x] Unavailable values remain nullable or explicitly unavailable with a
      reason; zero is never used as a substitute.
- [x] Infeasible policies cannot serialize a fake empty proposal.
- [x] Public contracts expose no save path, game path, screenshot path,
      process identifier, or exception detail.
- [x] Serialization order is deterministic.
- [x] Controller tests cover feasible, mixed, infeasible, unavailable, and
      observed-baseline responses.
- [x] API documentation includes backward-compatibility and versioning notes.

#### Evidence when complete

- `docs/architecture/LOADOUT-COMPARISON-API.md`.
- Updated combat-recommendation API documentation.
- Focused API test summary.
- Verification on 2026-08-08: API/controller tests passed 243/243,
  architecture tests passed 78/78, and solution formatting verification
  passed.

## Slice 4: Core UI

### E4-004 — Build the desktop comparison matrix

**Status:** Complete

**Priority:** P0

**Estimate:** L

**Dependencies:** E4-003

Add a comparison section to the combat recommendation page that lets desktop
users inspect Current, Safe, and Aggressive results together. Balanced remains
in the typed recommendation/API result behind the approved two-option UI.

#### Acceptance criteria

- [x] The matrix shows all five skill categories in stable order.
- [x] Every skill cell displays localized name, category, membership state,
      and any direction or breakthrough action.
- [x] Capacity summaries show used, total, remaining, effective cost where
      available, and 萬用 allocation.
- [x] Current baseline provenance is visible.
- [x] Infeasible policies show their diagnostic in place of loadout rows.
- [x] A legend explains statuses and does not rely on color alone.
- [x] All rows and differences-only modes preserve required manual actions.
- [x] Choosing a policy links to or selects the existing setup checklist and
      battle plan for that same policy.
- [x] Loading, failure, stale, and observation-cleared states replace the
      matrix atomically.
- [x] Component tests cover feasible, partially infeasible, unchanged,
      changed, and unavailable renderings.

#### Evidence when complete

- `TaiWuAPI/Components/Recommendations/LoadoutComparisonMatrix.razor` and
  focused mapper/rendering tests.
- `docs/architecture/LOADOUT-COMPARISON-PRESENTATION.md`.
- Verification on 2026-08-08: API/presentation tests passed 246/246 and
  architecture tests passed 78/78.

## Slice 5: Responsive and accessible UI

### E4-005 — Add narrow-screen, bilingual, and keyboard interaction

**Status:** Complete

**Priority:** P1

**Estimate:** L

**Dependencies:** E4-004

Make the same comparison facts usable without a three-column viewport and
complete the English/Traditional Chinese accessibility pass.

#### Acceptance criteria

- [x] Narrow screens show Current plus one explicitly selected policy.
- [x] Switching policies does not lose filters, category position, warnings,
      or the relationship to the setup checklist.
- [x] Desktop and narrow-screen modes expose equivalent comparison facts.
- [x] Keyboard users can reach the policy control, category navigation,
      differences filter, legend, warnings, and evidence details in logical
      order.
- [x] Focus remains stable after policy or filter changes.
- [x] Screen readers receive meaningful column, category, skill, and status
      labels.
- [x] Status is conveyed by text/icon semantics in addition to color.
- [x] All new user-facing text has complete English and Traditional Chinese
      localization.
- [x] Long bilingual skill names and unavailable reasons do not overflow or
      hide required actions.
- [x] Rendering tests cover both languages and representative desktop/narrow
      states.

#### Evidence when complete

- `docs/roadmap/epic-004/UI-004-loadout-comparison.md` implementation and
  verification record.
- Bilingual, selected-policy, filter-preservation, and accessibility rendering
  tests in `RecommendationComponentRenderingTests`.
- Verification on 2026-08-08: API/presentation tests passed 254/254,
  architecture tests passed 79/79, and solution formatting verification
  passed.

## Slice 6: Tactical explanation

### E4-006 — Compare threat coverage, requirements, and unresolved risks

**Status:** Complete

**Priority:** P1

**Estimate:** M

**Dependencies:** E4-002, E4-004

Add compact factual summaries that explain why policy proposals differ without
introducing new combat rules or probability language.

#### Acceptance criteria

- [x] Each policy shows verified covered threat codes and titles.
- [x] Unresolved threats, unmet/manual conditions, caveats, and unsupported
      mechanics remain visible.
- [x] Evidence references link tactical summaries to existing typed facts.
- [x] Policy score components retain their weights and explanations.
- [x] Cross-policy totals are not styled or labelled as win probability or a
      universal best score.
- [x] Differences-only mode cannot hide an unresolved critical risk.
- [x] Raw effect prose or evidence-only power cannot become new coverage.
- [x] Tests cover distinct coverage, identical coverage, unsupported evidence,
      critical caveats, and policy-local scoring.

#### Evidence when complete

- `docs/architecture/LOADOUT-COMPARISON-EXPLANATION.md`.
- Focused Application and Presentation test summary.

Completed on 2026-08-08. Focused verification passed 135/135 Application,
256/256 API/presentation, and 79/79 architecture tests.

## Slice 7: Verification and completion

### E4-007 — Verify comparison safety, parity, and determinism

**Status:** Complete

**Priority:** P1

**Estimate:** M

**Dependencies:** E4-003, E4-005, E4-006

Run the full automated and local read-only verification matrix, audit every
Epic 4 criterion, and record the product-owner decision.

#### Acceptance criteria

- [x] Domain tests cover all comparison invariants and composite actions.
- [x] Application tests prove manual-plan parity and deterministic ordering for
      all three backend policies.
- [x] API tests prove typed unavailable and infeasible states survive mapping.
- [x] Presentation tests cover bilingual desktop and narrow-screen workflows.
- [x] Architecture tests prevent file, process, screenshot, persistence,
      game-control, and mutation-capable dependencies.
- [x] Applying the same player/target observation repeatedly produces an
      equivalent comparison.
- [x] Clearing observations reproduces the save-only comparison.
- [x] A guarded current-save vertical verifies Current plus all available
      policy columns while preserving save, GameData, and language-resource
      fingerprints.
- [x] Release build, default test matrix, formatting, and diff checks pass.
- [x] Every Epic 4 acceptance criterion links to implementation or evidence.
- [x] Deferred export, persistence, screenshot assistance, lower-ranked
      candidate exploration, and outcome feedback remain explicit future work.
- [x] The product owner records the Epic 4 completion decision.

#### Evidence when complete

- `docs/reviews/E4-007-automated-verification.md`.
- `docs/reviews/E4-007-manual-verification.md`.
- Updated completion decision in [EPIC-004](./EPIC.md).

Implementation and verification for the reversible two-option design
completed on 2026-08-08 and the whole-page duplication pass completed on
2026-08-09. Release build passed
with zero warnings/errors; the default matrix passed 926 total with 917 passed
and 9 expected opt-in skips; the guarded current-save vertical passed 1/1 with
all inspected fingerprints unchanged. The product owner approved Epic 4 and
authorized its merge to `master` on 2026-08-10.

## Future work outside Epic 4

- Shareable bilingual recommendation cards built from the stable comparison
  projection.
- Persisted comparison or recommendation history with retention and deletion
  controls.
- Screenshot-assisted input after privacy and accuracy review.
- Arbitrary lower-ranked candidate exploration beyond policy winners.
- Player-reported battle outcomes and feedback.
- Cross-save or cross-target historical comparison.
