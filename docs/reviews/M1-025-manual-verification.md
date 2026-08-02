# M1-025 manual in-game verification

**Status:** Complete — accepted milestone outcome with documented attribution
limitations

## Boundary

This review checks a recommendation that the player applies manually. The
helper reads evidence and renders instructions only. It must not write a save,
equip a skill, change a direction, send game input, attach to the process, or
otherwise alter game-owned state.

## Preparation result

The read-only live snapshot found target `16317`, age 52, and analyzed the
four documented threats:

- distraction-mark accumulation;
- mind-resonance cascade; and
- positive-practice magic-sound mind damage.
- repeatable defeat-mark reset through Reverse 九色玉蟬法.

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

The target's persisted loadout is still unavailable, but a later local-only
battle frame explicitly shows Reverse 九色玉蟬法 triggering
`消除己之標記`. Version-matched effect `911` supplies the exact defeat-threshold
trigger, 9-point escalating Qiqiao true-Qi cost, and cleared mark types. The
reset is therefore a confirmed Critical threat. The recommendation remains a
mitigation plan because the available resource-pressure counter is random; its
deterministic score must not be interpreted as a win probability.

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

## Completion record

- [x] Capture or save the current complete loadout so all five category skill
      lists and used/capacity values refer to the same configuration.
- [x] Generate the recommendation from that current snapshot.
- [x] Cover the reset-aware recommendation correction with automated tests;
      refreshing and using that exact API result in battle is deferred.
- [x] Confirm every newly returned skill is available in the stated direction;
      老君拂塵功 must remain excluded as a Reverse counter until Reverse
      breakthrough is either completed or immediately achievable from the
      current read pages.
- [x] Confirm all five returned slot totals exactly match the game UI.
- [x] Confirm the returned weapon and activation requirements in the game.
- [x] Confirm 老君拂塵功's configured Reverse combat-start effect description is
      shown in the game; this describes the effect but not current access.
- [x] Apply a freshly corrected proposed loadout manually.
- [x] Confirm a real in-game victory against the target with the final manual
      loadout. Reverse 七轮感应法 was separately validated in-game, although
      its contribution was not part of this victory because it was unused and
      full beneficial pills were consumed.
- [x] Record the changed current-screen baseline as newer observation evidence,
      not as a mechanics-rule correction.
- [x] Record the failed battle outcome and the omitted reset threat as a rule
      correction rather than silently treating an equipped loadout as success.
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

## Failed first battle attempt and corrective action

The player confirmed that the returned skills, directions, capacity, weapon,
and execution conditions were usable, then reported that the target still
could not be defeated. This is a failed effectiveness check, not completion of
M1-025.

The review found that the engine still represented the target's Reverse
九色玉蟬法 reset as an unknown warning. Warnings are not scored, so candidate
generation optimized survival against mind damage and distraction marks while
omitting the mechanism that repeatedly prevents victory. The target can clear
all relevant marks by paying Qiqiao true-Qi at costs `9, 18, 27, ...`, up to
`99`; surviving longer is insufficient while those payments remain available.

The correction adds `DEFEAT_MARK_RESET_LOOP` as a Critical threshold threat,
maps target assistance passives into the read-only snapshot, and adds Reverse
七轮感应法 as a mitigation candidate. Its verified effect doubles initial
damage-state intensity and adds a slowly decreasing random-type true-Qi damage
state. The player owns Reverse 七轮感应法 and can equip it for two Assistance
slots. Because the drained type is random and requires a damage state first,
it is not described as a guaranteed Qiqiao drain or hard counter. A fresh
generated plan and another manual battle remain required.

The API process already running on port `5056` predates this correction and
still returns the former three-threat result plus
`UNRECOGNIZED_TARGET_MECHANIC`. It must be restarted before the next manual
attempt; that old process is not accepted as verification of the new rules.

## Successful battle with unresolved attribution

On 2026-08-01, the player confirmed that the same target was defeated. The
player did not use the newly recommended Reverse 七轮感应法 and took all
available beneficial pills before the battle.

This result establishes that the target is defeatable in the observed player
state, but by itself it does not validate the new counter rule or attribute the
victory to the recommendation. The consumables changed combat attributes, and
the recommended reset-pressure skill was not activated. The outcome is
therefore recorded as `Victory — recommended counter unused — full consumable
buffs`, not as evidence that the counter caused this victory.

The player subsequently confirmed that Reverse 七轮感应法 was separately
validated in-game. Its required Reverse direction and verified effect are
therefore accepted as player-validated. This does not change its rule strength:
the effect drains a random true-Qi type, so it remains Mitigation rather than a
guaranteed Qiqiao reset lockout.

No additional screenshot is required merely to prove the victory. If this
encounter can later be repeated from a player-chosen pre-fight save, the
minimum useful comparison is to record the actual loadout, which recommended
active skills were used, and whether full consumable buffs were present. The
helper must not reload, modify, or control that save on the player's behalf.

## Final winning loadout evidence

The player supplied a final local-only loadout screenshot with SHA-256
`7DA3C2CFD179506E437D7B851D377421E75B448EEA3E972B4871494EB105D08D`.
It confirms zero unallocated universal slots and these displayed totals:

| Category | Used / capacity | Visible skills |
|---|---:|---|
| Neigong | 6 / 6 | 沛然诀, 罗汉功, 十三太保横练功, 铁鼎金身功, 遍体火漆法, 封口固气法 |
| Attack | 9 / 10 | 霸王刀, 金猊镇魔刀, 鬼八式蟠龙刀, 狂刀 |
| Agility | 6 / 7 | 狮子奋迅, 上玉阁, 万花听雨式 |
| Defense | 8 / 9 | 水火硬气功, 九滚十八跌, 狮相铁头功, 损刚益柔, 精卫填海式, 拿脉功, 曼荼罗真言 |
| Assistance | 3 / 4 | 兵闻拙速, 墨玉功, 三部九候法 |

The player also confirmed using legendary-book effects. The overview does not
show their exact assignments, so no particular book-to-skill mapping is
inferred. The screenshot remains local-only and is not committed.

The product owner accepted M1-025 as complete on 2026-08-01. Completion means
the read-only vertical slice produced an actionable workflow that reached a
real target victory after player adjustments. It does not claim that Reverse
七轮感应法 caused the victory, nor that the contributions of the final loadout,
legendary books, and consumable buffs were independently measured. Reverse
七轮感应法 was separately validated after the recorded victory, but was not a
cause of that particular outcome.
