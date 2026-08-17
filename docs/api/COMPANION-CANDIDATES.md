# Companion candidates API

| Field | Value |
|---|---|
| Status | Implemented for E6-009 and extended by E6-013/E6-014 |
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
- whether the objective requires a discipline selection;
- localized purpose; and
- a localized score limitation.

Two objectives compare exact saved base aptitude in one selected martial or
life-skill discipline. The third compares the complete equal-category breadth
index and reports `requiresDisciplineSelection: false`. Discovery never claims universal quality, success
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
- snapshot capture time and exact complete/partial read status, save
  fingerprint, GameData version, profile mapping, discipline catalogue, and
  fingerprint-schema versions;
- catalogue status and installed catalogue source fingerprints;
- selected stable role, role/evaluation versions, typed discipline, localized
  purpose, and score limitation;
- unfiltered counts for total, exact candidate-universe eligible, ranked,
  tied, ineligible, incomplete, unsupported, and conflicting candidates plus
  visible filtered count;
- every canonical candidate entry and the references visible in the selected
  view;
- optional comparison over two entries from the same result; and
- snapshot, enrichment, shortlist, and candidate diagnostics.

Filters affect only `visibleCandidateReferences`. They do not remove canonical
candidate responses or change scores, ranks, ties, counts, comparison facts, or
the finder fingerprint.

`counts.eligible` is not `ranked + tied` and is not inferred as
`total - ineligible`. It counts only profiles whose typed candidate-universe
state is `Eligible`; role evidence may still leave one of those candidates
incomplete, unsupported, conflicting, or otherwise unranked.

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
- a versioned descriptive capability summary over the six saved base main
  attributes, 14 martial aptitudes, and 16 life-skill aptitudes;
- catalogue membership and definition states for saved combat-skill IDs, with
  detailed progress explicitly `NotRequestedByApprovedRole`; and
- candidate-owned diagnostics.

Missing, stale, unsupported, and conflicting facts have no current score value.
They are never serialized as zero, false, an empty confirmed collection, a
penalty, or ineligibility unless separate verified evidence proves that state.

### Capability summary

`capabilitySummary` is computed once from the candidate's immutable profile.
It exposes formula `EqualCategoryMean`, rule version `1`, a typed overall
state, nullable `breadthIndex`, and three ordered category summaries. Every
category includes its identity, state, confirmed/expected coverage, nullable
average, and every typed component with value/evidence state.

The category averages are arithmetic means of exactly 6 saved base main
attributes, 14 saved base martial aptitudes, and 16 saved base life-skill
aptitudes. A category average exists only when every expected component is
confirmed. The breadth index is the equal-weight mean of the three category
averages and exists only when all three categories are complete. Category
averages and the final index are rounded to two decimals.

This value is a saved-base descriptive overview. It does not change a martial-
or life-skill score, rank, tie, shortlist order, or comparison outcome. When
`COMPREHENSIVE_BASE_CAPABILITY` is explicitly selected, the complete breadth
index is that objective's role-local total and ranking basis. It is not a
success probability, future-potential model, universal suitability claim, or
action recommendation. Missing, incomplete, unsupported, stale, and
conflicting components remain explicit and never become zero.

### Comparison

A comparison copies the exact two shortlist entries and existing component
outcomes. Each row exposes stable dimension and field identity, both evidence
states and confirmed values when available, plus `FirstAdvantage`,
`SecondAdvantage`, `Equal`, `Unavailable`, `Conflicting`, or `Tradeoff`.

The response mapper does not call the role evaluator, ranking builder, merit
comparer, or source reader. It cannot create a second ranking path.

Presentation may place the two candidates' capability summaries in a separate
comparison table, including localized top-three confirmed values. That table
does not add capability facts to the role comparison or alter its outcome.

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

Presentation retains the API's exact snapshot-read, enrichment, and catalogue
statuses. It maps supported combinations to distinct bilingual recovery
guidance for candidate-partial, catalogue missing, installed sources missing,
stale, rebuilding, unsupported, source-read failure, repository failure, and
corrupt states rather than collapsing them into one generic partial notice.

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

Twenty-two focused API cases cover bilingual role discovery, complete mapping,
source versions, score components, facts, conflicts, enrichment, comparison,
exact candidate-universe counts, partial catalogue evidence, localized
candidate display context, language parity, route shape, validation, every
HTTP source state, cancellation,
serialization safety, and public contract types. Architecture checks forbid
local/mutation types and prevent the controller or mapper from evaluating or
ranking.
