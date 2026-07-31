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

`FromCounterRule` creates a direction-strict option using the exact verified
catalog effect. A caller may enable a manual direction change only when it has
separate evidence that the player can make that change. It may independently
allow an immediate breakthrough when the read-only snapshot proves the exact
required outcome is currently achievable. `RetainCurrentSkill` creates a
direction-independent option for an existing selection.

Options are curated inputs. Candidate generation does not enumerate every
learned skill or invent counters from names.

## Hard filters

Before combination search, each option must pass:

1. learned-skill ownership;
2. any mastery requirement;
3. strict current direction, explicit manual direction-change eligibility, or
   an explicitly allowed and immediately achievable breakthrough into the
   required direction;
4. availability of the requested Direct or Reverse effect; and
5. exact raw effect ID when the option comes from a verified counter.

Rejected options remain in diagnostics and are not included in the search.
When a rejected counter option is already equipped, it falls back to a plain
retention option so that an unusable direction-specific effect does not
silently remove the player's current skill.

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
| Curated options | 40 |
| Explored combinations | 65,536 |
| Emitted results | 256 |

Requests may choose smaller exploration and result limits. Hitting either
limit creates a diagnostic.

Equipped Neigong options are required because they can contribute category
capacity. Other plain current-skill options are treated as retention options.
The bounded include-first traversal enumerates only strategic counter and
replacement options; for each strategic combination it greedily retains every
current skill that remains feasible, considering lower-cost skills first.
This keeps the bounded search focused on tactical choices without silently
emptying unrelated categories.

Strategic options are normalized before include-first traversal:

1. combat-start counter first;
2. hard counter first;
3. greater threat coverage;
4. currently equipped counter first; and
5. ascending skill ID.

Feasible results use a deterministic pre-scoring order:

1. more combat-start counters;
2. more hard counters;
3. more distinct covered threats;
4. more retained current skills;
5. fewer selected skills; and
6. stable categorized skill key.

M1-017 applies policy scoring after this stage. The ordering here guarantees
repeatable candidate production and preserves current skills when otherwise
equally suitable; it does not claim which feasible loadout is tactically best.

## Diagnostics

Candidates with the same stable categorized loadout key are deduplicated
before result limiting.

Diagnostics distinguish:

- rejected options;
- incompatible active roles;
- infeasible combinations, including all feasibility failures;
- exploration-limit truncation;
- result-limit truncation; and
- an empty eligible-option pool.

Repeated identical diagnostics are aggregated with an occurrence count. This
lets later API and UI layers explain why a skill or complete loadout was not
emitted without returning thousands of duplicate warnings or treating
expected invalidity as an exception.
