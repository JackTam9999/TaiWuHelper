# Companion candidates API

| Field | Value |
|---|---|
| Status | Implemented for E6-009 |
| Base route | `/api/companion-candidates` |
| Network boundary | Localhost only |
| Application source | [Companion finder Application architecture](../architecture/COMPANION-FINDER-APPLICATION.md) |
| Evidence and comparison | [Companion candidate shortlist and comparison](../architecture/COMPANION-CANDIDATE-COMPARISON.md) |

## Purpose and safety boundary

The companion-candidates API exposes verified role discovery and one complete
information-only finder result. It cannot recruit, dismiss, train, move, equip,
assign, persist, upload, export, automate input, or control the game.

Kestrel remains bound through `ListenLocalhost(5056)`, and allowed hosts remain
`localhost`, `127.0.0.1`, and `[::1]`. Neither endpoint accepts a save path,
GameData path, raw role definition, weight, formula, sort expression, command,
or mutation option.

## `GET /api/companion-candidates/roles`

Returns the exact verified version-1 role presets. The optional `language`
query is `English` or `Chinese`.

Each role contains:

- stable reference, identity, role version, and evaluation-rule version;
- `Supported` state;
- typed discipline domain and supported type range;
- localized purpose; and
- a localized score limitation.

The two initial roles compare exact saved base aptitude in one selected martial
or life-skill discipline. Discovery never claims universal quality, success
probability, future development, teaching, recruitment, settlement output, or
combat synergy.

An invalid language returns HTTP `400` without reading a save.

## `POST /api/companion-candidates/find`

The JSON request accepts:

```json
{
  "roleIdentity": "MARTIAL_DISCIPLINE_APTITUDE",
  "roleVersion": "1",
  "disciplineDomain": "Martial",
  "disciplineType": 0,
  "filter": "All",
  "firstComparisonCharacterId": null,
  "secondComparisonCharacterId": null,
  "language": "English"
}
```

Role identity is bounded to 160 characters, role version to 40, discipline type
must be non-negative, enums must be defined, and comparison IDs must be absent
together or two different positive values. Invalid transport input returns
HTTP `400` before Application execution.

### Authoritative response

Complete, partial, empty, and invalid-comparison responses use the typed
`CompanionFinderResponse`. An authoritative response exposes:

- finder state and stable failure identity where applicable;
- deterministic semantic finder fingerprint;
- snapshot capture time, save fingerprint, GameData version, profile mapping,
  discipline catalogue, and fingerprint-schema versions;
- catalogue status and installed catalogue source fingerprints;
- selected stable role, role/evaluation versions, typed discipline, localized
  purpose, and score limitation;
- unfiltered counts for total, ranked, tied, ineligible, incomplete,
  unsupported, and conflicting candidates plus visible filtered count;
- every canonical candidate entry and the references visible in the selected
  view;
- optional comparison over two entries from the same result; and
- snapshot, enrichment, shortlist, and candidate diagnostics.

Filters affect only `visibleCandidateReferences`. They do not remove canonical
candidate responses or change scores, ranks, ties, counts, comparison facts, or
the finder fingerprint.

### Candidate evidence

Each candidate response retains:

- stable candidate reference and character ID;
- optional localized display name and location name sourced from the same
  guarded snapshot, with no raw-ID fallback;
- typed ranking and evaluation states, competition rank, and nullable total;
- ordered hard requirements, outcomes, stable reasons, localized explanation,
  and evidence references;
- every score component's stable rule and field identity, discipline, unit,
  direction, normalization range, raw and normalized values, weight,
  contribution, explanation identity, and provenance;
- strongest-contribution, material-limitation, exact-tie, or exclusion
  explanations copied from the Domain shortlist;
- score facts with explicit `Confirmed`, `Missing`, `Incomplete`,
  `Unsupported`, `Stale`, or `Conflicting` evidence state;
- typed values, provenance, unavailable reason, conflict candidates and
  decision, and evidence references;
- location evidence, with a separate list containing only current confirmed
  configured-save location facts;
- catalogue membership and definition states for saved combat-skill IDs, with
  detailed progress explicitly `NotRequestedByApprovedRole`; and
- candidate-owned diagnostics.

Missing, stale, unsupported, and conflicting facts have no current score value.
They are never serialized as zero, false, an empty confirmed collection, a
penalty, or ineligibility unless separate verified evidence proves that state.

### Comparison

A comparison copies the exact two shortlist entries and existing component
outcomes. Each row exposes stable dimension and field identity, both evidence
states and confirmed values when available, plus `FirstAdvantage`,
`SecondAdvantage`, `Equal`, `Unavailable`, `Conflicting`, or `Tradeoff`.

The response mapper does not call the role evaluator, ranking builder, merit
comparer, or source reader. It cannot create a second ranking path.

## HTTP status mapping

| HTTP | Finder condition |
|---:|---|
| `200` | `Complete` or `Empty` |
| `206` | `Partial`, including missing, stale, rebuilding, unsupported, corrupt, or failed optional catalogue enrichment |
| `400` | Invalid transport/Application request, unknown role, unsupported role version, or invalid comparison selection |
| `404` | Configured candidate save unavailable |
| `409` | Save revision changed during snapshot reading |
| `422` | Candidate source version unsupported |
| `499` | Caller cancellation reached the read workflow |
| `500` | Safe read or invariant failure |

Problem responses contain a stable `code` extension and localized safe detail.
They never return exception text, source contents, or a local path. An invalid
comparison uses HTTP `400` with the authoritative finder response retained and
no comparison payload.

Candidate conflict is a successful typed evidence state inside HTTP `200` or
`206`; it is not confused with the HTTP `409` changed-revision state.

## Localization and OpenAPI metadata

English and Traditional Chinese mapping changes only display strings. Stable
role, candidate, requirement, field, diagnostic, evidence, source, outcome,
score, order, rank, tie, and fingerprint values remain identical.

Candidate display name, location name, role purpose, score warning, evidence
explanation, and discipline label are presentation values. Missing display
text is nullable in the API and becomes a localized unavailable label in the
Blazor UI; a character ID or discipline type is never substituted into visible
copy. Display descriptors do not enter profile, shortlist, or finder
fingerprints.

Controller response metadata documents every supported HTTP state. Score
limitation and nullable total properties carry descriptions stating that scores
are role-local evidence, not universal rankings, probabilities, or action
recommendations.

## Verification

Twenty-one focused API cases cover bilingual role discovery, complete mapping,
source versions, score components, facts, conflicts, enrichment, comparison,
partial catalogue evidence, localized candidate display context, language
parity, route shape, validation, every HTTP source state, cancellation,
serialization safety, and public contract types. Architecture checks forbid
local/mutation types and prevent the controller or mapper from evaluating or
ranking.
