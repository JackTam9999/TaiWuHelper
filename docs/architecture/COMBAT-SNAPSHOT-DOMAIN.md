# Combat snapshot Domain model

## Purpose

The combat snapshot is the immutable Domain input for feasibility, threat
analysis, recommendation, and presentation. It replaces parsing of the legacy
line-oriented diagnostic report in later Milestone 1 slices.

The model contains no `GameData`, Infrastructure, ASP.NET Core, persistence, or
process-control type. Infrastructure will map locally read game values into
this vocabulary without allowing mutation-capable objects to cross the
boundary.

## Aggregate

| Type | Responsibility |
|---|---|
| `CombatSnapshot` | Root containing metadata, one player, one target, and warnings |
| `CombatSnapshotMetadata` | Save path, SHA-256, capture time, save modified time, and GameData version |
| `PlayerCombatSnapshot` | Learned skills, equipped loadout, equipment, slot budgets, generic allocation, owned legendary-book cost slots, and current assignments |
| `TargetCombatSnapshot` | Identity, age, features, learned skills, optionally available equipped loadout, and equipment |
| `CharacterFeatureSnapshot` | Target feature ID, configured display name, and level |
| `CombatSkillSnapshot` | Skill identity, category, actual grid cost, mastery, practice direction, slot contribution, and direct/reverse effect IDs |
| `CombatLoadoutSnapshot` | Equipped skill IDs separated into all five skill categories |

All collection inputs are copied into `ImmutableArray<T>`. Later caller
mutation cannot change a constructed snapshot.

## Domain values

### Practice direction

`PracticeDirection` preserves the verified source semantics as named values:

| Value | Meaning |
|---:|---|
| `-1` | `Reverse` |
| `0` | `Neutral`; neither direction-specific effect is active |
| `1` | `Direct` |

### Slot categories

`SkillCategory` explicitly represents `Neigong`, `Attack`, `Agility`,
`Defense`, and `Assistance`. `SlotBudgetSet` requires exactly one budget for
every category.

`GenericSlotAllocation` keeps a single total and separate allocation for the
four eligible outer categories. It rejects negative values and allocations
whose sum exceeds the total. Generic slots cannot be allocated to Neigong.

`SlotBudget` always retains the saved category capacity. Its used and remaining
values are explicit `SnapshotValue<int>` instances because standalone GameData
cost calculation can require an unavailable combat-effect runtime. The adapter
must leave those values unavailable instead of calling that runtime or guessing
from configured cost.

`SkillSlotContribution` permits negative category-specific adjustments because
locally verified inner-power configuration can reduce one category while
increasing another. Its generic contribution cannot be negative.

### Unavailable values

`SnapshotValue<T>` has two constructible states:

- `Available(value)`.
- `Unavailable(reason)`.

Unavailable values never silently become `0`, `false`, an empty string, or an
empty loadout. Reading `Value` while unavailable throws, forcing callers to
branch on `IsAvailable` and preserve the reason.

This is required for the golden target because the current disk save does not
contain its equipped skill loadout.

### Evidence source

`SnapshotDataSource` distinguishes save data, local game configuration, and a
current-screen observation. `LegendaryBookCostRule` requires an evidence
reference and source. `LegendaryBookCostSlot` gives an owned effect a stable
identity, while `LegendaryBookCostAssignment` separately records the current
or proposed selected skill and its provenance.

## Construction invariants

- Character IDs and skill IDs cannot be invalid.
- Available grid costs must be greater than zero.
- Available effect and equipment IDs cannot be negative.
- Available slot usage cannot be negative or exceed capacity.
- Every slot category is present exactly once.
- Generic slots cannot be allocated more than once.
- A skill cannot appear twice in one equipped loadout.
- Learned skills and equipment slots cannot be duplicated.
- Legendary-book fixed costs come only from named, evidence-backed rules.
- A current assignment must reference an owned slot and a learned skill in the
  same category.
- A slot can have at most one current assignment, and a skill can have at most
  one fixed-cost assignment.
- Proposed assignments cannot be stored in a current player snapshot.
- Snapshot SHA-256 values contain exactly 64 hexadecimal characters.
- Missing data always carries a non-blank reason.

These are construction invariants only. Later Domain services remain
responsible for ownership, effect availability, proposed-loadout feasibility,
and combat activation requirements.

## Application read port

`ICombatSnapshotReader` is the Application boundary for obtaining the
aggregate. Its only operation is:

```csharp
Task<CombatSnapshot> ReadAsync(
    CombatSnapshotReadRequest request,
    CancellationToken cancellationToken = default);
```

The request requires a save-file path and target character ID. The return type
contains source metadata and warnings as part of the immutable aggregate.

The port inherits `IReadOnlyGameDataSource`, uses query-only naming, supports
cancellation, and exposes no `GameData` type. The legacy line-report reader
remains a separate diagnostic port.

## Infrastructure adapter

`TaiwuCombatSnapshotReader` maps one loaded archive directly into the immutable
Domain aggregate. The legacy diagnostic report uses a separate projector over
the same `TaiwuArchiveReadSession`; structured consumers never parse diagnostic
lines.

