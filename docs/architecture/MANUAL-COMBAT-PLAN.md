# Manual combat recommendation plan

## Purpose

The manual plan converts the highest-ranked feasible candidate into structured
instructions that a player can follow in the game UI. It also exposes feasible
active defense and agility alternatives.

The plan is descriptive only. It has no port, adapter, or operation for
equipping skills, changing practice directions, controlling combat, writing a
save, or mutating game data.

## Loadout comparison

The planner compares the selected candidate with the current read-only player
snapshot, category by category:

- `Add` means the proposed loadout contains a skill that is not equipped.
- `Remove` means the current loadout contains a skill omitted by the proposal.
- `Retain` means the skill occurs in both loadouts.
- `ChangeDirection` means accepted candidate validation requires a different
  Direct or Reverse effect.

A direction change is an additional manual instruction. It does not indicate
that the application changed the skill.

## Active-role choices

The selected candidate supplies the primary active defense and active agility
choices. Lower-ranked feasible candidates supply up to three distinct
alternatives for each role, preserving ranking order.

Alternatives create switch conditions that explicitly say to choose them
before combat or between attempts when the primary activation requirements
cannot be met. No mid-combat loadout mutation is assumed.

## Opening sequence

Selected counter options are ordered by their verified activation timing:

1. combat-start passive;
2. equipped passive;
3. active defense;
4. active agility; and
5. active attack.

Passive instructions ask the player to confirm or retain equipment. Active
instructions tell the player to select or use a skill only when its recorded
requirements are satisfied.

## Reason references

Every returned instruction owns a `RecommendationReason` containing:

- a stable reason code;
- a presentation-neutral summary;
- one or more evidence references; and
- the relevant threat codes, when present.

This structure gives M1-019 and API/UI layers a factual explanation source
without parsing display text or inventing unsupported combat claims.
