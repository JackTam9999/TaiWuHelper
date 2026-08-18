# Village workforce Application workflow

| Field | Value |
|---|---|
| Status | Implemented — reusable coherent snapshot orchestration |
| Epic | [EPIC-007](../roadmap/epic-007/EPIC.md) |
| Backlog item | [E7-007](../roadmap/epic-007/BACKLOG.md#e7-007--orchestrate-one-coherent-village-workforce-result) |
| Snapshot port | [Village workforce snapshot](./VILLAGE-WORKFORCE-SNAPSHOT.md) |
| Domain result | [Village workforce shortlist and comparison](./VILLAGE-WORKFORCE-COMPARISON.md) |

## Purpose

Compose one path-free request into one immutable village-workforce result.
`FindVillageWorkforce` reads the configured snapshot once for stateless callers.
`BuildVillageWorkforce` performs the same deterministic calculation from an
already loaded coherent snapshot so an interactive client can inspect several
targets without rereading the save.

`IFindVillageWorkforce` is read only. It exposes no assignment, save path,
persistence, automation or game-control capability.

## Stable request

`VillageWorkforceFinderRequest` contains typed stable identities only:

- `ShopManagerTargetIdentity` with building coordinates and manager-list
  position;
- `WorkforceObjectiveIdentity` with objective kind and version;
- `WorkforceShortlistFilter`;
- an optional pair of `VillageWorkerIdentity` comparison selections; and
- an optional `VillageWorkerIdentity` proposed worker for manual review.

It contains no localized worker/building/discipline name, raw source label,
filesystem path, mutable collection or completion state. The workflow rejects
an unknown filter, a half-selected comparison or the same comparison worker
twice before reading the snapshot.

## One coherent flow

```text
validate typed controls
    -> optional IVillageWorkforceSnapshotReader.ReadAsync exactly once
    -> BuildVillageWorkforce over that immutable read result
    -> locate target in that exact snapshot
    -> resolve exact objective/source/target/discipline rule
    -> evaluate every worker
    -> build canonical shortlist and immutable filter view
    -> optional same-set comparison
    -> optional rankable non-current manual plan
    -> one authoritative result
```

No calculation stage rereads the save. Snapshot, resolved rule, evaluation set,
shortlist, view, comparison and plan are checked as one reference-consistent
chain. The interactive page retains the loaded snapshot until explicit refresh;
changing a target builds a new result from that same immutable workspace.

## Finder states

| State | Payload semantics |
|---|---|
| `Complete` | Complete source and no incomplete/unsupported/conflicting evaluations |
| `Partial` | Partial source or at least one needs-review evaluation; full coherent result retained |
| `InvalidRequest` | Control shape invalid; source was not read |
| `SaveUnavailable` | Trusted configured save unavailable |
| `UnsupportedSourceVersion` | Snapshot adapter rejected installed source version |
| `ConflictingSources` | Snapshot identities conflict |
| `ChangedRevision` | Stable-read guard detected replacement/change |
| `ReadFailed` | Bounded snapshot read failed safely |
| `TargetNotFound` | Stable target is absent from the newly read snapshot |
| `UnsupportedRule` | Objective or source tuple has no exact verified rule |
| `InvalidComparison` | A selected worker is outside the authoritative result |
| `InvalidProposal` | Proposed worker is absent, current, or unrankable |

Read failures map from every non-success snapshot status. Unsupported rule
results retain the exact typed `WorkforceRuleResolutionStatus` but no fallback
definition. Invalid comparison/proposal states retain the newly built
authoritative fingerprint and view while returning no invalid selection
artifact.

## Evaluation failure semantics

Incomplete, unsupported and conflicting worker evaluations do not fail the
whole request or disappear. They remain in their typed shortlist groups and
make the authoritative finder state `Partial`. Ineligible and current-only
workers are complete factual outcomes and do not by themselves make the source
partial.

The Application workflow does not catch Domain invariant violations or
unexpected adapter exceptions. Such programmer faults propagate to host
logging. `OperationCanceledException` also propagates normally. Cancellation
is checked before the read and between each bounded orchestration stage.

## Authoritative identity

`VillageWorkforceFinderResult.Fingerprint` includes:

- complete snapshot fingerprint;
- exact selected target fingerprint;
- typed objective kind and version;
- full resolved rule-definition fingerprint;
- canonical evaluation-set fingerprint; and
- canonical shortlist fingerprint.

Filter, comparison selection and proposed worker are view/session controls and
do not change that authoritative identity. A new save revision or worker fact
changes the snapshot, evaluation and finder fingerprints together.

## Result invariants

An authoritative result requires all of snapshot, resolved rule, evaluation
set, shortlist and view. The result validates exact object/reference links and
requires the view's source fingerprint to match the shortlist. Any comparison
must contain evaluations from that set; any manual plan must use its result
identity.

A failed result contains no partial Domain chain. It carries only typed finder,
snapshot-read and optional rule-resolution states plus a safe path-free failure
identity. It never returns adapter exception text or the snapshot reader's
failure message.

## Verification

Application tests cover complete and evaluation/source-partial results,
pre-read request rejection, every snapshot failure, missing target,
unsupported rule, valid/invalid comparison, valid/invalid proposal,
cancellation, unexpected faults, exact read counts and full replacement on a
new save revision.

```powershell
dotnet test tests\TaiWu.Application.UnitTests\TaiWu.Application.UnitTests.csproj -c Release --no-build -- --filter-class TaiWu.Application.UnitTests.VillageWorkforce.FindVillageWorkforceTests
```
