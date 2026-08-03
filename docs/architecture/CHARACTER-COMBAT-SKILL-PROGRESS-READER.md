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

Before opening the archive, the adapter checks a helper-owned structured SQLite
cache. A matching cache entry bypasses GameData archive loading. Cache misses
retain the full before/after SHA-256 verification described above.

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
| Breakthrough | The E2-010 mapper combines the decoded detail collection, activation state, `CanBreakout`, completed breakthrough, achievable directions, and the Direct/Reverse directions completed in Taiwu's saved breakthrough presets. |
| Active direction | Derived only from the currently selected supported activation state with completed breakthrough; it remains separate from completed inactive preset directions. |
| Attainment mastery | Unavailable because the save rule for the player-facing `已大成` label remains unverified. |
| Simplified | `ExtraDomain.IsCombatSkillMasteredByCharacter`; deliberately not labeled attainment mastery. |
| Activated | Whether any supported activation-state page bit is active. |
| Equipped | Membership in `CombatSkillEquipment.GetValidSkills`. |

The version-selected E2-010 decoder emits all 15 verified detail identities in
clockwise wheel order. Read and active states remain independent; exact missing
details and aggregate completeness are derived by the Domain model. Labels are
read from the selected installed UI language resource with fingerprint
provenance.

## Snapshot metadata and failures

Every available result carries one immutable metadata value containing the save
SHA-256, UTC read time, detected GameData version, and sanitized typed warnings.
Every progress entry must use that same snapshot identity and character ID.

Missing configuration or file, unsupported GameData, and safe read failure are
separate typed states. Failure messages omit the configured local path. Invalid
proficiency, reading, or activation values become unavailable fields plus
warnings rather than guessed defaults.

## Structured local cache

The derived raw progress snapshot is stored separately from the static skill
catalogue at
`%LOCALAPPDATA%\TaiWuHelper\save-cache\character-combat-skill-progress.db`.
It uses normalized rows for snapshot metadata, characters, and combat-skill
fields; it is not a serialized UI-result blob. The configured save path is
represented only by an opaque SHA-256 path key.

Each cached skill row stores the current activation mask separately from two
boolean preset facts: completed Direct breakthrough and completed Reverse
breakthrough. These facts are derived only from successful saved breakthrough
plates; an incomplete preset containing five normal pages without an outline
does not count as completed.

A hit requires the same file length and UTC modification time, GameData
version, mapping version, and requested character. The persisted save SHA-256
continues to identify all reconstructed Domain values. A changed save, game
version, or mapping version misses the cache and atomically replaces stale
derived rows after a verified read. Language labels are reapplied from the
current installed language source, so changing UI language does not reload the
save.

Cache read or write failure falls back to the read-only source path and never
blocks a valid save read. The cache path is helper-owned, cannot overlap the
game or save directories, and is guarded against reparse-point traversal. The
save, game files, running process, and in-game state remain unchanged.

Information-level timing logs separate label loading, cache lookup, archive
work, cache storage, file revision capture, both full fingerprints, GameData
archive loading, and projection. These timings make cold misses and persistent
cache hits directly distinguishable.

## Verification status

Pure mapping tests cover independent facts, missing proficiency, zero-state
learned skills, immediate Direct breakthrough, completed Reverse breakthrough,
and invalid values. SQLite tests cover structured round trips, revision,
GameData and mapping-version misses, atomic snapshot replacement, and multiple
characters. Architecture tests require typed GameData access, restrict
persistence to named helper-owned stores, and continue to reject archive or
game writes.

The opt-in golden integration assertion passed on 2026-08-02 against save
fingerprint
`77D88A43934E6369F9475AA3742B3161C79A2E9E749BCA6258A2A91391EA0673`.
Two reads produced the same stable 501-entry overlay and verified representative
learned zero-state, Direct, Reverse, activated, equipped, and ready-to-break-out
states. Every guarded save and game dependency had the same fingerprint before
and after the test.
