# Target lookup API

## Endpoint

`GET /api/targets?query={name-or-id}&maxResults={1-100}`

The endpoint searches the save configured in
`SaveGames:DefaultSaveFilePath`. `query` is required. `maxResults` defaults to
25.

Examples:

```http
GET /api/targets?query=何春石
```

```http
GET /api/targets?query=16317
```

A positive integer query is matched as an exact character ID. Other queries
use a case-insensitive display-name substring match.

## Structured response

Each match includes:

- stable `target:{characterId}` reference;
- character ID;
- display name;
- current age;
- area ID and block ID; and
- stable `location:{areaId}:{blockId}` reference.

Age and numeric location provide disambiguating context when multiple
characters share a name without depending on localized map display text.

The response also includes capture time, GameData version, structured lookup
warnings, and the total number of matches.

## Match status

- `Found` means exactly one source character matched.
- `NotFound` means no source character matched.
- `Ambiguous` means more than one source character matched.

`totalMatches` describes the full match count even when `maxResults` limits the
returned list. Matches use deterministic exact-name, name, area, block, and ID
ordering.

## Read-only source

The adapter enumerates the loaded read-only character view through the shared
archive session. The session fingerprints the save before and after the query
and discards the result if the source changed. Taiwu is excluded from target
results.

The endpoint cannot select a character in the game, alter a character, start
combat, or write a save.
