# Companion finder Application architecture

| Field | Value |
|---|---|
| Status | Implemented for E6-008 |
| Epic | [EPIC-006](../roadmap/epic-006/EPIC.md) |
| Backlog item | [E6-008](../roadmap/epic-006/BACKLOG.md#e6-008--orchestrate-one-coherent-companion-finder-result) |
| Snapshot | [Companion candidate snapshot](./COMPANION-CANDIDATE-SNAPSHOT.md) |
| Enrichment | [Companion candidate enrichment](./COMPANION-CANDIDATE-ENRICHMENT.md) |
| Ranking | [Companion role definition and evaluation](./COMPANION-ROLE-EVALUATION.md) |
| Shortlist | [Companion candidate shortlist and comparison](./COMPANION-CANDIDATE-COMPARISON.md) |

## Purpose and boundary

E6-008 composes the completed Epic 6 source and Domain slices into one
Application use case. `FindCompanionCandidates` resolves a verified role,
projects one candidate snapshot, enriches that exact snapshot, evaluates and
ranks its exact profiles, creates one shortlist, and then applies an optional
view filter and comparison selection.

The use case owns orchestration and failure mapping only. It does not implement
a role rule, normalize a fact, calculate a component, break a tie, rebuild a
catalogue, open a single-character progress reader, or translate player-facing
text.

## Request contract

`CompanionFinderRequest` is immutable and accepts only:

- stable role identity and role version text;
- a typed discipline domain and non-negative discipline type;
- one bounded `All`, `Ranked`, `NeedsReview`, or `Ineligible` filter; and
- either no comparison selection or two different positive character IDs.

It has no filesystem path, save selector, raw role definition, weight,
normalization rule, arbitrary expression, sorting policy, game command, or
mutation option. Invalid stable text, enum values, discipline identities,
filters, or comparison shapes return `InvalidRequest` before any source read.
Unknown role identity and unsupported role version remain distinct and also do
not read the save.

## Ordered workflow

`FindCompanionCandidates.ExecuteAsync` performs these steps:

1. validate the bounded request and resolve one exact verified role version;
2. call `ICompanionCandidateSnapshotReader.ReadAsync` once with the path-free
   `Current` request;
3. map a source failure without starting enrichment;
4. enrich the exact returned snapshot through the read-only E6-005 catalogue
   join;
5. pass the enrichment's unchanged profile objects to
   `CompanionRoleShortlistBuilder.EvaluateAndRank` once;
6. create one `CompanionRoleShortlist` from that exact ranking;
7. create a filter view over the exact shortlist entries; and
8. optionally compare two character identities already present in that
   shortlist.

The result constructor independently requires reference identity across the
snapshot, enrichment, ranking, shortlist, view, and optional comparison. A
payload from another source chain cannot be attached.

## Result states

| Application state | Meaning | Authoritative payload? |
|---|---|---:|
| `Complete` | Complete snapshot and enrichment with one or more candidates | Yes |
| `Partial` | Partial snapshot or any non-complete catalogue enrichment state | Yes |
| `Empty` | Complete sources and zero emitted candidates | Yes |
| `InvalidRequest` | Request shape or bounded value is invalid | No |
| `UnknownRole` | Stable role identity is not in the verified catalogue | No |
| `UnsupportedRoleVersion` | Role identity exists but requested version does not | No |
| `SaveUnavailable` | Configured candidate save cannot be read | No |
| `UnsupportedSourceVersion` | Snapshot projection does not support the source | No |
| `ChangedRevision` | The save changed during the bounded snapshot operation | No |
| `ReadFailed` | Snapshot projection failed safely | No |
| `InvalidComparison` | Authoritative result exists but a selected ID is absent | Yes, without a comparison |
| `Failed` | An unexpected invariant-safe orchestration failure occurred | No |

Candidate `Ineligible`, `Incomplete`, `Unsupported`, or `Conflicting` states do
not by themselves make the finder operation fail. They remain successful typed
shortlist evidence. Catalogue missing, stale, rebuilding, unsupported,
corrupt, or failed states produce a `Partial` finder result because the first
approved roles can still rank their independent saved base-qualification facts.
No catalogue definition or detailed progress value is fabricated.

## Coherent source identity

Every authoritative result exposes `CompanionFinderSourceIdentity`, which
binds:

- snapshot capture time and save SHA-256;
- GameData, profile-mapping, discipline-catalogue, and fingerprint-schema
  versions;
- catalogue status and installed catalogue source identity when available;
- stable role identity and role version;
- evaluation-rule version; and
- selected typed discipline identity.

The profile objects used by ranking are the same objects owned by the snapshot
and enrichment. A `ChangedRevision` read has no result payload. A later request
after a save change builds a complete new chain; no candidate from the earlier
revision is retained or refreshed in place.

## Authoritative fingerprint and local views

`CompanionFinderResult.Fingerprint` includes the exact candidate source
versions, enrichment fingerprint, and shortlist fingerprint. It deliberately
excludes:

- snapshot capture time;
- active status filter;
- comparison selection; and
- localized or other Presentation state.

Equivalent immutable facts therefore produce the same semantic finder
fingerprint across reruns. Changing the save revision changes it. Filters and
comparison selections return different local views over the same authoritative
identity and retain original unfiltered counts.

## Cancellation and failure hygiene

The caller's cancellation token reaches the snapshot reader, catalogue source,
catalogue repository, and the per-candidate evaluation loop. Cancellation is
checked between orchestration stages and before each candidate evaluation and
is rethrown rather than converted into a failure result.

Unexpected exceptions map to stable path-free failure identities. Exception
text, local paths, source contents, and proprietary raw data do not enter the
Application result contract or fingerprint.

## Dependency and safety boundary

The workflow depends only on the path-free read-only snapshot port, existing
combat-skill definition and helper catalogue read ports, immutable Domain
contracts, and .NET base class libraries. It contains one snapshot call site,
one ranking-builder call site, one shortlist-factory call site, and one
comparison-builder call site. It has no save writer, catalogue rebuild,
filesystem, process, network, screenshot, persistence, input automation, or
game-control dependency.

Nineteen focused Application test cases cover success, empty, partial snapshot,
missing and stale catalogue, conflict, unknown and unsupported roles, invalid
filter and comparison shapes, absent comparison identities, every snapshot
failure, cancellation, filter/comparison identity stability, and a complete
rebuild after a save revision change. Two architecture tests enforce the
bounded request and single orchestration paths; the Domain suite separately
proves evaluation-loop cancellation.

## E6-009 API integration

E6-009 implements the localhost
[companion candidates API](../api/COMPANION-CANDIDATES.md). It validates
transport input into `CompanionFinderRequest`, calls this use case once, and
maps the immutable result without recomputing a candidate state, score, rank,
explanation, filter, or comparison. Response types omit local paths, raw
catalogue definitions, and mutation-capable handles.
