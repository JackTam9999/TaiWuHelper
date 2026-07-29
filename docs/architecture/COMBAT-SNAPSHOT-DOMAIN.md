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
| `TargetCombatSnapshot` | Identity, age, learned skills, optionally available equipped loadout, and equipment |
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
- Slot usage cannot be negative or exceed capacity.
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