The shared archive session:

- Serializes access to GameData's process-wide static domains.
- Initializes configuration once.
- Clears monitored one-shot handlers before every archive load.
- Captures the save length, SHA-256, and modified time before loading.
- Discards a result if the same fingerprint is not present after projection.

The adapter maps configuration `GridCost`, confirmed mastery, activation-state
direction, and configured category/generic grid contributions independently.
It deliberately does not call `Character.GetCombatSkillGridCost`, because that
method enters `SpecialEffectDomain.ModifyData` and requires a live combat
runtime. Consequently, saved category capacity remains available while used
and remaining capacity are unavailable until verified cost rules are applied.

## Current-screen observations

`CombatSnapshotReadRequest` can carry one immutable
`PlayerLoadoutObservation`. It is helper-owned input containing:

- Observation time and an evidence reference.
- Equipped skill IDs grouped by category.
- Generic-slot allocation.
- Optional slot budgets read directly from the displayed screen.
- Optional legendary-book cost slots and current assignments, supplied
  together.

`CombatSnapshotObservationMerger.Merge` returns a new aggregate and never
changes the disk-derived snapshot. Before merging, every observed skill must be
learned by the player and reported under its configured category. An
observation whose timestamp is not newer than the save modified time is not
used and produces a warning. If the save timestamp is unavailable, explicit
current-screen source precedence is used with a warning.

Every replaced aggregate field receives a `SnapshotFieldSource` entry with a
stable field path, `CurrentScreenObservation` source, observation time, and
evidence reference. The current paths are:

- `player.equippedSkills`
- `player.genericSlotAllocation`
- `player.slotBudgets` when displayed budgets were reported
- `player.legendaryBookCostSlots` and
  `player.legendaryBookCostAssignments` when book state was reported

Observation data exists only in Domain/Application memory and the returned
snapshot. The merge operation has no persistence, file, process, input, or
game-control dependency.

## Effective skill cost

`CombatSkillCostCalculator` is a pure Domain service. It returns a
`CombatSkillCostBreakdown` containing configured base cost, confirmed mastery,
the applied evidence-backed legendary-book assignment, the derived reduction,
and effective cost.

The calculation order is:

1. Use configured `GridCost` as the base.
2. Reduce it by one only when mastery is available and confirmed.
3. Keep the mastery-adjusted result at or above one.
4. If one confirmed `收置` assignment applies, set the occupied cost to its
   evidence-backed fixed cost of one.

`收置` is modelled as a fixed cost rather than an additive reduction. Its
reported reduction is derived from the mastery-adjusted cost. Without a
`收置` assignment, a missing `GridCost` or unknown mastery leaves effective
cost unavailable. With a verified assignment, the exact cost of one remains
available while the derived reduction stays unavailable. Multiple fixed-cost
assignments for one skill are rejected rather than stacked.

The skill shown as `生效功法` is a replaceable assignment, not part of the
effect definition. `LegendaryBookCostAssignment.ProposeForSkill` returns a new
`Proposed` helper value with a new proposal reference. The calculator accepts
it only when the referenced slot is owned and the learned skill category
matches. This is an in-memory recommendation calculation only; it has no game,
save, process, input, or persistence dependency.

Owning a legendary book does not itself change any skill cost. An unassigned
`收置` slot is represented explicitly by an owned slot with no assignment and
leaves cost unchanged. Effects from books outside the player's verified owned
set remain unknown; they are never guessed or treated as available.

The separate `大盈` and `大成` category/generic-grid trade-offs are deliberately
not cost modifiers. They belong to slot-budget calculation in M1-008. The
verified screenshots and their hashes are recorded in
`docs/scenarios/M1-007-effective-skill-cost-evidence.md`.

## Slot-budget calculation

`CombatSlotBudgetCalculator` is a pure Domain service for the current immutable
player snapshot. It uses the verified empty-loadout capacities:

- Neigong: 6
- Attack, agility, defense, and assistance: 2 each

Only equipped Neigong skills contribute their configured
`SkillSlotContribution` values to the four outer category capacities.
Unequipped Neigong and non-Neigong contributions do not affect capacity.
`GenericSlotAllocation` then adds each assigned generic slot to exactly one
outer category; generic slots cannot be assigned to Neigong or allocated more
than their available total.

Used slots are the sum of `CombatSkillCostCalculator` results for the equipped
skills in that category. This composes configured cost, mastery, and verified
current `收置` assignments without duplicating cost rules. If any equipped
skill has unavailable effective cost, the category's used and remaining values
remain explicitly unavailable while its capacity stays available.

For a valid loadout the service returns a complete `SlotBudgetSet`, including
used, capacity, and remaining values for all five categories. Unknown skills,
wrong-category placement, negative derived capacity, and used capacity above
the calculated limit produce Domain validation errors.

`大盈` and `大成` remain evidence-backed contribution transformations rather
than occupied-cost rules. The budget calculator consumes the resulting
`SkillSlotContribution`; it does not infer either transformation unless an
upstream snapshot source can prove the current assignment.

