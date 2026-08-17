# Companion candidate shortlist and comparison architecture

| Field | Value |
|---|---|
| Status | Implemented for E6-007 |
| Epic | [EPIC-006](../roadmap/epic-006/EPIC.md) |
| Backlog item | [E6-007](../roadmap/epic-006/BACKLOG.md#e6-007--build-evidence-aware-shortlist-and-candidate-comparison-explanations) |
| Role evaluation | [Companion role definition and evaluation](./COMPANION-ROLE-EVALUATION.md) |
| Product contract | [Companion role evaluation and shortlist contract](./COMPANION-ROLE-EVALUATION-CONTRACT.md) |

## Purpose and boundary

E6-007 turns one immutable E6-006 role ranking into an evidence-aware
shortlist, filter views, and two-candidate comparison. It does not read a save,
resolve a role, evaluate a hard gate, normalize a fact, calculate a component,
or rank a candidate. Every explanation and comparison retains references to
the exact existing ranking, evaluation, gate, component, profile fact, and
evidence objects that support it.

The implementation is presentation-neutral Domain code. Localized character,
role, discipline, and field names remain outside stable identity and are added
only by later API and Presentation mappings.

## Shortlist contract

`CompanionRoleShortlistFactory.Create` accepts one validated
`CompanionRoleRanking` and produces one `CompanionRoleShortlist`. The result
retains:

- the exact source ranking and role definition;
- stable role, role-version, evaluation-rule, and discipline identities through
  that definition;
- the one exact candidate source-version set, including save fingerprint,
  GameData, profile mapping, discipline catalogue, and fingerprint schema;
- every canonical candidate entry in the ranking's original order;
- separate ranked/tied and excluded views over those entries;
- unfiltered `Ranked`, `Tied`, `Ineligible`, `Incomplete`, `Unsupported`,
  `Conflicting`, and total counts;
- profile diagnostics on their owning entries;
- stable role-local-score and information-only diagnostics; and
- a deterministic semantic fingerprint.

The constructor validates that every ranking candidate appears exactly once in
the same canonical order and that every count matches its typed state. Empty
rankings produce a valid empty shortlist with zero counts and the same scope
diagnostics.

## Explanations

Every ranked entry has:

- `StrongestContribution`, referencing the existing component or components
  with the greatest direction-aware contribution;
- `MaterialLimitation`, referencing every existing approved component and
  stating that the role score is limited to that declared scope; and
- `ExactTie` when the candidate shares an exact total and competition rank.

The first verified roles have one component, so the strongest contribution is
the exact saved base qualification. It is not converted into an adjective,
percentage, probability, or universal candidate rating.

Every excluded entry instead has one `Exclusion` explanation. It references
the exact non-passing gate from its immutable evaluation and retains the
evaluation's existing outcome identity. No component, total, rank, penalty, or
substitute reason is created.

Explanation identities are stable, non-localized facts for later Presentation
mapping. None recommends recruitment, training, travel, equipment, assignment,
or another action.

## Location evidence

An entry retains only the E6-000-approved current area and block profile facts
as `LocationEvidence`. A location fact enters `AvailableLocationFacts` only
when it is `Confirmed`, comes from the configured save, and its save revision
and profile-mapping version exactly match the candidate profile.

Incomplete, unsupported, stale, or conflicting location evidence remains typed
evidence with its unavailable reason, but is never exposed as an available
current location. Location does not affect an explanation, score, tie, order,
or fingerprint beyond the already-owned profile fact.

## Filter views

`CompanionRoleShortlistFilterer` creates immutable views for:

| Filter | Included states |
|---|---|
| `All` | Every shortlist entry |
| `Ranked` | `Ranked` and `Tied` |
| `NeedsReview` | `Incomplete`, `Unsupported`, and `Conflicting` |
| `Ineligible` | `Ineligible` |

A view retains the exact source shortlist, original unfiltered counts, and
references to the original entry objects in canonical relative order. It does
not have a new score or ranking fingerprint. A localized name query remains a
Presentation concern and may further hide rows without changing this source.

## Candidate comparison

`CompanionRoleComparisonBuilder.Compare` accepts two different stable
character identities from the same shortlist. Unknown or duplicate selections
fail before a comparison is constructed. The comparison retains the source
shortlist and exact source entries; selecting candidates never evaluates or
ranks them again.

One ordered row is created for every role score dimension. A row retains:

- the exact role dimension and typed profile-field identity;
- each candidate's exact profile fact and evidence references when present;
- each side's `Confirmed`, `Missing`, `Incomplete`, `Unsupported`, `Stale`, or
  `Conflicting` evidence state;
- a current numeric value only for confirmed evidence; and
- `FirstAdvantage`, `SecondAdvantage`, `Equal`, `Unavailable`, or
  `Conflicting` relative outcome.

For two ranked candidates, the row compares the existing direction-aware
component contributions. It does not repeat normalization, weighting, total
arithmetic, or ranking. Equal contributions remain `Equal`. If either
evaluation is unranked or required row evidence is unavailable, the row and
comparison are `Unavailable`; a conflict remains distinctly `Conflicting`.

The aggregate outcome precedence is conflict, unavailable, genuine tradeoff,
one-sided advantage, then equality. `Tradeoff` is reserved for a future
verified multi-dimension role whose existing rows contain advantages for both
candidates. The version-1 single-dimension roles cannot fabricate a tradeoff.

## Deterministic identity

The shortlist fingerprint includes the ranking fingerprint, validated counts,
canonical entry explanations and location evidence, and semantic diagnostics.
The comparison fingerprint includes the shortlist fingerprint, ordered source
entry identities, aggregate outcome, and ordered row facts and outcomes.

Equivalent rankings and selections therefore produce equivalent fingerprints
regardless of input enumeration order. Filters and localized display values do
not alter either identity. No fingerprint contains a local path, exception
text, capture timestamp, or localized name.

## Dependency and safety boundary

The implementation uses only `TaiWu.Domain.CompanionRoles`, the immutable
candidate-profile contracts, and .NET base class libraries. It has no
Application, Infrastructure, Presentation, filesystem, network, persistence,
process, archive, GameData, or mutation dependency.

Seventeen focused Domain test cases cover complete counts, ties, all exclusions,
component-backed explanations, exact gate reasons, advantages, equality,
direction-aware contribution reuse, missing and conflicting evidence, every
status filter, confirmed and unavailable location evidence, invalid selections,
equivalent reruns, and an empty shortlist.

## E6-008 handoff

The Application workflow must build one ranking and one shortlist from the
same immutable candidate snapshot and return their exact source/version
identity. Requests may select a view and comparison, but those operations must
continue to reference the authoritative shortlist and may not re-read a save,
re-evaluate a profile, or change ranking identity.
