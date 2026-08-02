# Combat-skill progress semantics

| Field | Value |
|---|---|
| Status | Verified for the detected version |
| GameData product version | `1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a` |
| Scenario | [E2-001 golden skill atlas](../scenarios/E2-001-golden-skill-atlas.md) |
| Backlog item | [E2-002](../roadmap/epic-002/BACKLOG.md#e2-002--verify-combat-skill-progression-and-study-detail-semantics) |

## Decision

The catalogue and character atlas must preserve learned membership,
cultivation proficiency, runtime power, read pages, active pages,
breakthrough readiness, completed breakthrough, practice direction,
equipment, and simplification as separate facts. No one field is a safe proxy
for another.

The mapping in this document applies only to the detected GameData product
version. A different version must be re-inspected before these constants or
labels are reused.

## Source map

| Player-facing fact | Authoritative installed API or field | Standalone status |
|---|---|---|
| Learned | `CombatSkillDomain.GetLearnedCombatSkillByType` and membership in `GetCharCombatSkills` | Available |
| Cultivation proficiency | `ExtraDomain.TryGetElement_CombatSkillProficiencies(CombatSkillKey, out int)` | Available only when the key exists |
| Runtime power / maximum power | `CombatSkill.GetPower()` / `GetMaxPower()` | Unavailable: calculation requires the live special-effect context |
| Pages read | `CombatSkill.GetReadingState()` plus `CombatSkillStateHelper.IsPageRead` | Available |
| Pages active | `CombatSkill.GetActivationState()` plus `CombatSkillStateHelper.IsPageActive` | Available |
| Reading prerequisite | `CombatSkill.CanBreakout()` / `IsReadNormalPagesMeetConditionOfBreakout` | Available |
| Completed breakthrough | `CombatSkillStateHelper.IsBrokenOut(activationState)`; agrees with `CombatSkillDomain.GetBreakSuccess` | Available |
| Active direction | `GetCombatSkillDirection(activationState)`, only after completed breakthrough | Available when the activation bitfield is supported |
| Equipped | `CombatSkillEquipment` membership | Available and independent of activation pages |
| Simplified | `ExtraDomain.GetCharacterMasteredCombatSkills` membership | Available; this is not the `已大成` attainment label |

The installed method named `GetLearnedCombatSkillByType` is the reason the
Domain term is **learned**, not guessed from zero-valued state. In the E2-001
snapshot, its type lists contain the same 484 IDs as `GetCharCombatSkills`.
All six golden IDs are learned, including skill `498`, whose reading and
activation bitfields are both zero.

## Proficiency and power

Cultivation proficiency and runtime power are different measurements.

- Cultivation proficiency is a keyed `Int32`. The installed
  `ChangeCombatSkillProficiency` path clamps changes to the inclusive range
  `0..999999999`; `GlobalConfig.MaxProficiency` is `999999999`.
- A missing proficiency key is **unavailable**, not zero. The E2-001 disk
  snapshot contains no proficiency key for the golden skills, while the newer
  game screen visibly reports `50%` for skill `456`.
- The exact conversion from persisted cultivation proficiency to that visible
  percentage could not be verified from the standalone archive and must remain
  unavailable. The atlas must not divide, clamp, or infer it.
- `Power` and `MaxPower` are runtime-derived `Int16` display values, with a
  technical representation range of `-32768..32767`. A narrower player-valid
  range, the installed live calculation, and any version-dependent upper bound
  are unavailable to the helper.
- The private `_power` and `_maxPower` fields are lazy caches. Their observed
  zero values before calculation are not player progress and must not be
  exposed as facts.

Consequently, neither power nor reading-page counts determine learned status,
cultivation percentage, `已大成`, or simplification.

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
“Mastered.” It must not be mapped to either of these unrelated facts:

- completed breakthrough (`BreakSuccess` / `IsBrokenOut`);
- `GetCharacterMasteredCombatSkills`, whose own UI describes martial-art
  simplification (`功法精解`) and a one-slot cost reduction.

The exact persisted rule that changes the attainment label from `已取得` to
`已大成` is unavailable in the E2-001 disk snapshot. Until a live-safe source
is verified, the atlas may show the manually observed label with provenance,
but a save-derived attainment stage must remain unavailable. E2-004 must name
the simplification field explicitly rather than reusing `Mastered`.

## Failure behavior

- Missing proficiency keys remain unavailable.
- Reading or activation values outside `0x0000..0x7FFF` make all mapped page
  details unavailable.
- An unknown direction value remains unavailable.
- A claimed reading prerequisite that disagrees with the five-page rule is an
  unavailable inconsistent observation.
- Runtime power, maximum power, the visible percentage conversion, and the
  `已大成` save rule remain unavailable in standalone mode.
- Source version or fingerprint changes never reuse golden expectations.

## Verification

- `CombatSnapshotMappingTests` cover every page group, stable key, bit order,
  wheel order, independent read/active values, unknown states, the five-page
  prerequisite, Direct/Reverse availability, and completed-breakthrough
  precedence.
- The E2-001 opt-in integration assertion is guarded by both
  `TAIWU_INTEGRATION_SAVE_PATH` and the recorded save SHA-256. A newer save is
  skipped rather than compared with stale raw values.
- The configured save advanced after E2-001 (its learned collection changed),
  and the guard correctly rejected the old expected snapshot without exposing
  the new path or fingerprint.
