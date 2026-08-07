# Target-loadout observation merge

## Purpose

E3-004 merges one verified current-screen sparring observation into an
immutable combat snapshot. The pure Domain operation applies only evidence
that the supported UI exposes, retains source disagreements, and never treats
an inaccessible hostile or story loadout as empty.

The merger does not read or write a save, screenshot, game process, catalogue,
or helper cache. Application code resolves visible names to typed catalogue
facts before calling it.

## Preconditions

`TargetLoadoutObservationMerger.Merge` requires:

- the observation target ID to equal the snapshot target ID;
- the observation to have been constructed for `Sparring`;
- the snapshot GameData version to equal the E3-000 verified version;
- typed static facts for any observed skill absent from the target snapshot;
- explicit confirmation when the save timestamp is unavailable.

Hostile and story contexts cannot construct `TargetLoadoutObservation`, so the
merge API cannot convert `秘而不宣` into an empty or partial loadout. Resolved
static facts are projected from the verified catalogue without raw combat-skill
descriptions, mastery, or unobserved character progress.

## Freshness and status

The result exposes one stable `TargetLoadoutMergeStatus`:

| Status | Meaning |
|---|---|
| `Applied` | The observation is newer than the save, or missing save time received explicit precedence confirmation |
| `Stale` | The observation time is not newer than the save time; evidence is retained but the target is unchanged |
| `PrecedenceConfirmationRequired` | Save time is unavailable and confirmation was not supplied |
| `UnsupportedVersion` | Snapshot GameData does not match the version-bound E3-000 evidence |

Every non-applied state returns a new snapshot carrying a stable warning while
leaving the original target values unchanged.

## Coverage merge

For `PartialLoadout`, listed skills are affirmative equipped evidence only.
When saved equipped membership is available, the merger unions the observed
IDs with it. When saved membership is unavailable, it remains unavailable and
the partial observation is attached as a separate overlay. Omitted skills are
never removed or converted into negative evidence.

For `CompleteCurrentLoadout`, construction already requires the exact
versioned completeness rule. The observed current displayed loadout replaces
saved equipped membership and can remove the existing
`TargetLoadoutNotPersisted` warning. It makes no claim about other presets.

## Skill and direction merge

An observed skill already present in learned target data retains every saved
field except an explicitly visible `Direct` or `Reverse` direction. An absent
skill is added only from resolved typed static facts. Mastery and any other
unobserved progress remain unavailable.

Direction evidence is field-specific. A differing saved direction produces a
`SAVE_SCREEN_CONFLICT` result containing both sources while the newer visible
direction becomes the effective snapshot value. The same conflict retention
applies when a complete visible loadout differs from saved membership.

## Provenance and determinism

The result exposes loadout evidence plus per-skill direction evidence. Screen
sources use the observation's UTC timestamp and opaque evidence reference;
save sources use a logical save evidence identity. No local path is added to
public provenance.

Learned skills, direction evidence, field sources, and generated warnings have
stable ordering. Inputs are copied or reused as immutable values; the original
snapshot and observation are never mutated.

## Verification

Focused Domain command:

```powershell
dotnet test tests/TaiWu.Domain.UnitTests/TaiWu.Domain.UnitTests.csproj --no-restore -- --filter-class TaiWu.Domain.UnitTests.CombatSnapshots.TargetLoadoutObservationMergerTests
```

Result on 2026-08-07: **10 passed, 0 failed, 0 skipped**.

Focused Application resolver command:

```powershell
dotnet test tests/TaiWu.Application.UnitTests/TaiWu.Application.UnitTests.csproj --no-restore -- --filter-class TaiWu.Application.UnitTests.CombatSkills.ResolveTargetSkillSelectionTests
```

Result on 2026-08-07: **19 passed, 0 failed, 0 skipped**.

Full Domain command:

```powershell
dotnet test tests/TaiWu.Domain.UnitTests/TaiWu.Domain.UnitTests.csproj --no-restore
```

Result on 2026-08-07: **285 passed, 0 failed, 0 skipped**.

Full Application command:

```powershell
dotnet test tests/TaiWu.Application.UnitTests/TaiWu.Application.UnitTests.csproj --no-restore
```

Result on 2026-08-07: **100 passed, 0 failed, 0 skipped**.

Architecture boundary command:

```powershell
dotnet test tests/TaiWu.Architecture.Tests/TaiWu.Architecture.Tests.csproj --no-restore --no-build
```

Result on 2026-08-07: **74 passed, 0 failed, 0 skipped**.

Formatting command:

```powershell
dotnet format TaiWu.slnx whitespace --no-restore --verify-no-changes
```

Result on 2026-08-07: **passed with no formatting changes required**.
