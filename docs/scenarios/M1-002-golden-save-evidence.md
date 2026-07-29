# M1-002: Golden-save evidence metadata

| Field | Value |
|---|---|
| Status | Complete |
| Golden scenario | [M1-001](./M1-001-golden-target-selection.md) |
| Machine-readable record | [M1-002-golden-save-metadata.json](./evidence/M1-002-golden-save-metadata.json) |
| Snapshot time | 2026-07-29T22:26:37.0091432+00:00 |

## Purpose

Record enough non-proprietary source metadata to identify the exact local save
and installed GameData build used by the golden scenario. The record does not
copy, embed, transform, or serialize any save or game-binary content.

## Save source

| Field | Value |
|---|---|
| Configured path | `C:\Program Files (x86)\Steam\steamapps\common\The Scroll Of Taiwu\SaveGames\world_1\local.sav` |
| Length | 208,538,049 bytes |
| Last modified (UTC) | 2026-07-28T22:38:08.0960649Z |
| SHA-256 before capture | `B9E86B80B564035CBE7D15F2C5F297AF3ACDE5470509B0550D930ED91DDF1930` |
| SHA-256 after capture | `B9E86B80B564035CBE7D15F2C5F297AF3ACDE5470509B0550D930ED91DDF1930` |
| Changed during capture | No |

The hash was read before and after collecting file metadata. The identical
values establish that this capture did not modify the save.

## Installed GameData source

| Field | Value |
|---|---|
| Local path | `C:\Program Files (x86)\Steam\steamapps\common\The Scroll Of Taiwu\Backend\GameData.dll` |
| Length | 8,891,904 bytes |
| Last modified (UTC) | 2026-07-25T17:28:51.6662610Z |
| Assembly version | `1.0.0.0` |
| File version | `1.0.0.0` |
| Product version | `1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a` |
| SHA-256 | `32478DF92B2ED6E44CC5524E7F980B99B633059DE971626E117ED78840D6A860` |

The version and hash identify the installed build. The DLL remains in the
local game installation and is not copied into the repository.

## Current-screen observations

Current-screen observations are deliberately separate from the save metadata.
They are player-provided, analysis-only evidence that may be newer than the
disk save:

| Observation | Evidence | Analysis effect |
|---|---|---|
| Populated player loadout | Local-only `M1-001-current-player-loadout.png` | Overrides stale equipped-card membership and final displayed capacities in the helper's analysis snapshot only |
| Empty slot capacities | Local-only `M1-001-empty-capacities.png` | Establishes the `6/2/2/2/2` unmodified capacity baseline |
| Individual 內功 contribution | Local-only `M1-001-inner-power-capacity-example.png` | Establishes its cost and category-slot contributions |

These observations cannot write to the save, game files, running process, or
in-game state. They do not change the recorded save hash or masquerade as data
read from the disk snapshot.

## Repository exclusion

- No `.sav` file is stored in Git.
- No `GameData` or Steam runtime binary is stored in Git.
- `.gitignore` excludes Taiwu `.sav` files and known proprietary runtime
  binary names.
- An architecture test scans the repository source tree and fails if a
  Taiwu save or known proprietary GameData runtime artifact appears outside
  build output.
- The committed JSON contains metadata only and explicitly declares that it
  contains neither save content nor game-binary content.
