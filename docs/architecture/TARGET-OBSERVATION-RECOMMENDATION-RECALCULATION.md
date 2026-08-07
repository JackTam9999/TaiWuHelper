# Target-observation recommendation recalculation

## Purpose

E3-008 runs the existing candidate-generation, feasibility, scoring, manual
plan, and explanation pipeline against the typed threats produced from the
merged sparring-target snapshot.

The observation does not bypass any decision boundary. It can change which
version-matched threat codes are supplied to verified counter selection, but
it cannot create a counter rule, grant the player a skill, change a player's
skill facts, relax a slot budget, or turn unresolved target evidence into a
score.

## Pipeline

For a successfully resolved target observation:

1. E3-004 creates `Merge.Snapshot` using field-level freshness and coverage.
2. E3-007 analyzes that snapshot with the exact-version target-threat rules.
3. E3-008 selects only counter rules whose typed threat codes are present.
4. `CombatLoadoutGenerator` rejects unavailable, wrong-direction,
   wrong-effect, over-budget, active-role-conflicting, requirement-invalid,
   and inner-power-incompatible player options before scoring.
5. `CombatRecommendationScorer` ranks only generated feasible candidates.
6. Manual plans and explanations use the same observation-enhanced typed
   threat set.

Requests without a target observation continue through the same save-only
pipeline and public API contract.

## Verified-evidence boundary

Only a `TargetThreat` emitted by the versioned analyzer can select a counter.
An observed relevant skill with an unavailable direction, unavailable effect
ID, or unrecognized effect remains a `TargetThreatWarning`. It has no severity
and supplies no threat code to counter selection or scoring.

This can remove an old save-derived counter recommendation when a newer,
version-matched observed direction invalidates the old rule match. The
unrecognized observation remains visible as a warning with its current-screen
evidence reference; the helper does not replace it with a favorable mechanic.

## Feasibility before scoring

An added verified threat may expose one or more verified counter options, but
the player snapshot still decides whether each option is possible. In
particular, observing Reverse 九色玉蝉法 can identify the verified
`DEFEAT_MARK_RESET_LOOP` threat and the Reverse 奇輪佐命功 counter rule. If
the player has not learned that counter, candidate generation records an
`OptionRejected` diagnostic and every policy receives an empty ranked set.

Scoring therefore never repairs or discounts an infeasible option. It sees
only accepted `GeneratedCombatLoadout` values.

## Policy stability

Safe, Balanced, and Aggressive retain their existing documented
`RecommendationPolicyWeights`. Observation evidence changes the threat set,
not the weights or ordering rules. Repeating identical evidence produces the
same candidate keys, score totals, selected options, warnings, and ordering
for every policy.

## Tested changes

The Application tests cover:

- an observed, snapshot-absent Reverse 九色玉蝉法 adding a verified reset
  threat and Reverse 奇輪佐命功 recommendation when the player owns it;
- the same threat leaving all recommendations infeasible when the player does
  not own the verified counter;
- confirmation of an unchanged Direct magic-sound threat preserving all
  policy decisions while upgrading membership provenance to
  `ObservedEquipped`;
- a newer unrecognized Reverse direction removing only the old verified
  threat-based recommendation while retaining a current-screen warning; and
- repeated equivalent requests returning identical threat and decision
  fingerprints.

The workflow remains information-only and session-bound. It does not mutate a
save, game data, process, input device, or game runtime.

## Verification result

On 2026-08-07, `dotnet build TaiWu.slnx --no-restore` completed with zero
warnings and zero errors. `dotnet test TaiWu.slnx --no-restore --no-build`
completed with 816 total tests: 811 passed, 0 failed, and 5 existing opt-in
local integration tests skipped because their environment switches were not
set.
