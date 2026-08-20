# Tactical combat Application orchestration

| Field | Value |
|---|---|
| Status | Implemented |
| Epic | [EPIC-008](../roadmap/epic-008/EPIC.md) |
| Backlog item | [E8-009](../roadmap/epic-008/BACKLOG.md#e8-009--orchestrate-one-coherent-tactical-recommendation-result) |
| Domain plan | [Conditional tactical combat plan](./CONDITIONAL-TACTICAL-PLAN.md) |

## One request, snapshot, and result

`IRecommendTacticalCombat` is the Application boundary for one tactical
recommendation. Its request contains stable player and target identities, the
existing recommendation policy enum, an exact replacement set of tactical
observations and proofs, and explicit discovery and search bounds. It does not
accept display labels or infer mechanics from localized text.

Trusted host adapters may omit the expected player identity when the configured
snapshot source is itself the authority. The workflow then derives the player
identity from the one snapshot read and records it in the result identity;
explicit callers still receive a source-failure result when their expected
player does not match.

The orchestrator reads one `CombatSnapshot` and passes that same in-memory
object through every downstream stage:

1. build the existing recommendation and Epic 4 comparison;
2. resolve the versioned target chain;
3. project current and proposed execution context;
4. discover and gate Direct and Reverse candidates;
5. apply proved pruning and bounded combination search;
6. score every retained complete loadout under the requested policy; and
7. compile the selected non-empty loadout into a conditional plan.

The snapshot reader remains the single owner of save, catalogue, atlas, role,
cost, and capacity acquisition. The Application workflow never rereads those
sources for a candidate or plan step. `TacticalRecommendationWorkCounts`
records zero-or-one counts for the snapshot, legacy recommendation,
comparison, rule, context, discovery, search, scoring, and compilation work.
Application tests assert every successful count is exactly one and verify one
reader call.

## Typed outcomes

The result status separates expected absence or incompatibility from faults:

| Status | Meaning |
|---|---|
| `Success` | A complete search produced a compiled non-empty plan. |
| `PartialEvidence` | Required exact evidence is incomplete or conflicting. |
| `UnsupportedChain` | The snapshot GameData version has no verified rule chain. |
| `NoCandidate` | Complete search found no non-empty feasible tactical loadout. |
| `SearchTruncated` | A bound stopped search; any returned plan is only the best retained result found. |
| `SourceFailure` | The requested snapshot could not be read or did not match the player. |
| `EvidenceFailure` | Observation or goal evidence was rejected. |
| `ContextFailure` | Legacy/context projection inputs were inconsistent. |
| `RuleFailure` | Verified rule or discovery invariants failed. |
| `SearchFailure` | Search inputs or invariants failed. |
| `ScoringFailure` | Scoring inputs or invariants failed. |
| `PlanningFailure` | Plan compilation inputs or invariants failed. |
| `UnexpectedFailure` | An unclassified implementation fault was logged. |

Cancellation is never converted into a result. It propagates before the read,
between stages, and through discovery, search, scoring, and compilation, so a
caller cannot receive a mixed partial result. Expected results expose stable
reason identities rather than exception text. Unexpected faults are reported
to the host logger through
`ITacticalCombatRecommendationFaultReporter`; the client receives only the
safe `UNEXPECTED_TACTICAL_RECOMMENDATION_FAILURE` identity.

## Semantic identity and diagnostics

`TacticalCombatRecommendationIdentity` binds the result to:

- source snapshot and exact observation revisions;
- player, target, requested target goals, and resolved transition/role states;
- verified rule-set fingerprint;
- discovery or bounded-search fingerprint;
- declared search bounds and recommendation policy;
- selected-loadout fingerprint, when selected; and
- compiled-plan fingerprint, when compiled.

Search coverage contributes its terminator and explored semantic result, but
not measured elapsed duration. Snapshot capture time, latest observation time,
cache counters, work counts, and elapsed measurements remain diagnostics and
are excluded from the recommendation identity.

## Observation replacement and compatibility

Each request supplies the complete current observation set. Applying a set,
repeating it, replacing it, or clearing it therefore causes one atomic
recalculation of chain, candidates, search, score, plan, comparison, and
identity. Equal snapshot and observation inputs are idempotent; a replacement
changes the semantic identity, and clearing back to the original empty set
restores the original identity.

The orchestrator builds the existing `CombatLoadoutRecommendation` and
`LoadoutComparison` before tactical rule compatibility is evaluated. An
unsupported tactical version therefore preserves the established
recommendation, comparison, and manual-plan consumer contract while returning
an explicit unsupported tactical state. It never substitutes tactical display
text into those legacy contracts.

## Verification

Application tests cover success, partial evidence, unsupported rules, no
candidate, bounded truncation, pre-read and in-search cancellation, atomic
observation apply/repeat/replace/clear behavior, typed source and evidence
failures, safe unexpected-fault reporting, semantic identity, and exact stage
and reader call counts. Infrastructure registration binds the fault reporter
to host logging and exposes the orchestrator through
`IRecommendTacticalCombat`.
