# Combat snapshot Domain model

## Purpose

The combat snapshot is the immutable Domain input for feasibility, threat
analysis, recommendation, and presentation. It replaces parsing of the legacy
line-oriented diagnostic report in later Milestone 1 slices.

The model contains no `GameData`, Infrastructure, ASP.NET Core, persistence, or
process-control type. Infrastructure will map locally read game values into
this vocabulary without allowing mutation-capable objects to cross the
boundary.

## Aggregate

| Type | Responsibility |
|---|---|
| `CombatSnapshot` | Root containing metadata, one player, one target, and warnings |
| `CombatSnapshotMetadata` | Save path, SHA-256, capture time, save modified time, and GameData version |
| `PlayerCombatSnapshot` | Learned skills, equipped loadout, equipment, slot budgets, generic allocation, and legendary-book modifiers |
| `TargetCombatSnapshot` | Identity, age, features, learned skills, optionally available equipped loadout, and equipment |
| `CharacterFeatureSnapshot` | Target feature ID, configured display name, and level |
| `CombatSkillSnapshot` | Skill identity, category, actual grid cost, mastery, practice direction, slot contribution, and direct/reverse effect IDs |
| `CombatLoadoutSnapshot` | Equipped skill IDs separated into all five skill categories |

All collection inputs are copied into `ImmutableArray<T>`. Later caller
mutation cannot change a constructed snapshot.

## Domain values

### Practice direction

`PracticeDirection` preserves the verified source semantics as named values:

| Value | Meaning |
|---:|---|
| `-1` | `Reverse` |
| `0` | `Neutral`; neither direction-specific effect is active |
| `1` | `Direct` |

### Slot categories

`SkillCategory` explicitly represents `Neigong`, `Attack`, `Agility`,
`Defense`, and `Assistance`. `SlotBudgetSet` requires exactly one budget for
every category.

`GenericSlotAllocation` keeps a single total and separate allocation for the
four eligible outer categories. It rejects negative values and allocations
whose sum exceeds the total. Generic slots cannot be allocated to Neigong.

`SlotBudget` always retains the saved category capacity. Its used and remaining
values are explicit `SnapshotValue<int>` instances because standalone GameData
cost calculation can require an unavailable combat-effect runtime. The adapter
must leave those values unavailable instead of calling that runtime or guessing
from configured cost.

`SkillSlotContribution` permits negative category-specific adjustments because
locally verified inner-power configuration can reduce one category while
increasing another. Its generic contribution cannot be negative.

### Unavailable values

`SnapshotValue<T>` has two constructible states:

- `Available(value)`.
- `Unavailable(reason)`.

Unavailable values never silently become `0`, `false`, an empty string, or an
empty loadout. Reading `Value` while unavailable throws, forcing callers to
branch on `IsAvailable` and preserve the reason.

This is required for the golden target because the current disk save does not
contain its equipped skill loadout.

### Evidence source

`SnapshotDataSource` distinguishes save data, local game configuration, and a
current-screen observation. `LegendaryBookModifier` requires an evidence
reference and source so an unverified fixed cost cannot enter later cost
calculation.

## Construction invariants

- Character IDs and skill IDs cannot be invalid.
- Available grid costs must be greater than zero.
- Available effect and equipment IDs cannot be negative.
- Available slot usage cannot be negative or exceed capacity.
- Every slot category is present exactly once.
- Generic slots cannot be allocated more than once.
- A skill cannot appear twice in one equipped loadout.
- Learned skills and equipment slots cannot be duplicated.
- Legendary-book fixed costs must be at least one and evidence-backed.
- A skill can have at most one legendary-book fixed-cost modifier.
- Snapshot SHA-256 values contain exactly 64 hexadecimal characters.
- Missing data always carries a non-blank reason.

These are construction invariants only. Later Domain services remain
responsible for ownership, effect availability, proposed-loadout feasibility,
and combat activation requirements.

## Application read port

`ICombatSnapshotReader` is the Application boundary for obtaining the
aggregate. Its only operation is:

