# EPIC-004: Side-by-side loadout comparison and change planning

| Field | Value |
|---|---|
| Status | In progress |
| Milestone | 4 |
| Target release | TBD |
| Last updated | 2026-08-08 |

## Summary

Turn the existing current loadout, Safe, Balanced, and Aggressive recommendation
results into one deterministic comparison surface. The player should be able to
see which skills are retained, added, removed, redirected, or blocked behind a
breakthrough; how category capacity and 萬用 allocation change; which threats
each proposal covers; and which requirements or evidence gaps remain.

Epic 4 is an explanation and planning feature. It derives comparison facts from
the same immutable combat snapshot and recommendation results delivered by
[Epic 1](../epic-001/EPIC.md), the stable skill identities delivered by
[Epic 2](../epic-002/EPIC.md), and the evidence-aware observations delivered by
[Epic 3](../epic-003/EPIC.md). It does not generate a second recommendation,
apply a loadout, or control the game.

## Context

The current recommendation page already returns three policy winners with
categorized skills, capacity, 萬用 allocation, manual loadout changes, threat
links, conditions, caveats, and evidence references. The manual plan can say
what to add or remove, but the player must still move between sections and
mentally reconstruct how the current setup differs from each policy.

The remaining gap is presentation with a trustworthy comparison contract. A
comparison assembled ad hoc in the browser could disagree with the Domain
manual plan, treat unavailable values as zero, compare recommendations built
from different evidence, or imply that a higher raw score is a win
probability. Epic 4 prevents those failures by deriving every column from one
recommendation result and giving each comparison state explicit semantics.

## Primary user story

> As a player preparing for a selected target, I want to compare my current
> loadout with the Safe, Balanced, and Aggressive recommendations in one view so
> I can choose a policy and follow the exact manual changes without overlooking
> capacity, direction, breakthrough, evidence, or unresolved-risk differences.

## Supporting user stories

- As a player, I can compare all five combat-skill categories without losing
  the category identity of a skill.
- As a player, I can distinguish retained, added, removed, direction-changed,
  and breakthrough-required skills.
- As a player, I can see current and proposed category capacity, used slots,
  remaining slots, and 萬用 allocation.
- As a player, I can tell whether the current baseline came from the save or a
  newer current-screen observation.
- As a player, I can compare threat coverage and unresolved risks without
  reading a score as a probability of winning.
- As a player, I can hide unchanged rows and focus on required manual changes.
- As a keyboard or mobile user, I can compare the current loadout with one
  selected policy without navigating an unusably wide table.
- As an API consumer, I receive the same typed comparison semantics as the UI.

## Goals

1. Define an immutable, presentation-neutral comparison vocabulary.
2. Compare the current player loadout with all feasible policy winners from a
   single recommendation result.
3. Reuse existing manual-plan change semantics instead of independently
   re-deriving add, remove, retain, direction, or breakthrough rules in the UI.
4. Make category capacity and 萬用 allocation differences directly scannable.
5. Explain threat coverage, requirements, caveats, and evidence gaps per
   proposal.
6. Preserve unavailable values and diagnostics without converting them to
   zero, empty, or feasible states.
7. Provide a bilingual, responsive, keyboard-accessible comparison workflow.
8. Keep ordering and output deterministic for identical evidence.
9. Preserve absolute game non-interference.

## Non-goals

- Generating new candidates or changing recommendation scoring.
- Exposing arbitrary lower-ranked candidates beyond the three policy winners.
- Claiming that one policy has a verified probability of winning.
- Equipping, unequipping, redirecting, breaking through, or reallocating slots
  in the game.
- Screenshot capture, upload, OCR, or image interpretation.
- Persisting comparison history, target observations, recommendations, or
  player preferences.
- Exporting or sharing recommendation cards.
- Recording battle outcomes or training a feedback model.
- Comparing different saves, targets, GameData versions, or observation states
  as though they were one simultaneous result.
- Adding new unverified combat-effect rules.

## Product principles

### One recommendation result defines the comparison boundary

Every column must come from the same `CombatLoadoutRecommendation`, snapshot
reference, target, catalogue identity, and observation state. The UI cannot
combine cached columns from different reads. Re-running the recommendation
replaces the comparison as one unit.

### Existing change semantics remain authoritative

