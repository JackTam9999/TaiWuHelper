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
reference and source so an unverified cost reduction cannot enter later cost
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
- Legendary-book reductions must be positive and evidence-backed.
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
