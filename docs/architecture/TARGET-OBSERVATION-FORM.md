# Target-observation form

## Purpose

E3-006 adds a bilingual, manual-first target-observation form to the combat
recommendation page. It lets the player report only an opponent loadout that
the supported game UI currently exposes during `切磋武功`.

This form is helper input. It does not read a screenshot, inspect process
memory, automate input, control the game, modify a save, or persist the
observation. Applying it creates a new in-memory recommendation request.

## Visibility boundary

The encounter choice is part of the evidence claim:

| Encounter | Form behavior | Evidence meaning |
|---|---|---|
| `Sparring` | Skill entry is available | Current displayed opponent loadout may be reported |
| `Hostile` | Explicit unavailable state; no skill entry | The game does not expose the opponent loadout page |
| `Story` | Explicit unavailable state; no skill entry | The game does not expose the opponent loadout page |

Hostile and story contexts are never converted into an empty or partial
loadout. `秘而不宣` therefore remains unavailable evidence. Switching from a
sparring context to either hidden context clears any selected skills.

## Form flow

1. The form starts disabled and requires a save-only recommendation first.
2. Target name, age, snapshot read time, and save-timestamp availability are
   shown before observation entry.
3. The player confirms `Sparring`, `Hostile`, or `Story`.
4. Only `Sparring` exposes coverage and skill controls.
5. Partial coverage confirms listed skills while omissions remain unknown.
6. Complete coverage means every category and empty slot on the one displayed
   preset was inspected; it does not cover another preset.
7. Skill names are resolved in the active Traditional Chinese or English
   catalogue. The player confirms ambiguous candidates using verified name,
   category, base slot cost, and match kind.
8. Category is catalogue-derived. Direction is optional and limited to
   visible `Direct` or `Reverse`; unsupported neutral direction is not offered.
9. Review freezes the UTC observation time and shows target, coverage, time,
   resolved skill identity, and evidence state before apply.
10. Apply creates a new recommendation request. Clear re-runs the save-only
    recommendation and removes all session observation state.

## State and accessibility

The editor exposes explicit initial, editing, searching, ambiguous, review,
applying, applied, stale, conflicting, unsupported, precedence-confirmation,
unavailable, error, and cleared states. Every state has Traditional Chinese
and English text.

Labels, fieldsets, radio buttons, checkboxes, native selects, and buttons keep
the flow keyboard accessible. Status and validation messages use `status` or
`alert` semantics, so meaning is not conveyed by color alone.

## Application boundary

`TargetObservationForm` calls `IResolveTargetSkillSelection` for catalogue
identity confirmation. The recommendation page sends the reviewed request to
`ITargetObservationRecommendationWorkflow`, which creates the immutable
observation and recommendation result described by E3-004 and E3-005.

The original save-only recommendation route remains unchanged. New target
selection, a fresh save-only recommendation, or Clear resets the optional
observation state.

## Verification

Verification is provided by:

- editor-state unit tests for prerequisites, hidden contexts, resolution,
  typed request construction, merge-result states, and clearing;
- component rendering tests for bilingual guidance, both hostile and story
  unavailable states, semantic controls, status text, and absence of hidden
  skill inputs;
- localization tests covering every target-observation state in both
  languages;
- architecture checks reviewing all UI event handlers and enforcing the
  read-only, no-game-control boundary.