```csharp
Task<CombatSnapshot> ReadAsync(
    CombatSnapshotReadRequest request,
    CancellationToken cancellationToken = default);
```

The request requires a save-file path and target character ID. The return type
contains source metadata and warnings as part of the immutable aggregate.

The port inherits `IReadOnlyGameDataSource`, uses query-only naming, supports
cancellation, and exposes no `GameData` type. The legacy line-report reader
remains a separate diagnostic port.

## Infrastructure adapter

`TaiwuCombatSnapshotReader` maps one loaded archive directly into the immutable
Domain aggregate. The legacy diagnostic report uses a separate projector over
the same `TaiwuArchiveReadSession`; structured consumers never parse diagnostic
lines.

The shared archive session:

- Serializes access to GameData's process-wide static domains.
- Initializes configuration once.
- Clears monitored one-shot handlers before every archive load.
- Captures the save length, SHA-256, and modified time before loading.
- Discards a result if the same fingerprint is not present after projection.

The adapter maps configuration `GridCost`, confirmed mastery, activation-state
direction, and configured category/generic grid contributions independently.
It deliberately does not call `Character.GetCombatSkillGridCost`, because that
method enters `SpecialEffectDomain.ModifyData` and requires a live combat
runtime. Consequently, saved category capacity remains available while used
and remaining capacity are unavailable until verified cost rules are applied.

## Current-screen observations

`CombatSnapshotReadRequest` can carry one immutable
`PlayerLoadoutObservation`. It is helper-owned input containing:

- Observation time and an evidence reference.
- Equipped skill IDs grouped by category.
- Generic-slot allocation.
- Optional slot budgets read directly from the displayed screen.

`CombatSnapshotObservationMerger.Merge` returns a new aggregate and never
changes the disk-derived snapshot. Before merging, every observed skill must be
learned by the player and reported under its configured category. An
observation whose timestamp is not newer than the save modified time is not
used and produces a warning. If the save timestamp is unavailable, explicit
current-screen source precedence is used with a warning.

Every replaced aggregate field receives a `SnapshotFieldSource` entry with a
stable field path, `CurrentScreenObservation` source, observation time, and
evidence reference. The current paths are:

- `player.equippedSkills`
- `player.genericSlotAllocation`
- `player.slotBudgets` when displayed budgets were reported

Observation data exists only in Domain/Application memory and the returned
snapshot. The merge operation has no persistence, file, process, input, or
game-control dependency.

## Effective skill cost

`CombatSkillCostCalculator` is a pure Domain service. It returns a
`CombatSkillCostBreakdown` containing configured base cost, confirmed mastery,
the applied evidence-backed legendary-book modifier, the derived reduction,
and effective cost.

The calculation order is:

1. Use configured `GridCost` as the base.
2. Reduce it by one only when mastery is available and confirmed.
3. Keep the mastery-adjusted result at or above one.
4. If one confirmed `收置` modifier applies, cap the occupied cost at its
   evidence-backed fixed cost of one.

`收置` is modelled as a fixed cost rather than an additive reduction. Its
reported reduction is derived from the mastery-adjusted cost. A missing
`GridCost` or unknown mastery leaves effective cost unavailable; if `收置`
applies, its derived reduction is unavailable for the same reason. Multiple
fixed-cost modifiers for one skill are rejected rather than stacked.

The skill shown as `生效功法` is a replaceable assignment, not part of the
effect definition. `LegendaryBookModifier.ForSkill` returns a new immutable
helper value for evaluating a proposed assignment while preserving the current
value. This is an in-memory recommendation calculation only; it has no game,
save, process, input, or persistence dependency.

Owning a legendary book does not itself create a cost modifier. An unassigned
`收置` slot is represented by the absence of a modifier and leaves cost
unchanged. Effects from books outside the player's verified owned set remain
unknown; they are never guessed or treated as available.

The separate `大盈` and `大成` category/generic-grid trade-offs are deliberately
not cost modifiers. They belong to slot-budget calculation in M1-008. The
verified screenshots and their hashes are recorded in
`docs/scenarios/M1-007-effective-skill-cost-evidence.md`.
