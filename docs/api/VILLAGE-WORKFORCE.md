# Village workforce API

| Field | Value |
|---|---|
| Status | Implemented for E7-008 |
| Base route | `/api/village-workforce` |
| Network boundary | Localhost only |
| Application source | [Village workforce Application workflow](../architecture/VILLAGE-WORKFORCE-APPLICATION.md) |
| Result semantics | [Village workforce shortlist and comparison](../architecture/VILLAGE-WORKFORCE-COMPARISON.md) |

## Purpose and safety boundary

Expose supported village-workforce objectives and occupied shop targets, then
return one complete information-only comparison result. Both actions use
`GET`. There is no assignment, proposal-write, building, collection,
persistence, export, upload, process, input-control or game-control route.

Kestrel remains bound to localhost. Neither endpoint accepts a save path,
GameData path, raw rule, weight, formula, source label, mutation flag or command.

## Public tokens

Request controls are exact case-sensitive strings rather than internal enums:

| Control | Supported tokens |
|---|---|
| Objective | `SHOP_MANAGER_BASE_LIFE_SKILL_QUALIFICATION` |
| Objective version | `1` |
| Filter | `ALL`, `COMPARABLE`, `NEEDS_REVIEW`, `INELIGIBLE` |
| Language | `en`, `zh-Hant` |

Unknown identities, numeric strings such as `0`, malformed comparison pairs,
non-positive character IDs and invalid target coordinates return safe HTTP
`400` before Application execution. An exact objective with an unsupported
version reaches rule resolution and returns HTTP `422`; no fallback rule is
selected.

All response enums are owned by `TaiWuAPI.Contracts.VillageWorkforce`. Global
JSON configuration emits named enum strings and rejects numeric enum values.
Serialized request property names and representative public tokens are pinned
by fixture tests.

## `GET /api/village-workforce/options`

Query:

```text
language=en
```

The endpoint performs one guarded snapshot read and returns:

- the stable objective reference, identity, objective/rule versions, localized
  label and exact-unit description; and
- every supported occupied target with stable reference, area, block,
  building-block index, original manager-list position, required life-skill
  type, `NoExplicitVacancy` state and localized structural label.

The endpoint does not return worker names, a save hash, save path, raw archive
record or GameData configuration object. Language changes text only and cannot
change objective or target identity.

## `GET /api/village-workforce/result`

Example query:

```text
areaId=1&blockId=2&buildingBlockIndex=7&managerSlotIndex=0&objective=SHOP_MANAGER_BASE_LIFE_SKILL_QUALIFICATION&objectiveVersion=1&filter=ALL&firstComparisonCharacterId=202&secondComparisonCharacterId=101&proposedCharacterId=202&language=en
```

Comparison character IDs must be absent together or contain two different
positive values. `proposedCharacterId` is optional and creates only an
information-only manual review plan; it cannot issue an assignment.

### Authoritative response

Complete, partial, invalid-comparison and invalid-proposal responses use
`VillageWorkforceResultResponse`. It preserves:

- finder state, safe failure identity/message where applicable, and canonical
  semantic fingerprint;
- capture time, typed snapshot state, GameData/mapping/universe/fingerprint
  schema versions, without the save SHA;
- localized objective, exact target, required discipline and
  `NoExplicitVacancy` boundary;
- current saved assignment as target/worker references and character ID;
- unfiltered total, comparable, ranked, tied, current-only, ineligible,
  incomplete, unsupported and conflicting counts plus visible count;
- every canonical worker evaluation and the references visible through the
  selected filter;
- optional same-result comparison;
- shared limitations once at result level;
- optional current/proposed manual review plan; and
- snapshot and worker diagnostic states.

Filters affect `visibleCandidateReferences` only. They do not remove canonical
candidate responses or change components, scores, ranks, ties, counts,
comparison, limitations or the authoritative fingerprint.

### Worker evaluation

Each candidate contains:

- stable API reference, character ID, structural localized label and current
  marker;
- API-owned worker/evaluation states, localized state text, optional
  competition rank and nullable exact total;
- all five ordered requirements with typed outcome, stable reason, localized
  explanation, redacted evidence references and typed conflict values;
- the optional exact saved base-qualification component with discipline, raw
  and normalized value, weight, contribution, unit, explanation identity/text
  and redacted evidence; and
- worker diagnostics.

Evidence exposes only reference identity, API-owned source kind and safe source
version. It deliberately omits provenance source identity, save revision,
archive hash, path and raw source object. Conflict values receive the same
redaction.

### Comparison and manual plan

Comparison outcomes are `Higher`, `Lower`, `Equal`, `Unavailable`,
`Incompatible` or `NotComparable`. Values and unit appear only where the
underlying evaluation provides them.

The manual plan identifies current and proposed worker references and returns
typed prerequisite, fact-to-verify and caution items. No item has a completion
flag, timestamp or mutation behavior. The final caution states that no action
was sent to the game.

## HTTP status mapping

| Finder outcome | HTTP |
|---|---:|
| Complete | `200` |
| Partial | `206` |
| Invalid request/comparison/proposal | `400` |
| Missing save or target | `404` |
| Conflicting source or changed revision | `409` |
| Unsupported source or rule | `422` |
| Safe read failure | `500` |

Failure `ProblemDetails` uses localized bounded text and a stable `code`.
Adapter failure messages, exception text and private paths are never copied.
Cancellation and unexpected programmer exceptions propagate to ASP.NET host
handling/logging.

## Contract ownership and verification

Every public village-workforce request/response property is primitive,
framework collection or API-owned contract type. The architecture suite's
recursive property inventory proves that no nested Domain or Application type
enters this API surface.

Controller and contract tests cover discovery, complete and partial results,
bilingual identity stability, components/evidence/diagnostics, JSON tokens,
source redaction, unsupported/missing/unstable reads, invalid target and
comparison, cancellation, unexpected faults, HTTP mapping and GET-only routes.

```powershell
dotnet test tests\TaiWu.API.UnitTests\TaiWu.API.UnitTests.csproj -c Release --no-build -- --filter-class TaiWu.API.UnitTests.Controllers.VillageWorkforceControllerTests --filter-class TaiWu.API.UnitTests.Contracts.ApiJsonContractTests
```
