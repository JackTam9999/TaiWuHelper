# Versioned combat-effect catalog

## Purpose

The catalog is the Domain boundary between raw, version-specific Taiwu effect
records and mechanics the helper is allowed to reason about. It is deliberately
small: only effects required for the golden anti-magic scenario are verified.
It is not a copy of the game's configuration.

The catalog is descriptive and read-only. It does not invoke an effect, alter a
practice direction, equip a skill, write a save, or control the game.

## Version boundary

`CombatEffectCatalog.GameDataVersion` is an exact identity, not a compatible
version range. A lookup must provide:

- the observed GameData version;
- skill ID;
- Direct or Reverse direction; and
- raw effect ID.

All four values must match before typed mechanics are returned. A different
GameData version yields `VersionMismatch`; a changed effect ID yields
`EffectIdMismatch`. This prevents verified behavior from silently surviving a
game update that may have changed it.

## Evidence retained

Every `CombatEffectCatalogEntry` retains:

- skill ID and localized skill name;
- Direct or Reverse practice direction;
- raw effect ID;
- exact source text for that effect;
- the local configuration source key; and
- zero or more typed `CombatEffectMechanic` values.

An entry with no typed mechanics resolves as `Unrecognized` while retaining its
raw evidence. A raw effect absent from the catalog also resolves as
`Unrecognized` and keeps its observed identity in the result. Neither case is
matched by name, nearby ID, or inferred similarity.

## Golden anti-magic scope

The verified catalog targets GameData product version
`1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a` and contains these selected
records:

| Skill ID | Skill | Direction | Effect ID | Typed mechanic |
|---:|---|---|---:|---|
| 604 | 金猊鎮魔刀 | Direct | 338 | Suppress enemy Reverse practice |
| 604 | 金猊鎮魔刀 | Reverse | 1064 | Suppress enemy Direct practice |
| 686 | 老君拂塵功 | Direct | 696 | Remove own injury marks |
| 686 | 老君拂塵功 | Reverse | 1422 | Remove own hindrance marks |
| 134 | 萬花聽雨式 | Direct | 247 | Extend enemy mind-resonance duration |
| 134 | 萬花聽雨式 | Reverse | 973 | Shorten own mind-resonance duration |
| 267 | 墨玉功 | Direct | 165 | Shorten own distraction-mark duration |
| 267 | 墨玉功 | Reverse | 891 | Extend enemy distraction-mark duration |
| 624 | 伏龍刀法 | Direct | 508 | Increase own attack-skill power |
| 624 | 伏龍刀法 | Reverse | 1234 | Reduce enemy attack-skill power |
| 611 | 鬼庖丁刀法 | Direct | 439 | Transfer own hindrance marks |
| 611 | 鬼庖丁刀法 | Reverse | 1165 | Transfer own hindrance marks |
| 291 | 七輪感應法 | Reverse | 915 | Amplify enemy damage states; drain a random true-Qi type |

Direction is part of the catalog key. Similar-looking Direct and Reverse
effects are never collapsed, and common typed mechanics do not imply identical
conditions or source text.

## Local evidence procedure

On 2026-07-30:

1. The installed `GameData.dll` product version was read from file metadata.
2. Skill IDs were selected from the local `CombatSkill.ref.txt` mapping.
3. Direct and Reverse effect IDs were read through the existing read-only
   snapshot inspection.
4. Exact descriptions were selected by effect ID from
   `Language_CNH/SpecialEffect_language.txt`.
5. The inspection compared the save fingerprint before and after reading and
   reported no change.

Only the 13 selected descriptions and their source keys are retained in Domain
code. No complete local configuration file, GameData binary, save content,
generated report, machine-specific path, or file fingerprint is committed.
