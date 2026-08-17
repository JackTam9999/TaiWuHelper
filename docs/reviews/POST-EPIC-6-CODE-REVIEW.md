# Post-Epic 6 code review and refactoring

| Field | Result |
|---|---|
| Review date | 2026-08-18 |
| Epic | [EPIC-006](../roadmap/epic-006/EPIC.md) |
| Epic merge | Local `master` fast-forwarded to `b5d2be2` |
| Review branch | `refactor/post-epic-6-review` |
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

## Verification

The post-refactor Release solution build completes with zero warnings and zero
errors. The full default matrix reports:

| Total | Passed | Skipped | Failed |
|---:|---:|---:|---:|
| 1,275 | 1,263 | 12 | 0 |

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

The next functional feature can start from this verified baseline.
