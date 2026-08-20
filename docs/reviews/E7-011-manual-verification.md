# E7-011 representative village-workforce verification

| Field | Value |
|---|---|
| Status | Complete |
| Evidence date | 2026-08-20 |
| Epic | [EPIC-007](../roadmap/epic-007/EPIC.md) |
| Backlog item | [E7-011](../roadmap/epic-007/BACKLOG.md#e7-011--validate-the-representative-assignment-and-close-epic-7) |
| Automated evidence | [E7-010 verification](./E7-010-automated-verification.md) |
| UI evidence | [E7-009 review](./E7-009-village-workforce-ui.md) |

## Current result

The technical and configured-save representative gates are complete. One
supported shop-manager target was carried through the production snapshot
reader, verified rule resolver, evaluator, shortlist, current-versus-
alternative comparison, and information-only manual checklist twice. Both
runs retained identical normalized source, objective, target, rule, state,
rank, value-unit, comparison-outcome, and checklist facts. The configured save
and both inspected GameData assemblies remained byte for byte unchanged.

The prescribed in-app browser now completes successfully. English wide and
Traditional Chinese narrow visual confirmation, native comparison selection,
progressive evidence, responsive labels, horizontal-overflow checks, and focus
restoration after clearing comparison all pass against the live configured-save
route. The product owner approved Epic 7 closure on 2026-08-20. No implementation
or verification blocker remains.

## Independent-review corrections

| Finding | Correction and proof |
|---|---|
| Current/comparison evidence was incomplete | One reusable disclosure now renders hard gates, exact components, and redacted provenance for the current worker, every visible alternative, and both comparison workers; rendering and live DOM checks pass. |
| Comparison state and clear focus were misleading | Result identity keys recreate native controls from authoritative state; the selected checkbox remains checked and enabled, other unchecked choices disable, and clear restores focus to the original control. |
| Discovery outlived the page | Initial snapshot discovery now receives a component-lifetime cancellation token; disposal regression coverage proves it is cancelled. |
| Failure language could become stale | Typed discovery and inspection failure states are retained and remapped when the cascading language changes; a live-component regression covers the transition. |
| Omitted coordinates became valid zeroes | All four target coordinates are nullable required inputs and the workflow rejects an omitted coordinate with HTTP 400 before finder execution. |
| Warm projection exceeded three seconds | Reuse performs one full pre-projection SHA-256 plus a post-projection revision check; the configured guard passes and live warm reads measured 1.449–1.565 seconds. |
| Narrow comparison values lost worker identity | Every reflowed comparison value now repeats its worker label; the 390-pixel DOM and visual check show no horizontal overflow. |
| A verified current-only worker was reported as not comparable | Current-only evaluations with an exact result now compare numerically with ranked or tied alternatives; both directions are covered by a Domain regression test. |

## Privacy and non-interference boundary

The representative test resolves the configured path only inside the guarded
Infrastructure composition. It selects the first canonical supported target
and the first rankable alternative that is not the current manager, then
reduces the result to typed assertions and aggregate status. It does not print
or commit the save path, source hash, settlement/building/worker identity,
exact qualification, rank, or proprietary source text.

The test fingerprints these three files before discovery and after all finder
runs:

1. the configured save;
2. `GameData.dll`; and
3. `GameData.Shared.dll`.

All three before/after records match in file name, length, last-write time, and
SHA-256. The workflow calls no assignment setter, efficiency calculator,
archive save, helper persistence, process, network, upload, automated-input,
or game-control API.

## Representative workflow review

The configured source remains compatible with the E7-000 decision:

- the public work-candidate result supplies the alternative universe;
- supported occupied shop-manager entries supply target and current-assignment
  facts;
- the target's typed required life-skill discipline chooses exactly one saved
  base qualification fact;
- the verified version-1 rule preserves the raw qualification point, weight
  `1`, and contribution without hidden normalization;
- only rankable alternatives enter competition ranking;
- the current manager remains factual even when outside the alternative
  universe; and
- no efficiency, output, revenue, capacity, vacancy, productivity, success
  probability, recruitment, or universal character-quality claim is made.

The representative request produced an authoritative `Complete` or `Partial`
result, at least one rankable non-current alternative, a comparison, and all
five unique manual-checklist facts. Every comparable evaluation used
`BaseQualificationPoint` and the exact discipline required by the selected
target. The proposed assignment retained `ProposedHelper` origin and the final
checklist explicitly states that no action was sent to the game and efficiency
was not calculated.

## Capture time and repeated-run identity

The post-review correction keeps the honest UTC capture time as observation
metadata and excludes it from semantic identity. Fresh projections of the same
source facts now produce identical snapshot and downstream result fingerprints.
The repeated representative requests prove equality for:

- `WorkforceSourceVersions`, including the configured-save SHA;
- target, objective, and rule-version identities;
- shortlist state counts;
- worker order, competition ranks, evaluation states, result units, and exact
  result values;
- comparison outcome; and
- every manual-checklist kind and category.

E7-010 separately proves that two executions over the exact same immutable
snapshot produce identical top-level, evaluation-set, shortlist, comparison,
and manual-plan fingerprints. Capture time can differ without invalidating any
of those semantic identities.

## Synthetic state review

The committed Domain, Application, API, and executable Razor matrices cover:

| Scenario | Required visible/semantic result | Evidence |
|---|---|---|
| Empty comparable replacement set | Current-only result; no invented vacancy or proposal | `One_current_only_worker_yields_empty_comparable_replacement_set` |
| Exact tie | Shared competition rank; stable identity only orders display | `Evaluation_is_deterministic_and_marks_exact_ties`, rendering matrix |
| Ineligible high value | Candidate gate fails before score; no value fallback | `High_qualification_cannot_override_failed_candidate_gate` |
| Missing fact | `Incomplete`; no zero component or rank | `Missing_required_fact_is_incomplete_without_zero_component` |
| Unsupported fact/source | `Unsupported`; no stale mapping or score | evaluator and finder status theories |
| Conflicting fact | `Conflicting`; both values and conflict count retained | cross-layer parity matrix |
| Source/read/target failure | Typed safe guidance in both languages | controller and rendering failure theories |
| Current outside alternative universe | `CurrentOnly` with descriptive saved value | Domain and cross-layer current-only cases |
| Previous result after target change | Visible previous marker and inert controls | executable Razor rendering case |

Unknown, unavailable, unsupported, and conflicting facts never become zero,
false, a low rank, or an implied productivity penalty.

## Bilingual and responsive review

Executable Razor tests prove English and Traditional Chinese contain the same
target, current worker, candidate values/unavailable states, ties, comparison,
manual checklist, shared limitations, and failure paths. Typed localization
coverage requires every key to return nonblank text in both languages.
Architecture tests require one candidate DOM, native controls and disclosures,
scoped tables, live status, raw-ID hiding, container reflow at 959 pixels, and
single-column controls at 620 pixels.

The final visual check is complete through the prescribed in-app browser:

1. English at 1280 by 900 CSS pixels exposed target discovery, current
   assignment evidence, candidate rows, comparison evidence, and the static
   checklist without document-level horizontal overflow.
2. Traditional Chinese at 390 by 844 CSS pixels retained the same facts and
   ordering. Candidate and comparison tables reflowed into labelled cards;
   every comparison value repeated its worker label after the wide header was
   hidden, and no document-level horizontal overflow appeared.
3. Native comparison selection retained a checked, enabled control for the
   chosen alternative, disabled only the remaining unchecked controls, and
   clearing comparison returned focus to the original checkbox.

The current assignment, each visible candidate, and both comparison workers
expose closed hard-gate/component/provenance disclosures. The review used the
same read-only live route as the configured integration and sent no game
action.

## Final Release gate

| Gate | Result |
|---|---|
| Release solution build | Passed, 0 warnings, 0 errors |
| Full default matrix | 1,420 total; 1,405 passed; 15 explicit local-source skips; 0 failed |
| Configured representative workflow | Passed, 0 skipped |
| Representative source guard | 3 of 3 files unchanged |
| Live `/village-workforce` route | HTTP 200; opened for product-owner review |

The additional default skip is the E7-011 configured representative test
itself. It reports the missing `TAIWU_INTEGRATION_SAVE_PATH` prerequisite
explicitly and passed when run with the trusted local configuration.

## Epic acceptance traceability

| Epic criterion | Implementation or evidence | State |
|---|---|---|
| Version-matched worker universe and eligibility | [E7-000 evidence](../scenarios/E7-000-village-workforce-evidence.md), snapshot projection, rule/evaluator tests | Verified |
| Representative exact source, target, unit, and rule | E7-000 plus guarded E7-011 workflow above | Verified |
| Current assignment and helper proposal are separate | Domain assignment origins, manual-plan tests, guarded representative | Verified |
| Unknown/conflict never becomes zero or penalty | Domain state matrix and cross-layer parity | Verified |
| Only verified components affect order | Version-1 rule/evaluator and no-rerank Presentation tests | Verified |
| Attributes and unrelated aptitudes remain descriptive | Rule has one typed required life-skill component; scope audit | Verified |
| Identical inputs are deterministic | E7-010 complete fingerprint repetition | Verified |
| One bounded save session | Reader architecture test and guarded integrations | Verified |
| Typed path-free API | Contract, JSON, controller, and loopback architecture tests | Verified |
| Bilingual responsive accessible UI | Executable rendering, architecture checks, and wide English/narrow Chinese in-app browser review | Verified |
| Manual checklist cannot trigger a change | Domain origin/checklist contracts, Razor text, capability scans | Verified |
| Automated and representative non-interference | [E7-010](./E7-010-automated-verification.md) and guarded E7-011 workflow | Verified |
| Deferred mechanics remain outside scope | Deferred-scope section below and capability scan | Verified |
| Product-owner decision | Approved on 2026-08-20 | Verified |

## Deferred mechanics and review decision

Whole-village optimization, vacancies and capacity, current modified
qualification, efficiency/productivity/output/revenue, resource routing,
construction/upgrades/demolition, recruitment and party changes, character
development, training and future potential, library/books, persisted proposals
or preferences, save writing, export, process access, automation, input, and
game control remain outside Epic 7.

The only efficiency call remains in the opt-in E7-000 evidence probe, where it
records the standalone-runtime limitation; the production snapshot, evaluator,
finder, API, and UI do not call it or present an efficiency result.

An independent full Epic 7 review was completed. Its performance, evidence,
comparison-state, current-only comparison, focus, lifecycle-cancellation,
language-remapping, and API validation findings are corrected and covered by
the verification recorded above. Existing semantic, architecture, contract,
full Release, guarded-source, and live browser reviews report no unresolved
production finding.

## Product-owner decision

Approved on 2026-08-20. This decision closes E7-011 and Epic 7 without adding
any deferred mutation, persistence, optimization, recruitment, development,
library, automation, or game-control capability.
