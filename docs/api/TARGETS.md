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
- target kind (`RegularCharacter` or `StoryCharacter`);
- the fixed character-template ID when the name came from a story-character
  template; and
- area ID and block ID; and
- stable `location:{areaId}:{blockId}` reference.

Age and numeric location provide disambiguating context when multiple
characters share a name without depending on localized map display text.

Fixed story characters can have an empty ordinary full name. The reader first
uses the ordinary save name, then falls back to the installed `Character`
language entry only when the character reports that it was created from a
fixed template. The resulting match is explicitly marked `StoryCharacter`;
the template never replaces the real save character ID or its combat data.

When the save contains both a map-placed story instance and an unplaced
same-name instance, a name query returns the map-placed instance. An exact
numeric character-ID query can still retrieve the unplaced instance.

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

Failure to localize a map location no longer discards an otherwise readable
character. The numeric area and block remain available, and a structured
`TARGET_LOCATION_UNAVAILABLE` warning preserves the partial-read boundary.

The endpoint cannot select a character in the game, alter a character, start
combat, or write a save.
