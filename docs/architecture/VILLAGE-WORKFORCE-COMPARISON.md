# Village workforce shortlist and comparison

| Field | Value |
|---|---|
| Status | Implemented — canonical shortlist, comparison, and manual review plan |
| Epic | [EPIC-007](../roadmap/epic-007/EPIC.md) |
| Backlog item | [E7-006](../roadmap/epic-007/BACKLOG.md#e7-006--build-the-worker-shortlist-comparison-and-manual-checklist) |
| Evaluator input | [Village workforce evaluator](./VILLAGE-WORKFORCE-EVALUATOR.md) |
| Product contract | [Village workforce evaluation contract](./VILLAGE-WORKFORCE-EVALUATION-CONTRACT.md) |

## Purpose

Turn one immutable evaluation set into the canonical result consumed by later
Application, API and UI slices. The result retains the selected target's
current worker, exact competition ranks, all unranked state groups, shared
limitations, typed comparison outcomes, immutable filter views, and a factual
manual-review plan.

It does not select a worker automatically, write completion state, persist a
proposal or issue a command to the game.

## Canonical shortlist

`VillageWorkforceShortlist` owns one exact
`VillageWorkforceEvaluationSet`. Its canonical groups are:

- comparable `Ranked` and `Tied` evaluations with competition rank;
- `CurrentOnly`;
- `Ineligible`;
- `Incomplete`;
- `Unsupported`; and
- `Conflicting`.

`WorkforceShortlistCounts` preserves the unfiltered total and each state count.
Comparable count is ranked plus tied. The current evaluation is referenced
separately without moving it ahead of a better alternative or granting it a
score bonus.

Comparable evaluations sort by exact total descending. Stable worker identity
sorts equal values for rendering only. Competition ranks skip positions after
a tie:

```text
80, 80, 60 -> 1, 1, 3
```

The evaluator already marks the first two results as semantic ties. The
shortlist never changes that state or turns worker identity into merit.

## Vacancy boundary

The first vertical has the single typed state `NoExplicitVacancy`. The snapshot
contains occupied shop-manager positions only, and the source evidence did not
establish a vacant-slot contract. The shortlist is therefore an occupied
replacement comparison and never converts list capacity, zero, or a missing
entry into a vacancy.

An empty comparable group means no rankable alternative evidence is available.
It does not imply an empty shop position.

## Shared limitations

The shortlist exposes the resolved rule's immutable limitation collection once
at result level:

- saved base qualification only;
- no efficiency, output or revenue calculation; and
- occupied shop replacement only.

Worker evaluations do not duplicate these shared limitations. Filters and
comparisons reference the same shortlist result fingerprint.

## Relative comparison

`VillageWorkforceShortlist.Compare` resolves both worker identities from the
same evaluation set. This guarantees one snapshot fingerprint, target,
objective, semantic rule version and exact rule definition.

The comparison outcome is one of:

| Outcome | Meaning |
|---|---|
| `Higher` | Both values are rankable and the first exact value is higher |
| `Lower` | Both values are rankable and the first exact value is lower |
| `Equal` | Both compatible rankable values are exactly equal |
| `Unavailable` | At least one required value is incomplete or unsupported |
| `Incompatible` | Component identity or unit differs, so subtraction would be invalid |
| `NotComparable` | A worker is ineligible/current-only or required evidence conflicts |

Different result identities and the same worker on both sides are rejected.
No result uses a percentage, tolerance or current-worker preference.

## Immutable filters

`ApplyFilter` returns a `VillageWorkforceShortlistView`:

| Filter | Visible states |
|---|---|
| `All` | Comparable, current-only, needs-review, then ineligible groups |
| `Comparable` | Ranked and tied only |
| `NeedsReview` | Incomplete, unsupported and conflicting |
| `Ineligible` | Ineligible only |

Every view retains the canonical shortlist fingerprint and the exact same
unfiltered count object. Applying a filter cannot change an evaluation,
component, score, rank, tie, current worker, limitation or comparison outcome.

## Manual review plan

A plan may be created only for a rankable worker other than the current worker.
It contains:

- the immutable result identity;
- current worker identity;
- a typed `ProposedShopManagerAssignment` with `ProposedHelper` origin; and
- five stable checklist facts.

The checklist is semantic data, not interactive task state:

| Item | Category |
|---|---|
| Target identity must match | Fact to verify |
| Reassignment availability must be verified | Prerequisite |
| Qualification and evidence must be reviewed | Fact to verify |
| Efficiency was not calculated | Caution |
| No action was sent to the game | Caution |

`WorkforceManualChecklistItem` has no Boolean, checked flag, timestamp, owner,
completion method or persistence identity. Each item is phrased as a fact or
boundary rather than a game command. The plan fingerprint covers current and
proposed identities plus the complete canonical checklist.

## Invariants and determinism

The evaluation set must contain its selected current worker. Shortlist
construction therefore cannot silently produce a missing-current result.
Rank entries require a rankable exact value. Manual plans reject current,
unrankable and out-of-result workers.

The shortlist fingerprint contains the evaluation-set fingerprint, all state
counts, vacancy boundary, competition ranks, evaluation fingerprints and
shared limitations. Identical canonical inputs produce identical shortlist
and plan fingerprints.

## Verification

Tests cover no comparable alternative, one worker, current-best,
alternative-best, exact ties, all state groups, incomplete evidence,
no-explicit-vacancy, missing-current rejection, filter immutability, every
comparison outcome and manual-plan shape.

```powershell
dotnet test tests\TaiWu.Domain.UnitTests\TaiWu.Domain.UnitTests.csproj -c Release --no-build -- --filter-class TaiWu.Domain.UnitTests.VillageWorkforce.VillageWorkforceShortlistTests
```
