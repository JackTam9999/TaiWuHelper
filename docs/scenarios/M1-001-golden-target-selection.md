# M1-001: Golden-target selection

| Field | Value |
|---|---|
| Status | Complete |
| Epic | [EPIC-001](../roadmap/EPIC-001-combat-skill-recommendation.md) |
| Backlog item | [M1-001](../roadmap/BACKLOG-milestone-1.md#m1-001--select-the-golden-target-and-objective) |
| Inspection date | 2026-07-29 |

## Purpose

Select one reproducible opponent and player objective for the first complete
combat-recommendation vertical slice.

The selection must have:

- A stable character ID and player-visible name.
- A target present in the configured save.
- An equipped target loadout or equally reliable current-screen evidence.
- A manually observed critical mechanic.
- A player-confirmed weapon and victory objective.
- A manually verified player loadout to use as the comparison baseline.

## Existing player intent

The previously verified player profile identifies the intended scenario as:

- Fight a 奇書 opponent centered on 正練魔音.
- Survive 失神 marks and 心韻激盪 before optimizing damage.
- Account for an observed reset whenever the enemy reaches 36 defeat marks.
- Investigate that reset as an apparent reverse 九色玉蟬法 effect.
- Prefer a 刀-based counter setup.
- Do not rely on pure-Yang skills.
- Do not assume reverse 即身成佛 is available.

The confirmed initial objective is:

> **Safe:** prevent a 失神 defeat, control the target's core positive-practice
> magic-sound skill, then achieve a reliable defeat despite repeated
> 36-defeat-mark resets.

The player confirmed the 52-year-old 樂器奇書 as the intended opponent and
answered yes to the proposed 刀 and `Safe` setup.

## Read-only inspection

The configured save was inspected through the hidden read-only inspector. Its
SHA-256 hash was identical before and after every inspection.

The save contains the following active or recorded 奇書 owners:

| Book type | Character ID | Standalone name token | Equipped target skills | Selection note |
|---:|---:|---|---|---|
| 3 | 7649 | `Surname_31GivenName_31` | None in current snapshot | Mechanic not confirmed |
| 4 | 22678 | `Surname_25GivenName_25` | Present | Mechanic not manually confirmed |
| 5 | 12968 | `Surname_7GivenName_7` | None in current snapshot | Mechanic not confirmed |
| 7 | 21563 | `Surname_25GivenName_25` | Present | Mechanic not manually confirmed |
| 9 | 31168 | `Surname_25GivenName_25` | Present | Mechanic not manually confirmed |
| 13 | 16317 | `Surname_6GivenName_6` | None in current snapshot | Confirmed 52-year-old 樂器奇書 |

The standalone runtime does not resolve the localized surname and given-name
tokens, so a current game screen is required to record the player-visible
name.

## Confirmed golden target

The player confirmed character `16317` through the in-game description
`樂器奇書（52歲）`. The game does not display internal character IDs, so the
match uses multiple independent fields:

- The save records `16317` as the owner for book type 13.
- The saved age is 52.
- Its learned type-13 attacks use the mind-damage distribution exclusively.
- Several of those learned attacks are in the positive practice direction.
- This aligns with the existing manual observations of 正練魔音, 失神, and
  心韻激盪.

The player-visible golden-target label is `樂器奇書（52歲）`; the standalone
reader's unresolved `Surname_6GivenName_6` token is retained only as diagnostic
evidence.

The current snapshot contains no equipped skills for character `16317`, so it
cannot prove which skills were active during the observed fight. The character
may represent a completed or inactive encounter. This limitation must remain
visible until a fresh pre-fight snapshot or current-screen target evidence is
available.

## Critical mechanic to verify

For the confirmed character `16317`, the expected critical mechanic is:

1. Positive-practice magic-sound attacks accumulate mind-loss damage.
2. Mind-loss damage produces 失神 marks.
3. The first mark begins the 心韻 countdown.
4. 心韻激盪 produces repeated mind-loss pressure and persistent marks.
5. At 36 total defeat marks, the target may consume increasing 奇竅 energy to
   clear its marks through reverse 九色玉蟬法.

The first four points come from the previously verified magic-sound rules. The
fifth is based on the player's observed reset pattern and remains an explicit
hypothesis until the exact equipped target skill is confirmed.

## Verified player baseline

The player supplied a current in-game 運功 screenshot on 2026-07-29. The
local-only `M1-001-current-player-loadout.png` evidence is the
authoritative baseline because it is newer than the disk save.

The screenshot has SHA-256
`D657FD6829378D801B82323680303C79EFD95E0577DD47356F7E194413DECF9A`.

### Capacity model

The local-only `M1-001-empty-capacities.png` evidence proves that the
unmodified capacities are:

| 內功 | 摧破 | 輕靈 | 護體 | 奇竅 |
|---:|---:|---:|---:|---:|
| 6 | 2 | 2 | 2 | 2 |

Its SHA-256 is
`D773B8620FA34E14D89AB1420848E188358DCB480CF207ABF77C63F01B16A564`.

Equipped 內功 consumes the six 內功 slots and modifies the four outer-category
capacities through each skill's 功法欄位 values. It can also generate 萬用
slots, which increase a category only after allocation. The
local-only `M1-001-inner-power-capacity-example.png` evidence, for
example, proves a cost of one 內功 slot and contributions of 摧破 `+1`,
輕靈 `+0`, 護體 `+2`, 奇竅 `+0`, and 萬用 `+0`.

The tooltip image has SHA-256
`FD844C3D9896BBE6C90BA3B86A59A61F3F475EF08512CF5FF81CBDA6E9483FA6`.

The capacity calculation is therefore:

> Empty base + selected 內功 category adjustments + allocated 萬用 slots +
> other independently verified slot modifiers.

The populated screen's `10/8/8/2` values are the final current capacities, not
base values. Its top-level `萬用欄位 0` means no universal slots remain
unallocated; it does not mean the selected 內功 generated none.

| Category | Used / capacity | Equipped skills and actual cost |
|---|---:|---|
| 內功 | 6 / 6 | 相抵鐵鼎金身功 1；相抵十三太保橫練功 1；正羅漢功 1；相抵銅人腧穴圖經 1；相抵沛然訣 1；正遍體火漆法 1 |
| 摧破 | 10 / 10 | 相抵金猊鎮魔刀 3；相抵霸王刀 2；正斬鰲刀法 2；相抵魔障刀法 1；相抵九牛二虎刀 2 |
| 輕靈 | 8 / 8 | 相抵橫江鎖 1；正上玉閣 1；相抵小縱躍功 1；正震山步 1；相抵鐵橋功 1；正獅子奮迅 2；逆牽牛環身步 1 |
| 護體 | 8 / 8 | 正損剛益柔 1；相抵拿脈功 1；正獅相鐵頭功 1；相抵水火硬氣功 1；相抵精衛填海式 1；正曼荼羅真言 2；逆九滾十八跌 1 |
| 奇竅 | 2 / 2 | 相抵霸王舉鼎 2 |

The screenshot shows no unallocated 萬用欄位. The read-only save inspection
records six already allocated universal slots: four to 摧破 and two to 輕靈.
This explains part of the increase from the empty configuration. The inspector
did not resolve every extra-slot source, so no reconstructed total is allowed
to override the final capacity displayed by the current game screen.

The installed game configuration supplied the `GridCost` values. Practice
directions were cross-checked against the read-only skill activation state.
The save's equipped-card list is slightly older than the screenshot for
摧破 and 奇竅, so it is not used to override current screenshot membership.

This is a comparison baseline, not the recommended anti-magic-sound loadout:

- 金猊鎮魔刀 is currently direction-neutral, so its reverse-only shutdown of
  positive-practice magic-sound skills is inactive.
- The player previously ruled out pure-Yang skills, but the current baseline
  still equips 遍體火漆法. This conflict must be resolved by a later
  recommendation rather than silently changing the baseline.
- The current 奇竅 section does not contain the verified anti-失神 passive
  set.
- Reverse 即身成佛 remains unavailable and is not assumed.

## Later evidence

M1-001 is complete. If available, a pre-fight target screen showing the
equipped 樂器奇書 skills is still required by later snapshot and
threat-analysis work, but it does not change the confirmed target ID or this
player baseline.

No save or game-owned data was modified during this investigation.
