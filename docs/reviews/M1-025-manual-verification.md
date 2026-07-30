# M1-025 manual in-game verification

**Status:** In progress

## Boundary

This review checks a recommendation that the player applies manually. The
helper reads evidence and renders instructions only. It must not write a save,
equip a skill, change a direction, send game input, attach to the process, or
otherwise alter game-owned state.

## Preparation result

The read-only live snapshot found target `16317`, age 52, and analyzed the
three documented threats:

- distraction-mark accumulation;
- mind-resonance cascade; and
- positive-practice magic-sound mind damage.

The disk snapshot reports skill 604 as Neutral. The player has already stated
that Reverse practice is not currently available, so the helper must not
recommend its Reverse hard-counter effect. With direction changes kept strict,
the current disk snapshot instead produces a mitigation plan using
already-Reverse skills 624 and 686.

This is not yet the accepted in-game recommendation. The disk snapshot has an
older outer-skill arrangement than the latest game screen, and exact runtime
slot capacities require the displayed used/capacity values. The latest known
display is `6/6`, `10/10`, `8/8`, `8/8`, and `2/2`, but the complete current
skill IDs and those budgets must describe the same current configuration
before final verification.

## Corrections made before manual verification

- Aggregate identical generation diagnostics with occurrence counts.
- Search strategic counter combinations separately from plain retention.
- Score compatibility by the share of the current loadout retained.
- Preserve source-backed runtime capacity adjustments during proposal checks.
- Never use a collection's implementation `Capacity` as combat-grid capacity.
- Accept complete displayed slot budgets as optional current-screen evidence.
- Keep required practice direction strict unless explicit evidence permits a
  manual direction change.
- Fall back to plain retention when an equipped counter is rejected.

## Verification still required

- [ ] Capture or save the current complete loadout so all five category skill
      lists and used/capacity values refer to the same configuration.
- [ ] Generate the recommendation from that current snapshot.
- [ ] Confirm every returned skill is available in the stated direction.
- [ ] Confirm all five returned slot totals exactly match the game UI.
- [ ] Confirm weapon and activation requirements in the game.
- [ ] Apply the instructions manually and confirm the opening plan addresses
      distraction marks and mind-resonance pressure.
- [ ] Record any discrepancy as a rule correction.
- [ ] Reconfirm the helper did not modify any game-owned state.

## Automated and read-only checks

- Solution formatting verification passed.
- Default solution tests passed with the opt-in local read explicitly skipped.
- The opt-in local integration suite passed both tests against the configured
  local save.
- A post-run inspection reported that the source save was unchanged.
