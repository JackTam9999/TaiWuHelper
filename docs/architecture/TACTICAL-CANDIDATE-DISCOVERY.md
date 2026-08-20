# Verified tactical candidate discovery

| Field | Value |
|---|---|
| Status | Implemented |
| Epic | [EPIC-008](../roadmap/epic-008/EPIC.md) |
| Backlog item | [E8-005](../roadmap/epic-008/BACKLOG.md#e8-005--discover-verified-tactical-candidates-from-all-learned-skills) |
| Context boundary | [Coherent tactical execution context](./TACTICAL-EXECUTION-CONTEXT.md) |
| Rule contract | [Versioned tactical rules](./TACTICAL-COMBAT-RULES.md) |

## Purpose

Discover the complete, deterministic set of tactical skill-direction options
before combination search. Discovery considers the full learned-skill snapshot,
but admits only an exact version-matched role whose direction, raw effect,
typed mechanics, timing, evidence, execution requirements, backlash state,
effective cost, and capacity gates are all usable in the same execution
context.

`DiscoverTacticalCandidates` reads one `CombatSnapshot`, projects its tactical
context and exact rule resolution without rereading, then passes the same
immutable player snapshot, context, and resolution to the Domain discovery
engine. The use case depends only on `ICombatSnapshotReader`; unit tests pin one
port call and zero calls after pre-cancellation.

## Canonical consideration set

Every learned skill produces exactly two stable entries:

- `<skill-id>:DIRECT`; and
- `<skill-id>:REVERSE`.

The result constructor rejects a missing direction, duplicate direction, or a
count other than two per learned skill. The learned atlas may arrive in any
order; entries are ordered by numeric skill ID and direction identity. The
semantic fingerprint uses typed IDs, gates, roles, requirements, costs, and
evidence only.

Direction availability is evaluated separately from role support:

- the active Direct or Reverse direction is available;
- a previously completed opposite direction is available through a verified
  direction change;
- an immediately achievable breakthrough direction is available and marked
  `RequiresBreakthrough`;
- missing direction and breakthrough data is unknown; and
- a known inactive, incomplete, or unreachable direction is infeasible.

Completed, active, and immediately available evidence for the same direction
collapses into one entry. Search can inspect both directional entries, while
the existing `ProposedCombatLoadout` invariant rejects selecting two candidates
with the same skill ID.

## Support, admission, and retention

Support and admission are independent:

| Dimension | States |
|---|---|
| Support | Verified role, unsupported effect, unsupported GameData version |
| Admission | Admitted, retained only, infeasible, unknown context, unsupported |

A currently equipped skill with no target-specific tactical role is
`RetainedOnly`; it is not evidence that the skill has tactical value. The core
E8-002 `TacticalCandidateConsideration` remains the search-facing decision:
admitted maps to `Admitted`, a failed hard gate maps to `Rejected`, and unknown,
retained-only, or unsupported entries map to `Unsupported`.

Unsupported effects remain visible with their exact skill and direction. A
role verified only for the opposite direction reports
`TACTICAL_ROLE_WRONG_DIRECTION`. An unsupported GameData version exposes no
historical role projection and admits nothing.

## Exact role projection

`TacticalCandidateRoleProjection` strips raw effect display content and keeps:

- typed role identity and purpose;
- exact skill ID, direction, and raw effect ID;
- transition timing;
- the complete typed mechanic set;
- exact selected target-goal and transition identities;
- limitation identity; and
- stable evidence identities.

Discovery matches only skill ID plus direction, then separately verifies the
learned snapshot's raw Direct or Reverse effect ID. Localized name, faction,
category label, weapon label, and raw effect text never select a role. Category
is used only after role selection for capacity feasibility.

## Pre-search gates

Every entry receives exactly one result for every gate:

1. learned-skill ownership;
2. mastery status;
3. active, completed, or immediately achievable direction;
4. exact learned raw effect;
5. exact tactical role;
6. role evidence applicability;
7. typed execution requirements;
8. inner-power backlash on active use;
9. effective category cost;
10. category capacity;
11. universal-slot allocation; and
12. current-loadout retention.

Gate states are `Passed`, `Failed`, `Unknown`, `Conflicting`, `Unsupported`, or
`NotApplicable`. Each has a stable reason and evidence identities. A failed
gate makes a verified role infeasible. Unknown or conflicting evidence prevents
unconditional admission and preserves the missing context. Unsupported role or
version evidence cannot become a generic candidate.

Mastery is not invented as a universal prerequisite: verified unmastered state
can pass while producing the unreduced cost. Unknown mastery blocks cost and
admission. Cost uses an explicit proposed legendary assignment, explicitly no
assignment, or the proof that no legendary cost slot exists. A configurable
assignment that was not supplied remains unknown.

Shared counter requirements are evaluated against proposed facts without
turning a missing set into empty. Equipped-passive, active-defense,
active-agility, distance, weapon, unlock, and resource requirements therefore
remain unknown until their exact facts are supplied. Trick counts are not in
the E8-004 verified context and remain unknown. The conditional Reverse `611`
release has no complete typed shared-counter requirement, so it remains
`EXECUTION_REQUIREMENTS_NOT_TYPED` rather than being admitted from its name or
limitation text.

Backlash is checked only for active use. Missing inner power or skill element
is unknown; an exact element match with `BacklashOnUseElement` is infeasible.
Passive roles report the gate as not applicable.

## Bounded diagnostics

`TacticalCandidateDiscoveryLimits` bounds learned-skill input and retains at
most 1–20 example candidate keys per rejection reason. Counts are never
truncated. The result reports:

- learned skill and directional entry counts;
- available, considered, and admitted verified-role coverage;
- unsupported count;
- one count for every admission state;
- aggregated failed, unknown, conflicting, and unsupported reasons; and
- a deterministic semantic SHA-256 fingerprint.

Cancellation is checked before input validation and inside both the learned
skill and direction loops. All loops are bounded by immutable snapshot, rule,
requirement, or result collections.

## Verification

Domain tests cover admitted, retained-only, infeasible, unknown-context,
unsupported-effect, wrong-version, wrong-direction, immediate breakthrough,
mastery, active-role, backlash, conditional-untyped requirement, duplicate
direction, one-direction-per-loadout, enumeration-order/raw-display
independence, bounded examples with exact counts, and pre-cancellation cases.
Application tests prove the context and discovery share exactly one source
read. Architecture tests include candidate files in the mutation, persistence,
network, game-control, and unbounded-enumeration scan and forbid raw atlas
display properties in the discovery result.
