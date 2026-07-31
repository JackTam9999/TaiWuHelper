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
the recommendation instead uses already-Reverse skills 624 and 686 as
mitigations.

The player then supplied a newer complete loadout screen after making the
changes manually. That local-only evidence has SHA-256
`1C8629A919958943FC72BEEE273C96390F8777F51ADA02F80FE2FC1903E562CB`.
Its timestamp is newer than both the configured save and the earlier M1-001
screen, so it is now the authoritative current-screen observation.

| Category | Current observed skill IDs | Displayed used / capacity |
|---|---|---:|
| Neigong | 41, 21, 5, 42, 0, 97 | 6 / 6 |
| Attack | 599, 598, 616, 603, 602, 624, 686 | 10 / 10 |
| Agility | 148, 158, 1, 146, 147, 149, 128 | 8 / 8 |
| Defense | 289, 253, 266, 2, 292, 251, 244 | 8 / 8 |
| Assistance | 252, 280 | 2 / 2 |

The observed six universal slots remain allocated as four Attack and two
Agility slots. The observation is immutable helper input and is not written to
the game.

## Generated Safe recommendation

The API generated the following feasible Safe candidate from the newest
complete current-screen observation:

```text
Neigong:    41, 21, 5, 42, 0, 97
Attack:     599, 598, 616, 603, 602, 624, 686
Agility:    148, 158, 1, 146, 147, 149, 128
Defense:    289, 253, 266, 2, 292, 251, 244
Assistance: 252, 280
```

The returned candidate exactly matches the newest screen. The API reports no
remaining Add, Remove, or ChangeDirection action. This is a newer baseline,
not the earlier 2026-07-29 row with skills 604, 617, 601, and 254; source
precedence requires the latest screen to supersede that stale observation.

The following two manually introduced mitigations are visible in the returned
`10/10` Attack row:

| Skill | Direction | Cost | Role |
|---|---|---:|---|
| 624 伏龍刀法 | Reverse | 1 | Active mitigation of the target's attack-skill power |
| 686 老君拂塵功 | Reverse | 2 | Combat-start passive mitigation of distraction marks |

The game screen verifies the final totals as `6/6`, `10/10`, `8/8`, `8/8`,
and `2/2`, with zero unallocated universal slots. No practice-direction change
is requested.

Two local-only detail screens provide additional evidence:

- `FCDFD3794AE1688E3D071064DA9CEF04970D8EBDBB7F1B76A2B63DCFA9605A8E`
  shows 伏龍刀法's Reverse rule: reduce all enemy Attack-skill power in
  proportion to the skill's performance until combat ends. It also shows
  aptitude `174/30` and connection success `100%`.
- `366A56E74764FBDBE5FE87E5326109BEB6416C2D8514B1A6191180CFF7421FFF`
  shows 老君拂塵功's Reverse rule: begin combat with six layers and, after
  total defeat marks exceed half the defeat condition, consume layers to
  remove hindrance marks.

The read-only save independently confirms that both skills are currently
Reverse. The screens confirm their configured effects, but only a live battle
can prove that 伏龍刀法 can be activated with the selected weapon and that
老君拂塵功's six-layer state appears and consumes correctly.

The generated opening instructions are:

1. Confirm Reverse 老君拂塵功 is equipped before combat so its combat-start
   passive is present.
2. Use Reverse 伏龍刀法 at the opening only when its live weapon and
   activation requirements are satisfied.

The target's persisted loadout is still unavailable, and the observed reset at
36 defeat marks still only resembles Reverse 九色玉蟬法. The recommendation
therefore remains a mitigation plan rather than a confirmed hard-counter plan.
Its deterministic score must not be interpreted as a win probability.

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

- [x] Capture or save the current complete loadout so all five category skill
      lists and used/capacity values refer to the same configuration.
- [x] Generate the recommendation from that current snapshot.
- [x] Confirm every newly returned skill is learned and available in the stated
      direction according to the read-only save: 624 Reverse and 686 Reverse.
- [x] Confirm all five returned slot totals exactly match the game UI.
- [ ] Confirm 伏龍刀法's weapon and activation requirements in the game.
- [x] Confirm 老君拂塵功's Reverse combat-start effect description is shown in
      the game.
- [x] Apply the proposed loadout manually.
- [ ] Confirm in battle that the opening plan addresses distraction marks and
      mind-resonance pressure.
- [x] Record the changed current-screen baseline as newer observation evidence,
      not as a mechanics-rule correction.
- [x] Reconfirm the helper did not modify the configured save during the
      automated read and recommendation run.

## Automated and read-only checks

- Solution formatting verification passed.
- Default solution tests passed with the opt-in local read explicitly skipped.
- The opt-in local integration suite passed both tests against the configured
  local save.
- A fresh prescribed inspection reported `saveModified=False`.
- The newest complete screenshot observation produced a Safe candidate that
  exactly matches the displayed loadout and has no remaining manual changes.
- A post-run fingerprint check confirmed that the configured save remained
  byte-for-byte unchanged.
