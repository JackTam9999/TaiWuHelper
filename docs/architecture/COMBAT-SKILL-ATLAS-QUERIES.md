# Combat-skill catalogue and character-atlas queries

## Purpose

Epic 2 exposes the installed combat-skill catalogue and current-character
progress as read-only Application results. Static definitions and save-derived
progress remain separate authoritative values. The atlas joins them in memory
by stable combat-skill ID and never writes the joined result back to either
source.

## Source precedence and union

The atlas is the union of all bounded catalogue definitions and all progress
records returned for the requested character:

| Available values | Result |
|---|---|
| Definition and progress | A joined entry retaining both original immutable values |
| Definition only | An unlearned entry, proven by absence from the complete learned-skill collection |
| Progress only | A visible entry with `STATIC_DEFINITION_MISSING` |

Definition-only learned state is an exact negative fact with save-snapshot
provenance. It is not an inferred default. A missing definition does not cause
save progress to disappear.

Every list key is `combat-skill:{skillId}`. Detail lookup uses the same stable
ID. Definition provenance remains on catalogue fields; progress provenance and
the save fingerprint/read time remain on progress fields and result metadata.

## Search, ordering, and paging

Search considers both Traditional Chinese and English names. Query and name
text are normalized with Unicode NFKC, invariant uppercase, trimmed ends, and
collapsed whitespace. This makes equivalent full-width, case, and whitespace
forms deterministic without language-dependent collation.

Exact bilingual matches rank first. Remaining entries are ordered by name
availability, normalized selected display name, and numeric skill ID. Language
selection uses the requested language when present and the other supported
language as a deterministic fallback. Missing or fallback localization raises
`PartialLocalization`.

Offset and limit are validated before reading data. The query processes at
most the catalogue contract's candidate bound, exposes
`CandidateSetMayBeTruncated`, and applies paging only after the stable ordering.
Identical source snapshots and requests therefore produce identical page keys.

## Filters and unavailable data

Static filters cover category, grade, faction, element, and equipment type.
Progress filters cover learned membership, proficiency availability, study
completeness, breakthrough readiness, completed breakthrough, active Direct or
Reverse practice, attainment mastery, simplification, activation, and equipped
state.

An unavailable typed value never matches either `true` or `false`. This keeps
unknown data distinct from a verified negative. `HasProficiency` is the one
availability filter: `true` selects progress with a readable current
proficiency, while `false` selects progress whose current proficiency field is
explicitly unavailable. Entries with no progress do not satisfy that filter.

A static filter excludes progress-only entries because their definition fields
are unavailable. An empty optional filter does not exclude partial entries.

## Costs

`BaseGridCost` is copied only by reference from the immutable static definition.
`CurrentEffectiveGridCost` is a separate save-aware result. The verified
character-intrinsic rule currently subtracts one grid for a simplified learned
skill and never reduces below one. It is unavailable when the definition,
base cost, learned membership, or simplification fact is unavailable.

Legendary-book cost assignments are loadout context, not an intrinsic property
of a character's learned skill, so they are intentionally not folded into this
catalogue/atlas value.

## Result states and diagnostics

All search, atlas, and detail responses retain the catalogue status, including
`Missing`, `Stale`, `Rebuilding`, unsupported-source, and failure states. Atlas
and joined detail results also retain progress status, save metadata, adapter
warnings, and sanitized field provenance when progress is available.

Issue flags are additive:

| Flag | Meaning |
|---|---|
| `PartialLocalization` | Requested localization was missing or fallback was used |
| `MissingDefinition` | Save progress had no matching static definition |
| `UnsupportedStudyMapping` | Study details were absent or included unavailable mapped details |
| `ProgressWarnings` | The progress adapter returned one or more warnings |
| `EffectiveCostUnavailable` | The character-effective cost could not be established |

Diagnostics use stable codes and may identify a stable skill ID. List results
aggregate issues and diagnostics before paging so a partial candidate set is
not presented as complete merely because the affected entry is off the current
page. Detail results use the same join and issue derivation as atlas entries.

## Safety boundary

Ordinary status, search, atlas, and detail queries cannot rebuild or replace the
catalogue. Only the explicit ensure use case can atomically replace the fixed,
helper-owned derived store. Query contracts contain no game path, save path,
SQLite type, HTTP type, or mutable GameData object, and no query changes
game-owned state.
