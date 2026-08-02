# M1-002: Golden-save evidence metadata

| Field | Value |
|---|---|
| Status | Complete |
| Golden scenario | [M1-001](./M1-001-golden-target-selection.md) |
| Sanitized record | [M1-002-golden-save-metadata.json](./evidence/M1-002-golden-save-metadata.json) |

## Purpose

Record the reproducibility boundary without committing private local paths,
save fingerprints, timestamps, identifiers, save content, or game-binary
content.

The local inspection verifies that the save fingerprint is identical before
and after capture. The actual fingerprint and other machine-specific values
remain local. The installed GameData product version is retained because
mapping behavior can vary by game build.

## Current-screen observations

Current-screen observations are deliberately separate from save metadata.
They are player-provided, analysis-only evidence that may be newer than the
disk save:

| Observation | Evidence | Analysis effect |
|---|---|---|
| Populated player loadout | Local-only `M1-001-current-player-loadout.png` | Overrides stale equipped-card membership and final displayed capacities in the helper's analysis snapshot only |
| Empty slot capacities | Local-only `M1-001-empty-capacities.png` | Establishes the `6/2/2/2/2` unmodified capacity baseline |
| Individual 內功 contribution | Local-only `M1-001-inner-power-capacity-example.png` | Establishes its cost and category-slot contributions |

These observations cannot write to the save, game files, running process, or
in-game state.

## Repository exclusion

- No `.sav` file is stored in Git.
- No `GameData` or Steam runtime binary is stored in Git.
- No evidence screenshot is stored in Git.
- `.gitignore` excludes all three artifact classes.
- An architecture test scans the repository source tree for proprietary
  artifacts.
- Publishing is blocked so a local build cannot accidentally become a
  redistributable package containing proprietary dependencies.
- The committed JSON is deliberately sanitized and contains no local path,
  save fingerprint, exact capture time, character identifier, or binary hash.
