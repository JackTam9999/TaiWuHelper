# Target-observation threat analysis

## Purpose

E3-007 feeds a successfully merged sparring-target observation into the
versioned `TargetThreatAnalyzer`. Hostile and story contexts still cannot
construct an observation, so this integration cannot treat an inaccessible
opponent loadout as empty.

This slice changes threat analysis only. Recommendation feasibility, counter
selection, scoring, and explanation continue to use the original save-only
snapshot until E3-008. This keeps the two behavior changes independently
testable.

## Candidate precedence

The analyzer reads target skills in this order:

1. skills explicitly confirmed in the applied current-screen observation,
   ordered by category, visible slot, and stable skill ID;
2. remaining equipped skills supplied by the save snapshot, ordered by the
   existing category and loadout order;
3. remaining learned skills, ordered by stable skill ID.

This order works even when the save does not expose any equipped target
loadout. A partial observation can therefore confirm an equipped subset
without claiming that omitted skills are absent.

Each matched `TargetThreatSource` records a `TargetThreatSourceKind` and opaque
evidence reference:

| Kind | Meaning | Evidence reference |
|---|---|---|
| `ObservedEquipped` | Current sparring screen confirmed equipped membership | Observation reference |
| `SaveEquipped` | Save snapshot reported equipped membership | Save hash reference |
| `LearnedUnconfirmed` | Skill is learned but current equipped membership is not confirmed | Save hash reference |

Every typed threat also retains its existing `VerifiedRule` evidence. Thus an
observation-used threat carries both the current-screen membership source and
the versioned rule evidence that permits severity and downstream handling.

## Coverage behavior

Partial coverage adds observed equipped membership. It never removes saved
equipped membership or learned possibilities. An unavailable full loadout
remains unavailable, while the confirmed subset is still analyzed first.

Complete coverage replaces only current equipped membership. A previously
saved equipped skill omitted by the complete observation remains a learned,
unconfirmed possibility rather than disappearing from history. Any differing
save and screen loadouts remain in `LoadoutEvidence` as a deterministic
conflict.

## Direction and version boundary

An observed `Direct` or `Reverse` direction participates only after the E3-004
merge accepts the exact E3-000 GameData version. If direction is omitted, the
saved direction remains in use. If the observation version is unsupported or
the observation is stale, its snapshot is not applied and cannot change a
rule match.

`TargetThreatAnalyzer` independently requires the exact rule-set GameData
version. A relevant skill with an unknown direction, unavailable effect ID,
or unrecognized effect produces a warning and no typed threat. For an
observed skill, that warning carries the current-screen evidence reference;
it never receives severity or score.

## Original and merged snapshots

`TargetObservationProcessingResult` retains both:

- `OriginalSnapshot`, used as the stable before-state for impact comparison
  and the temporary E3-007 recommendation-decision boundary; and
- `Merge.Snapshot`, used for returned snapshot metadata and target threat
  analysis.

This avoids reconstructing the save baseline from conflict evidence and keeps
E3-005 added/removed impact calculations correct.

## Compatibility and determinism

The public save-only API contract is unchanged. No response DTO property was
added or changed by E3-007, and requests without target observations still
analyze and decide from the same snapshot.

Deterministic tests cover:

- a snapshot-absent observed skill adding a verified threat;
- an observed direction replacing a saved rule match;
- omitted direction leaving the saved threat unchanged;
- unsupported observation versions leaving the save direction in force;
- complete coverage demoting stale saved membership while retaining conflict
  evidence; and
- repeated equivalent inputs producing equivalent ordered threats, sources,
  warnings, and save-only recommendation decisions.

All inputs remain immutable helper values. No save, game data, process, or
runtime state is modified.

## Verification result

On 2026-08-07, `dotnet build TaiWu.slnx --no-restore` completed with zero
warnings and zero errors. `dotnet test TaiWu.slnx --no-restore --no-build`
completed with 813 total tests: 808 passed, 0 failed, and 5 existing opt-in
local integration tests skipped because their environment switches were not
set. Whitespace verification also passed.
