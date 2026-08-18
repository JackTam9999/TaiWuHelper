# Post-Epic 6 code review and refactoring

| Field | Result |
|---|---|
| Review date | 2026-08-18 |
| Epic | [EPIC-006](../roadmap/epic-006/EPIC.md) |
| Epic merge | Local `master` fast-forwarded to `b5d2be2` |
| Review branch | `refactor/post-epic-6-review` |
| Architecture-hardening branch | `refactor/architecture-hardening` from `6424302` |
| Result | Complete |

## Outcome

The Epic 6 branch was merged into local `master` by fast-forward with no
conflicts and no synthetic merge commit. Review and behavior-preserving
refactoring then continued on a dedicated branch so the completed feature
baseline remains identifiable.

Nine implementation review batches are committed:

1. `eeedfe0` fixes duplicate localization keys and invalid-state exception
   construction.
2. `5f12ae2` centralizes the target framework, nullable context, implicit
   usings, and warnings-as-errors policy.
3. `e1b676e` makes cancellation tokens consistently last in public and
   repository method signatures.
4. `320b4d1` separates candidate, comparison, and notice presentation mapping
   without changing the public mapper API or comparison behavior.
5. `f3d7ff8` restores nullable catalogue-replacement compatibility and renders
   complete bilingual pagination labels.
6. `fa11a2d` extracts target adjustment and evidence projection into a focused
   internal mapper.
7. `749e9d1` batch-hydrates catalogue definitions instead of issuing child
   queries once per skill.
8. `f8f670e` extracts the loadout comparison's tactical section into a tested
   child component and centralizes shared column labels.
9. `2891a18` records the formatter-only using-directive cleanup separately
   from behavior and structural changes.

An independent full-architecture review then produced a second bounded pass.
Nine additional batches close its production and test-quality findings:

1. `da17736` maps unknown target identities to an explicit API `404`.
2. `f94422f` verifies cached save content and makes the progress database
   recover after deletion or replacement.
3. `9fedcc4` requires antiforgery validation on helper-maintenance posts.
4. `cdd1f68` stops hiding unexpected companion workflow faults.
5. `ab45d64` pins the internal loopback JSON contract, rejects numeric enums,
   and removes the legacy save-report type from the response boundary.
6. `ef20a6c` removes absolute save paths from Domain metadata and injects one
   registered `TimeProvider` into snapshot readers.
7. `86e9a65` replaces process-global catalogue coordination with one injected
   singleton and injects application use cases into controllers/components.
8. `3f722e7` enforces complete typed companion localization.
9. `2ef46f8` adds Roslyn semantic capability checks alongside the existing
   inexpensive source-pattern guards.

## Findings resolved

### Duplicate localization identities

Four duplicate `UiText` keys could silently replace an earlier translation.
The duplicates were removed, the catalogue page summary now uses the intended
pagination translation, and an invalid enum state fails explicitly instead
of selecting an unrelated label. `CA2244` is now a warning, so the same defect
cannot return without failing the build.

### Invalid exception parameter reporting

Several impossible-state branches constructed `ArgumentOutOfRangeException`
with state descriptions in the parameter-name position. They now report
invalid internal states accurately. `CA2208` is also enforced as a warning.

### Project configuration drift

All ten projects repeated the same framework and compiler settings. Those
settings now live in `Directory.Build.props`; warnings are now errors for the
whole solution and individual project files contain only project-specific
configuration.

### Cancellation API consistency

The two `CA1068` findings were corrected while preserving HTTP binding. The
repository overload now places the token last; callers using the former
five-positional-argument order must migrate to the new order. A null optional
legendary-book-effect collection retains its former empty-collection meaning.
`CA1068` is enforced as a warning to prevent regression.

### Companion presentation responsibility

The 669-line companion mapper mixed candidate projection, comparison
projection, and recovery notices. It is now one public partial mapper split
across focused source files. The architecture safety test scans every mapper
part and still proves there is exactly one comparison-builder call and no
source-read or re-evaluation path.

### Complete pagination labels

Traditional Chinese pagination previously composed `第 1` without the `頁`
suffix. The complete localized format now renders `第 1 頁`, with English and
Chinese phrase-level coverage.

### Target adjustment mapping responsibility

The target strategy mapper passed profile, archetype, goal, counter, gap,
threat, skill-name, and language context through each adjustment mapping call.
A focused internal mapper now owns that context and all adjustment-reference
and evidence projection. The public target strategy facade is unchanged and
was reduced from about 1,009 to 637 lines.

### Catalogue query amplification

