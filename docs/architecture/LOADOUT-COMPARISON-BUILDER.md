# Loadout comparison builder

## Purpose

E4-002 adds `CombatLoadoutComparisonBuilder` in the Application layer. It
normalizes one immutable `CombatLoadoutRecommendation` into the Domain
comparison contract without reading a save, catalogue, database, network,
clock, process, screenshot, or UI state.

The builder is a pure static projection. Reusing the same recommendation
produces the same logical references, columns, ordering, membership, actions,
capacity, provenance, and tactical facts.

## Input boundary

The only public operation is:

```csharp
LoadoutComparison Build(CombatLoadoutRecommendation recommendation)
```

The recommendation already retains:

- the exact `CombatSnapshot` used by the calculation;
- target threat analysis;
- bounded candidate-generation output;
- Safe, Balanced, and Aggressive style results;
- each selected feasible candidate and validated slot budget;
- the authoritative manual plan;
- explanation, caveat, condition, score, and evidence facts; and
- applied player/target observation state when present.

The builder invokes only pure Domain calculations needed to project a retained
fact, such as `CombatSkillCostCalculator`. It does not call
`ICombatSnapshotReader`, Infrastructure, localization, persistence, or a
second recommendation/scoring pipeline.

## Build pipeline

The projection runs in this order:

1. Validate that style policies are known and not duplicated.
2. Derive deterministic opaque snapshot and comparison references.
3. Build Current from `recommendation.Snapshot.Player`.
4. Visit Safe, Balanced, and Aggressive in fixed policy order.
5. Emit a feasible policy from its selected manual plan, or a typed
   infeasible/unavailable diagnostic.
6. Normalize Current provenance from snapshot field sources.
7. Construct the immutable `LoadoutComparison`, allowing its Domain
   invariants to reject any inconsistent projection.

No input collection order becomes output order. Categories use Domain enum
order, skill cells use ascending stable skill ID, action kinds use their typed
order, policies use Safe/Balanced/Aggressive, score components use component
kind, and logical-reference collections use ordinal order.

## Deterministic logical references

Machine-local source paths never become public references.

The snapshot reference is a SHA-256 digest over a canonical sequence
containing the source save fingerprint, snapshot capture time, player and
target IDs, ordered field-source identities, and target-observation identity
when present. The comparison reference hashes the snapshot reference,
requested policy, ordered threat codes, column statuses, diagnostics, and
selected candidate stable keys.

These hashes are identities, not security claims or persisted history. They
allow clients to detect atomic comparison replacement without exposing the
save path.

Existing evidence references are retained when they already satisfy the
public logical-reference contract. A legacy evidence reference that resembles
a repository/local path is deterministically replaced by an
`evidence:{SHA-256}` identity. The raw path is not copied into the comparison.

## Current column

Current membership comes directly from the player loadout in the retained
snapshot. The builder:

- emits one `Present` cell for each equipped learned skill;
- verifies that the skill's learned category matches its equipped row;
- calculates effective cost with the existing evidence-aware cost calculator;
- preserves unavailable cost and its reason;
- preserves each current `SlotBudget` used/capacity/remaining state;
- derives category-specific contribution only from equipped Neigong skills;
- copies the complete current 萬用 allocation; and
- emits all five category rows even when they contain no skills.

The builder never reads a newer player state. The Current column is therefore
the exact baseline against which every style in the recommendation was
calculated.

## Policy status

For each policy:

- a present style with a manual plan becomes `Available`;
- a present style without a feasible plan becomes `Infeasible` and retains
  its non-blank manual-plan diagnostic; and
- an absent style becomes `Unavailable` with a typed diagnostic.

An infeasible or unavailable policy has no loadout or tactical summary. The
builder never converts it to five empty rows, zero capacities, or zero manual
actions.

For a feasible style, the builder verifies that the manual plan selects that
style's highest-ranked candidate and that the candidate policy matches the
style policy.

## Manual-plan normalization and parity

`ManualCombatPlan.LoadoutChanges` is the sole source of comparison membership
and actions. The selected proposal is used only to validate parity and supply
the already validated capacity/allocation facts.

For every category, the builder verifies:

1. the manual-plan identity groups exactly equal the union of Current and the
   proposal;
