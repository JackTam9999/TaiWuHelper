# Loadout comparison contract

## Purpose

Epic 4 compares the current player loadout with the policy winners carried by
the typed backend result. This document fixes the meaning of that comparison
across Domain, API, and Presentation.

The comparison is a read-only projection of one immutable
`CombatLoadoutRecommendation`. It does not generate candidates, rescore a
candidate, infer a missing value, persist a preference, or apply a loadout.

The immutable comparison and API retain all four columns. During the Epic 4
two-option product trial, Presentation projects only Current, Safe, and
Aggressive. Balanced is neither deleted nor recomputed and can be restored
without changing the Domain or API contract.

## Comparison boundary

One comparison owns all of the following identities:

- one opaque logical comparison reference;
- one opaque logical snapshot reference;
- one target identity;
- one catalogue/GameData identity when available;
- one player-baseline provenance summary; and
- exactly four ordered columns: Current, Safe, Balanced, and Aggressive.

All four columns must be built in one operation from the same
`CombatLoadoutRecommendation`. A UI refresh replaces the whole comparison.
Presentation must never merge a current column or policy column from an older
result, a different target, a different observation state, or a separately
requested policy.

The public logical references are opaque identifiers. A save path, GameData
path, screenshot path, process identifier, exception detail, or other
machine-local value is not a comparison identity and must not cross the
public contract.

## Columns and policy status

The columns have this fixed order and meaning:

| Column | Source | Meaning |
|---|---|---|
| Current | `recommendation.Snapshot.Player` | The player baseline used to generate every policy result |
| Safe | the single `Safe` style result | The highest-ranked feasible Safe candidate or a typed diagnostic |
| Balanced | the single `Balanced` style result | The highest-ranked feasible Balanced candidate or a typed diagnostic |
| Aggressive | the single `Aggressive` style result | The highest-ranked feasible Aggressive candidate or a typed diagnostic |

Current is not a recommendation policy and has no recommendation score.
Every policy column has exactly one of these states:

- `Feasible`: a selected candidate, manual plan, capacity, and tactical facts
  are present.
- `Infeasible`: the style produced no selected feasible candidate and exposes
  its non-blank diagnostic.
- `Unavailable`: the style result itself cannot be established and exposes a
  non-blank reason. This state protects future versioned readers and API
  evolution; it is not an empty proposal.

An infeasible or unavailable policy remains a visible column. It has no
fabricated skill cells, zero capacities, empty proposal, or manual-change
count.

## Stable identity and ordering

A comparison skill identity is the tuple `(SkillCategory, SkillId)`. Skill ID
alone is not sufficient for row validation, and a localized name is never an
identity.

The canonical category order is the `SkillCategory` enum order already used by
the Domain:

1. `Neigong` (`內功`);
2. `Attack` (`摧破`);
3. `Agility` (`輕靈`);
4. `Defense` (`護體`); and
5. `Assistance` (`奇竅`).

Within a category, the builder orders rows by numeric stable skill ID. Display
names, language, membership state, score, evidence text, and input collection
order do not affect ordering. Action lists retain authoritative manual-plan
order: Remove, CompleteBreakthrough, Add, ChangeDirection, then Retain, with
the manual plan's category and skill-ID tie breakers.

Changing language may replace display labels, but it cannot change identity,
row order, column order, filter results, or selection.

## Skill membership and actions

### Authoritative source

For a feasible policy, `ManualCombatPlan.LoadoutChanges` is authoritative. The
comparison builder normalizes these changes; it does not run a second set
difference in Presentation.

Each skill cell separates membership from additional instructions:

| Contract fact | Authoritative manual change | Meaning |
|---|---|---|
| `Retained` | `Retain` | Present in Current and the proposal |
| `Added` | `Add` | Absent from Current and present in the proposal |
| `Removed` | `Remove` | Present in Current and absent from the proposal |
| direction action | `ChangeDirection` | The proposal requires the recorded Direct or Reverse direction |
| breakthrough action | `CompleteBreakthrough` | The exact recorded Direct or Reverse breakthrough is proven available now but incomplete |

