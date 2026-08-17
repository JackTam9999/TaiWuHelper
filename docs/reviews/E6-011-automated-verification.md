# E6-011 automated companion-finder verification

| Field | Value |
|---|---|
| Status | Complete |
| Evidence date | 2026-08-17 |
| Epic | [EPIC-006](../roadmap/epic-006/EPIC.md) |
| Backlog item | [E6-011](../roadmap/epic-006/BACKLOG.md#e6-011--verify-safety-batching-determinism-and-cross-layer-parity) |
| Source boundary | [E6-000 evidence](../scenarios/E6-000-companion-candidate-evidence.md) |

## Result

The complete Epic 6 technical vertical passes its synthetic, cross-layer,
architecture, Release, and guarded local checks. The default non-integration
matrix contains 1,223 passing tests. The three focused companion integration
scenarios also pass against the configured representative save and installed
sources.

No local path, save or candidate identity, exact candidate value, proprietary
source text, or source fingerprint is recorded here. Test output contains only
typed states, aggregate counts, timing limits, and guarded-file counts.

## Verification matrix

| Boundary | Coverage and invariant | Result |
|---|---|---|
| Domain | Candidate/profile immutability, evidence states, hard gates before score, exact components, ties, exclusions, shortlist, comparison, explanations, fingerprints | Passed |
| Both roles | Martial and life-skill synthetic positive, ineligible, incomplete, unsupported, conflicting, and tied matrices | Passed, 2 role cases |
| Application | One coherent source chain, filter/comparison identity, cancellation, partial catalogue states, changed-revision rebuild | Passed |
| Infrastructure | One archive-session call site, complete aggregate mapping, bilingual display isolation, no per-candidate reopen | Passed |
| API | Typed HTTP states, localization parity, nullable unavailable values, comparison, serialization and public-contract safety | Passed |
| Presentation | Same immutable result through the API mapper, complete bilingual states, filtering, comparison, focus, native semantics, no raw IDs | Passed |
| Architecture | No mutation-capable GameData, persistence, process, screenshot, upload, input, or game-control path; one evaluation path | Passed |
| Browser | English desktop and Traditional Chinese narrow fact parity with no document overflow or console error | Passed in [E6-010 review](./E6-010-companion-finder-ui.md) |

## Complete two-role synthetic matrix

`Every_verified_role_covers_the_complete_synthetic_state_matrix` runs the same
seven-candidate matrix independently against
`MARTIAL_DISCIPLINE_APTITUDE` and `LIFE_SKILL_DISCIPLINE_APTITUDE`:

| Synthetic case | Required result |
|---|---|
| Higher confirmed base qualification | Rank 1 for that role and discipline only |
| Confirmed ineligible universe with a higher value | Ineligible, no score or component |
| Missing required fact | Incomplete, no zero fallback |
| Unsupported required fact | Unsupported, no score |
| Confirmed fact from another revision | Conflicting, no automatic precedence |
| Two equal confirmed values | Shared competition rank and explicit tie |

Reversing the input order preserves candidate order, evaluation fingerprints,
tie ranks, and the complete ranking fingerprint for both role families.
Existing shortlist and comparison matrices additionally prove deterministic
explanations, filters, unavailable comparison outcomes, diagnostics, and
fingerprints.

## Guarded local vertical

`CompanionCandidateSnapshotIntegrationTests` passed 3 of 3 in Release:

1. candidate snapshot repeatability, cold/warm budget, and read-only guard;
2. catalogue enrichment repeatability, version identity, and read-only guard;
3. complete martial/life companion-finder repeatability, bilingual discipline
   display, cold/warm budget, and expanded read-only guard.

The full-finder scenario runs both role families twice. Each result is
authoritative, retains at least one ranked candidate, preserves its semantic
fingerprint on repetition, uses the same save revision across roles, and keeps
the same bilingual candidate display descriptors. The installed discipline
source returns all 14 martial and 16 life-skill names in both languages. Its
typed aggregate state may be `Partial` because the installed language packs
contain non-fatal parser diagnostics; no requested label is missing.

The integration assertions enforce the E6-000 product budgets directly:

- first cold full-finder request at most 30 seconds;
- every subsequent unchanged-revision martial/life request at most 2 seconds;
- one archive-session projection per request; and
- no archive reopen per candidate.

The full class completed in about 31 seconds in one Release process. Before and
after states compare length, last-write time, and SHA-256 for the configured
save, runtime assemblies, catalogue configuration, combat/special-effect/book
language sources, and all four martial/life discipline language sources. Every
guarded file remained identical.

## Release and default matrix

| Project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Domain | 493 | 0 | 0 |
| Application | 178 | 0 | 0 |
| Infrastructure | 145 | 0 | 0 |
| Architecture | 93 | 0 | 0 |
| API and Presentation | 314 | 0 | 0 |
| **Total** | **1,223** | **0** | **0** |

The Release solution build completes with zero warnings and zero errors.
`dotnet format --verify-no-changes`, changed-document link validation,
repository leak/capability scans, and `git diff --check` pass.

## Epic acceptance traceability

| Epic criterion | Implementation or verification evidence | State after E6-011 |
|---|---|---|
| Version-matched candidate universe, not target lookup | [E6-000 evidence](../scenarios/E6-000-companion-candidate-evidence.md), snapshot mapping and safety tests | Verified |
| Two evidence-gated role presets | Verified role catalogue plus the two-role matrix above | Verified |
| Stable versioned rules, hard requirements, weights, ties | [Role evaluation architecture](../architecture/COMPANION-ROLE-EVALUATION.md) and Domain rule tests | Verified |
| Immutable typed profiles, source identity, conflicts, diagnostics, fingerprints | Candidate profile and snapshot contracts/tests | Verified |
| Eligibility and hard gates before score | Evaluator and shortlist state matrices | Verified |
| Unknown evidence never becomes zero or ineligibility | Domain, API, and Presentation unavailable-state tests | Verified |
| Only comparable candidates rank; retained candidates have reasons | Shortlist, explanation, and rendering tests | Verified |
| Components and explanations retain evidence and rule versions | Component, shortlist, comparison, and API mapping tests | Verified |
| Localization and raw text never become mechanics | Enrichment, display-isolation, language-parity, and fingerprint tests | Verified |
| Current ability remains distinct from future potential | E6-000 source decision and explicit unsupported-current-value tests | Verified |
| One bounded archive read | Snapshot reader call-site architecture test, Infrastructure unit tests, guarded local scenario | Verified |
| Coherent save/catalogue/GameData/rule identity | Finder source-chain constructors and Application revision tests | Verified |
| Filters and localization preserve identity | Finder, response, mapper, and session-state tests | Verified |
| API and UI state parity | Shared API mapper, Presentation mapper, rendered tests, no-reranking architecture checks | Verified |
| Bilingual responsive keyboard-native UI | [E6-010 browser review](./E6-010-companion-finder-ui.md) and rendered semantics tests | Verified |
| Required automated state coverage | 1,223-test matrix plus 3 guarded integration cases | Verified |
| Game-owned bytes unchanged | Expanded before/after guarded integration set | Verified |
| No mutation, persistence, process, screenshot, or input capability | Epic 6 architecture suites | Verified |
| Every criterion has linked evidence | This table | Verified |
| Product-owner completion decision | Owned by E6-012 | Pending explicit input |

## Remaining gate

No technical E6-011 blocker remains. E6-012 still requires representative
manual review and the product owner's explicit Epic 6 completion decision.
