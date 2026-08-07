# Target skill-selection resolution

## Purpose

E3-003 resolves a player-visible sparring-opponent skill name to an explicitly
confirmed stable catalogue identity. It reuses the Epic 2 catalogue lifecycle,
normalization, bilingual fallback, filtering, and deterministic ranking rather
than maintaining a second name-matching implementation.

The resolver is an Application use case. It reads helper-owned catalogue data
through existing ports and returns immutable Application/Domain values. It
does not read a screenshot, game process, arbitrary file path, or raw mechanic
claim.

## Request

`TargetSkillSelectionRequest` carries:

- a `Sparring` observation context;
- preferred Traditional Chinese or English language;
- the visible skill-name query;
- the player-reported `SkillCategory` row;
- an optional explicitly confirmed skill ID;
- optional visible `Direct` or `Reverse` direction and slot index;
- optional skill IDs already present in the target snapshot.

Hostile/story contexts are rejected before catalogue access. Query length,
IDs, category, direction, slot index, and snapshot IDs are validated and
copied. A repeated target-snapshot ID is invalid.

## Search and confirmation flow

`ResolveTargetSkillSelection` delegates search to
`SearchCombatSkillDefinitions`, including its Unicode Form KC, case, full-width
character, and whitespace normalization. Search considers names in both
languages. The preferred language controls display only; verified fallback
remains explicit through `CombatSkillDisplayName.UsedFallback`.

Candidates preserve the existing ordering:

1. exact bilingual name matches;
2. partial name matches;
3. normalized display name using ordinal comparison;
4. stable skill ID.

One candidate returns `ConfirmationRequired`; multiple candidates return
`Ambiguous`. Neither state resolves a skill. Only a confirmed ID from the
current candidate set can return `Resolved`.

## Category verification

The player-reported row is never trusted as the static category. The resolver
maps the verified `CombatSkillEquipmentType` by name to `SkillCategory`:

| Catalogue equipment type | Observation category |
|---|---|
| `Neigong` | `Neigong` |
| `Attack` | `Attack` |
| `Agility` | `Agility` |
| `Defense` | `Defense` |
| `Assistance` | `Assistance` |

Missing or unsupported static category data returns
`DefinitionUnsupported`. A confirmed ID under the wrong player-reported row
returns `CategoryMismatch` and no resolved selection.

## Catalogue lifecycle

Only `CombatSkillCatalogueStatus.Current` permits candidates. Other states map
without guessing:

| Catalogue state | Selection status |
|---|---|
| `Missing`, `MissingSources` | `CatalogueMissing` |
| `Stale` | `CatalogueStale` |
| `Rebuilding` | `CatalogueRebuilding` |
| `UnsupportedVersion` | `CatalogueUnsupportedVersion` |
| read, repository, or corruption failure | `CatalogueUnavailable` |

An unmatched query returns `DefinitionMissing`; a confirmed ID outside the
current candidates returns `ConfirmationInvalid`.

## Static-fact projection

`TargetSkillStaticFacts` deliberately contains only the verified catalogue
values required by later snapshot/threat analysis:

- stable skill ID and bilingual display name;
- verified observation category;
- base grid cost and slot contribution;
- element;
- typed direct and reverse effect IDs.

It does not expose `RawCombatSkillDescription`. Raw text remains display-only
catalogue material and cannot become a typed threat or scoring mechanic.

`TargetSkillSnapshotPresence` distinguishes `Present`, `Absent`, and `Unknown`.
An observed skill absent from the target save can still resolve from these
static facts; absence does not fabricate mastery, progress, or unrelated
character state.

## Verification

Focused command:

```powershell
dotnet test tests/TaiWu.Application.UnitTests/TaiWu.Application.UnitTests.csproj --no-restore -- --filter-class TaiWu.Application.UnitTests.CombatSkills.ResolveTargetSkillSelectionTests
```

Result on 2026-08-07: **19 passed, 0 failed, 0 skipped**.

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