A feasible policy must have exactly one membership fact for every skill in
the union of Current and that proposal. A direction or breakthrough action is
attached to that membership fact and never replaces it. Examples include:

- `Added` plus `ChangeDirection(Reverse)`;
- `Added` plus `CompleteBreakthrough(Direct)`; and
- `Retained` plus `ChangeDirection(Direct)`.

The model therefore carries an ordered collection of actions. UI badges may
summarize it, but they cannot collapse a composite into one misleading label.
The current manual-plan builder treats breakthrough and direction change as
alternative validation outcomes for one skill; the contract does not invent
both when only one authoritative change exists.

`Unchanged` is a Presentation grouping, not a sixth manual-change kind. A
policy cell is unchanged only when it is `Retained` and has no direction or
breakthrough action. A Current cell states `Present` or `Absent`; it never
claims that the current loadout is a policy proposal.

If membership or an action cannot be proven, the cell is `Unavailable` with a
non-blank reason. It cannot be coerced to Retained, Removed, or Absent.

### Manual-plan parity invariant

For every feasible policy:

1. each `Add`, `Remove`, or `Retain` maps to exactly one matching membership
   fact with the same category and skill ID;
2. each `ChangeDirection` maps to exactly one direction action with the same
   required direction;
3. each `CompleteBreakthrough` maps to exactly one breakthrough action with
   the same required direction;
4. every comparison membership/action traces to one manual change and its
   reason/evidence references; and
5. a missing, duplicate, wrong-category, or untraceable change makes building
   fail instead of yielding a partial comparison.

## Numeric and capacity semantics

All numeric display facts use an explicit available/unavailable wrapper at the
comparison boundary, including values whose current upstream type is always
available. An available zero is a real value. An unavailable value always has
a non-blank reason and displays that reason instead of `0`, `0/0`, or an empty
cell.

| Fact | Current source | Proposed source | Availability rule |
|---|---|---|---|
| used slots | current `SlotBudget.Used` | selected feasible loadout `SlotBudgets.Used` | Preserve `SnapshotValue`; never sum unknown costs in Presentation |
| total capacity | current `SlotBudget.Capacity` | selected feasible loadout `SlotBudgets.Capacity` | Available only when supplied by the validated budget |
| remaining slots | current `SlotBudget.Remaining` | selected feasible loadout `SlotBudgets.Remaining` | Preserve the derived available/unavailable state and reason |
| effective skill cost | `CombatSkillCostCalculator` fact retained by the result/projection | corresponding selected-candidate cost fact | Missing grid cost or mastery remains unavailable unless an evidence-backed fixed cost establishes it |
| category-specific contribution | current validated snapshot fact | selected proposal fact | Never inferred from display capacity |
| 萬用 allocation | `PlayerCombatSnapshot.GenericSlotAllocation` | selected proposal `GenericSlotAllocation` | Expose all four eligible categories and total together or expose one unavailable reason |

Neigong cannot receive 萬用 allocation. A policy requires a manual
allocation change when any eligible category allocation differs from Current.
The comparison reports the complete before/after allocation and the changed
categories; it does not treat the same total with different category
assignments as unchanged.

Capacity is scoped to its column. Presentation must not combine current used
slots with proposed capacity, or one policy's Neigong-derived budget with
another policy's allocation.

## Provenance and evidence

The Current header summarizes provenance for these independently sourced
fields:

- `player.equippedSkills`;
- `player.genericSlotAllocation`;
- `player.slotBudgets`; and
- `player.legendaryBookCostSlots` and assignments when relevant to cost.

A matching `SnapshotFieldSource` identifies current-screen observation
provenance, its UTC observation time, and its opaque evidence reference. When
no observation superseded a field, the field is save-derived. Mixed
provenance is displayed as mixed rather than assigning one label to the whole
baseline.

Stale, rejected, conflicting, or source-precedence conditions remain visible
through existing snapshot warnings and observation results. A rejected
observation does not relabel save-derived facts as observed.

Every normalized manual action retains the existing `RecommendationReason`
code, summary, threat codes, and opaque evidence references. Evidence text may
be progressively disclosed, but the primary status and required action cannot
be hidden inside disclosure.

