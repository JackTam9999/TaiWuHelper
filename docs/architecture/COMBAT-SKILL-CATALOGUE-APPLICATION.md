# Combat-skill catalogue Application boundary

## Purpose

Epic 2 keeps installed combat-skill definitions, the helper-owned derived
catalogue, and save-derived character progress behind separate Application
ports. This prevents static catalogue lifecycle concerns from being confused
with a character's current progress and keeps GameData, SQLite, filesystem,
and HTTP types out of the use-case layer.

## Ports

| Port | Responsibility | Boundary |
|---|---|---|
| `ICombatSkillDefinitionSource` | Read immutable definitions and the installed bilingual source identity | Read-only GameData-source marker; no GameData objects or paths |
| `ICombatSkillCatalogueRepository` | Read the stored manifest, query/get definitions, or atomically replace derived definitions | No source or destination path; Infrastructure owns the fixed helper path |
| `ICharacterCombatSkillProgressReader` | Read immutable progress for one character from the configured save | Character ID only; no request-supplied save path |

The source identity contains the detected GameData version plus independent
SHA-256 fingerprints for the imported GameData configuration assembly and the
Traditional Chinese and English sources. The repository stores that value
with its build time and definition count. It does not decide whether a
catalogue is current.

## Freshness policy

`ReadCombatSkillCatalogueStatus` compares the installed identity and imported
definition count with the stored manifest:

| Condition | Status |
|---|---|
| Manifest identity and count match | `Current` |
| No helper catalogue exists | `Missing` |
| Identity or count differs | `Stale` |
| Installed sources are absent | `MissingSources` |
| Installed version is unsupported | `UnsupportedVersion` |
| Source read fails | `SourceReadFailed` |
| Stored catalogue is corrupt, failed, or unreadable | `RepositoryFailed` |

Only `EnsureCombatSkillCatalogue` requests replacement. It leaves a current
catalogue untouched, rebuilds a missing, stale, corrupt, or recoverably failed
catalogue, and returns `RebuildFailed` for either an adapter diagnostic or an
unexpected replacement exception. Cancellation always propagates rather than
being converted to a failure status.

## Query policy

`SearchCombatSkillDefinitions` accepts typed category, grade, faction,
element, and equipment filters. Candidate reads are bounded to 2,000 and a
page is bounded to 100. Application matches the optional text against both
installed names case-insensitively, ranks an exact match first, then orders by
the resolved display name and stable skill ID before applying offset and
limit.

Language selection belongs here: the preferred language is used when present,
then the other supported language is selected deterministically. Results
preserve the actual localized value, its source, and whether fallback was
used. SQL adapters therefore filter structure but do not parse UI language
strings or choose display text.

`ReadCombatSkillDetails` returns one static definition. A missing stable ID is
an ordinary not-found result, distinct from catalogue unavailability.

`ReadCharacterCombatSkillAtlas` first requires a current catalogue, then asks
the progress port for the selected character. Save missing, save read failure,
and unsupported save version remain distinct. Entries are ordered by stable
skill ID and retain progress even if a definition is unexpectedly absent;
that entry receives an explicit unavailable display-name reason.

## Dependency and safety guarantees

- Application references Domain only. It has no Infrastructure, SQLite,
  GameData implementation, ASP.NET Core, or Presentation reference.
- Public requests carry typed IDs and filters, never filesystem paths.
- Ordinary status, search, details, and atlas use cases cannot replace the
  catalogue.
- Catalogue replacement targets derived helper data only; the fixed validated
  path remains an Infrastructure responsibility under ADR-0002.
- Character progress and static definitions remain separate immutable values
  until the atlas use case joins them by stable skill ID.
