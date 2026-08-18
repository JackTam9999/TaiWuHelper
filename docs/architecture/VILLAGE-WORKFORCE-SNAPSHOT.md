# Village workforce snapshot reader

| Field | Value |
|---|---|
| Status | Implemented — one-pass read-only projection |
| Epic | [EPIC-007](../roadmap/epic-007/EPIC.md) |
| Backlog item | [E7-003](../roadmap/epic-007/BACKLOG.md#e7-003--project-a-one-pass-read-only-settlement-snapshot) |
| Domain contract | [Village workforce Domain](./VILLAGE-WORKFORCE-DOMAIN.md) |
| Evidence boundary | [E7-000 village-workforce evidence](../scenarios/E7-000-village-workforce-evidence.md) |
| Supported GameData | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` |

## Purpose

Project the version-1 work-candidate universe, occupied shop-manager targets,
current assignments and target-required saved base qualifications into one
immutable `VillageWorkforceSnapshot`. The adapter reads one trusted configured
save revision and exposes only Application and Domain contracts.

## Application boundary

`IVillageWorkforceSnapshotReader` extends `IReadOnlyGameDataSource` and accepts
only the path-free `VillageWorkforceSnapshotReadRequest.Current` request. A
caller cannot supply a filesystem path or request mutation.

`VillageWorkforceSnapshotReadResult` has these states:

| State | Meaning |
|---|---|
| `Complete` | Every emitted worker and target fact required by the snapshot was projected |
| `Partial` | A worker fact was incomplete/unsupported or an invalid candidate entry was safely omitted |
| `SaveUnavailable` | Trusted configuration or configured file is unavailable |
| `UnsupportedVersion` | Installed GameData product version has no approved mapping |
| `ConflictingSources` | Duplicate candidate or target identities make one coherent snapshot impossible |
| `ChangedRevision` | Before/after save evidence differs; the result is discarded |
| `ReadFailed` | A bounded safe read or mapping failure occurred |

Cancellation remains standard task cancellation: a requested
`CancellationToken` produces `OperationCanceledException`, never a partial
success payload. Unit tests pin pre-cancelled behavior.

Failure identities and messages are safe and path-free. The reader logs the
exception for bounded safe failures but never returns exception text, a local
path or a hash to Presentation.

## One-pass flow

```text
path-free request
    -> trusted configured-save resolver
    -> exact GameData version gate
    -> TaiwuArchiveReadSession.ReadAsync (one callback)
        -> source SHA-256 and capture time
        -> work-candidate IDs
        -> Taiwu building areas and shop configuration
        -> occupied manager-list positions and current assignments
        -> distinct required life-skill disciplines
        -> one base-qualification buffer read per emitted worker
        -> immutable Domain snapshot
    -> before/after revision and SHA-256 verification
    -> typed complete/partial/failure result
```

No worker, building, slot or discipline opens the archive again. The existing
process-wide archive lock and same-size/same-time SHA guard remain authoritative.

## Source mapping

### Worker universe

The alternative universe is the distinct positive IDs from
`GetVillagersForWork(true, false)`. Positive saved current-manager IDs are
unioned only so current assignment evidence cannot disappear. A worker in the
candidate result is `Eligible`; a current worker outside it is `CurrentOnly`.

The reader adds confirmed typed facts for candidate-universe membership and
current-assignment membership. It does not infer complete village membership
or use the broader availability diagnostic, Taiwu group, target lookup,
location or localized name.

### Targets and current assignments

The adapter enumerates Taiwu building areas and non-empty building blocks in
stable numeric order. A target is emitted only when:

- typed configuration confirms a shop;
- required life-skill type is in the verified `0..15` range;
- a saved shop-manager collection exists; and
- one collection position contains a positive current character ID.

Target identity is building key plus original manager-list position. The
current assignment preserves the character at that exact position with
configured-save provenance. Non-positive entries do not create invented
vacancies.

### Worker facts

The reader calculates the distinct required-discipline set once. It reads each
emitted character's fixed base life-skill buffer once and emits one confirmed
`Int16` fact per required discipline. A missing character becomes `Incomplete`;
an unsafe or incompatible buffer becomes `Unsupported`. Neither state receives
zero.

The adapter never calls current qualification, current attainment,
`CalcTaiwuVillagerEfficiencyInBuilding`, output, shop-revenue or mutation APIs.

## Coherence and provenance

Every profile receives the same `WorkforceSourceVersions`:

- configured-save SHA-256;
- installed GameData product version;
- mapping version `1`;
- candidate-universe version `1`; and
- fingerprint schema version `1`.

Configured-save provenance must use that exact SHA as revision identity.
Installed-GameData provenance must use the exact supported product version.
The Domain snapshot rejects a mixed revision before it can reach a use case.
Capture time is UTC and source identity never contains the save path.

Stable reads produce equal source versions, worker/profile fingerprints,
target fingerprints, assignments and diagnostics. Capture time remains an
honest per-request fact, so the complete snapshot fingerprint may differ while
the normalized source projection is equal.

## Dependency injection

`AddTaiwuInfrastructure` registers one singleton
`IVillageWorkforceSnapshotReader`. It receives the existing singleton archive
session, trusted save-path provider, registered `TimeProvider` and logger. No
Presentation layer constructs the adapter.

## Safety enforcement

The architecture suite verifies that the reader contains the approved typed
getter calls and no efficiency calculation, assignment setter, villager setter,
building setter, collection mutator or archive-save call. Existing global
source scans also reject file writes, destructive operations, persistence,
process control and game-control capabilities.

GameData types are confined to the internal Infrastructure class. The public
Application request, result and port, and every Domain snapshot property, are
GameData-free.

## Local verification

The guarded stable-save run on 2026-08-18 reported:

| Observation | Result |
|---|---:|
| Read status | `Complete` |
| Emitted workers | 306 |
| Occupied manager targets | 216 |
| Current assignments | 216 |
| Snapshot diagnostics | 1 informational standalone-runtime boundary |
| Cold load and production projection | 18.663 seconds |
| Warm unchanged-revision projection | 2.636 seconds |
| Guarded save/GameData files unchanged | 3 of 3 |

The production budget is 30 seconds cold and 3 seconds warm. It retains two
full SHA-256 guards and constructs complete immutable profiles, provenance,
diagnostics and fingerprints. This intentionally differs from the exploratory
E7-000 aggregate-probe budget, whose smaller output passed its own 2-second
warm target.

The two production reads had equal normalized source projection. The
configured save, `GameData.dll` and `GameData.Shared.dll` retained the same
length, last-write time and SHA-256 before and after. No local identity, name,
path or hash was recorded.

```powershell
dotnet test tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj -c Release --no-build -- --filter-class TaiWu.Infrastructure.IntegrationTests.VillageWorkforceSnapshotIntegrationTests
```
