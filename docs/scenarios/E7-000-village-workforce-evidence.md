# E7-000: Village-workforce evidence

| Field | Value |
|---|---|
| Status | Complete |
| Epic | [EPIC-007](../roadmap/epic-007/EPIC.md) |
| Backlog item | [E7-000](../roadmap/epic-007/BACKLOG.md#e7-000--verify-settlement-sources-and-select-the-first-assignment-vertical) |
| Inspection date | 2026-08-18 |
| GameData assembly version | `1.0.0.0` |
| GameData product version | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` |
| GameData.Shared assembly/product version | `1.0.0.0` / `1.0.0` |

## Purpose

Select one trustworthy village assignment comparison before adding Epic 7
Domain or API contracts. The evidence gate answers:

1. which public source owns the bounded worker-candidate result;
2. which typed building facts identify a supported assignment target;
3. how current assignments are represented;
4. which objective-specific worker value is safe in a standalone archive
   reader; and
5. which tempting productivity calculations must remain unsupported.

## Method

The inspection combined public metadata from the installed, version-matched
GameData assemblies with a guarded local integration probe. The probe opened
one stable configured save through `TaiwuArchiveReadSession`, projected all
facts in one callback, repeated the same projection through the unchanged-save
cache, and compared size, last-write time, and SHA-256 before and after for the
save, `GameData.dll`, and `GameData.Shared.dll`.

Only versions, aggregate counts, numeric ranges, exception types, and timing
are recorded. No local path, hash, character or building identity, localized
name, or proprietary data is committed.

## Source inventory

| Owner | Public member or field | Runtime shape | Decision |
|---|---|---|---|
| `BuildingDomain` | `GetTaiwuBuildingAreas()` | `List<Location>` | Owns the bounded Taiwu building-area catalogue |
| `BuildingDomain` | `GetBuildingBlockList(Location)` | `List<BuildingBlockData>` | Supplies existing building blocks inside one area |
| `BuildingBlockKey` | `AreaId`, `BlockId`, `BuildingBlockIndex` | `Int16` tuple | Stable, language-independent building identity |
| `BuildingBlockData.ConfigData` | `IsShop`, `RequireLifeSkillType` | `Boolean`, `SByte` | Selects shop targets and the exact life-skill discipline required by each target |
| `BuildingDomain` | `TryGetElement_ShopManagerDict(BuildingBlockKey, out CharacterList)` | Ordered character-ID collection | Owns current manager assignments; collection position is the manager-slot position |
| `TaiwuDomain` | `GetVillagersForWork(true, false)` | `List<Int32>` | Selected bounded alternative-worker universe for this version |
| `TaiwuDomain` | `GetAllVillagersAvailableForWork(false)` | `List<Int32>` | Broader diagnostic only; it does not enlarge the selected universe |
| `TaiwuDomain` | `GetVillagerWorkDict()` | `Dictionary<Int32, VillagerWorkData>` | A different map-work assignment family; retained as evidence and excluded from shop-manager ranking |
| `Character` | `GetBaseLifeSkillQualifications()` | Fixed 16-entry `Int16` buffer | Standalone-safe selected comparison component |
| `Character` | `GetLifeSkillAttainment(SByte)` | `Int16` | Rejected: enters unavailable live special-effect context in standalone reading |
| `BuildingDomain` | `CalcTaiwuVillagerEfficiencyInBuilding(BuildingBlockKey, Int32)` | `Int32` | Rejected as an alternative score: assignment-dependent and not standalone-safe for current managers |

Localized building names, manager labels, and descriptions are presentation
only. They never identify a target, slot, discipline, rule, or score.

## Candidate-universe decision

The version-1 alternative universe is exactly the distinct positive character
IDs returned by `GetVillagersForWork(includeUnlockedWorkingVillagers: true,
farmerFirst: false)`, in stable ID order. Current manager IDs are unioned into
the snapshot only to preserve current-assignment evidence; being current does
not make a person an eligible alternative.

This is deliberately called a **work-candidate result**, not complete village
membership. The installed public API does not expose the settlement's internal
member collection as a supported read contract. Epic 7 does not bypass that
boundary with reflection and does not infer membership from location, the
Taiwu group, target lookup, a display label, or general character enumeration.

The broader `GetAllVillagersAvailableForWork(false)` result is recorded for
diagnostics but has different cardinality and does not override the selected
source. `GetVillagerWorkDict()` likewise describes saved map work, not shop
manager slots. Recruitment and companion eligibility remain unrelated.

## Selected first vertical

The first delivery vertical is:

> **Shop manager-slot base life-skill qualification comparison**

A target is an existing Taiwu-area building block whose typed configuration
has `IsShop = true` and a non-negative `RequireLifeSkillType`. Its stable
assignment identity is the `BuildingBlockKey` plus manager-slot index. The
current worker is the positive character ID at that index in the saved
shop-manager collection. Version 1 exposes occupied saved positions only; the
installed sources did not establish vacancy capacity or create selectable
unassigned positions.

For each comparable alternative, the only ordering component is the exact
saved base life-skill qualification at the target's required discipline index.
Higher exact qualification orders first for this target and rule only. Equal
values are exact ties; stable character identity may order display inside a
tie but never breaks it.

The value is an `Int16` saved base qualification (`資質`). It is not current
modified attainment, manager efficiency, production, success probability,
capacity, a percentage, or a universal worker score. The guarded save observed
values from 4 through 300 and 190 distinct values; these are observations, not
a contractual range. Missing or unreadable qualification makes the comparison
unavailable rather than zero.

## Rejected calculation paths

Metadata and method-body inspection showed that
`CalcTaiwuVillagerEfficiencyInBuilding` depends on the Taiwu-village location,
the saved shop-manager collection and slot position, character work state and
age, current life-skill attainment, and an installed global divisor. It cannot
evaluate an alternative without that character first appearing in the current
manager collection, and current attainment enters live special-effect logic.

The guarded run confirmed the boundary:

- 2,759 alternative calls returned the negative unsupported sentinel;
- 196 current-manager calls entered `NullReferenceException` at the absent
  standalone live-runtime context; and
- zero calls produced a supported efficiency value.

Epic 7 therefore does not reproduce, approximate, partially port, or label
that calculation as output. It compares one verified input only. Resource
yield, revenue, shop progress, personalities, dependencies, features, current
modified values, and output prediction remain deferred.

## Guarded local evidence

The stable aggregate run reported:

| Observation | Result |
|---|---:|
| Taiwu building areas | 1 |
| Existing non-empty building blocks | 309 |
| Selected work-candidate IDs | 89 |
| Broader availability diagnostic IDs | 309 |
| Saved map-work records | 252 |
| Supported shop targets | 31 |
| Shop targets with current manager collections | 31 |
| Raw manager entries | 217 |
| Current manager slots across targets | 217 |
| Explicit unoccupied manager entries | 0 |
| Candidate/target qualification pairs | 2,976 |
| Qualification read failures | 0 |
| Targets with at least two distinct qualification values | 31 |
| Cold archive load and projection | 15.418 seconds |
| Warm unchanged-revision projection | 1.881 seconds |
| Guarded files unchanged | 3 of 3 |

Both projections produced the same aggregate signature. The cold run passed
the 30-second budget and the warm run passed the 2-second budget. Cancellation
is checked while enumerating areas, targets, and candidate pairs. Every fact
came from one archive-session callback per projection; no candidate or target
caused another archive open.

Verification command:

```powershell
dotnet test tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj -c Release --no-build -- --filter-class TaiWu.Infrastructure.IntegrationTests.VillageWorkforceEvidenceIntegrationTests
```

## Representative scenarios

| Scenario | Synthetic evidence | Expected state |
|---|---|---|
| `E7-REP-SYN-CURRENT-001` | Supported shop slot has a saved current manager and two candidate-universe alternatives | Preserve current marker; compare all available base qualifications under the same target rule |
| `E7-REP-SYN-NO-TARGET-001` | Snapshot has no occupied supported shop-manager position | Empty target catalogue; do not invent a vacant slot |
| `E7-REP-SYN-ORDER-001` | Two alternatives have distinct base qualification in the required discipline | Higher exact value orders first for this target only |
| `E7-REP-SYN-TIE-001` | Two alternatives have equal base qualification | Shared rank and visible tie; identity orders display only |
| `E7-REP-SYN-CURRENT-OUTSIDE-001` | Current manager is not in the selected alternative universe | Preserve factual current assignment but do not silently make it an eligible proposal |
| `E7-REP-SYN-INCOMPLETE-001` | Required base qualification is missing | `Incomplete`; no zero or rank |
| `E7-REP-SYN-UNSUPPORTED-001` | GameData version differs or only current/runtime-dependent value is available | `Unsupported`; no old mapping or estimated output |
| `E7-REP-SYN-CONFLICT-001` | Target configuration and current-assignment key disagree | `Conflicting`; preserve evidence and omit comparison |

## Deferred mechanics

The first vertical does not implement or infer:

- full village membership or every form of work availability;
- construction, development, migration, resource collection or routing;
- shop revenue, productivity, progress, yield, success rate or capacity;
- vacancies, maximum slot counts, maintenance or building dependencies;
- features, personalities, attributes, current attainment or special effects;
- villager roles, farming, recruitment, training, teaching or companion value;
- library, book, repair, acquisition or equipment planning; or
- any assignment, building, collection, recruitment, save-write or game-control
  action.

## Resolved decisions

1. Use the public work-candidate result as a bounded universe and explicitly
   leave complete village membership unsupported.
2. Select existing shop manager slots, identified by building key and slot
   index, as the first assignment family.
3. Preserve current shop-manager collections independently from alternative
   eligibility.
4. Use only the target-required saved base life-skill qualification as the
   first objective-local ordering component.
5. Reject current attainment and building-efficiency calculation in standalone
   mode; make no productivity or output claim.
6. Keep exact ties, missing evidence, unsupported versions, conflicts, and an
   empty occupied-target catalogue explicit.
7. Preserve one-snapshot reads, deterministic projection, cancellation,
   performance budgets, and byte-for-byte non-interference.