`ManualLoadoutChange` already defines `Add`, `Remove`, `Retain`,
`ChangeDirection`, and `CompleteBreakthrough`. Epic 4 may normalize and group
those facts, but it must not implement a competing set-difference algorithm in
Presentation. A skill can have more than one applicable instruction, such as
Add plus ChangeDirection, and the comparison must preserve both.

### Unknown is not zero

Unavailable used slots, cost, direction, capacity, requirements, evidence, or
recommendation results remain visibly unavailable with their reason. An empty
loadout is a positive claim and cannot be manufactured from missing data.

### Policy scores are local rankings, not probabilities

Safe, Balanced, and Aggressive use different weights. Their score totals may be
shown with component explanations, but cross-policy totals must not be styled
as a universal numeric winner or chance to win.

### The current column retains provenance

The current baseline must state whether equipped skills, displayed capacity,
and 萬用 allocation came from the save or a newer current-screen observation.
Stale, rejected, or conflicting observations remain visible through the
existing warning and evidence model.

### Responsive comparison is still the same comparison

Desktop may show the current loadout and all three policy columns. Narrow
screens should show the current loadout plus one selected policy and provide an
accessible policy switcher. The data, status vocabulary, and ordering must not
change with viewport size.

### Game non-interference is permanent

Epic 4 follows
[ADR-0001](../../architecture/ADR-0001-absolute-game-non-interference.md).
It reads existing immutable helper results and presents advice. It creates no
port, endpoint, command, or UI action that changes game-owned state.

## Comparison vocabulary

### Columns

- `Current`: the player loadout used by the recommendation calculation.
- `Safe`: the feasible Safe policy winner or its explicit diagnostic.
- `Balanced`: the feasible Balanced policy winner or its diagnostic.
- `Aggressive`: the feasible Aggressive policy winner or its diagnostic.

### Skill states

- `Retained`: present in Current and the proposal.
- `Added`: absent from Current and present in the proposal.
- `Removed`: present in Current and absent from the proposal.
- `DirectionChangeRequired`: the proposal depends on a different verified
  Direct or Reverse effect.
- `BreakthroughRequired`: the exact required breakthrough is currently proven
  available but is not yet complete.
- `Unchanged`: no manual skill action is required; this is a display grouping,
  not a replacement for the authoritative `Retain` change.
- `Unavailable`: the comparison fact cannot be established and carries a
  reason.

Direction and breakthrough are additional instructions rather than mutually
exclusive membership states. Stable identity is skill ID plus category;
localized names are labels, not comparison keys.

### Category summaries

Each loadout category may expose:

- used slots and unavailable reason;
- total capacity;
- remaining slots and unavailable reason;
- effective skill costs where available;
- category-specific and 萬用 slot contribution;
- current and proposed 萬用 allocation; and
- whether a manual allocation change is required.

### Tactical summaries

Each proposal may expose:

- covered threat codes and titles;
- uncovered or unresolved threats;
- unmet or manually confirmed weapon/resource conditions;
- active defense and agility choices;
- caveats and unsupported mechanics;
- manual-change count; and
- score components with their policy weights and evidence.

## Functional scope

### 1. Immutable comparison contract

Define typed comparison values for baseline identity, policy columns, category
rows, skill cells, composite change states, capacity values, evidence, and
diagnostics. Constructors reject duplicate columns, duplicate category rows,
duplicate skill identities, mismatched categories, invalid policies, and blank
references.

### 2. Deterministic comparison builder

Build the comparison from one `CombatLoadoutRecommendation`. The builder uses
the current `PlayerCombatSnapshot`, each style's selected feasible loadout, its
manual plan, threat analysis, and evidence references. It does not read files,
call Infrastructure, or recalculate recommendation scores.

Ordering is fixed by category, localized-independent stable skill ID, change
priority, and stable reference. Identical inputs produce structurally equal
output.

### 3. Capacity and allocation comparison

Show Current and proposed category capacity without guessing missing values.
The comparison identifies changed 萬用 allocation and uses the same proposed
Neigong-derived budgets already validated by the recommendation engine.

### 4. Threat, condition, and caveat comparison

Summarize what each policy covers and what remains unresolved. Threat coverage
uses verified threat codes already linked to recommended options. Raw effect
text and unsupported observations remain non-scoring and cannot be promoted to
coverage by the comparison layer.

### 5. Typed API projection

Expose comparison data through the existing recommendation response or a
versioned read-only comparison projection. Public contracts contain stable
logical references and unavailable reasons, never local file paths or raw
exceptions. Existing clients remain compatible according to the documented
contract strategy.