Catalogue queries previously hydrated every selected skill with five separate
child reads. Definition IDs are now processed in bounded batches of 500; each
batch reads sources, names, fields, requirements, and descriptions once while
preserving the original order and read-only transaction. At the supported
2,000-candidate ceiling, the main path drops from roughly 10,001 queries to
roughly 21. A 501-definition round trip verifies the batch boundary.

### Loadout comparison component responsibility

The tactical risk, requirement, caveat, and policy-score section now has a
dedicated child component. Shared column, policy, and status labels live in one
formatter rather than being duplicated. Parent rendering tests retain the same
visible facts and the architecture scan covers both component files.

## Architecture hardening findings resolved

| Finding | Resolution |
|---|---|
| Unknown target escaped as an unhandled exception | A typed not-found exception is translated to a stable `404` problem response; unrelated failures still surface as server faults |
| Loopback maintenance posts were browser-CSRF reachable | Catalogue rebuild and progress-cache clear endpoints require antiforgery validation, with hosted missing-token and valid-token HTTP coverage |
| Size/mtime cache identity could accept replaced content | Archive reuse and progress-cache hits verify SHA-256 content identity; preserved-metadata replacement tests cover the adversarial case |
| Deleted/replaced progress database could not self-heal | Schema readiness is revalidated and recreated instead of relying on permanent process state |
| Companion orchestration hid programmer faults | Only expected typed result states are mapped; cancellation and unexpected exceptions propagate to the host logging/error boundary |
| API wire tokens followed internal enum serialization defaults | The API remains explicitly internal/loopback, numeric enum values are rejected, selected request tokens are pinned, and cross-layer contract types—including nested collection members—are inventoried |
| Domain metadata retained an absolute save path | Domain snapshots now retain opaque content identity only; infrastructure keeps path context locally |
| Snapshot timestamps used ambient time | `TimeProvider.System` is registered once and readers accept injected time for deterministic tests |
| Catalogue lifecycle used static coordination | A DI-owned singleton now owns the process-local gate and rebuilding state; concurrency/status behavior is tested |
| Presentation constructed application use cases | Controllers and Razor components receive registered use cases from the composition root |
| Companion localization could regress to prose-key fallback | Every typed identity is verified in English and Chinese, unknown languages throw, and architecture coverage rejects legacy fallback calls in the feature |
| Regex-only capability tests could miss aliases | Semantic symbol analysis now covers file mutation at protected boundaries and production process, native-control, runtime-patching, and network calls; alias and fully qualified probes prove the guard |

## Verification

The post-refactor Release solution build completes with zero warnings and zero
errors. The full default matrix reports:

| Total | Passed | Skipped | Failed |
|---:|---:|---:|---:|
| 1,299 | 1,287 | 12 | 0 |

The 12 skipped cases are existing opt-in local GameData/save integration
scenarios; no configured local integration source was present for this run.
Focused companion, target-strategy, catalogue, and loadout-comparison suites
pass. Whitespace formatting and all warning-level analyzer checks pass.

## Residual guidance

The reviewed hotspots no longer require work before the next functional
feature. The following boundaries should guide later changes:

| Area | Current state | Later-change guidance |
|---|---|---|
| SQLite combat-skill catalogue store | Batch hydration removes the demonstrated query-amplification defect; schema, replacement, and read behavior remain in one tested persistence boundary | Extract schema or command collaborators only alongside a concrete schema/repository change; do not split into partial files for line count alone |
| Target strategy presentation | Adjustment/evidence mapping is isolated behind the existing public facade | Extract another collaborator only when one projection family changes independently |
| Loadout comparison | Tactical rendering is isolated and parent/child architecture coverage is explicit | Keep tactical behavior in the child and shared labels in the formatter |
| Other large Razor pages | No correctness or cohesive-responsibility finding remains from this review | Avoid `.razor.cs` or child-component moves based only on file length |
| API route/version boundary | The HTTP API is an internal loopback implementation with pinned JSON fixtures; it is not an external compatibility promise | Before supporting external clients, introduce API-owned V1 tokens/mappers and versioned routes rather than annotating Domain enums |
| Legacy prose-key localization | Existing untouched features still use the legacy dictionary and dynamic formatter | Require typed identities for new strings and migrate one feature at a time when it is otherwise being changed |
| Architecture enforcement | Source-pattern tests remain fast defense in depth; semantic tests cover the highest-risk compiled calls | Expand semantic coverage only with a concrete capability risk; retire a pattern assertion only after equivalent semantic/behavioral coverage exists |

The architecture-hardening branch was fast-forwarded into local `master` after
this verification. The next functional feature can start from this baseline.
