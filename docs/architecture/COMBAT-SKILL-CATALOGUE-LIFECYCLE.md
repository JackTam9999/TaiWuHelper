# Combat-skill catalogue lifecycle

## Manifest identity

The helper treats a catalogue as current only when all installed-source facts
and stored counts agree. `CombatSkillCatalogueSourceIdentity` contains:

- the installed GameData product version;
- the positive importer version owned by
  `TaiwuCombatSkillDefinitionSource`;
- the SHA-256 fingerprint of the imported GameData configuration assembly;
- the SHA-256 fingerprint of the Traditional Chinese combat-skill resource;
- the SHA-256 fingerprint of the English combat-skill resource;
- both Traditional Chinese and English special-effect resource fingerprints;
- both Traditional Chinese and English legendary-book slot resource
  fingerprints.

The schema version remains an Infrastructure storage concern. Schema version 4
stores the complete source identity, UTC build time, combat-skill definition
count, legendary-book effect count, warning count, and error count in its
singleton manifest. `ReadStateAsync` rejects an unknown schema or a count that
does not match the stored rows as corrupt.

Importer behavior that can change mapped catalogue content must increment
`TaiwuCombatSkillDefinitionSource.ImporterVersion`. A compatible schema change
must increment `SqliteCombatSkillCatalogueStore.SchemaVersion`. Either change
invalidates the previous catalogue without inferring freshness from file
timestamps.

## State and action

| Observed state | Ensure result and action |
|---|---|
| Current identity and definition count | `Current`; no database write or timestamp change. |
| Missing database | Build the complete schema and content transactionally, then report `Rebuilt`. |
| Ready but source identity or count differs | Rebuild in one transaction. A failure rolls back and reports the preserved catalogue as `StaleCataloguePreserved`; it is never returned as current. |
| Corrupt, empty, or old-schema database | Build the validated sibling rebuild file completely, replace the corrupt file only after commit, then report `Rebuilt`. A failure leaves the original file and reports `CorruptCatalogueRemains`. |
| Repository inaccessible | Do not attempt a write; report `RepositoryUnavailable`. |
| Installed sources missing, unsupported, or unreadable | Preserve the corresponding typed source status and do not write the catalogue. |

`ReadCombatSkillCatalogueStatus` exposes corrupt and inaccessible repositories
as different statuses. This allows the lifecycle to recover data corruption
without treating an access-control or filesystem failure as permission to
replace a file.

## Coordination and determinism

All `EnsureCombatSkillCatalogue` instances share one process-wide asynchronous
gate because the architecture defines one fixed local catalogue. A caller
waiting for another ensure request re-reads both installed sources and stored
state after acquiring the gate. Consequently, the first request performs the
rebuild and later concurrent requests observe `Current` instead of rebuilding
again. The store also retains its writer gate for direct repository callers.

Combat-skill definitions and legendary-book effects are imported and persisted
in stable numeric-ID order. Diagnostics are ordered by source-record identity
and code; child collections retain their typed Domain order. Repeating an
import from identical sources produces the same counts, query order, and
complete field-level content identity. Build time is intentionally
observational metadata and is not part of this content-equivalence claim.

## Recovery and non-interference

Both the primary and rebuild filenames come exclusively from
`CatalogueStoragePathProvider`. The lifecycle accepts no request path, reads
installed sources through the E2-006 read-only adapter, and writes only derived
helper-owned files. Recovery does not delete, replace, rename, or write any
game installation, language resource, save, runtime memory, or process state.

Missing, empty, malformed, interrupted, count-mismatched, old-schema, and
inaccessible database cases have automated coverage. The opt-in local
integration assertion imports all configured definitions twice, persists both
results in a temporary helper-owned database, compares stable content identity,
and fingerprints every inspected installed source before and after the run.
