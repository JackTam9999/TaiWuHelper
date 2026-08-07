# Target-loadout observation Domain model

## Purpose

Epic 3 accepts helper-owned, manually confirmed observations without reading
hidden game state. E3-000 establishes the supported UI boundary:

- a `切磋武功` opponent can expose the current displayed `運功` loadout;
- hostile and story targets do not expose that page;
- `秘而不宣` is unavailable evidence, never an empty loadout;
- a complete capture covers only the current displayed loadout, not other
  presets;
- `正` and `逆` are visible, while `相抵` remains unsupported.

The Domain model makes those distinctions construction invariants so later
Application and Presentation code cannot broaden the evidence claim.

## Values

| Type | Responsibility |
|---|---|
| `TargetObservationContext` | Distinguishes `Sparring`, `Hostile`, and `Story`; only `Sparring` can construct an observation |
| `ObservedTargetCombatSkill` | Stable non-negative skill ID, verified category, optional visible direction, and optional category-relative slot index |
| `TargetLoadoutCoverage` | Distinguishes `PartialLoadout` from `CompleteCurrentLoadout` and exposes whether omission can establish absence |
| `TargetLoadoutCompletenessEvidence` | Binds complete coverage to the exact E3-000 rule, GameData version, `CNH` layout, sparring context, and evidence reference |
| `TargetLoadoutObservation` | Target ID, encounter context, UTC observation time, opaque evidence reference, coverage, and immutable observed skills |

These types use only Domain and base-library values. They contain no GameData,
ASP.NET Core, SQLite, filesystem, screenshot, or process type.

## Access invariant

`TargetLoadoutObservation` rejects `Hostile` and `Story` contexts. Those enum
values exist so callers can retain and display the reason the workflow is
unavailable, but they cannot be converted into an empty or partial
observation. The first release therefore asks for loadout input only after the
player confirms a sparring context.

This is stricter than accepting a player reconstruction of a hidden target.
Epic 3 is current-screen evidence, so information the supported UI does not
show remains unavailable.

## Coverage invariant

`PartialLoadout` confirms only the listed equipped skills. Its
`CanEstablishAbsence` value is false, and `EstablishesAbsenceOf` returns false
for every omitted ID.

`CompleteCurrentLoadout` requires a
`TargetLoadoutCompletenessEvidence.FromE3000` value. That factory accepts only
the exact supported GameData version
`1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a` and binds the result to rule
`TAIWU-CNH-TARGET-LOADOUT-1.0.0-68032f25` plus evidence
`E3-000-CAP-002`. Any version change fails construction until new observation
evidence introduces a new rule.

Complete coverage means every equipped skill omitted from the captured
current displayed sparring loadout is absent from that loadout. It makes no
claim about another preset or an inaccessible hostile/story encounter.

## Skill invariant

An observed skill:

- accepts stable IDs greater than or equal to zero;
- requires one defined `SkillCategory`;
- permits no direction, `PracticeDirection.Direct`, or
  `PracticeDirection.Reverse`;
- rejects `PracticeDirection.Neutral` and unknown enum values because E3-000
  did not verify a visible `相抵` example;
- optionally carries a non-negative category-relative slot index.

An observation rejects duplicate skill IDs. When slot indices are supplied,
it also rejects duplicate indices within the same category; the same index in
different category rows is valid.

## Immutability and equality

The constructor copies observed skills into `ImmutableArray<T>`. Later caller
mutation cannot affect the observation. `TargetLoadoutObservation` implements
sequence-based value equality and hashing so separately constructed equivalent
evidence behaves deterministically.

## Verification

Focused command:

```powershell
dotnet test tests/TaiWu.Domain.UnitTests/TaiWu.Domain.UnitTests.csproj --no-restore -- --filter-class TaiWu.Domain.UnitTests.CombatSnapshots.TargetLoadoutObservationTests
```

Result on 2026-08-07: **20 passed, 0 failed, 0 skipped**.

Full Domain command:

```powershell
dotnet test tests/TaiWu.Domain.UnitTests/TaiWu.Domain.UnitTests.csproj --no-restore
```

Result on 2026-08-07: **260 passed, 0 failed, 0 skipped**.

Architecture boundary command:

```powershell
dotnet test tests/TaiWu.Architecture.Tests/TaiWu.Architecture.Tests.csproj --no-restore --no-build
```

Result on 2026-08-07: **74 passed, 0 failed, 0 skipped**. The existing
compiled runner was used because the sandbox denied an unnecessary NuGet
source check; no dependency download was performed.

Formatting command:

```powershell
dotnet format TaiWu.slnx whitespace --no-restore --verify-no-changes
```

Result on 2026-08-07: **passed with no formatting changes required**.