2. each identity has exactly one Add, Remove, or Retain change;
3. Add means absent from Current and present in the proposal;
4. Remove means present in Current and absent from the proposal;
5. Retain means present in both;
6. each direction/breakthrough action belongs to the same membership group;
7. a removed skill has no proposal action;
8. action direction is explicitly Direct or Reverse; and
9. category and learned-skill identity agree.

Any violation stops the build with an invariant error. The builder does not
silently repair or partially display a malformed manual plan.

The comparison mapping is:

| Manual change | Comparison fact | Additional data retained |
|---|---|---|
| `Add` | `Added` membership | category, skill ID, effective cost |
| `Remove` | `Removed` membership | category, skill ID, effective cost |
| `Retain` | `Retained` membership | category, skill ID, effective cost |
| `ChangeDirection` | `DirectionChangeRequired` action | required direction, reason, threat/evidence references |
| `CompleteBreakthrough` | `BreakthroughRequired` action | required direction, reason, threat/evidence references |

Membership and actions are constructed separately, so Add plus direction and
Add plus breakthrough remain composite cells.

## Proposed capacity and 萬用 allocation

The policy column copies category budgets from the selected candidate's
`FeasibleCombatLoadout.SlotBudgets`. These values have already passed the
existing feasibility and Neigong-budget rules. The builder does not calculate
a different capacity.

The complete proposal `GenericSlotAllocation` is retained. Per-category 萬用
contribution comes from that allocation, while category-specific contribution
is the sum of the proposal's equipped Neigong contributions. Neigong itself
always receives zero 萬用 contribution.

This makes a reallocation visible even when the total number of 萬用 slots is
unchanged. Current and proposed values remain scoped to their own column.

## Tactical projection

For each feasible policy, the tactical summary retains:

- manual changes other than Retain as the manual-action count;
- the plan's primary active defense and agility, or unavailable reasons;
- verified threat codes on the selected candidate as covered;
- analyzed threat codes absent from that verified set as unresolved;
- condition identities from structured skill explanations;
- caveat identities from structured recommendation caveats;
- combined reason, option, score, condition, caveat, and threat evidence; and
- the selected candidate's score components, weights, availability,
  explanations, and evidence.

The builder does not promote raw effect prose or observation-only power to
coverage. It does not compare policy totals or create probability language.

## Baseline provenance

The comparison emits Current provenance for equipped skills, 萬用
allocation, slot budgets, and legendary-book assignments.

A matching `SnapshotFieldSource` retains its source, UTC capture time, and
logical evidence reference. A field without an overriding source is labelled
Save and references the snapshot identity. The paired legendary-book slots and
assignments must have identical source metadata; conflicting metadata fails
instead of producing one misleading label.

Rejected or stale observations never add replacing field sources, so the
builder correctly leaves those Current facts save-derived while existing
snapshot warnings remain available to later API/UI mapping.

## Manual-plan parity test matrix

| Scenario | Verified result |
|---|---|
| Current empty, counter selected | Added membership in every feasible policy |
| Added counter requires another verified direction | Added plus DirectionChangeRequired |
| Added unbroken counter can break through now | Added plus BreakthroughRequired |
| Capacity forces current skill replacement | One Remove and one Add with exact manual-plan parity |
| Current loadout is already selected | Retained with no additional action |
| Neigong supplies one 萬用 slot | Proposal reallocation and validated capacity are preserved |
| Missing grid cost/current usage | Effective cost, used, and remaining stay unavailable with reasons |
| No feasible candidate | Three visible diagnostic policy columns; no fake loadouts |
| Current-screen player fields applied | Observed fields and save-derived fields remain distinguishable |
| Repeated identical input | Identical opaque references and flattened structural order |
| Unordered snapshot skill IDs | Current rows are normalized by stable skill ID |
| Legacy path-like evidence | Public comparison contains only path-safe logical references |

## Verification

Focused verification:

```powershell
dotnet test tests\TaiWu.Application.UnitTests\TaiWu.Application.UnitTests.csproj --no-restore
```

The E4-002 suite is `CombatLoadoutComparisonBuilderTests`. Architecture tests
continue to enforce Application-to-Domain dependency direction and the absence
of mutation-capable game dependencies.
