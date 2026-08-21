# E8-F05: Package-aware tactical search

| Field | Value |
|---|---|
| Status | Complete — coherent packages are searched and scored explicitly |
| Backlog item | [E8-F05](../roadmap/epic-008/BACKLOG.md#e8-f05--add-package-aware-full-loadout-discovery-search-and-scoring) |
| Inspection date | 2026-08-21 |
| Runtime GameData | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` |
| Rule fingerprint | `64051C1234CECDFDCE070134FDA0380826154D16C1F171B52B6F7FE1C64ECD5D` |
| Context schema | `TACTICAL_EXECUTION_CONTEXT_V3` |
| Sanitized record | [E8-F05 metadata](./evidence/E8-F05-package-aware-tactical-search-metadata.json) |

## Decision

Every hard-gate-admitted exact role now enters the bounded loadout search
directly from tactical discovery. Search is no longer limited to a separate
legacy option whitelist. The existing loadout feasibility validator remains
the final authority for direction, cost, category capacity, universal slots,
weapon, trick, distance, resource, activation and backlash requirements.

Manual execution conditions are now explicit context facts. A stable code such
as `USABLE_BLADE_TRICKS` or `USABLE_WHISK_TRICKS` is satisfied only when the
current observation or proposed requirement context explicitly confirms it.
An absent code remains unknown; it never becomes false evidence or an
optimistic default.

## Reverse 604 recovery package

Selecting exact current Reverse `604` creates one typed recovery contract:

- at least one admitted exact Reverse recovery role resolves three sequential
  cast steps; the same feasible cast may be repeated because the mechanic
  removes one lock layer per cast;
- every step retains its candidate identity, effective loadout slot cost and
  typed weapon, resource and manual execution requirements; and
- no feasible recovery role produces
  `REVERSE_604_RECOVERY_CASTS_UNRESOLVED` with zero invented steps.

Complete and not-applicable packages rank before an unresolved Reverse `604`
branch. A numeric score therefore cannot relabel or outrank away the package
constraint. Relevance and dominance proofs are also forbidden from pruning an
admitted recovery member while exact current Reverse `604` is admitted; the
stable diagnostic is
`RECOVERY_PACKAGE_MEMBER_CANNOT_BE_PRUNED_WHILE_REVERSE_604_IS_ADMITTED`.

## Active-role rotations

Exact current switch-only defense and agility roles may enter search even when
another skill is the proposed active role. Each feasible result records:

- one primary active defense and zero or more equipped switch backups;
- one primary active agility skill and zero or more equipped switch backups;
  and
- an unresolved rotation when equipped choices exist but no primary is
  confirmed.

Only the confirmed primary from each rotation enters causal, timing, layering
and finish scoring. Backups still consume capacity and retain all non-activation
execution requirements, but their active-only effects are not treated as
simultaneous. This preserves real switch options without double-counting
protection.

## Search and scoring invariants

The search still uses deterministic include-first traversal, canonical option
ordering, per-request projection and feasibility caches, cancellation, and
explicit option, exploration, elapsed-time and result bounds. Truncation keeps
its first typed terminator and never claims optimality.

Package semantics are included in the search fingerprint. Scoring continues to
reward distinct causal transitions, supported timing windows, observable
execution, documented layering and typed finish evidence. Recovery scoring now
also exposes each resolved cast or the unresolved branch. Repeated resource
requirements use evidence-qualified identities, preventing cross-skill input
collisions without inventing damage or duplicate protection.

## Verification

`CurrentTacticalLoadoutPackageTests` proves five invariants rather than one
hard-coded winner:

- Reverse `604` plus executable Reverse `686` yields exactly three audited cast
  steps;
- missing whisk-trick confirmation rejects `686` and leaves an explicit
  unresolved branch;
- two equipped defenses and two equipped agility skills form rotations, while
  backups do not increase the score as if simultaneously active;
- coupled recovery candidates cannot be pruned; and
- manual conditions pass only through an explicit stable confirmation code.

The Release build completed with zero warnings and errors. The full suite
passed 1,610 of 1,634 tests with 24 expected guarded-local skips and no
failures. Domain, Application and Architecture projects passed 676, 232 and
114 tests respectively. No save, GameData, helper cache or runtime state was
modified.

