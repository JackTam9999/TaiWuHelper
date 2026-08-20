# Deterministic tactical loadout search

| Field | Value |
|---|---|
| Status | Implemented |
| Epic | [EPIC-008](../roadmap/epic-008/EPIC.md) |
| Backlog item | [E8-006](../roadmap/epic-008/BACKLOG.md#e8-006--add-deterministic-target-aware-pruning-and-bounded-search) |
| Candidate boundary | [Verified tactical candidate discovery](./TACTICAL-CANDIDATE-DISCOVERY.md) |
| Feasibility authority | [Bounded feasible-loadout generation](./CANDIDATE-LOADOUT-GENERATION.md) |

## Purpose

Search the verified, hard-gate-admitted candidate set without inventing value
from missing evidence or claiming completeness after a bound is reached. The
search is pure and read-only: it selects proposed combinations, rehydrates the
typed execution context for each proposal, and emits only loadouts accepted by
`CombatLoadoutFeasibilityValidator`.

`SearchTacticalLoadouts` reads exactly one `CombatSnapshot`. The same immutable
player atlas is used to project the context, resolve rules, discover candidates,
and search combinations. `TimeProvider` is injected so elapsed-limit behavior
can be verified without making wall-clock duration part of result semantics.

## Admission before pruning

Pruning receives the complete discovery result but can remove only entries in
`Admitted` state. An unsupported, unknown, retained-only, or hard-gate-rejected
entry cannot be made to look like a target-aware pruning decision.

There are exactly two typed pruning rules:

| Rule | Required proof |
|---|---|
| `IrrelevantToTarget` | Evidence that no selected verified target role or transition applies in this exact context |
| `DominatedInSameContext` | One same-context comparison covering role value, timing, requirements, effective cost, conflicts, and execution risk |

Each proof carries the 64-character tactical context semantic fingerprint and
evidence matching both the resolved GameData version and the tactical rule
version. Irrelevance is never inferred from absent evidence. Dominance requires
exactly one evidence item for every comparison dimension. A dominator must be
an admitted, unpruned root. An equal-value proof retains the lexicographically
smaller canonical `<skill-id>:<direction>` identity; strict dominance records
that it is strictly better.

Every removed entry becomes exactly one `TacticalPrunedCandidate` and exactly
one terminal candidate decision. The output retains its rule, stable reason,
evidence, and dominator when applicable.

## Deterministic ordering and traversal

Eligible options are ordered entirely by semantic values:

1. selected target-goal identities;
2. typed tactical purpose;
3. typed transition timing;
4. effective cost; and
5. canonical candidate identity.

Display names, descriptions, localized text, input enumeration order, elapsed
time, and filesystem paths do not participate. Search uses deterministic
include-first depth-first traversal. It never selects both directions of one
skill. Currently equipped unsupported entries marked `RetainedOnly` remain a
fixed retention input; pruning does not relabel them as verified tactical
choices.

For each leaf, the engine builds a `ProposedCombatLoadout` from the selected
candidate directions, proposed active roles, universal-slot allocation, and
legendary cost assignments. The existing feasibility validator is then the
sole authority for ownership, direction, requirements, slot budgets, and final
acceptance. Discovery capacity is an admission gate, not permission to bypass
complete-loadout feasibility.

## Explicit bounds

Every request supplies all four bounds:

| Bound | Valid range | Terminator |
|---|---:|---|
| Eligible options | 1–24 | `OptionLimit` |
| Explored combinations | 1–1,000,000 | `ExplorationLimit` |
| Elapsed search time | greater than zero through 10 minutes | `TimeLimit` |
| Retained feasible results | 1–10,000 | `ResultLimit` |

Cancellation produces `Cancelled`. Coverage records only the first limiting
condition. An option bound may still permit search of its deterministic prefix,
while combination, time, result, and cancellation terminators stop traversal.
Any terminator makes `IsComplete` false. `IsOptimal` is always false because
E8-006 performs feasibility search, not policy scoring or an optimality proof.

The result accounts exactly once for every learned direction: admitted,
rejected, unsupported, irrelevant, or dominated. It also reports role-supported
entries, searched options, explored combinations, feasible results, retained
results, elapsed diagnostics, and the first terminator. A result-limit event
counts the first feasible result that could not be retained, making truncation
observable rather than silently dropping it.

## Semantic fingerprints and caches

The search fingerprint includes the tactical context fingerprint, semantic
candidate decisions, semantic result identities, bounds, counts, and
terminator. It deliberately excludes observed elapsed duration and cache hit
counts. Two otherwise identical complete searches therefore have the same
fingerprint even if one took longer. A time-limited result still includes the
`TimeLimit` terminator and partial semantic counts.

Per-request caches are private to one search:

- candidate projections are keyed by canonical candidate identity and bounded
  by searched options; and
- full feasibility results are keyed by canonical selected-candidate set and
  bounded by explored combinations.

Diagnostics expose hits and misses for both caches. Snapshot acquisition,
learned atlas construction, context projection, and rule resolution happen once
outside traversal; no leaf rereads the save or catalogue.

## Verification

Domain tests cover shuffled inputs, explicit irrelevance, pruning-after-
admission, complete dominance dimensions, deterministic dominance ties, every
bound, elapsed-independent fingerprints, cancellation, no-candidate search,
cache reuse, and a combination that discovery admits but the final feasibility
validator rejects. Application tests prove one snapshot read, no read after
pre-cancellation, and injected elapsed-limit behavior. Architecture tests scan
the search source for filesystem mutation, persistence, network, process, game
control, and unbounded-source capabilities and pin its read-only dependency
registration.