## Tactical and score semantics

A feasible policy may summarize only facts already retained by its matching
style result:

- verified threat codes addressed by selected counter options;
- analyzed threats not addressed by those verified options;
- unmet or manually confirmed requirements;
- primary active defense and agility choices;
- caveats and unavailable-data explanations;
- authoritative manual-action count; and
- score components, component availability, weights, explanations, and
  evidence references.

Raw effect prose, an unsupported observation, or evidence-only power cannot
be promoted to threat coverage. An unavailable or unsupported threat remains
unresolved.

Safe, Balanced, and Aggressive use different weights. A total score ranks
candidates only within its own policy. The UI may show a policy's components
and weights, but must not:

- label a total as win chance, success rate, confidence, or probability;
- place unlike totals on one universal winner scale;
- highlight the numerically largest cross-policy total as best; or
- use a cross-policy score difference to hide factual risks.

## Difference filtering

`All rows` is the default. `Differences only` applies only to skill rows and
does not change the underlying comparison.

A skill row remains visible in differences-only mode when at least one visible
feasible policy cell is Added, Removed, has a direction action, has a
breakthrough action, or is Unavailable. A row whose visible feasible policy
cells are all unchanged Retained cells may be hidden. An infeasible or
unavailable policy does not cause all Current rows to appear changed.

The filter never hides:

- a changed 萬用 allocation or category capacity summary;
- an infeasible/unavailable policy diagnostic;
- a warning, caveat, unmet condition, or unresolved critical risk;
- the provenance summary; or
- the legend and active-filter announcement.

In narrow mode, "visible policy" means the selected policy. Switching policy
re-evaluates row visibility from the same immutable comparison.

## Responsive and interaction contract

At a comparison-container width of 1280 CSS pixels or more, the trial matrix
shows Current plus Safe and Aggressive. Below 1280 pixels, it shows Current plus
one explicitly selected user-facing policy. Responsive mode changes only visibility and
layout; facts, state vocabulary, and ordering are identical.

The selected policy defaults to the recommendation's requested policy when
it is Safe or Aggressive and feasible, otherwise to the first feasible policy
in Safe, Aggressive order, otherwise Safe so its diagnostic is immediately available.
The selection is session-only UI state.

The rendered structure provides:

- one comparison heading associated with the snapshot/target summary;
- column headings for Current and every visible policy;
- row-group headings for the five categories;
- skill row labels that include localized name and category;
- text for every membership/action state, with icons or color only as
  reinforcement;
- a visible and assistive announcement after policy/filter changes; and
- a link from each feasible policy header to that same policy's existing
  manual checklist and battle plan.

Keyboard order is: comparison heading/summary, warnings, page-level policy
buttons, row filter, category navigation, matrix content, selected tactical
summaries, legend/evidence details, then the selected policy's checklist link.
Native controls keep their expected arrow/space/enter behavior.

After a policy change, focus remains on the selected policy control and an
announcer reports the newly visible column. After a filter change, focus
remains on the filter and the visible-row count is announced. Atomic result
replacement moves focus to the comparison heading only when initiated by the
user; background layout changes do not move focus.

## Lifecycle and failure states

The comparison is request/session presentation state. Selecting a new target,
changing recommendation inputs, applying or clearing a player or target
observation, changing source snapshot, or requesting a refresh rebuilds and
replaces it atomically.

Language changes may remap labels on the same comparison identity. Viewport
changes may alter the visible columns. Neither operation rereads game data or
changes comparison facts.

Before a successful result the surface may be `NoRecommendation`, `Loading`,
or `Failed`. Loading keeps old facts out of the active matrix. Failure shows a
recoverable message and no half-new/half-old columns. Comparison history,
selected policy, filters, and expanded evidence are not persisted across
application sessions.

## Safety boundary

The comparison has no save writer, catalogue writer, database mutation,
process control, screenshot capture, image upload, game hook, input
automation, loadout application, export, battle-outcome feedback, or
recommendation-history persistence.

Every setup action is phrased as an instruction for the player. The persistent
information-only notice states that TaiWu Helper cannot perform the change.
