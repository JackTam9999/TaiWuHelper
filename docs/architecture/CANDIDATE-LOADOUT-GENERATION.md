# Bounded feasible-loadout generation

## Purpose

Candidate generation explores a curated set of evidence-backed skill options
and emits only complete loadouts that pass the Domain feasibility validator.
It is a bounded search stage, not the final recommendation score.

The generator is pure and read-only. It cannot equip a skill, change a
direction, alter allocation in the game, write a save, or control the runtime.

## Option boundary

Each `CombatLoadoutOption` contains:

- one `CombatSkillCandidate`;
- activation and combat requirements;
- covered threat codes;
- whether the skill is currently equipped;
- evidence reference;
- optional counter strength and activation timing; and
- optional expected raw effect ID.

`FromCounterRule` creates a direction-change-capable option using the exact
verified catalog effect. `RetainCurrentSkill` creates a direction-independent
option for an existing selection.

Options are curated inputs. Candidate generation does not enumerate every
learned skill or invent counters from names.

## Hard filters

Before combination search, each option must pass:

1. learned-skill ownership;
2. any mastery requirement;
3. strict current direction or explicit manual direction-change eligibility;
4. availability of the requested Direct or Reverse effect; and
5. exact raw effect ID when the option comes from a verified counter.

Rejected options remain in diagnostics and are not included in the search.

Each explored combination must then:

1. select at most one active agility and one active defense;
2. construct a proposed categorized loadout;
3. construct its proposed requirement context;
4. include all selected candidate and requirement specifications;
5. use the requested generic-slot allocation; and
6. pass `CombatLoadoutFeasibilityValidator`.

Only its accepted-only `FeasibleCombatLoadout` can enter the emitted result.

## Bounds and determinism

Hard limits prevent combinatorial growth:

| Bound | Maximum |
|---|---:|
| Curated options | 24 |
| Explored combinations | 65,536 |
| Emitted results | 256 |

Requests may choose smaller exploration and result limits. Hitting either
limit creates a diagnostic.

Options are normalized before include-first traversal:

1. combat-start counter first;
2. hard counter first;
3. greater threat coverage;
4. currently equipped first; and
5. ascending skill ID.

Feasible results use a deterministic pre-scoring order:

1. more combat-start counters;
2. more hard counters;
3. more distinct covered threats;
4. fewer selected skills;
5. more retained current skills; and
6. stable categorized skill key.

M1-017 applies policy scoring after this stage. The ordering here guarantees
repeatable candidate production and preserves current skills when otherwise
equally suitable; it does not claim which feasible loadout is tactically best.

## Diagnostics

Diagnostics distinguish:

- rejected options;
- incompatible active roles;
- infeasible combinations, including all feasibility failures;
- exploration-limit truncation;
- result-limit truncation; and
- an empty eligible-option pool.

This lets later API and UI layers explain why a skill or complete loadout was
not emitted without treating expected invalidity as an exception.
