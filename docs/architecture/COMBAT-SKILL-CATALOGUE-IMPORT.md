# Read-only bilingual combat-skill catalogue import

## Scope and source boundary

E2-006 implements `ICombatSkillDefinitionSource` with the internal
`TaiwuCombatSkillDefinitionSource` adapter. It imports static configuration
only; it does not open a save, load an archive, call a combat calculation, or
return a GameData object.

The Infrastructure-owned installation locator uses the trusted
`TAIWU_GAME_DIRECTORY` environment setting when present, otherwise the loaded
game assembly location or the standard Windows Steam installation. No
Application request or HTTP model contains a source path. The only accepted
source filenames are:

- `Backend/GameData.Shared.dll`, which contains the imported combat-skill
  configuration;
- `Language_CNH/CombatSkill_language.txt`;
- `Language_EN/CombatSkill_language.txt`;
- `Language_CNH/SpecialEffect_language.txt`;
- `Language_EN/SpecialEffect_language.txt`;
- `Language_CNH/LegendaryBookSlot_language.txt`;
- `Language_EN/LegendaryBookSlot_language.txt`.

The adapter fingerprints all seven files with explicitly read-only streams
before import and again afterward. A changed source invalidates the entire
result. It also requires the installed configuration assembly's version and
hash to match the loaded read-only adapter assembly, so a game update cannot
silently be interpreted with stale compiled contracts.

`Config.CombatSkill.Init()` initializes the static configuration table when it
has not already been initialized. The importer then uses only configured
fields. It does not invoke character, combat, direction, effectiveness,
damage, requirement-evaluation, or other runtime-only calculations.

## Deterministic import

Configuration keys are enumerated in stable numeric order. The configuration
reader immediately copies every record into an internal primitive/immutable
source value; GameData objects do not cross the adapter boundary. A record
read or mapping failure produces a stable diagnostic keyed by
`combat-skill:{id}` and does not hide successfully imported records.

Traditional Chinese and English files are parsed independently with
case-sensitive keys and first-value-wins duplicate handling. Missing text in
one language does not copy or invent text from the other language. Application
performs display fallback later. Names and descriptions retain separate
language-resource provenance.

The configured `Desc` text and the normal direct/reverse effect text at
`Desc_{effectId}_0` are imported as `RawCombatSkillDescription` values with
`IsVerifiedMechanic == false`. They are for display only and cannot feed a
recommendation rule without a separate verified typed mechanic. Detailed
effect variants are intentionally not substituted for the normal in-game
descriptions.

The current in-game legendary-book slot resources are mapped separately from
the encyclopedia assets. Each non-empty `Name_{effectId}` or
`Desc_{effectId}` value becomes a bilingual `LegendaryBookEffectDefinition`
with field-level provenance. This preserves the current UI text; for example,
blade effect `83` is `解破`, and its current description does not inherit the
obsolete 50% cast-time penalty still present in the encyclopedia TSV.

## Faction display profiles

The faction filter uses a separate read-only
`ICombatSkillFactionProfileSource`. It reads `Config.Organization` from the
same installed, version-checked `GameData.Shared` assembly and immediately
projects primitive values into Domain-owned records. No GameData object crosses
the Infrastructure boundary, and these presentation profiles are not persisted
in the helper catalogue database.

| Installed field | Display meaning | Unknown handling |
|---|---|---|
| `Organization.FiveElementsType` | Main inner-power element; colors the active-language faction initial and label | Neutral color plus `Unavailable` text |
| `Organization.MainMorality`, normalized through `BehaviorType.GetBehaviorType` | Primary alignment; colors the circular outer ring | White ring plus `Unavailable` text |

The UI always writes the localized element and alignment beside the mark, so
neither meaning depends on color perception. Installed language colors are
copied as CSS values only; no icon, texture, or proprietary artwork is read or
redistributed.

## Field mapping

| Installed field | Domain field | Invalid or absent handling |
|---|---|---|
| Configuration key | `SkillId` | Record diagnostic if it cannot be read or represented |
| `Name` localization key | Independent Traditional Chinese / English names | Language remains absent |
| `Type` | `CombatSkillDiscipline` | `Unsupported` raw value |
| `Grade` | `CombatSkillGrade` | `Unsupported` outside 0–8 |
| `SectId` | `CombatSkillFactionId` | `Unsupported` if negative |
| `FiveElements` | `CombatSkillElement` | `Unsupported` raw value |
| `EquipType` | `CombatSkillEquipmentType` | `Unsupported` raw value |
| `GridCost` | `CombatSkillGridCost` | `Unavailable` if non-positive |
| `SpecificGrids`, `GenericGrid` | `SkillSlotContribution` | `Unsupported` unless four specific and all values non-negative |
| `UsingRequirement` | Stable property/slot requirement ID and required value | Negative value is `Unsupported` |
| `PrepareTotalProgress` | Preparation progress | Negative value is `Unsupported` |
| `BreathStanceTotalCost` | Breath/stance cost | Negative value is `Unsupported` |
| `CastSpeed` | Cast speed | Negative value is `Unsupported` |
| `DirectEffectID`, `ReverseEffectID` | Typed effect references | Non-positive value is `Unavailable` |
| `SpecialEffect Desc_{effectId}_0` | Display-only localized direct/reverse descriptions | Language remains absent |
| No verified neutral-effect field | Neutral effect reference | Explicitly `Unavailable` |
| `Desc` localization key | Display-only localized effect description | Language remains absent |

Requirement IDs include their configured property ID and source slot. This
preserves repeated properties deterministically without claiming a runtime
requirement interpretation that has not been verified.

## Golden local inventory

The opt-in test was run against GameData product version
`1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a` on 2026-08-02. Source hashes
matched before and after; the GameData configuration hash is intentionally not
committed. The imported inventory was:

| Inventory | Result |
|---|---:|
| Configuration records / imported definitions | 946 / 946 |
| Traditional Chinese names | 946 |
| English names | 946 |
| Category, grade, faction, element, equipment, and grid cost available | 946 each |
| Slot contributions | 940 available, 6 unsupported |
| Preparation, breath/stance, and cast-speed fields available | 946 each |
| Direct effects | 945 available, 1 unavailable |
| Reverse effects | 946 available |
| Neutral effects | 946 unavailable by verified schema policy |
| Typed requirement entries | 3,724 available, 0 unsupported |
| Localized display-only base descriptions | 1,512 |
| Legendary-book slot effects / localized rows | 84 / 168 |
| Import diagnostics | 2 warnings, 0 errors |

Both warnings are deterministic `LANGUAGE_VALUE_MISSING` diagnostics for
trailing keys without a following value line. They do not affect any of the
946 names in either language.

The importer returned the same source identity and stable skill-ID order on
two consecutive reads. Golden skill `456` resolved independently to
`黑血蠱降` and `Corruptive Gu Infection`. The importer-version-3 verification
on 2026-08-04 also round-tripped all 84 legendary-book effects through SQLite;
effect `83` resolved to the current `解破` text without the obsolete penalty.

The inventory above records importer version 1, before direct/reverse effect
descriptions were added. Importer version 2 fingerprints both SpecialEffect
language files and imports those extra display-only rows. Importer version 3
also fingerprints both LegendaryBookSlot language files and imports their
current names and descriptions.
