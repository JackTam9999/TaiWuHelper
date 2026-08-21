# E8-013 representative tactical-combat verification

| Field | Value |
|---|---|
| Status | Complete |
| Evidence date | 2026-08-21 |
| Epic | [EPIC-008](../roadmap/epic-008/EPIC.md) |
| Backlog item | [E8-013](../roadmap/epic-008/BACKLOG.md#e8-013--validate-the-golden-tactical-plan-and-close-epic-8) |
| Automated evidence | [E8-012 verification](./E8-012-automated-verification.md) |
| UI evidence | [E8-011 browser verification](./assets/epic-008/E8-011-browser-verification.md) |
| Completion decision | Approved on 2026-08-21 after independent closure review |
| Proprietary data committed | None |

## Current result

The representative historical-version synthetic vertical passes the complete
manual contract review. The selected plan suppresses the verified core cast,
keeps mark and resonance mitigation separate, represents the suppression
self-lock as an execution cost, requires conditional Reverse-practice recovery,
and remains visibly fallback-only because E8-000 found no typed finish rule.
Hard feasibility gates, exact accepted-loadout costs and allocations, bounded
search diagnostics, policy differences, bilingual rendering, and every required
synthetic lifecycle state remain coherent across Domain, Application, API, and
Presentation.

Epic 8 is complete. After an initial unstable attempt was rejected, a stable
retry passed both guarded local tactical integrations. The authorized
independent closure review found no remaining actionable defect, and the
product owner approved completion on 2026-08-21. Current-version behavioral
reauthorization remains separate follow-up work and is not implied by closure.

## Representative tactical-plan review

The review uses the sanitized E8-000 scenario rather than a current character
or target identity. The installed runtime remains newer than the pinned
historical rules, so current-version behavior is `Unsupported`; unchanged
public IDs or localized descriptions do not bypass that gate.

| Required invariant | Reviewed result | Evidence |
|---|---|---|
| Core cast suppression | Reverse `604` / effect `1064` is the exact primary response to the verified Direct-practice cast transition | E8-000 counter boundary, tactical rule resolver, candidate discovery, and plan compiler |
| Mark mitigation | Direct `267` / effect `165` remains a separate equipped-passive role | E8-000 counter boundary and exact-role fixtures |
| Resonance mitigation | Reverse `134` / effect `973` is conditional on its active-agility requirement | E8-000 counter boundary and active-role hard-gate fixture |
| Known gaps | Live mark count, resonance count, target resource, and temporary layers remain manual observations or unresolved branches | E8-000 context inventory and plan unknown-trigger fixture |
| Suppression cost | Three layers that prevent Direct-practice casts are retained as a verified self-lock cost | scorer recovery component and compiler fixtures |
| Recovery | Each otherwise feasible Reverse-practice cast removes one layer; three casts are scheduled only when explicitly preselected | ordered-recovery and recovery-gap fixtures |
| Reset pressure | Reverse `291` / effect `915` remains random-resource mitigation and never a guaranteed reset lockout | E8-000 reset boundary and fallback plan |
| Finish | `FallbackOnly`; no damage, success, optimality, or victory-probability claim | compiler fallback fixture and UI information-only boundary |

This is a conditional paper plan. It does not advance combat state, select
hidden behavior, send input, or modify the game.

## Loadout and hard-gate reconciliation

Candidate discovery and final search both retain the existing validator as the
loadout authority. Ownership, learned membership, exact direction, mastery,
breakthrough, raw effect, active role, weapon/style, trick, distance, resource,
inner-power, backlash, effective category cost, category capacity, universal
slot allocation, and legendary fixed-cost facts must be satisfied. Unknown,
unsupported, and conflicting values cannot become zero, empty, or satisfied.

The preparation checklist is compiled from the selected feasible proposal. Its
skill directions, effective costs, category capacities, universal-slot
allocation, and required manual changes are the same values shown in the
accepted loadout and comparison. If the legacy comparison is unavailable, it
contains only its diagnostic and cannot publish a competing proposal.

## Search and policy review

Option, exploration, elapsed-time, and result bounds have separate deterministic
fixtures. Candidate-universe, relevance, dominance, explored, feasible, retained,
and truncated counts remain visible. A bounded result names its first terminator
and makes no completeness or optimality claim after truncation.

Safe, Balanced, and Aggressive use the same hard-feasibility result. Their
weights and semantic fingerprints remain distinct both with complete evidence
and when finish evidence is unavailable. Policy selection changes ranking
preference only; it cannot make an infeasible skill feasible or invent a finish
component.

## Synthetic state review

| Required state | Reviewed semantic result | Principal fixture |
|---|---|---|
| Complete | One coherent selected loadout, score, comparison, and six-stage fallback plan | `Execute_builds_one_coherent_result_from_one_snapshot` |
| Partial | Legacy result retained while missing tactical evidence stays typed | `Execute_retains_legacy_result_when_evidence_is_partial` |
| Unsupported | Historical rules and candidates are withheld for the installed version | `Execute_stops_safely_on_an_unsupported_rule_chain` |
| No candidate | No empty loadout is promoted as a recommendation | `Execute_reports_no_candidate_without_promoting_empty_loadout` |
| Truncated | Bound and terminator remain visible; no optimality claim | `Execute_labels_plan_from_bounded_search_as_truncated` |
| Cancelled | Cancellation propagates without a mixed partial result or ranking | cancellation fixtures in search, scoring, compiler, Application, and API |
| Fallback-only | Finish is unavailable; fallback has no damage or victory claim | `Compile_builds_six_stage_conditional_fallback_only_plan` |
| Observation replaced | One atomic replacement changes all dependent semantic identities | `Execute_treats_observation_set_as_atomic_replacement` |
| Failure | Source, evidence, and unexpected faults become safe typed guidance | Application failure fixtures and API/Presentation mapping |

The post-refactor focused E8-013 state audit passed 82 tests: 35 Domain
plan/search/score, 12 Application workflow, and 35 API/Presentation contract
and rendering tests. E8-012 additionally records the pre-review 1,595-test
Release matrix.

## Bilingual and responsive parity

English and Traditional Chinese are generated from the same typed semantic
structure. Exhaustive localization tests cover fixed copy plus every typed
stage, condition, evidence state, policy, finish state, candidate decision,
score component, search terminator, direction, and source value.

The E8-011 browser review confirmed the same ordered conditions, manual actions,
purposes, evidence, gaps, progressive disclosures, and information-only boundary
at 1280 by 720 and 390 by 844 CSS pixels. Both viewports had no document or
component horizontal overflow, and native disclosure keyboard focus remained
available.

## Determinism and source audit

Repeated and shuffled requests retain every semantic fingerprint, selected
proposal, candidate order, pruning group, score, plan stage, comparison, cache
count, and work count. Capture time and elapsed duration remain honest
diagnostic metadata outside semantic identity.

The 2026-08-21 immutable helper-catalogue audit reported schema 4, 946
definitions, 4 warnings, 0 errors, and no WAL. All eight representative skill
records were present with typed fields and direction-specific descriptions.
The catalogue hash, length, and timestamp were unchanged across the manifest
and batch queries. The manifest remains historical/display evidence and does
not authorize the newer installed runtime.

The first current save/runtime/language retry did not pass its stability
precondition: the archive changed during the first read, and the next guard
could not open the exclusively locked save. The app discarded both attempts.
A later stable retry passed `TacticalExecutionContextIntegrationTests` and
`TacticalCombatEvidenceIntegrationTests`. Together they proved repeatable
projection, cancellation, the expected unsupported historical-rule boundary,
the representative aggregate invariants, performance bounds, and unchanged
save, GameData, and bilingual language sources.

The standalone skill inspector could not rebuild because its event compiler
recursively included unrelated generated C# sources; it failed before performing
a save inspection. The guarded repository integrations provide the successful
source proof, while the inspector failure remains evidence that the alternate
path failed closed.

| Source gate | Current E8-013 result |
|---|---|
| Immutable helper catalogue | Pass; read-only, no WAL, unchanged hash/length/timestamp |
| Guarded current save | Pass; stable repeated reads and unchanged hash/length/timestamp |
| Guarded GameData and bilingual language sources | Pass; the bounded tactical probes retained every guarded source unchanged |
| Current seven-source tactical capture | Pass; 7 of 7 guarded files unchanged |

No path, source hash, character or target identity, item identity, raw source
text, save content, binary content, or screenshot from the local audit is
committed.

## Self-review and refactor corrections

This earlier maintainer self-review preceded, and did not replace, the
independent closure review recorded below.

| Finding | Correction and proof |
|---|---|
| Rule resolution, context projection, and latest-observation metadata were duplicated across four Application workflows | One internal `TacticalExecutionContextProjection` boundary now supplies all four workflows. A regression compares recommendation and direct context-reader semantic fingerprints and capture metadata. |
| Candidate pagination survived replacement by a different semantic result | `TacticalCandidatePagingState` now keys expansion to the full result fingerprint, retains expansion only for the same result, and resets to 25 entries for a replacement. |
| The first projection refactor draft overrode an explicit proposal with the default proposal | The new parity regression exposed the difference; caller-supplied proposal precedence was restored before completion. |
| The first paging draft widened the Razor event payload from a typed group identity to the complete group object | The presentation capability guard rejected the change; the event again carries only the enum identity and resolves the group locally. |
| A missing proposal synthesized unavailable unlocked-weapon and resource facts as known empty collections, while standalone context reads treated them differently | All Application workflows now use one explicit current-loadout baseline that copies only captured facts. Domain and Application regressions prove unknown facts remain unavailable and recommendation/context-reader fingerprints agree. |
| Presentation recovered a step's skill by parsing numeric tokens from its manual-action identity | The plan and response contracts now carry a nullable typed skill ID, the mapper consumes only that field, and the `TACTICAL_COMBAT_PLAN_V2` fingerprint includes it. Domain and rendering regressions prove both semantic identity and decoy numeric tokens are handled correctly. |

Post-refactor verification passed 99 tactical Domain, 27 tactical Application,
36 tactical API/Presentation, and 6 tactical architecture tests. A fresh
Release solution build completed with 0 warnings and 0 errors. The latest
combined Release matrix contains 1,601 tests: 1,584 passed, 17 expected guarded
local skips, and 0 failed. The architecture assembly was rerun from its normal
repository-relative output because its source-boundary tests intentionally
locate the repository from the assembly path; all 114 architecture tests passed.

## Independent closure review

The product owner authorized the independent closure review on 2026-08-21. The
review was performed after the maintainer refactors and treated the committed
Epic 8 implementation, contracts, evidence, and tests as the review subject.

The review independently checked:

- exact-version rule rejection and the installed-version `Unsupported` result;
- candidate admission through typed roles and every existing hard feasibility
  gate;
- proposal precedence, current-loadout unavailable facts, and one-snapshot
  context parity;
- accepted-loadout, comparison, score, preparation, plan-step skill identity,
  and full-result fingerprint agreement;
- search bounds, truncation, cancellation, fallback-only finish semantics, and
  absence of optimality or victory claims;
- candidate paging replacement, bilingual typed rendering, and narrow/wide
  information parity; and
- filesystem, process, screenshot, upload, persistence, automation, input,
  game-control, and mutation capability exclusions.

No remaining actionable finding was identified. A fresh Release build passed
with 0 warnings and 0 errors. The complete non-opt-in suite passed with 1,601
tests: 1,584 passed, 17 expected guarded-local skips, and 0 failed. Both
authorized tactical guarded integrations then passed against the current local
save with no skips; their before/after source checks retained the save, GameData,
and language sources unchanged.

## Deferred work

Additional targets and skill roles, current-version behavioral reauthorization,
persisted plans or outcomes, outcome learning, screenshot capture or
interpretation, combat simulation, damage or victory probabilities, unbounded
user formulas, process access, automation, input control, save writing, and
game-state mutation remain explicitly outside Epic 8. The complete list remains
in the [Epic 8 backlog](../roadmap/epic-008/BACKLOG.md#future-work-outside-epic-8).

## Closure gates

- [x] Rerun the two guarded tactical integrations against a stable, unlocked
      current save and record unchanged save, GameData, and language sources.
- [x] Complete an independent Epic 8 closure review and correct every actionable
      finding. No remaining actionable finding was identified.
- [x] Request and record the product-owner completion decision after all
      technical and independent-review gates pass.

All three gates are complete. The product owner approved Epic 8 completion on
2026-08-21; the current-version expansion remains a separate evidence-gated
follow-up.
