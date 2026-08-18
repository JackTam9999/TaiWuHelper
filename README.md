# TaiWu Helper

A local .NET 10 API for reading data from *The Scroll of Taiwu* save files and
providing recommendations.

This is an unofficial community project. It is not affiliated with, endorsed
by, or sponsored by the game developer or publisher.

TaiWu Helper is an information-only system. It never modifies saves, game
files, configuration, runtime memory, runtime state, or in-game data; it never
injects into, hooks, patches, automates, or controls the game. Recommendations
are carried out manually by the player.

## Architecture

- `TaiWu.Domain`: save-report model.
- `TaiWu.Application`: save-reading use case and infrastructure port.
- `TaiWu.Infrastructure`: Taiwu `GameData` integration and focused report
  sections for overview, combat skills, legendary books, and story state.
- `TaiWuAPI`: HTTP adapter.
- `TaiWu.Application.UnitTests`: xUnit v3 tests using NSubstitute.

Dependencies point inward: API and Infrastructure depend on Application, while
Application depends only on Domain.

The permanent safety boundary is documented in
[ADR-0001: Absolute game non-interference](docs/architecture/ADR-0001-absolute-game-non-interference.md).
All user interfaces also follow the project-wide
[UI presentation guidelines](docs/architecture/UI-PRESENTATION-GUIDELINES.md).

## Requirements

- .NET 10 SDK
- The 64-bit Windows version of *The Scroll of Taiwu*

The build looks for the default Steam installation at:

```text
C:\Program Files (x86)\Steam\steamapps\common\The Scroll Of Taiwu
```

For another installation directory:

```powershell
dotnet build -p:TaiwuGameDirectory="D:\SteamLibrary\steamapps\common\The Scroll Of Taiwu"
```

The proprietary `GameData` and Steam runtime binaries are loaded from the
user's own game installation for local builds. They are excluded from Git and
must not be redistributed. `dotnet publish` is intentionally blocked until a
distribution design can run without packaging those files.

## Read a save

Store the local save path in .NET user secrets. This keeps machine-specific
paths out of Git:

```powershell
dotnet user-secrets set --project .\TaiWuAPI `
  "SaveGames:DefaultSaveFilePath" `
  "C:\Program Files (x86)\Steam\steamapps\common\The Scroll Of Taiwu\SaveGames\world_1\local.sav"
```

Run the API:

```powershell
dotnet run --project .\TaiWuAPI --launch-profile http
```

Read the configured save:

```http
GET /api/save-games/read
```

To inspect a target character while using the configured save:

```http
GET /api/save-games/read?targetCharacterId=12345
```

The API deliberately has no request field for a filesystem path. To switch
saves, update the local user-secret configuration and restart the API. Kestrel
binds only to localhost on port `5056`; the helper is not a remotely exposed
service.

The HTTP surface is an internal loopback integration boundary, not a supported
external-client API. Current routes therefore remain unversioned. JSON enums
accept named tokens only (numeric enum values are rejected), and contract tests
pin the current request tokens. A future externally supported API must introduce
API-owned versioned contracts and routes instead of changing these internal
tokens in place.

The response contains a `schemaVersion`, immutable `lines`, and `legacyText`,
preserving the original reader's diagnostic format without exposing its Domain
object directly.

## Tests

```powershell
dotnet test
```

## Roadmap

- [Product roadmap index](docs/roadmap/README.md)
- [EPIC-001: Target-specific combat-skill recommendations](docs/roadmap/epic-001/EPIC.md)
- [EPIC-002: Version-aware skill catalogue and character skill atlas](docs/roadmap/epic-002/EPIC.md)
- [EPIC-003: Verified target observations and evidence-aware recommendations](docs/roadmap/epic-003/EPIC.md)
- [EPIC-004: Side-by-side loadout comparison and change planning](docs/roadmap/epic-004/EPIC.md)
- [EPIC-005: Target archetypes and counter playbooks](docs/roadmap/epic-005/EPIC.md)
- [EPIC-006: Evidence-aware companion role and candidate finder](docs/roadmap/epic-006/EPIC.md)
