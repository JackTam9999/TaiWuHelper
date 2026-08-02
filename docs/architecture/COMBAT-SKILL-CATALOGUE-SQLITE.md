# Helper-owned combat-skill catalogue schema

## Purpose and boundary

`SqliteCombatSkillCatalogueStore` is the only production adapter permitted to
write derived catalogue data. It implements the Application-owned
`ICombatSkillCatalogueRepository` port and maps every row back to immutable
Domain models; SQLite types never cross the Infrastructure boundary.

The adapter receives `CatalogueStoragePathProvider`, not a string path. It can
therefore open only `combat-skill-catalogue.db` inside the validated
helper-owned catalogue directory established by
[ADR-0002](./ADR-0002-helper-owned-catalogue-storage.md). The path provider
rejects game and save directories, traversal, arbitrary filenames, and
existing reparse points. Reads do not create the directory or database.

The generated database, write-ahead log, shared-memory sidecar, and rebuild
database match the repository's `combat-skill-catalogue*.db` ignore rules.
They are local derived data and are never publish inputs or committed
artifacts.

## Schema version 1

All tables are SQLite `STRICT` tables. Foreign keys use cascade deletion only
inside the helper-owned database.

| Table | Stored data and key |
|---|---|
| `catalogue_manifest` | One row (`singleton_id = 1`) containing schema version, installed GameData version, three source fingerprints, UTC build time, and definition count. |
| `definitions` | One row per non-negative stable skill ID, with definition-level source provenance. |
| `localized_names` | Optional Traditional Chinese or English value and provenance, keyed by `(skill_id, language)`, plus a normalized search value. |
| `definition_fields` | Typed category, grade, faction, element, equipment, grid cost, slot contribution, timing, and effect-reference values keyed by `(skill_id, field_key)`. Status, reason, and optional provenance preserve unavailable and unsupported facts. |
| `requirements` | Ordered typed requirements keyed by `(skill_id, requirement_id)`, with a unique per-skill order. |
| `raw_descriptions` | Ordered display-only localized text and provenance. These values are not recommendation rules. |
| `import_diagnostics` | Deterministically ordered warning/error code, source-record identity, and reason. Complete source files and binaries are never stored. |

The schema constrains stable skill IDs, the two supported languages, enum and
status ranges, required provenance, unavailable/unsupported reasons, unique
language values, and unique ordered children. Available composite values use
integer columns so filters remain structural rather than parsing raw text.

Indexes support normalized bilingual name lookup, typed filters, ordered
requirements/descriptions, and diagnostic lookup:

- `ix_localized_names_search(search_text, skill_id)`
- `ix_definition_fields_filter(field_key, status, value_1, skill_id)`
- `ix_requirements_skill_order(skill_id, sort_order)`
- `ix_descriptions_skill_order(skill_id, sort_order)`
- `ix_diagnostics_source(source_record_identity, code)`

## Replacement and observation semantics

Definitions are sorted by stable skill ID. Diagnostics are sorted by source
record identity and code. Child values retain their Domain-defined order.
Every replacement recreates the schema and inserts the complete manifest and
content in one transaction with foreign keys enabled, WAL journaling, full
synchronous durability, and a bounded busy timeout.

The process-local replacement gate prevents competing writers. SQLite readers
use a read transaction and observe either the previously committed catalogue
or the complete replacement; they cannot observe intermediate schema or row
changes. Any validation, mapping, persistence, cancellation, or injected
mid-write failure rolls the transaction back. A previously committed catalogue
therefore remains intact.

`ReadStateAsync` distinguishes missing, ready, corrupt, and inaccessible
storage. It validates the schema version and reconciles the manifest definition
count with stored rows. Failure messages are sanitized and do not reveal the
local catalogue path.

Source invalidation, rebuild coordination across lifecycle requests, and
recovery decisions build on this atomic repository in E2-008.
