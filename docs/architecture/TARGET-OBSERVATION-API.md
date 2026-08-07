# Target-observation API vertical

## Purpose

E3-005 adds an optional manually confirmed sparring-target observation to the
existing `POST /api/combat-recommendations` request. The API resolves visible
skill names through the current bilingual catalogue, runs the immutable
E3-004 merge, and returns sanitized resolution, provenance, and snapshot-field
impact metadata.

At the E3-005 boundary this slice did not yet feed the merged target into
threat analysis or scoring. E3-007 added analysis of `Merge.Snapshot`, and
E3-008 now runs feasibility, counter selection, scoring, and explanation
against the resulting typed threats. `OriginalSnapshot` remains the impact
comparison baseline. The typed E3-005 request, validation, and response
contracts remain unchanged.

## Request contract

`CombatRecommendationApiRequest.TargetObservation` is optional. When present,
it carries:

- `Context`, which must be `Sparring`;
- `ObservedAt` and an opaque `EvidenceReference`;
- `PartialLoadout` or `CompleteCurrentLoadout` coverage;
- explicit save-time precedence confirmation when required;
- selected visible skills with name, verified category, optional confirmed
  catalogue ID, optional visible direction, and optional slot index.

The top-level request already supplies the target character ID. It does not
accept a save path; the server continues to use its configured read-only save.
No target-observation property accepts a game path, screenshot path, process
identifier, raw GameData value, raw mechanic description, or executable
command. Evidence references reject whitespace, directory separators,
traversal syntax, and drive-qualified identities.

Hostile and story contexts fail before any target observation is constructed.
The API therefore cannot turn an inaccessible `秘而不宣` view into an empty
loadout claim.

## Workflow boundary

`TargetObservationRecommendationWorkflow` is separate from the core
recommendation namespace. It performs this sequence:

1. read one immutable combat snapshot through `ICombatSnapshotReader`;
2. resolve every visible name with `ResolveTargetSkillSelection`;
3. require explicit stable-ID confirmation and verified category;
4. project only typed static catalogue facts for snapshot-absent skills;
5. construct and merge the target observation;
6. invoke the existing catalogue-independent recommendation builder.

Cancellation is checked before and after the save read, every catalogue
resolution, the merge, threat analysis, candidate generation, and each policy
result. The original save-only `RecommendCombatLoadout` rejects accidental
target-observation input and remains catalogue-independent.

## Validation response

A non-resolved skill returns HTTP 400 `ProblemDetails` with:

- a stable target-observation problem type;
- the `TargetSkillSelectionStatus` code;
- the zero-based selected-skill index;
- sanitized candidates containing ID, available display name, verified
  category, match kind, and target-snapshot presence.

No exception message, local path, catalogue source location, or raw
description is copied into this problem. Structurally invalid observations use
the stable `InvalidObservation` code.

Catalogue resolution failure is invalid input for the requested observation.
Evidence states produced after valid resolution are not HTTP errors. `Stale`,
`UnsupportedVersion`, partial coverage, and save/screen conflicts return HTTP
200 with typed merge and evidence statuses.

## Response projection

`CombatRecommendationResponse.TargetObservation` is null for existing
save-only requests. For observation requests it contains:

- target identity, UTC observation time, opaque evidence, and coverage;
- merge status and loadout evidence status;
- resolved skill identity, safe display name, category, optional direction,
  slot, and snapshot presence;
- sanitized field provenance with source kind, time, and opaque reference;
- added target skills, added/removed equipped membership, and changed
  directions when the merge was applied.

The mapper branches on availability/status before reading any guarded value.
It never returns Domain evidence objects directly, so serializing stale,
conflicting, and unavailable results cannot evaluate a throwing `Value`
getter.

## Compatibility

Requests without `TargetObservation` still use `IRecommendCombatLoadout`, read
the snapshot once, and return the existing threats, styles, warnings, and
inner-power state. Their new observation metadata property is null. No
catalogue resolution is attempted on that route.

## Verification

Focused Application command:

```powershell
dotnet test tests/TaiWu.Application.UnitTests/TaiWu.Application.UnitTests.csproj --no-restore -- --filter-class TaiWu.Application.UnitTests.CombatRecommendations.RecommendCombatLoadoutTargetObservationTests
```

Result on 2026-08-07: **4 passed, 0 failed, 0 skipped**.

Focused API command:

```powershell
dotnet test tests/TaiWu.API.UnitTests/TaiWu.API.UnitTests.csproj --no-restore -- --filter-class TaiWu.API.UnitTests.Controllers.CombatRecommendationTargetObservationControllerTests
```

Result on 2026-08-07: **5 passed, 0 failed, 0 skipped**.

Full Domain command:

```powershell
dotnet test tests/TaiWu.Domain.UnitTests/TaiWu.Domain.UnitTests.csproj --no-restore
```

Result on 2026-08-07: **285 passed, 0 failed, 0 skipped**.

Full Application command:

```powershell
dotnet test tests/TaiWu.Application.UnitTests/TaiWu.Application.UnitTests.csproj --no-restore
```

Result on 2026-08-07: **104 passed, 0 failed, 0 skipped**.

Full API command:

```powershell
dotnet test tests/TaiWu.API.UnitTests/TaiWu.API.UnitTests.csproj --no-restore
```

Result on 2026-08-07: **181 passed, 0 failed, 0 skipped**.

Architecture command:

```powershell
dotnet test tests/TaiWu.Architecture.Tests/TaiWu.Architecture.Tests.csproj --no-restore --no-build
```

Result on 2026-08-07: **75 passed, 0 failed, 0 skipped**.

API build command:

```powershell
dotnet build TaiWuAPI/TaiWuAPI.csproj --no-restore
```

Result on 2026-08-07: **succeeded with 0 warnings and 0 errors**.

Formatting command:

```powershell
dotnet format TaiWu.slnx whitespace --no-restore --verify-no-changes
```

Result on 2026-08-07: **passed with no formatting changes required**.
