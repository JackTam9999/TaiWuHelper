# E2-001: Golden skill-catalogue and character-atlas scenario

| Field | Value |
|---|---|
| Status | Complete |
| Epic | [EPIC-002](../roadmap/epic-002/EPIC.md) |
| Backlog item | [E2-001](../roadmap/epic-002/BACKLOG.md#e2-001--define-the-golden-catalogue-and-character-progress-scenario) |
| Sanitized record | [E2-001-golden-skill-atlas-metadata.json](./evidence/E2-001-golden-skill-atlas-metadata.json) |
| Observation date | 2026-08-02 |

## Purpose

Define one repeatable, read-only scenario for building and verifying the local
combat-skill catalogue and current-character overlay. The scenario intentionally
contains both confirmed progress and unresolved source conflicts so the atlas
must distinguish `false`, stale, unknown, and unavailable values.

No save, game binary, language pack, or screenshot is committed. The sanitized
metadata contains source identities, hashes, timestamps, stable skill IDs, and
minimal observations only.

## Source identity

| Source | Identity used by the scenario |
|---|---|
| GameData | Product version `1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a`; assembly length `8,891,904` bytes |
| Traditional Chinese combat-skill language pack | Length `204,654`; SHA-256 `9932B589389DF643981A3CB6E6E8DFFD9B7B1FC814BBA30ACD34C6C18CF1CFF4` |
| English combat-skill language pack | Length `267,598`; SHA-256 `F89C3B8AD7DEFE0E6E587EA4F1E109E983817B3F609C34946379FC82314D5229` |
| Configured save | File name `local.sav`; length `213,937,298`; SHA-256 `C9EB00A368A6CE25B2D816DAE941AFAC67B6217ED561FF7563F613C3B297CECA` |

The save's last-write time is
`2026-08-02T11:51:15.0516489Z`. Both new screen observations are later, so
their visible character state takes precedence when it conflicts with the
persisted snapshot. The installed GameData product version matches the version
already used by Epic 1.

The GameData binary hash is deliberately not committed. Product version and
length identify the installed mapping boundary without publishing a binary
fingerprint or binary content.

## Local-only screen observations

The screenshots remain outside the repository. Stable aliases below describe
their purpose; SHA-256 values allow the local evidence to be matched without
committing it.

| Local alias | File timestamp (UTC) | SHA-256 | Observation |
|---|---|---|---|
| `E2-001-character-skill-list.png` | `2026-08-02T11:52:19.8465663Z` | `BC016080C3139737C43AAC227F1BFBA5BB504D198822D89FBBBBA3D7F3C43F32` | Category list showing independent `已大成`, `已取得`, Direct, and Reverse labels |
| `E2-001-black-blood-study-detail.png` | `2026-08-02T11:54:11.7478343Z` | `5A8BFC6B3A863D5258C52BBB4BC36960A5C1540F7F4B7D7231B40E7C06572097` | 黑血蠱降 detail screen showing `50%`, a segmented progress indicator, and a fifteen-position detail wheel with five visually highlighted sectors |

The five highlighted labels in the 黑血蠱降 wheel are `用`, `奇`, `巧`, `化`,
and `絕`. This scenario records only that they are visually highlighted. It
does not yet call them studied, selected, available, Direct, or Reverse; E2-002
must verify those semantics.

## Golden skills

Traditional Chinese and English names are resolved from the two installed
language packs by the same stable `Name_<skillId>` key.

| ID | Traditional Chinese | English | Scenario role |
|---:|---|---|---|
| 40 | 封口固氣法 | Sealed Breath | Newer screen shows Reverse and `已大成`; save reports equipped with raw `read=32767`, `active=14881`, and `direction=1` |
| 41 | 十三太保橫練功 | Heroic Stance | Newer screen shows Direct and `已大成`; save reports equipped with raw `read=32767`, `active=996`, and `direction=0` |
| 361 | 大拙手 | The Clumsy Strike | Newer screen shows `已取得`; save reports raw `read=4`, `active=0`, no active direction, and not equipped |
| 456 | 黑血蠱降 | Corruptive Gu Infection | Newer detail screen shows `50%` and partial visual detail state; older save reports raw `read=32767` and `active=31744`, deliberately exercising source conflict |
| 498 | 蠍子勾魂腳 | Scorpion Kick | Configured definition with raw `read=0`, `active=0`, and no direction; whether that means not obtained is intentionally unavailable until E2-002 verifies collection semantics |
| 686 | 老君拂塵功 | Laojun's Whisk Style | Prior verified screen evidence shows breakthrough incomplete; raw `read=9928` allows an immediate Direct breakthrough only, while Reverse remains unavailable |

### Independent progress coverage

| Required fact | Golden evidence |
|---|---|
| Configured definition | All six IDs resolve in both installed language packs |
| Obtained label | 361 is visibly labeled `已取得` |
| Not obtained | Not asserted: the save collection contains zero-state definitions, so E2-002 must verify what membership means before the atlas uses `未取得` |
| Partial progress | 456 visibly shows `50%`; 361 has a sparse raw reading state for comparison |
| Breakthrough-ready | 686 can immediately produce Direct breakthrough from the verified current mapper and prior manual evidence |
| Direct broken-through/mastered | 41 is visibly Direct and `已大成` |
| Reverse broken-through/mastered | 40 is visibly Reverse and `已大成` |
| Equipped or activated | 40 and 41 are equipped in the persisted snapshot; several skills have non-zero raw activation states |
| Partial study details | 456 has five visually highlighted wheel sectors; their meaning remains a required E2-002 result |
| Missing or unsupported | Current and maximum power remain unavailable from the standalone save because the live special-effect context is absent |

## Resolved source distinctions

E2-002 and E2-F06 established the precise relationships; see
[combat-skill progress semantics](../architecture/COMBAT-SKILL-PROGRESS-SEMANTICS.md).
The diagnostic `mastered=False` value is membership in the martial-art
simplification list (`功法精解`), not the `已大成` attainment label.

For 黑血蠱降, `read=32767` means all fifteen page details were read, while
`active=31744` selects the five Reverse details. That activation mask exactly
matches the five orange sectors `用`, `奇`, `巧`, `化`, and `絕` in the newer
screen. The visible `50%` is the final `CombatSkillDisplayData.Power` value,
not a reading ratio or persisted-proficiency conversion. Its historical value
cannot be reconstructed from this older standalone disk snapshot.

The atlas must therefore preserve source freshness, keep current power
separate from proficiency and page state, and retain read and active bits
independently. Zero reading and activation values also do not mean
`未取得`: the installed learned-skill API includes skill `498`.

## Fingerprint preservation

The save and both language packs were hashed before and after the diagnostic
read. Length, last-write time, and SHA-256 were unchanged. The helper opened the
configured save through its existing information-only workflow and did not
write to the game installation, save directory, screenshots, or running game.

| Source | Before | After | Result |
|---|---|---|---|
| Configured save | `C9EB00A368A6CE25B2D816DAE941AFAC67B6217ED561FF7563F613C3B297CECA` | Same | Unchanged |
| Traditional Chinese pack | `9932B589389DF643981A3CB6E6E8DFFD9B7B1FC814BBA30ACD34C6C18CF1CFF4` | Same | Unchanged |
| English pack | `F89C3B8AD7DEFE0E6E587EA4F1E109E983817B3F609C34946379FC82314D5229` | Same | Unchanged |

## Repeat procedure

After catalogue import and rebuild exist:

1. Confirm the installed GameData product version and both language-pack
   identities match this scenario, or record a new versioned scenario.
2. Confirm the configured save hash matches without copying or changing it.
3. Rebuild the helper-owned catalogue through its fixed safe storage boundary.
4. Search for all six stable skill IDs in both languages.
5. Read the current character overlay from the same save snapshot.
6. Compare raw persisted fields with the recorded newer screen observations
   using documented source precedence.
7. Verify every unavailable or conflicting fact remains visible rather than
   becoming a false negative.
8. Re-hash the save and language packs and require byte-for-byte equality.

If any source identity changes, the old expected values are not reused. A new
evidence record must identify the new mapping boundary.

## Repository exclusions

- No `.sav` file or save path is committed.
- No GameData binary or binary hash is committed.
- No complete language pack is committed.
- No screenshot or game artwork is committed.
- No character identifier is required by this scenario.
- The metadata contains only reproducibility values and minimal observed
  progress facts.
