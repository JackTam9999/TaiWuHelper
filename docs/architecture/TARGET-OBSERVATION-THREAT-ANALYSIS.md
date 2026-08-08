# Target-observation threat analysis

## Purpose

E3-007 feeds a successfully merged target observation into the versioned
`TargetThreatAnalyzer`. E3-012 permits hostile/story observations only for
skill effects visible in the combat UI. Those observations remain partial and
never treat an inaccessible opponent loadout as empty or known.

At the E3-007 boundary this slice changed threat analysis only, keeping
recommendation decisions on the original save-only snapshot so the two
behaviors were independently testable. E3-008 now runs feasibility, counter
selection, scoring, and explanation against this observation-enhanced typed
threat set.

## Candidate precedence

The analyzer reads target skills in this order:

1. skills explicitly confirmed in the applied current-screen observation,
   ordered by category, visible slot where applicable, and stable skill ID;
2. remaining equipped skills supplied by the save snapshot, ordered by the
   existing category and loadout order;
3. remaining learned skills, ordered by stable skill ID.

This order works even when the save does not expose any equipped target
loadout. A sparring partial observation can confirm an equipped subset. A
hostile/story partial observation confirms only a currently visible active
effect. Neither can claim that omitted skills are absent.

Each matched `TargetThreatSource` records a `TargetThreatSourceKind` and opaque
evidence reference:

| Kind | Meaning | Evidence reference |
|---|---|---|
| `ObservedEquipped` | Current sparring screen confirmed equipped membership | Observation reference |
| `ObservedActiveEffect` | Hostile/story combat panel confirmed a listed active effect, not equipped membership | Observation reference |
| `SaveEquipped` | Save snapshot reported equipped membership | Save hash reference |
| `LearnedUnconfirmed` | Skill is learned but current equipped membership is not confirmed | Save hash reference |

Every typed threat also retains its existing `VerifiedRule` evidence. Thus an
observation-used threat carries both the current-screen membership source and
the versioned rule evidence that permits severity and downstream handling.

## Coverage behavior

Sparring partial coverage adds observed equipped membership. Hostile/story
partial coverage leaves saved equipped membership byte-for-byte unchanged and
adds a separate `BattleVisibleActiveEffect` analysis source. Neither removes
saved membership or learned possibilities. An unavailable full loadout
remains unavailable, while the confirmed visible effects are still analyzed
first.

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

Visible power is not part of a threat signature. Replacing 142% with 204% on
otherwise identical evidence produces the same threats, feasibility, and
ranking. Raw panel effect prose also cannot enter the analyzer: the selected
catalogue identity and verified direction supply the versioned effect ID.

## Original and merged snapshots

`TargetObservationProcessingResult` retains both:

- `OriginalSnapshot`, used as the stable before-state for impact comparison;
  and
- `Merge.Snapshot`, used for returned snapshot metadata, target threat
  analysis, and E3-008 recommendation decisions.

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
