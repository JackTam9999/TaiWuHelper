# Combat-skill progress semantics

| Field | Value |
|---|---|
| Status | Verified for the detected version |
| GameData product version | `1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a` |
| Scenario | [E2-001 golden skill atlas](../scenarios/E2-001-golden-skill-atlas.md) |
| Backlog item | [E2-002](../roadmap/epic-002/BACKLOG.md#e2-002--verify-combat-skill-progression-and-study-detail-semantics) |

## Decision

The catalogue and character atlas must preserve learned membership,
cultivation proficiency, displayed power, read pages, active pages,
breakthrough readiness, current successful breakthrough, practice direction,
equipment, and simplification as explicitly named facts. For this version the
player-facing `已大成` label is exactly the current-Taiwu successful-
breakthrough predicate; it is not martial-art simplification.

The mapping in this document applies only to the detected GameData product
version. A different version must be re-inspected before these constants or
labels are reused.

## Source map

| Player-facing fact | Authoritative installed API or field | Standalone status |
|---|---|---|
| Learned | `CombatSkillDomain.GetLearnedCombatSkillByType` and membership in `GetCharCombatSkills` | Available |
| Cultivation proficiency | `ExtraDomain.TryGetElement_CombatSkillProficiencies(CombatSkillKey, out int)` | Available only when the key exists |
| Centre percentage / current power | `CombatSkillDisplayData.Power`; the UI appends `"%"` directly | Typed, but unavailable from a standalone save because calculation requires the live special-effect context |
| Requirements-layer power cap | `CombatSkillDisplayData.MaxPower` / `CombatSkill.GetMaxPower()` | Typed, but unavailable from a standalone save for the same reason |
| Pages read | `CombatSkill.GetReadingState()` plus `CombatSkillStateHelper.IsPageRead` | Available |
| Pages active | `CombatSkill.GetActivationState()` plus `CombatSkillStateHelper.IsPageActive` | Available |
| Reading prerequisite | `CombatSkill.CanBreakout()` / `IsReadNormalPagesMeetConditionOfBreakout` | Available |
| Completed breakthrough and current-Taiwu `已大成` | `CombatSkillStateHelper.IsBrokenOut(activationState)`, equivalent to `(activationState & 0x001F) != 0`; agrees with `CombatSkillDomain.GetBreakSuccess` | Available |
| Active direction | `GetCombatSkillDirection(activationState)`, only after completed breakthrough | Available when the activation bitfield is supported |
| Completed preset directions | For Taiwu, `TaiwuDomain.GetCombatSkillBreakPreset(skillId).Presets`; only successful `BreakPlate.SelectedPages` values that satisfy `IsBrokenOut` contribute Direct or Reverse | Available independently of the active preset |
| Equipped | `CombatSkillEquipment` membership | Available and independent of activation pages |
| Simplified | `ExtraDomain.GetCharacterMasteredCombatSkills` membership | Available; this is not the `已大成` attainment label |

The installed method named `GetLearnedCombatSkillByType` is the reason the
Domain term is **learned**, not guessed from zero-valued state. In the E2-001
snapshot, its type lists contain the same 484 IDs as `GetCharCombatSkills`.
All six golden IDs are learned, including skill `498`, whose reading and
activation bitfields are both zero.

## Proficiency and displayed power

Cultivation proficiency and displayed power are different measurements. The
centre percentage is not a proficiency percentage and is not calculated as
current divided by maximum.

- Cultivation proficiency is a keyed `Int32`. The installed
  `ChangeCombatSkillProficiency` path clamps changes to the inclusive range
  `0..999999999`; `GlobalConfig.MaxProficiency` is `999999999`.
- A missing proficiency key is **unavailable**, not zero. The E2-001 disk
  snapshot contains no proficiency key for the golden skills, while the newer
  game screen visibly reports `50%` for skill `456`.
- The study-screen UI renders `CombatSkillDisplayData.Power + "%"` without a
  division. A visible `120%` means current final power `120`.
- `Power` and `MaxPower` are runtime-derived `Int16` display values. `MaxPower`
  limits the requirements-derived layer; later fixed and percentage modifiers
  can make final `Power` exceed it. The Domain therefore deliberately permits
  values such as current `113` and maximum `100`.
- The requirements layer applies the adjusted requirement multiplier to each
  required attribute with integer truncation, caps each result at `MaxPower`,
  optionally replaces the lowest result with combat-practice performance, and
  averages the resulting values using integer division.
- Final power then applies fixed power additions, general percentage changes,
  and final increase/decrease multipliers in order. The normal result is
  clamped to the game's final bounds; a combat-sealed skill can instead display
  zero, and verified special effects can substitute another skill's power.
- Simplification changes the requirements multiplier: an original two-slot
  skill adds `150%` (normally `100%` to `250%`), while an original three-slot
  skill adds `100%` (normally `100%` to `200%`). Legendary-book placement adds
  `50%`. These are power inputs, not attainment conditions.
- The private `_power` and `_maxPower` fields are lazy caches. Their observed
  zero values before calculation are not player progress and must not be
  exposed as facts.
- Both installed calculation entry points enter
  `SpecialEffectDomain.ModifyData`. The standalone archive does not reconstruct
  that live context, so the reader exposes current and maximum power as typed
  unavailable values instead of invoking the methods or reproducing a partial
  formula.

Consequently, power does not determine learned status, `已大成`, or
simplification.

## The fifteen page details

Both `readingState` and `activationState` are unsigned fifteen-bit masks.
`CompleteReadingState` is `32767` (`0x7FFF`). Bit 15 and all higher bits are
unsupported for this version and make the mapped result unavailable.

Each detail has two independent booleans:

- `IsRead`: the bit is set in `readingState`;
- `IsActive`: the bit is set in `activationState`.

The UI should use labels such as **read** and **active/selected**. It must not
call an orange wheel sector “studied” merely because it is active.

| Stable ID | Internal bit | Mask | Group | Localization key | 中文 | English |
|---|---:|---:|---|---|---|---|
| `outline-0` | 0 | `0x0001` | Outline | `LK_CombatSkill_First_Page_Type_0` | 承 | Resilience |
| `outline-1` | 1 | `0x0002` | Outline | `LK_CombatSkill_First_Page_Type_1` | 合 | Unity |
| `outline-2` | 2 | `0x0004` | Outline | `LK_CombatSkill_First_Page_Type_2` | 解 | Realization |
| `outline-3` | 3 | `0x0008` | Outline | `LK_CombatSkill_First_Page_Type_3` | 異 | Peculiar |
| `outline-4` | 4 | `0x0010` | Outline | `LK_CombatSkill_First_Page_Type_4` | 獨 | Unique |
| `direct-0` | 5 | `0x0020` | Direct | `LK_CombatSkill_Direct_Page_0` | 修 | Might |
| `direct-1` | 6 | `0x0040` | Direct | `LK_CombatSkill_Direct_Page_1` | 思 | Aptitude |
| `direct-2` | 7 | `0x0080` | Direct | `LK_CombatSkill_Direct_Page_2` | 源 | Beginnings |
| `direct-3` | 8 | `0x0100` | Direct | `LK_CombatSkill_Direct_Page_3` | 參 | Integrity |
| `direct-4` | 9 | `0x0200` | Direct | `LK_CombatSkill_Direct_Page_4` | 藏 | Possession |
| `reverse-0` | 10 | `0x0400` | Reverse | `LK_CombatSkill_Reverse_Page_0` | 用 | Efficiency |
| `reverse-1` | 11 | `0x0800` | Reverse | `LK_CombatSkill_Reverse_Page_1` | 奇 | Eccentricity |
| `reverse-2` | 12 | `0x1000` | Reverse | `LK_CombatSkill_Reverse_Page_2` | 巧 | Authenticity |
| `reverse-3` | 13 | `0x2000` | Reverse | `LK_CombatSkill_Reverse_Page_3` | 化 | Persistence |
| `reverse-4` | 14 | `0x4000` | Reverse | `LK_CombatSkill_Reverse_Page_4` | 絕 | Supreme |

The stable logical order is internal bit `0..14`. The verified clockwise wheel
order, beginning at twelve o'clock, is:

`outline-2`, `outline-3`, `outline-4`, `direct-0`, `direct-1`, `direct-2`,
`direct-3`, `direct-4`, `reverse-4`, `reverse-3`, `reverse-2`, `reverse-1`,
`reverse-0`, `outline-0`, `outline-1`.

The five outline pages are alternative first-page types. Direct and Reverse
normal pages are paired alternatives by group index. A read bit says that
page detail is known; activation records the selected breakthrough layout.
The helper preserves any unusual combination instead of silently normalizing
it.

For skill `456`, the E2-001 save has all fifteen read bits but only the five
Reverse activation bits (`31744`, or `0x7C00`). Those five bits match the five
orange sectors `用`, `奇`, `巧`, `化`, and `絕` in the local screen. This proves
that the wheel highlight is activation state, not a complete-study indicator.

## Breakthrough rules

The installed version uses three distinct stages:

1. The reading prerequisite is met after five normal pages are read in total.
   `CanBreakout()` continues to return true after breakthrough, so it does not
   mean “can break through now.”
2. Before breakthrough, a direction is achievable when at least three of its
   five normal pages are read. A `3/2` split enables only the majority
   direction; enough pages in both groups enables both directions.
3. Completed breakthrough comes from `activationState`. The golden data proves
   that five active normal pages without an active outline page are not
   complete, while an outline page plus five active normal pages is complete.
   The active direction is the majority of those normal pages.

Immediate readiness is therefore:

`not completed` + `five normal pages read` + `at least one achievable direction`.

Taiwu's three breakthrough presets are an additional, independent dimension.
`activationState` still describes only the currently selected preset. The
helper also unions the direction of each successful saved breakthrough plate.
It does not treat a preset as completed merely because five Direct or Reverse
normal page bits are present: the selected state must include an outline and
pass `IsBrokenOut`. The atlas therefore renders a completed skill as follows:

- the current direction uses its Direct/Reverse colour and active state;
- an inactive direction completed in another preset uses its normal colour;
- an opposite direction with no completed preset remains visible in grey.

Golden examples:

| Skill | Reading / activation conclusion |
|---:|---|
| `40` | Completed; Reverse; not immediately breakable even though `CanBreakout()` is true |
| `41` | Completed; Direct; not immediately breakable even though `CanBreakout()` is true |
| `456` | Not completed; all pages read; both directions available; five Reverse pages active without an outline |
| `498` | Learned, no pages read or active, not ready |
| `686` | Not completed; `3` Direct and `2` Reverse normal pages read; Direct is immediately available |

## `已大成`, breakthrough, and simplification

The newer skill-list screen uses `已大成`, whose English localization is
“Mastered.” For the current Taiwu, its precise installed condition is:

`has skill data && (activationState & 0x001F) != 0`.

The low five bits are the five alternative outline/玄關 positions.
`CombatSkillStateHelper.IsBrokenOut` implements the same predicate. In normal
play this means a successful breakthrough selection containing one outline and
five normal pages has been saved. It does not require all fifteen pages,
100-percent cultivation, maximum power, simplification, equipment, or every
breakthrough bonus. Clearing the outline selection returns the label to the
non-mastered state, and a mastered skill may be broken through again.

The atlas retains `AttainmentMastered` as a named presentation fact, but for
this version its available value must equal current
`Breakthrough.IsBrokenOut`. For an explicitly requested non-Taiwu character it
remains unavailable because this player-facing predicate is scoped to the
current Taiwu.

`GetCharacterMasteredCombatSkills` / `IsCombatSkillMasteredByCharacter` is the
separate martial-art simplification (`功法精解`) list. Simplification reduces a
two-slot skill to one slot or a three-slot skill to two, while substantially
raising its performance requirements. It is reversible, preset-specific, and
does not require `已大成`. The two flags must never be substituted for one
another.

## Failure behavior

- Missing proficiency keys remain unavailable.
- Reading or activation values outside `0x0000..0x7FFF` make all mapped page
  details unavailable.
- An unknown direction value remains unavailable.
- A claimed reading prerequisite that disagrees with the five-page rule is an
  unavailable inconsistent observation.
- Current-Taiwu `已大成` is available only when the activation state is
  supported; it is unavailable for another character or unsupported state.
- Current and maximum power remain unavailable in standalone mode because the
  verified calculation needs live special-effect state. No proficiency
  percentage is exposed: the visible centre percentage is current power.
- Source version or fingerprint changes never reuse golden expectations.

## Verification

- `CombatSnapshotMappingTests` cover every page group, stable key, bit order,
  wheel order, independent read/active values, unknown states, the five-page
  prerequisite, Direct/Reverse availability, and completed-breakthrough
  precedence.
- `CombatSkillProgressMappingTests` prove that current-Taiwu attainment agrees
  with `IsBrokenOut`, remains distinct from simplification, permits final power
  above `MaxPower`, and preserves unavailable live-derived power.
- The E2-001 opt-in integration assertion is guarded by both
  `TAIWU_INTEGRATION_SAVE_PATH` and the recorded save SHA-256. A newer save is
  skipped rather than compared with stale raw values.
- The configured save advanced after E2-001 (its learned collection changed),
  and the guard correctly rejected the old expected snapshot without exposing
  the new path or fingerprint.
