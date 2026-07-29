# TaiWu Helper

A local .NET 10 API for reading data from *The Scroll of Taiwu* save files.
The reader is read-only and retains the line-oriented output of the original
`太吾存檔武功讀取器.cs`.

## Architecture

- `TaiWu.Domain`: save-report model.
- `TaiWu.Application`: save-reading use case and infrastructure port.
- `TaiWu.Infrastructure`: Taiwu `GameData` integration and focused report
  sections for overview, combat skills, legendary books, and story state.
- `TaiWuAPI`: HTTP adapter.
- `TaiWu.Application.UnitTests`: xUnit v3 tests using NSubstitute.

Dependencies point inward: API and Infrastructure depend on Application, while
Application depends only on Domain.

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

## Read a save

Run the API:

```powershell
dotnet run --project .\TaiWuAPI --launch-profile http
```

Send a request:

```http
POST /api/save-games/read
```

When running with the `http` development launch profile, the default save is
configured in `TaiWuAPI/appsettings.Development.json`. No request body is
required.

The configured save can also be read with GET:

```http
GET /api/save-games/read
```

To inspect a target character while using the configured save:

```http
GET /api/save-games/read?targetCharacterId=12345
```

To read another save or inspect a target character, override either value:

```http
POST /api/save-games/read
Content-Type: application/json

{
  "saveFilePath": "D:\\...\\SaveGames\\world_2\\local.sav",
  "targetCharacterId": 12345
}
```

The response contains `lines`, preserving the original reader's diagnostic
format.

## Tests

```powershell
dotnet test
```
