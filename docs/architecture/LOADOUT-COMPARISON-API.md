# Loadout comparison API projection

## Purpose

E4-003 projects the immutable loadout comparison through the existing
`POST /api/combat-recommendations` response. Clients receive the same typed
membership, action, availability, provenance, diagnostic, and tactical
semantics as the Domain builder; they do not need to reconstruct a comparison
from the older style arrays.

The endpoint remains information-only. No route, verb, command, or contract
can apply a loadout or change the game.

## Compatibility strategy

`CombatRecommendationResponse` gains one final nullable property:

```csharp
LoadoutComparisonResponse? Comparison = null
```

The property is additive. Existing request fields and existing response fields
retain their shape and meaning. Clients that ignore unknown JSON properties
continue to consume the response unchanged. Source consumers constructing the
record retain compatibility through the optional final parameter.

Successful responses produced by the current server always populate
`comparison`. The nullable declaration protects source compatibility and
allows older serialized responses to be read.

`snapshotReference` remains an opaque shared snapshot identity. Its value now
comes from the deterministic comparison builder rather than a formatted
capture timestamp. The format remains `snapshot:{opaque-value}`; clients must
compare it as a whole string and must not parse the suffix. Every existing
style and the new comparison carry that same reference.

No existing style score changes meaning. The new tactical score projection
adds an explicit policy-local notice and does not replace existing score
components.

## Top-level comparison

`LoadoutComparisonResponse` contains:

| Property | Meaning |
|---|---|
| `reference` | Opaque identity for this exact comparison result |
| `snapshotReference` | Opaque identity for the one immutable snapshot boundary |
| `targetReference` | Logical target identity, never a local path |
| `columns` | Current, Safe, Balanced, Aggressive in fixed order |
| `baselineProvenance` | Source metadata for Current loadout, 萬用 allocation, budgets, and legendary-book assignments |

The response does not expose the configured save path. Comparison, snapshot,
target, diagnostic, reason, threat, caveat, condition, and evidence references
are logical strings. Legacy path-shaped evidence is hashed by the builder
before it enters this projection.

## Column states

Each `LoadoutComparisonColumnResponse` contains typed `kind`, `status`, and
optional `policy` plus exactly one status-valid payload:

| Kind/status | `loadout` | `tacticalSummary` | `diagnostic` |
|---|---:|---:|---:|
| Current/Available | object | null | null |
| Policy/Available | object | object | null |
| Policy/Infeasible | null | null | object |
| Policy/Unavailable | null | null | object |

An infeasible or unavailable policy therefore cannot serialize a fake empty
loadout. The column remains present with a stable diagnostic code, safe
summary, and path-safe evidence references.

## Loadout and skill projection

Every available loadout contains five ordered category objects and one
complete available/unavailable 萬用 allocation value. A category contains:

- typed category identity;
- used, capacity, remaining, category contribution, and 萬用 contribution;
  and
- stable-ID-ordered skill cells.

A skill cell contains:

- category plus stable skill ID;
- name availability and value/reason from the exact player snapshot;
- current practice-direction availability and value/reason;
- comparison membership availability and value/reason;
- effective-cost availability and value/reason; and
- an ordered list of direction/breakthrough actions.

Each action retains required direction and its structured reason: logical
reason code, summary, evidence references, and threat references. Membership
is not replaced by an action, so an Added skill may serialize together with a
direction or breakthrough action.

## Available/unavailable values

The API does not use null alone to overload missing, zero, or unavailable.
Typed value objects use this shape:

```json
{
  "isAvailable": false,
  "value": null,
  "unavailableReason": "Used slots were not established."
}
```

When `isAvailable` is true, `value` is present and
`unavailableReason` is null. An available numeric zero is represented as
`{ "isAvailable": true, "value": 0, "unavailableReason": null }`.

Dedicated value contracts exist for integers, decimals, strings, practice
direction, membership, skill identity, and complete 萬用 allocation. This
keeps the generated OpenAPI schema concrete while preserving the same
two-state semantics for every fact.

## Tactical summary

Each feasible policy exposes:

