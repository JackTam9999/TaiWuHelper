# ADR-0002: Constrain helper-owned catalogue storage

| Field | Value |
|---|---|
| Status | Accepted |
| Date | 2026-08-02 |
| Epic | [EPIC-002](../roadmap/epic-002/EPIC.md) |
| Backlog item | [E2-000](../roadmap/epic-002/BACKLOG.md#e2-000--constrain-helper-owned-catalogue-persistence) |

## Context

Epic 2 introduces a rebuildable SQLite catalogue derived from installed combat
skill configuration and language resources. This is the first production
feature that needs to write persistent helper-owned data.

The permanent boundary in
[ADR-0001](./ADR-0001-absolute-game-non-interference.md) forbids changing any
save, game file, game database, game configuration, running process, runtime
memory, or in-game state. A broad file-write exception would undermine that
boundary even if its first use were harmless.

The catalogue therefore needs a smaller storage boundary that proves both
where helper writes may occur and where they may never occur.

## Decision

### One trusted path provider

`TaiWu.Infrastructure.Catalogue.CatalogueStoragePathProvider` is the only
component that calculates and validates catalogue file locations. It is an
internal Infrastructure type and is not an Application port or public API
contract.

The default database location is derived from the operating system's local
application-data directory:

```text
<LocalApplicationData>/TaiWuHelper/catalogue/combat-skill-catalogue.db
```

The provider returns a fixed database name. It may validate direct sibling
files used for transactional rebuild or recovery, but it rejects relative
paths, traversal, nested destinations, directories used as files, and every
path outside the exact catalogue directory.

### Protected game-owned directories

The provider receives the trusted game installation and save directories as
protected roots. The catalogue directory must not:

- Equal a protected root.
- Be inside a protected root.
- Contain a protected root.

Path comparison uses operating-system case semantics and directory boundaries;
string-prefix similarities do not count as containment.

The provider checks every existing segment of the helper-owned path and rejects
symbolic links, junctions, or other reparse points. It repeats this check when a
path is requested so a link introduced after construction is not silently
trusted.

### One named persistence adapter

When SQLite persistence is implemented, production file and database write
APIs are allowed only in:

```text
src/TaiWu.Infrastructure/Catalogue/SqliteCombatSkillCatalogueStore.cs
```

The adapter must obtain every destination from the path provider immediately
before use. Other Infrastructure code, Domain, Application, and Presentation
remain unable to write persistent data directly.

Architecture tests scan production source for file-write, destructive-file,
write-capable stream, directory-creation, and SQLite APIs. Any occurrence
outside the named adapter fails the build.

### No path in public contracts

Domain, Application, and HTTP contracts do not contain catalogue file or
directory parameters. A future rebuild action may request rebuild of the one
configured helper catalogue; it cannot select a source or destination path.

### No eager filesystem mutation

The path provider normalizes and validates paths but creates no directory or
file. Directory creation, database replacement, recovery, and deletion remain
responsibilities of the named SQLite adapter and must validate their exact
target immediately before acting.

## Operational rules

- Generated catalogue databases and SQLite sidecar files are excluded from Git
  and release artifacts.
- The catalogue database is derived cache data and may be rebuilt from
  permitted read-only sources.
- A source path is never reused as a catalogue destination.
- A malformed or unsafe storage configuration fails closed.
- Recovery never searches broadly for files to delete or replace.
- Tests may write only inside unique test-owned temporary directories.
- Source fingerprints remain unchanged before and after catalogue operations.

## Consequences

- SQLite can be added without granting general filesystem-write permission.
- Application and Presentation remain independent of physical storage paths.
- Users cannot redirect catalogue writes through HTTP input.
- Symlinked helper application-data locations fail closed and require an
  explicitly safe non-overlapping location.
- The exact adapter filename is architecture-significant; renaming it requires
  an ADR and architecture-test update.
- Future helper-owned persistence requires a separate boundary decision rather
  than inheriting catalogue permission automatically.
