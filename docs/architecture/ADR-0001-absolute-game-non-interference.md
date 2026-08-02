# ADR-0001: Absolute game non-interference

| Field | Value |
|---|---|
| Status | Accepted |
| Date | 2026-07-29 |
| Epic | [EPIC-001](../roadmap/epic-001/EPIC.md) |
| Backlog item | [M1-000](../roadmap/epic-001/BACKLOG.md#m1-000--enforce-absolute-game-non-interference) |

## Context

TaiWu Helper reads locally available information to explain game state and
recommend a combat-skill loadout. It is not a mod, trainer, cheat, bot,
automation tool, treatment system, or game-control system.

The boundary must remain enforceable as the solution grows. Describing the
reader as merely "safe" or saying that it should "avoid" modification is not
strong enough: no current or future feature may introduce a path that changes
game-owned data or controls the game.

## Decision

TaiWu Helper is permanently an information-only recommendation system.

It may observe game-owned data through query-only operations and copy relevant
values into immutable helper-owned models. It must never modify game-owned
data, the running game, or in-game state, directly or indirectly.

This decision is an architectural invariant. A proposed feature that conflicts
with it is rejected rather than deferred.

## Definitions

### Game-owned data

Game-owned data includes:

- Save files and their directory structure.
- Installed game binaries and configuration.
- Files or databases used by the game.
- The running game process and its memory.
- In-game character, combat, equipment, and world state.
- Game input or commands that would change in-game state.

### Helper-owned data

Helper-owned data includes:

- Immutable in-memory snapshots derived from reads.
- Recommendation results and explanations.
- Logs and test artifacts outside game-owned storage.
- User-provided current-screen observations.
- Optional future SQLite history, feedback, or cache data stored outside
  game-owned storage.

Helper-owned data must never be written back or presented to the game as a
command.

## Permitted operations

The helper may:

- Open permitted save and configuration files with read-only access.
- Hash source bytes and verify that a source remained unchanged during a read.
- Load a save through the minimum `GameData` query surface needed to inspect
  it.
- Initialize library state inside the helper process when required to perform
  a read.
- Map library objects into Domain or Application-owned immutable models.
- Analyze snapshots and produce deterministic recommendations.
- Accept user-reported observations that affect helper analysis only.
- Copy, print, or export recommendation text to helper-owned storage.
- Persist helper-owned history or feedback outside game-owned storage if a
  later milestone explicitly introduces it.

Initializing or populating objects inside the helper process is not a game
mutation: those objects are isolated read models and are not connected to the
running game. They must never be persisted back into game-owned storage.

## Prohibited operations

The helper must never:

- Create, update, delete, repair, convert, re-serialize, replace, rename, move,
  copy over, or overwrite a save or game-owned file.
- Invoke a `GameData` save, setter, modification, equipment, or other
  mutation-capable operation against game-owned state.
- Expose a game-data command port from Domain or Application.
- Expose a mutation-capable `GameData` type outside Infrastructure.
- Attach to, debug, inject into, hook, patch, or control the game process.
- Read or write the running game's process memory.
- Start, stop, or terminate the game.
- Send keyboard, mouse, controller, or other automated input to the game.
- Apply, equip, or execute a recommendation for the player.
- Place helper-owned output in game-owned storage for the game to consume.

HTTP `POST` may be used for an information query when its structured request
does not fit a query string. Its presence does not authorize command behavior.

## Architecture enforcement

| Boundary | Enforcement |
|---|---|
| Domain | Has no outer-layer or `GameData` assembly references |
| Application | Uses `IReadOnlyGameDataSource` query ports and has no Infrastructure, API, or `GameData` reference |
| Infrastructure | Keeps `GameData` types internal and implements only query ports |
| Save adapter | Uses read-only helper-controlled file access and rejects a result when before/after fingerprints differ |
| API | Exposes information queries and recommendations, never game commands |
| Presentation | Uses recommendation and manual-instruction language and has no apply or control action |

The `TaiWu.Architecture.Tests` project enforces:

- Clean Architecture reference direction.
- Query-only method names on game-data source ports.
- No `GameData` type in Infrastructure's public signatures.
- No game-mutation controller actions.
- No save-write or destructive filesystem APIs in the save adapter.
- No process-control, injection, hook, patch, process-memory, or automated-input
  APIs in production source.
- Read-only fingerprint behavior against temporary test files.

These automated checks complement code review. They do not weaken the absolute
boundary when a new API or library is not yet recognized by a test rule; the
decision remains authoritative.

## Read consistency

Before loading a save, Infrastructure captures its length and SHA-256 hash
through a stream opened with `FileAccess.Read`. It repeats the fingerprint
after mapping the report.

If the bytes changed during the operation, the result is discarded and the
caller is asked to retry after the save is stable. This does not claim that the
helper caused the change; the game or another process may have written the
save concurrently.

The fingerprint is a consistency and regression safeguard. The main protection
is that the helper contains no game-data write or control path.

## Consequences

- The player always performs suggested loadout changes manually.
- Features requiring game modification are permanently out of scope.
- Recommendation correctness can be tested without authorizing game control.
- Reading and hashing add some latency, accepted in favour of consistency.
- Source reads are serialized around the current stateful `GameData` runtime.
- SQLite catalogue storage follows the narrow helper-owned boundary in
  [ADR-0002](./ADR-0002-helper-owned-catalogue-storage.md) and never becomes a
  game-data write-back mechanism.
