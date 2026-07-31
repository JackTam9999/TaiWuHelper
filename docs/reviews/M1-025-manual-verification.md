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

The latest in-game breakthrough screen confirms that 老君拂塵功 has not
completed breakthrough and cannot currently provide its Reverse effect. This
newer player-visible evidence invalidates the earlier recommendation that
treated it as an available Reverse mitigation.

Review of the installed GameData contract found the adapter defect: GameData
uses `-1 = None`, `0 = Direct`, and `1 = Reverse`, while the Domain enum uses a
different numeric order. The adapter had mapped the raw numbers directly and
therefore misreported `None` as Reverse. GameData also returns `None` before a
skill completes breakthrough. The adapter now maps the values by meaning and
marks an unbroken skill's direction unavailable.

The reader now also maps breakthrough availability as a separate value. It
uses GameData's read-only `CanBreakout` result and the currently read normal
pages to determine the exact outcomes available now. This does not activate an
effect. A recommendation may use it only by adding a mandatory manual
`CompleteBreakthrough` prerequisite.

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

## Invalidated Safe recommendation

Before the direction-mapping correction, the API generated the following
candidate from the newest complete current-screen observation:

```text
Neigong:    41, 21, 5, 42, 0, 97
Attack:     599, 598, 616, 603, 602, 624, 686
Agility:    148, 158, 1, 146, 147, 149, 128
Defense:    289, 253, 266, 2, 292, 251, 244
Assistance: 252, 280
```

The returned candidate exactly matched the newest loadout screen, but that
screen proved only membership and displayed capacity. It did not prove each
skill's active practice direction. The candidate is not accepted as a
verified recommendation because 老君拂塵功 was incorrectly classified as
Reverse.

The following two manually introduced skills are visible in the returned
`10/10` Attack row, but only 伏龍刀法 remains a direction candidate until a
fresh corrected read is reviewed:

| Skill | Direction | Cost | Role |
|---|---|---:|---|
| 624 伏龍刀法 | Reverse | 1 | Active mitigation of the target's attack-skill power |
| 686 老君拂塵功 | Unavailable | 2 | Reverse effect cannot activate before breakthrough |

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

The effect-detail screens confirm what the configured Reverse effects do; they
do not prove that the player can currently activate those directions. The
latest breakthrough screen is authoritative for 老君拂塵功 and shows that its
Reverse combat-start effect is not yet available.

The instruction to rely on Reverse 老君拂塵功 is withdrawn. Its current read
pages allow an immediate Direct breakthrough only, not the required Reverse
breakthrough. A corrected recommendation may use a completed required
direction or an exact immediately achievable breakthrough accompanied by a
manual prerequisite; 伏龍刀法 still requires its separate weapon and
activation check.

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
- Fall back to plain retention when an equipped counter is rejected for an
  otherwise usable mismatch; do not retain it in the recommendation when its
  practice direction is unavailable because breakthrough is incomplete.
- Map GameData practice-direction values by their declared meaning rather than
  by numeric compatibility with the Domain enum.
- Reject a direction-specific effect while the skill has not completed
  breakthrough unless the exact required direction is immediately achievable
  and the option explicitly permits a mandatory manual breakthrough step.

## Verification still required

- [x] Capture or save the current complete loadout so all five category skill
      lists and used/capacity values refer to the same configuration.
- [x] Generate the recommendation from that current snapshot.
- [ ] Generate a fresh recommendation after the GameData direction-mapping
      correction.
- [ ] Confirm every newly returned skill is available in the stated direction;
      老君拂塵功 must remain excluded as a Reverse counter until Reverse
      breakthrough is either completed or immediately achievable from the
      current read pages.
- [x] Confirm all five returned slot totals exactly match the game UI.
- [ ] Confirm 伏龍刀法's weapon and activation requirements in the game.
- [x] Confirm 老君拂塵功's configured Reverse combat-start effect description is
      shown in the game; this describes the effect but not current access.
- [ ] Apply a freshly corrected proposed loadout manually.
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
- The previous Safe candidate matched the displayed loadout but was invalidated
  by the newer 老君拂塵功 breakthrough screen and the direction-mapping defect.
- A post-run fingerprint check confirmed that the configured save remained
  byte-for-byte unchanged.