## Combat-skill candidate eligibility

`CombatSkillCandidateValidator` is a pure Domain service that checks one
recommendation candidate against the immutable player snapshot. A candidate
identifies a skill and may declare that its recommendation depends on mastery
and/or a direction-specific effect.

The validator applies these rules:

- The skill ID must exist in `PlayerCombatSnapshot.LearnedSkills`.
- A mastery-dependent candidate requires an available, confirmed `Mastered`
  value. Skills that do not depend on mastery are not rejected merely because
  they are unmastered.
- A direction-independent candidate may use a Neutral skill.
- A strict Direct requirement needs current Direct practice and an available
  `DirectEffectId`.
- A strict Reverse requirement needs current Reverse practice and an available
  `ReverseEffectId`.
- Neutral means the direct and reverse counts are tied; it activates neither
  direction-specific effect.
- Unknown direction, an opposite direction, and unavailable effect data are
  separate rejection reasons.

A candidate may opt in to `AllowDirectionChange` for a helper-side proposal.
In that mode, an available current Neutral or opposite direction can be
accepted for a Direct or Reverse recommendation, but the requested
direction-specific effect must still be available. Unknown current direction
and a requested Neutral effect remain invalid.

An accepted mismatch is exposed as `RequiredDirectionChange` in the validation
result. This is manual recommendation data for later presentation, not a game
operation.

Expected ineligibility is returned as
`CombatSkillCandidateRejection`, not thrown as control flow. Each rejection has
a stable `CombatSkillCandidateRejectionCode` and a non-blank explanation.
`CombatSkillCandidateValidationResult` returns all independently detectable
reasons so later feasibility and UI slices can explain every failed condition.
Unknown skill identity is the only check that prevents further skill-state
validation because there is no learned snapshot to inspect.

The validator never changes practice direction, mastery, a save, or the game.
It reports current eligibility for a recommendation that the player may carry
out manually.

## Activation and combat requirements

M1-010 represents combat conditions as evidence-backed `CombatRequirement`
types:

- `WeaponRequirement`
- `TrickRequirement`
- `RangeRequirement`
- `ResourceRequirement` for Neili, stance, or breath
- `WeaponUnlockRequirement`
- `SkillActivationRequirement` for an equipped passive, active defense, or
  active agility skill

Every requirement records `Hard` or `Conditional` criticality and a non-blank
evidence reference. `CombatRequirementContext` is immutable and contains the
current equipped and unlocked weapon types, trick counts, distance, resources,
equipped skills, and at most one active defense and one active agility skill.
An active skill must also be equipped, and one skill cannot occupy both active
roles.

`CombatRequirementEvaluator` returns one `CombatRequirementEvaluation` per
input requirement:

- `Satisfied` means the current context meets it.
- `Unsatisfied` means the current facts disprove it.
- `Unknown` means required context, such as current distance or resource
  amount, is unavailable.

Unsatisfied or unknown hard requirements appear in `Rejections` and make the
result ineligible. Unsatisfied or unknown conditional requirements appear in
`Warnings` without being reported as satisfied. All independently detectable
results are retained.

The model supports the golden anti-magic scenario without hard-coding game
configuration: equipped reverse 老君 as a passive, actively running reverse
萬花, blade unlock and trick prerequisites for 鬼庖丁, and range conditions for
三部/長目 can all be expressed with their local evidence. M1-012 remains
responsible for mapping verified GameData effect IDs and source text into these
generic Domain requirements.

Requirement evaluation is descriptive and read-only. It cannot equip a weapon,
spend resources, activate a skill, unlock an item, change distance, or control
the game.

## Proposed-loadout feasibility

M1-011 makes the complete proposed loadout an explicit Domain boundary.
`ProposedCombatLoadout` contains the skill selection, generic-slot allocation,
candidate eligibility specifications, evidence-backed requirements, and the
context in which those requirements are evaluated.

`CombatLoadoutFeasibilityValidator` composes the earlier Domain rules and
reports all independently detectable failures:

- every selected skill must have exactly one candidate specification;
- candidate ownership, mastery, direction, and effect availability must pass;
- the requirement context must describe exactly the proposed equipped skills;
- every hard combat requirement must be satisfied;
- the generic-slot total must equal persistent slots plus generic slots
  contributed by the proposed Neigong selection; and
- effective slot usage must be available and fit every category capacity.

Expected invalidity is returned as `CombatLoadoutFeasibilityFailure`, with a
stable code and explanation, rather than used as exception-driven control
flow. Conditional requirement failures remain warnings in the nested
requirement result and do not make an otherwise valid proposal infeasible.

The validator returns `FeasibleCombatLoadout` only when there are no failures
and a complete `SlotBudgetSet` is available. Its constructor is internal, so a
later scoring service can require this accepted-only type instead of accepting
an unchecked proposal. This prevents an invalid loadout from entering scoring
by construction.

Feasibility validation remains descriptive and read-only. It cannot equip the
proposed loadout, reassign slots in the game, write a save, or control the game
runtime.