- manual-action count with availability;
- primary active defense and agility with availability;
- covered and unresolved threat reference/code/title triples;
- condition, caveat, and evidence references;
- ordered policy score components with weight, available/unavailable score,
  explanation, and evidence; and
- `scoreScopeNotice`.

The current English notice is:

> Scores rank candidates only inside this policy; they are not win odds.

Threat titles come from the same recommendation threat analysis. The mapper
does not interpret raw effect prose, recalculate coverage, or compare policy
totals.

## Current provenance

Each `LoadoutComparisonProvenanceResponse` exposes:

- typed baseline field;
- `SnapshotDataSource`;
- UTC capture time; and
- opaque evidence reference.

This allows clients to present a mixed baseline accurately. For example,
equipped skills and 萬用 allocation may be current-screen observations
while slot budgets remain save-derived. A stale/rejected observation does not
change those save-derived entries.

## Deterministic ordering

Serialization preserves builder order:

1. Current, Safe, Balanced, Aggressive columns;
2. Neigong, Attack, Agility, Defense, Assistance categories;
3. ascending stable skill ID;
4. typed action order;
5. score-component kind; and
6. ordinal logical-reference order.

The response records declare properties in stable contract order. Mapping the
same recommendation twice produces byte-equivalent comparison JSON with the
same serializer options.

## Representative JSON fragment

Property names below use the configured web JSON naming policy. The fragment
is abbreviated but shows the important state boundaries.

```json
{
  "snapshotReference": "snapshot:4F...",
  "comparison": {
    "reference": "comparison:9A...",
    "snapshotReference": "snapshot:4F...",
    "targetReference": "target:16317",
    "columns": [
      {
        "kind": "Current",
        "status": "Available",
        "policy": null,
        "loadout": {
          "categories": [
            {
              "category": "Attack",
              "capacity": {
                "used": {
                  "isAvailable": false,
                  "value": null,
                  "unavailableReason": "Used slots were not established."
                }
              },
              "skills": []
            }
          ]
        },
        "tacticalSummary": null,
        "diagnostic": null
      },
      {
        "kind": "Safe",
        "status": "Available",
        "policy": "Safe",
        "loadout": {
          "categories": [
            {
              "category": "Attack",
              "skills": [
                {
                  "identity": { "category": "Attack", "skillId": 604 },
                  "membership": {
                    "isAvailable": true,
                    "value": "Added",
                    "unavailableReason": null
                  },
                  "actions": [
                    {
                      "kind": "DirectionChangeRequired",
                      "requiredDirection": "Reverse"
                    }
                  ]
                }
              ]
            }
          ]
        },
        "diagnostic": null
      },
      {
        "kind": "Balanced",
        "status": "Infeasible",
        "policy": "Balanced",
        "loadout": null,
        "tacticalSummary": null,
        "diagnostic": {
          "code": "NO_FEASIBLE_BALANCED",
          "summary": "No feasible scored candidate is available for a manual combat plan."
        }
      }
    ]
  }
}
```

## Mapper boundary

`CombatRecommendationResponseMapper` builds the comparison once, uses its
snapshot reference for the top-level and existing style projections, and then
delegates to `LoadoutComparisonResponseMapper`.

The comparison mapper may look up player display names/current direction and
threat titles only in the same retained recommendation. A comparison skill
missing from that snapshot or using a mismatched category fails mapping rather
than producing an unrelated name.

The mapper has no access to configuration paths, request save paths,
Infrastructure exceptions, screenshots, processes, or game-control services.

## Verification

Controller and mapper tests cover:

- a fully feasible four-column response;
- stable skill names, five categories, tactical threat titles, and the score
  scope notice;
- mixed feasible/infeasible policy columns;
- a missing style mapped to Unavailable;
- unavailable effective cost, used slots, and remaining slots with reasons;
- observed versus save-derived Current provenance;
- deterministic serialization; and
- absence of configured save paths and exception detail from comparison JSON.

Focused command:

```powershell
dotnet test tests\TaiWu.API.UnitTests\TaiWu.API.UnitTests.csproj --no-restore
```
