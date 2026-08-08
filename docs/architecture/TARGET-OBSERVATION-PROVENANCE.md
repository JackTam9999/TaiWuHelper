# Target-observation provenance and conflict results

## Purpose

Epic 3 combines evidence without silently replacing its sources. E3-002 adds
a Domain vocabulary for retaining available, unavailable, stale, and
conflicting field evidence while keeping all public identities deterministic
and safe to expose.

The vocabulary describes evidence quality only. It is not a win probability,
combat-success score, or statistical confidence estimate.

## Source kinds

`SnapshotDataSource` distinguishes:

| Source | Meaning |
|---|---|
| `Save` | A value read from the configured save snapshot |
| `GameConfiguration` | A value mapped from the installed, version-matched game configuration |
| `CurrentScreenObservation` | A manually confirmed current-screen value with capture time and evidence reference |
| `VerifiedRule` | A versioned helper Domain rule backed by recorded evidence |

`SkillProgressSourceKind` retains its existing save, current-screen, and
verified-rule values and adds `InstalledConfiguration`. Existing numeric enum
values and player-loadout behavior remain unchanged.

## Field provenance

`SnapshotFieldSource` carries a logical field path, source kind, UTC capture
time, and short opaque evidence reference. Field paths and evidence references
reject whitespace, filesystem separators, and traversal syntax. They cannot
contain a local screenshot path, save path, or multiline exception detail.

Examples of valid public references include:

- `save:abc123`
- `gamedata:68032f25`
- `E3-000-CAP-002`
- `rule:E3-000`

The target merger uses separate logical paths for separate evidence claims:

| Field path | Claim |
|---|---|
| `target.equippedSkills` | A sparring `運功` screen reported equipped membership |
| `target.visibleActiveEffects` | A hostile/story combat panel reported only the listed active effects |
| `target.loadoutObservation` | The session observation and its coverage/context |

`target.visibleActiveEffects` never replaces or unions into
`target.equippedSkills`. Its coverage is always partial, so an omitted skill
remains unknown and cannot become an absence claim.

The source contains no `Exception`, file, process, GameData, persistence, or
ASP.NET Core type.

## Evidence field states

`SnapshotEvidenceField<T>` has four constructible states:

| Status | Value semantics | Retained observations |
|---|---|---|
| `Available` | Exposes one selected value and source | Exactly the selected observation |
| `Unavailable` | Exposes no value | None |
| `Stale` | Exposes no current value | One or more older observations |
| `Conflicting` | Exposes no selected value | At least two observations with distinct values |

Reading `Value` outside `Available` throws. Callers must branch on `Status`
instead of converting unavailable, stale, or conflicting evidence into a
default value.

Non-available states carry an uppercase stable `ReasonCode`, not an arbitrary
exception string. Codes accept only uppercase ASCII letters, digits, and
underscores and must begin with a letter. Presentation translates these codes
into bilingual user text later; it never exposes local exception messages.

## Conflict ordering

`SnapshotFieldObservation<T>` retains the value and its
`SnapshotFieldSource`. Stale and conflicting factories copy the input and sort
observations by:

1. UTC capture time;
2. source kind;
3. evidence reference using ordinal comparison;
4. logical field path using ordinal comparison.

Every retained observation must describe the same field path. Duplicate
source identities are rejected, removing the only ambiguous ordering case.
A conflict also requires at least two distinct values. These rules make output
stable regardless of caller collection order.

## Compatibility

The existing `SnapshotValue<T>`, `SkillProgressField<T>`,
`PlayerLoadoutObservation`, and `CombatSnapshotObservationMerger` contracts are
unchanged. The new richer field is additive and will be consumed by target
observation merge work in E3-004. Existing player current-screen behavior is
covered by both its original tests and the E3-002 compatibility test.

E3-012 adds an optional visible-power percentage to an observed target skill.
It is retained as current-screen evidence but is deliberately absent from
threat signatures, feasibility, and scoring. Unlabelled combat indicators are
not represented at all.

## Verification

Focused command:

```powershell
dotnet test tests/TaiWu.Domain.UnitTests/TaiWu.Domain.UnitTests.csproj --no-restore -- --filter-class TaiWu.Domain.UnitTests.CombatSnapshots.SnapshotEvidenceFieldTests
```

Result on 2026-08-07: **15 passed, 0 failed, 0 skipped**.

Full Domain command:

```powershell
dotnet test tests/TaiWu.Domain.UnitTests/TaiWu.Domain.UnitTests.csproj --no-restore
```

Result on 2026-08-07: **275 passed, 0 failed, 0 skipped**.

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