### 6. Bilingual responsive UI

Add a comparison surface to the combat recommendation workflow:

- desktop current/Safe/Balanced/Aggressive matrix;
- narrow-screen current-plus-selected-policy mode;
- category navigation;
- show-all versus differences-only control;
- visible legend for composite change states;
- provenance, warning, caveat, and infeasible-state disclosure; and
- a path from the chosen policy to its existing setup checklist and battle
  plan.

The interface must be operable by keyboard, expose meaningful headings and
table/list relationships to assistive technology, and not rely on color alone.

### 7. Lifecycle

The comparison is request/session presentation state only. Selecting a target,
changing policy inputs, applying or clearing observations, switching language,
or rebuilding a recommendation replaces the comparison from the new immutable
result. No comparison preference or history is persisted in Epic 4.

### 8. Verification

Tests cover Domain invariants, deterministic building, manual-plan parity,
capacity and allocation values, infeasible policies, unavailable data,
provenance, API mapping, bilingual rendering, responsive modes, keyboard
semantics, observation apply/clear behavior, and architecture safety.

## User-visible states

- no recommendation yet;
- comparison loading;
- Current plus three feasible policies;
- one or more infeasible policies with diagnostics;
- current-screen player observation applied;
- current-screen observation rejected as stale;
- target observation applied or cleared;
- unavailable cost, capacity, direction, condition, or evidence;
- all rows versus differences only;
- desktop four-column mode;
- narrow-screen selected-policy mode; and
- recommendation read or calculation failure.

## Epic acceptance criteria

- [ ] One immutable recommendation result supplies every comparison column.
- [ ] Current, Safe, Balanced, and Aggressive columns use stable typed
      identities and deterministic ordering.
- [ ] Skill membership and manual actions agree exactly with the existing
      manual plan for every feasible policy.
- [ ] Composite Add/Remove/Retain, direction, and breakthrough states cannot be
      collapsed into misleading single labels.
- [ ] Capacity, remaining slots, effective cost, and 萬用 allocation preserve
      unavailable states and reasons.
- [ ] Infeasible policies remain visible with diagnostics and never appear as
      empty feasible loadouts.
- [ ] Threat coverage, unresolved risks, conditions, caveats, and evidence are
      traceable to existing typed recommendation facts.
- [ ] Cross-policy score display cannot be interpreted as win probability.
- [ ] Save-derived and current-screen player baselines remain distinguishable.
- [ ] Applying and clearing target or player observations rebuilds the entire
      comparison without stale columns.
- [ ] The API exposes the same comparison semantics as the UI.
- [ ] Traditional Chinese and English layouts are complete and accessible.
- [ ] Desktop and narrow-screen modes expose equivalent facts.
- [ ] The comparison remains session-bound and information-only.
- [ ] Automated tests cover feasible, infeasible, unchanged, changed,
      unavailable, observed, stale, and cleared states.
- [ ] Local vertical verification proves all inspected save and game sources
      remain byte-for-byte unchanged.
- [ ] The product owner records the Epic 4 completion decision.

## Success measures

- A player can identify every required skill and slot change without manually
  comparing separate recommendation tabs.
- A player can explain why the three policies differ using threat, condition,
  caveat, and evidence information already produced by the engine.
- The differences-only view never hides a required direction, breakthrough, or
  萬用-allocation action.
- Mobile and keyboard users can reach the same decision as desktop users.
- Repeated identical inputs produce identical comparison ordering and content.
- No comparison operation changes game-owned or save-owned state.

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| A wide matrix becomes unusable on mobile | Use Current plus one selected policy on narrow screens |
| UI comparison disagrees with setup checklist | Build from authoritative manual-plan changes and test parity |
| Missing data appears as zero or empty | Use typed unavailable values and required reasons |
| Multiple actions on one skill are hidden | Model membership, direction, and breakthrough as composite states |
| Scores imply universal ranking or win probability | Label policy-local weights and emphasize factual differences |
| Columns come from different snapshots after refresh | Replace comparison atomically from one recommendation result |
| Evidence details overwhelm the primary decision | Progressive disclosure with stable evidence links |
| Feature drifts toward game control | Enforce ADR-0001 with architecture tests and information-only UI language |

## Delivery reference

Implementation order and item-level evidence are tracked in
[the Epic 4 backlog](./BACKLOG.md).
