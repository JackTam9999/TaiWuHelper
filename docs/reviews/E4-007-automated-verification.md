# E4-007 automated verification

| Field | Value |
|---|---|
| Status | Passed — two-option trial awaiting product-owner review |
| Evidence date | 2026-08-09 |
| Epic | [EPIC-004](../roadmap/epic-004/EPIC.md) |
| Backlog item | [E4-007](../roadmap/epic-004/BACKLOG.md#e4-007--verify-comparison-safety-parity-and-determinism) |

## Release and default matrix

Release build:

```powershell
dotnet build TaiWu.slnx -c Release --no-restore
```

Result: passed with zero warnings and zero errors.

Default matrix:

```powershell
dotnet test TaiWu.slnx -c Release --no-build --no-restore
```

Result: **926 total; 917 passed; 0 failed; 9 expected opt-in
skips**. Two immediately repeated default runs also passed. The skips are the
documented local GameData/save checks when their process-local environment
variables are absent.

Two earlier cold parallel attempts each produced a different isolated
single-test failure outside the comparison suites. Neither reproduced when
run alone; a ten-run repeat of the affected observation lifecycle test passed,
and the final matrix plus two consecutive repeats were clean. No comparison
failure was observed.

Formatting and diff checks:

```powershell
dotnet format TaiWu.slnx --no-restore --verify-no-changes
git diff --check
```

Result: formatting passed. The final diff check is repeated immediately before
the two-option review-build commit.

## Focused comparison verification

| Layer | Result | E4-007 evidence |
|---|---:|---|
| Domain | 311/311 | Comparison identity/order, column states, category and skill uniqueness, numeric availability, capacity arithmetic, provenance, composite actions, malformed cells, tactical threat partitioning, score order, and supporting-fact validation |
| Application | 135/135 | Four-column construction, exact manual-plan parity, deterministic order/references, capacity/allocation, infeasible/unavailable states, observation repeat, and clear-to-save-only comparison fingerprints |
| API/presentation | 259/259 | Typed mapping/serialization, unavailable and infeasible states, Safe/Aggressive UI projection, hidden Balanced controls/content, grouped warnings/provenance, compact empty plans, bilingual responsive rendering, interaction-state restoration, tactical evidence, and information-only controls |
| Architecture | 79/79 | Layer direction plus file, process, screenshot, persistence, game-control, public-path, and mutation-capable dependency guards |

The observation lifecycle tests now compare the complete normalized comparison
projection as well as the recommendation result. Reapplying the same typed
observation produces the same comparison fingerprint. Clearing it produces
the original save-only comparison fingerprint.

The two-option product trial changes only the Presentation projection. Domain,
Application, and serialized API verification still cover Safe, Balanced, and
Aggressive in deterministic order. Component and selection-state tests prove
that the player-facing form and single result-level button group expose only
Safe and Aggressive, default to Safe, and cannot select a hidden Balanced
result. Rendering and architecture tests also prove that matching warning cards
are consolidated without hiding their messages, only the selected tactical
card is rendered, repeated supporting panels are absent, detailed skill cards
are collapsed, and an empty battle plan produces one message.

## Guarded current-save vertical

The existing E3-010 read-only vertical was extended with E4-007 comparison
assertions and rerun against the current local save:

```powershell
$env:TAIWU_INTEGRATION_SAVE_PATH = '<current-local.sav>'
dotnet test `
  tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj `
  -c Release --no-build --no-restore `
  --filter-class '*TargetObservationReadOnlyIntegrationTests*'
```

Result: **1 passed; 0 failed; 0 skipped** on the verified E4-007 run.

A post-Presentation-change recheck on the same date stopped before reading
because the running game held the save file open. The helper did not bypass the
lock or alter the game process. The two-option change does not touch the guarded
Domain/Application comparison or filesystem path, and the complete 924-test
Release matrix passed after that change; the live guard can be repeated once
the game releases the save.

The vertical verifies:

- exactly one available Current column;
- one typed column for every Safe, Balanced, and Aggressive style;
- available policies carry loadout and tactical summaries;
- infeasible policies carry diagnostics and no fabricated loadout;
- repeated observation application produces equivalent comparison facts;
- clearing reproduces the save-only comparison facts; and
- the save, inspected GameData runtime files, and Traditional Chinese/English
  language resources have identical length, timestamp, and SHA-256 state in
  the `finally` guard.

Machine-specific paths, file fingerprints, save contents, and proprietary
runtime files are not committed.

## Epic acceptance audit

| Epic 4 criterion | Implementation or evidence | Result |
|---|---|---|
| One immutable recommendation supplies all columns | [comparison contract](../architecture/LOADOUT-COMPARISON-CONTRACT.md), builder input boundary, `Builds_current_and_all_policy_columns_from_one_result` | Pass |
| Typed Current/Safe/Balanced/Aggressive identity and deterministic order | Domain model tests, builder deterministic/order tests, API serialization test | Pass |
| Manual actions agree with every feasible policy plan | [builder parity design](../architecture/LOADOUT-COMPARISON-BUILDER.md), `Manual_changes_and_comparison_actions_have_exact_parity` | Pass |
| Composite membership, direction, and breakthrough remain distinct | Domain composite/malformed-action tests and Application add-plus-action tests | Pass |
| Capacity, cost, remaining, and 萬用 preserve unavailable reasons | Domain capacity/value tests, builder unavailable/capacity tests, API mapping/render tests | Pass |
| Infeasible policies remain diagnostic columns | Domain column invariant, Application infeasible-style test, API mixed-status tests | Pass |
| Tactical facts trace to existing typed recommendation evidence | [tactical explanation](../architecture/LOADOUT-COMPARISON-EXPLANATION.md) and mapper/render tests | Pass |
| Scores cannot read as cross-policy win probability | Policy-local component UI, explanatory copy, render assertions excluding universal-best/probability labels | Pass |
| Save and observed baselines remain distinguishable | Builder provenance test, API observed-baseline test, visible provenance panel | Pass |
| Observation apply/clear atomically rebuilds comparison | Application comparison fingerprints and guarded current-save vertical | Pass |
| API and UI expose the same comparison semantics | [API contract](../architecture/LOADOUT-COMPARISON-API.md), controller and mapper tests | Pass |
| Traditional Chinese and English are complete and accessible | Safe/Aggressive projection rendering tests and [manual workflow](./E4-007-manual-verification.md) | Pass |
| Desktop and narrow modes expose equivalent facts | [presentation contract](../architecture/LOADOUT-COMPARISON-PRESENTATION.md), CSS/render tests, live manual workflow | Pass |
| Comparison is session-bound and information-only | Presentation state ownership, persistent notice, architecture guards | Pass |
| Feasible, infeasible, unchanged, changed, unavailable, observed, stale, and cleared states are automated | Domain/Application/API/Presentation state matrix across Epic 4 plus observation suites | Pass |
| Local inspected sources remain byte-for-byte unchanged | Final guarded vertical: 1/1, before/after fingerprints equal | Pass |
| Product-owner completion decision | Explicit decision not yet recorded | Pending |

## Explicit future work

Epic 4 does not partially implement export/share cards, persisted comparison
history or preferences, screenshot capture/upload/OCR, lower-ranked candidate
exploration, or battle-outcome feedback. Those remain separately scoped future
work with their own privacy, persistence, and evidence decisions.

## Decision boundary

All implementation and verification criteria are satisfied for the two-option
review build. Epic 4 remains open for product-owner review and the explicit
completion decision.
