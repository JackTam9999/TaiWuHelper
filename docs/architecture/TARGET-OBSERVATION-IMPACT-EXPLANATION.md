# Target observation impact explanation

## Scope

E3-009 compares the save-only recommendation with the result rebuilt from a
validated current-screen observation. It explains which conclusions changed
without exposing raw diagnostics, local paths, screenshot data, or a claimed
win probability.

The workflow remains read only. It reads one combat snapshot, builds a
save-only baseline, applies the validated in-memory observation merge, and
builds the observed result from that merged snapshot. Neither result is
persisted.

## Typed comparison

`TargetObservationRecommendationImpactAnalyzer` produces a deterministic,
typed comparison:

- threats are `Added`, `Confirmed`, `Demoted`, `Removed`, or `Unchanged`;
- selected recommendations are `Added` or `Removed` for each policy;
- recommendation changes are separated into `Feasibility` and `Scoring`;
- unrecognized target effects remain unsupported rather than receiving a
  severity or score;
- hostile/story confirmations use the distinct `ObservedActiveEffect` source
  instead of being relabelled as equipped membership;
- partial observations retain an explicit remaining-unknown flag; and
- conflicting save and current-screen fields retain both source timestamps
  while identifying current-screen field precedence.

A recommendation change is a feasibility change when that skill/direction
option was not present in the other result's feasible candidate set. If it was
feasible in both results but selection changed, it is a scoring change.

## Evidence and presentation

The API returns stable identifiers and evidence references for automation.
The bilingual presentation maps those identifiers to skill and threat names,
then displays a user-facing evidence chain from the changed threat or
feasibility conclusion to the added or removed counter. Raw warning codes,
conflict reason codes, and evidence references are intentionally not rendered.

Confidence text describes evidence provenance only. It is never described as
a probability of winning.

## Verification

Verified on 2026-08-07:

- `dotnet build TaiWuAPI/TaiWuAPI.csproj --no-restore`: passed with zero
  warnings and zero errors;
- `dotnet test tests/TaiWu.Application.UnitTests/TaiWu.Application.UnitTests.csproj --no-restore`:
  111 passed;
- the two English/Chinese target-impact rendering tests: passed; and
- the five target-observation API controller tests: passed.

The focused API runs excluded the unrelated, uncommitted
`RegionStoriesRenderingTests.cs` file. At verification time that concurrent
file did not compile, and the concurrent `MainLayout` navigation change also
left two pre-existing renderer tests without a registered `NavigationManager`.
No RegionStories file or test was changed for E3-009.
