# Read-only character combat-skill progress

## Boundary

`TaiwuCharacterCombatSkillProgressReader` implements the Application-owned
`ICharacterCombatSkillProgressReader` port. Its request contains only a
character ID. The adapter obtains the save from the trusted host configuration
key `SaveGames:DefaultSaveFilePath`; no HTTP request or Application contract can
supply a filesystem path.

The adapter reuses `TaiwuArchiveReadSession`, including its process-wide reader
lock, read-only archive loader, before/after SHA-256 capture, and rejection of a
save that changes during the read. It projects typed GameData values directly
and never consumes `SaveGameReport`, legacy report lines, or diagnostic text.

The verified mapping is restricted to the main GameData domain assembly's
product version
`1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a`. Other versions return the
typed `UnsupportedVersion` result before the save is opened.

## Mapped progress

For every entry in `CombatSkillDomain.GetCharCombatSkills`, ordered by stable
skill ID, the adapter maps these facts independently:

| Domain field | Read-only source and behavior |
|---|---|
| `Learned` | Membership in the verified learned combat-skill collection; every projected collection entry is `true`. Zero reading/activation state does not mean unlearned. |
| Proficiency current | `ExtraDomain.TryGetElement_CombatSkillProficiencies(CombatSkillKey, out int)`; a missing key is unavailable and an out-of-range value becomes unavailable with a warning. |
| Proficiency maximum | Verified GameData limit `999999999`, with E2-002 rule provenance. |
| Proficiency percentage | Unavailable because the persisted-to-visible conversion remains unverified. |
| Breakthrough | The existing E2-002 mapper combines reading state, activation state, `CanBreakout`, completed breakthrough, and achievable directions. |
| Active direction | Derived only from a supported activation state with completed breakthrough. |
| Attainment mastery | Unavailable because the save rule for the player-facing `已大成` label remains unverified. |
| Simplified | `ExtraDomain.IsCombatSkillMasteredByCharacter`; deliberately not labeled attainment mastery. |
| Activated | Whether any supported activation-state page bit is active. |
| Equipped | Membership in `CombatSkillEquipment.GetValidSkills`. |

Individual study-detail values remain empty with an explicit metadata warning
until E2-010 applies the version-selected detail decoder. This prevents E2-009
from creating a second partial decoder while retaining all raw inputs needed by
the already verified breakthrough/activation mapping.

## Snapshot metadata and failures

Every available result carries one immutable metadata value containing the save
SHA-256, UTC read time, detected GameData version, and sanitized typed warnings.
Every progress entry must use that same snapshot identity and character ID.

Missing configuration or file, unsupported GameData, and safe read failure are
separate typed states. Failure messages omit the configured local path. Invalid
proficiency, reading, or activation values become unavailable fields plus
warnings rather than guessed defaults.

The overlay remains in memory and is never inserted into the static SQLite
catalogue. The save, game files, running process, and in-game state remain
unchanged.

## Verification status

Pure mapping tests cover independent facts, missing proficiency, zero-state
learned skills, immediate Direct breakthrough, completed Reverse breakthrough,
and invalid values. Architecture tests require typed GameData access and reject
legacy report parsing or file writes.

The opt-in golden integration assertion passed on 2026-08-02 against save
fingerprint
`77D88A43934E6369F9475AA3742B3161C79A2E9E749BCA6258A2A91391EA0673`.
Two reads produced the same stable 501-entry overlay and verified representative
learned zero-state, Direct, Reverse, activated, equipped, and ready-to-break-out
states. Every guarded save and game dependency had the same fingerprint before
and after the test.
