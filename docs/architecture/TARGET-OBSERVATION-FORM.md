# Target-observation form

## Purpose

E3-006 adds a bilingual, manual-first target-observation form to the combat
recommendation page. E3-012 extends it to accept only the labelled skill
effects visibly exposed during hostile/story combat while keeping the full
opponent loadout unavailable.

This form is helper input. It does not read a screenshot, inspect process
memory, automate input, control the game, modify a save, or persist the
observation. Applying it creates a new in-memory recommendation request.

## Visibility boundary

The encounter choice is part of the evidence claim:

| Encounter | Form behavior | Evidence meaning |
|---|---|---|
| `Sparring` | Skill entry is available | Current displayed opponent loadout may be reported |
| `Hostile` | Full loadout unavailable; partial effect entry is available | Only labelled battle-visible active effects are confirmed |
| `Story` | Full loadout unavailable; partial effect entry is available | Only labelled battle-visible active effects are confirmed |

Hostile and story contexts are never converted into an empty or partial
equipped loadout. `秘而不宣` therefore keeps the full loadout unavailable.
The separately listed active effects are always partial evidence; omitted
skills and equipment slots remain unknown. Switching contexts clears any
selected skills so claims cannot cross evidence modes.

## Form flow

1. The form starts disabled and requires a save-only recommendation first.
2. Target name, age, snapshot read time, and save-timestamp availability are
   shown before observation entry.
3. The player confirms `Sparring`, `Hostile`, or `Story`.
4. `Sparring` exposes partial/complete coverage. `Hostile` and `Story` expose
   skill controls with coverage fixed to partial battle-visible effects.
5. Partial coverage confirms listed skills while omissions remain unknown.
6. Complete coverage means every category and empty slot on the one displayed
   preset was inspected; it does not cover another preset.
7. Skill names are resolved in the active Traditional Chinese or English
   catalogue. The player confirms ambiguous candidates using verified name,
   category, base slot cost, and match kind.
8. Category is catalogue-derived. Direction is optional and limited to
   visible `Direct` or `Reverse`; unsupported neutral direction is not offered.
   Visible power may be recorded as a non-negative percentage, labelled as
   evidence-only, and does not affect legality or scoring.
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

- editor-state unit tests for prerequisites, hostile/story partial contexts, resolution,
  typed request construction, merge-result states, and clearing;
- component rendering tests for bilingual guidance, both hostile and story
  full-loadout-unavailable states, partial skill input, semantic controls, and
  status text;
- localization tests covering every target-observation state in both
  languages;
- architecture checks reviewing all UI event handlers and enforcing the
  read-only, no-game-control boundary.
