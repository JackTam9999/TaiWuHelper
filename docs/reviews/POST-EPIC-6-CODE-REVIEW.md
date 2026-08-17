# Post-Epic 6 code review and refactoring

| Field | Result |
|---|---|
| Review date | 2026-08-18 |
| Epic | [EPIC-006](../roadmap/epic-006/EPIC.md) |
| Epic merge | Local `master` fast-forwarded to `b5d2be2` |
| Review branch | `refactor/post-epic-6-review` |
| Result | Complete; remaining structural work requires a scope choice |

## Outcome

The Epic 6 branch was merged into local `master` by fast-forward with no
conflicts and no synthetic merge commit. Review and behavior-preserving
refactoring then continued on a dedicated branch so the completed feature
baseline remains identifiable.

Four review batches are committed:

1. `eeedfe0` fixes duplicate localization keys and invalid-state exception
   construction.
2. `5f12ae2` centralizes the target framework, nullable context, implicit
   usings, and warnings-as-errors policy.
3. `e1b676e` makes cancellation tokens consistently last in public and
   repository method signatures.
4. `320b4d1` separates candidate, comparison, and notice presentation mapping
   without changing the public mapper API or comparison behavior.

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

## Verification

The post-refactor Release solution build completes with zero warnings and zero
errors. The full default matrix reports:

| Total | Passed | Skipped | Failed |
|---:|---:|---:|---:|
| 1,271 | 1,259 | 12 | 0 |

The 12 skipped cases are existing opt-in local GameData/save integration
scenarios; no configured local integration source was present for this run.
Focused companion Presentation tests passed 37 of 37 and the companion
architecture safety suite passed 5 of 5. Whitespace formatting and all
warning-level analyzer checks pass.

## Remaining structural hotspots

These are maintenance risks, not demonstrated correctness defects:

| Area | Current signal | Recommended boundary | Decision needed |
|---|---|---|---|
| SQLite combat-skill catalogue store | About 1,974 lines combining schema validation, reads, replacement transactions, and command mapping | Extract tested schema/command and row-mapping collaborators in several small commits | Yes; this changes a persistence boundary and deserves a dedicated refactor scope |
| Target strategy presentation mapper | About 1,009 lines covering profile, archetype, counter, adjustment, gap, and status projection | Keep one public partial mapper while separating those projection families and extending the architecture scan | Yes; safe but belongs to Epic 5 presentation rather than Epic 6 |
| Large Razor presentation components | Several pages/components exceed 900 lines | Extract only cohesive rendered sections with focused rendering tests | Yes; choose the next UI area before changing component boundaries |

The next functional feature can start from the verified Epic 6 baseline. If a
deeper refactor is desired first, the target strategy mapper is the lowest-risk
next batch; the SQLite store offers more value but needs a deliberately scoped
persistence refactor rather than a mechanical file split.
