# E7-010 automated village-workforce verification

| Field | Value |
|---|---|
| Status | Complete |
| Evidence date | 2026-08-18 |
| Epic | [EPIC-007](../roadmap/epic-007/EPIC.md) |
| Backlog item | [E7-010](../roadmap/epic-007/BACKLOG.md#e7-010--verify-safety-batching-determinism-and-cross-layer-parity) |
| Source boundary | [E7-000 evidence](../scenarios/E7-000-village-workforce-evidence.md) |

## Result

The complete Epic 7 technical vertical passes its Domain, Application,
Infrastructure, API, Presentation, localization, architecture, Release, and
guarded local verification. The full default matrix contains 1,409 tests:
1,395 passed, 14 guarded local-source scenarios skipped explicitly, and none
failed. The Release solution build completed with zero warnings and zero
errors.

Two additional parity checks close the remaining verification gaps. Repeating
an identical finder request now compares the result, evaluation-set,
shortlist, comparison, and manual-plan fingerprints as well as evaluation
states, ties, ranks, and worker order. A cross-layer state matrix proves that
the API and Presentation retain ranked, current-only, ineligible, incomplete,
unsupported, and conflicting outcomes without inventing a numeric fallback.

The two configured-save workforce probes pass together in a non-parallel test
collection. They preserve the configured archive and both inspected GameData
assemblies byte for byte. No local path, raw character or building identity,
exact source fingerprint, or proprietary source text is recorded here.

## Verification matrix

| Boundary | Coverage and invariant | Result |
|---|---|---|
| Domain | Typed identities and evidence, immutable snapshot, verified rules, evaluator states, exact ties, shortlist filters/comparison, manual-plan identity | 45 passed |
| Application | One snapshot read per execution, status mapping, cancellation, revision isolation, deterministic orchestration and complete identity repetition | 22 passed |
| Infrastructure unit | Registration and archive reuse, preserved-metadata replacement detection, changed-during-read rejection, and cache invalidation | 9 passed |
| Infrastructure guarded | One bounded archive session, repeatable projection, 30-second cold/3-second warm budgets, and three-file before/after guard | 2 passed, 0 skipped |
| API and Presentation | Contract mapping, JSON safety, every evaluation/unavailable state, conflict counts, bilingual rendering, native semantics, and raw-ID hiding | 41 passed |
| Localization | Every typed key returns nonblank English and Traditional Chinese text; dynamic labels are bilingual; unknown languages fail closed | Passed |
| Architecture | Semantic alias-aware file/process/network/game-control scan plus workforce route, one-read, one-evaluation, responsive, persistence, upload, and input boundaries | 5 focused passed; full project passed |

## Deterministic identity proof

`Identical_requests_repeat_every_semantic_identity_and_order` invokes the
finder twice with the same immutable snapshot, target, two comparison workers,
and proposed worker. It requires equality for:

- the top-level finder fingerprint;
- evaluation-set, shortlist, comparison, and manual-plan fingerprints;
- each worker's stable identity, typed evaluation state, and evaluation
  fingerprint; and
- comparable-worker competition ranks and deterministic order, including an
  exact tie.

The existing Domain matrices additionally reverse input order and verify stable
rules, hard-gate outcomes, ranked/unranked partitions, filters, comparison
outcomes, diagnostics, and manual-plan identities. The Application reader mock
is required to receive exactly one snapshot request per finder execution.

## Cross-layer unavailable-state parity

`VillageWorkforceCrossLayerParityTests` sends one synthetic result through the
production API response mapper and Presentation view-model mapper. Both layers
must preserve the same counts and these states:

| State | Numeric fallback | Required parity |
|---|---|---|
| Ranked | Exact verified value | Same state and value |
| Current only | Descriptive saved value only | Same state, value, and current marker |
| Ineligible | None | Same state; no value inferred from available facts |
| Incomplete | None | Same state; missing evidence remains missing |
| Unsupported | None | Same state; no stale rule fallback |
| Conflicting | None | Same state and exact conflict count |

Every presented state has a nonblank localized label. Conflicting evidence is
retained in the requirement payload rather than flattened into a generic
failure or numeric zero.

## Guarded local archive and performance

E7-000 originally observed a 15.418-second cold projection and a 1.881-second
warm projection. A later architecture hardening requires full content
fingerprints before and after every reused projection. That verification is
necessary to detect a save replaced with the same size and timestamp and a
save changed during projection, so E7-010 does not remove it to preserve the
old two-second target.

Sequential Release measurements on the current configured source were:

| Probe | Cold | Warm | Guard result |
|---|---:|---:|---|
| E7-000 aggregate evidence | 18.838 s | 2.753 s | 3 of 3 unchanged |
| E7-003 production snapshot | 19.218 s | 2.714 s | 3 of 3 unchanged |

The enforced budgets are therefore 30 seconds cold and three seconds warm.
Both tests share `TaiwuArchivePerformanceCollection`, whose disabled
parallelization prevents another real-save cold load from consuming the same
process-wide GameData lock inside the timed interval. Running both classes in
one command passed 2 of 2 in 33.694 seconds with zero skips.

The default matrix does not receive a private save path. Both cases then report
an explicit `TAIWU_INTEGRATION_SAVE_PATH` skip; they are two of the 14 expected
environment-dependent skips rather than silent omissions.

## Capability and batching boundaries

The production source scan resolves invocation symbols with Roslyn, so aliases
and fully qualified names cannot hide destructive file operations, process
control, network clients, automated input, process-memory access, runtime
patching, or native game-control calls. Companion regex and reflection checks
retain the named helper-store persistence whitelist and reject upload,
mutation, and command-shaped Presentation behavior.

Workforce-specific architecture checks require exactly one snapshot read and
one finder execution in the page, no source reader in the result component,
one candidate DOM, and only local filter, name-query, comparison, and evidence
interactions after the result is loaded. The Infrastructure adapter test also
forbids efficiency guessing and save/building/worker mutation APIs.

## Release evidence

| Gate | Result |
|---|---|
| `dotnet build TaiWu.slnx -c Release --no-restore` | Passed, 0 warnings, 0 errors |
| Full default Release matrix | 1,409 total; 1,395 passed; 14 skipped; 0 failed |
| Epic 7 Domain namespace | 45 passed |
| Epic 7 Application namespace | 22 passed |
| Focused Infrastructure unit boundary | 9 passed |
| Focused API/Presentation and JSON boundary | 41 passed |
| Focused semantic/workforce architecture boundary | 5 passed |
| Guarded workforce integration | 2 passed; 0 skipped |
| `git diff --check` | Passed |

## Remaining gate

No automated E7-010 blocker remains. E7-011 owns representative manual review,
the final wide/narrow bilingual visual check deferred by E7-009, epic-wide
traceability, and the product owner's explicit completion decision.
