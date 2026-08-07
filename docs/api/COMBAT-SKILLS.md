# Combat-skill catalogue and character-atlas API

## Scope and source precedence

These local endpoints expose the helper-owned static catalogue and current
save-derived character progress. A response keeps the two sources separate:

1. installed GameData and language resources define static skill fields;
2. the helper-owned catalogue is a derived, versioned copy of those fields;
3. the configured save defines character progress;
4. list and detail queries join the immutable values by stable skill ID.

Static definitions never overwrite save progress, and save progress is never
written into the catalogue. Progress without a definition remains visible with
`STATIC_DEFINITION_MISSING`. A definition absent from the complete learned
collection receives a verified `learned: false` field with save-snapshot
provenance.

No endpoint accepts a game path or save path. The progress reader always uses
the locally configured save.

## Catalogue status

`GET /api/combat-skills/status`

Returns `Current`, `Missing`, `Stale`, `Rebuilding`, `MissingSources`,
`UnsupportedVersion`, `SourceReadFailed`, `RepositoryFailed`, or `Corrupt`.
The response includes installed/stored source fingerprints, definition count,
and catalogue build time when available. Local exception text and filesystem
locations are not returned.

## Search catalogue definitions

`GET /api/combat-skills`

Supported query parameters:

| Parameter | Values |
|---|---|
| `query` | Optional Traditional Chinese or English name text, maximum 100 characters |
| `language` | `TraditionalChinese` or `English` |
| `sort` | `DisplayName`, `SkillId`, or `Grade` |
| `category` | A `CombatSkillDiscipline` name |
| `grade` | `0` through `8` |
| `faction` | A non-negative stable faction ID |
| `element` | `Metal`, `Wood`, `Water`, `Fire`, `Earth`, or `Mixed` |
| `equipmentType` | `Neigong`, `Attack`, `Agility`, `Defense`, or `Assistance` |
| `offset` | Non-negative, within the 2,000-candidate bound |
| `limit` | `1` through `100` |

Example:

```http
GET /api/combat-skills?query=body&language=English&category=Neigong&sort=Grade&offset=0&limit=25
```

Search uses Unicode NFKC, invariant case normalization, collapsed whitespace,
and both supported names. Exact matches rank before the selected stable sort.
Every item key uses `combat-skill:{skillId}`.

## Joined skill detail

`GET /api/combat-skills/{skillId}`

Parameters:

- `language` selects the preferred display language;
- omitting `characterId` joins the current Taiwu recorded in the configured
  save; a non-negative value selects an explicit character instead.

The response distinguishes a missing definition from unavailable character
progress. A progress-only skill can therefore return `definitionFound: false`
with a non-null `characterState` and a missing-definition diagnostic.

The full static response contains localized names, typed fields, requirements,
timing values, effect references, source provenance, and raw descriptions.
Raw descriptions have `isVerifiedMechanic: false`: they are display text, not
rules used by recommendations, feasibility, scoring, or loadout generation.
The `kind` distinguishes the configured original text (`Effect`) from normal
direct-practice (`DirectEffect`) and reverse-practice (`ReverseEffect`) effect
descriptions. Each language remains independent.

## Character skill atlas

`GET /api/character-skill-atlas`

When `characterId` is omitted, the read-only save adapter selects the current
Taiwu recorded in the configured save. A non-negative `characterId` can be
supplied for an explicit character overlay.

The atlas accepts the same `query`, language, static filters, `offset`, and
`limit` parameters as catalogue search, plus independent progress filters:

- `learned`
- `hasProficiency`
- `studyComplete`
- `breakthroughReady`
- `brokenThrough`
- `activeDirection` (`Direct` or `Reverse`)
- `attainmentMastered`
- `simplified`
- `activated`
- `equipped`

Unknown progress facts do not match `true` or `false`. `hasProficiency` is the
explicit availability filter: `false` selects an unavailable current
proficiency field rather than a numeric zero.

The response includes catalogue status, save SHA-256/read time, GameData
version, warnings, paging metadata, issue flags, diagnostics, and stable entry
keys. Base grid cost and current character-effective grid cost are distinct.
`proficiency` contains only the persisted current value and storage maximum.
The separate `power` object contains current final power, maximum power, and
its calculation context. Current power is the game's centre percentage value,
not a current/maximum ratio, so clients must permit current power above maximum
power. In standalone-save responses both power fields are unavailable with a
reason because the live special-effect calculation context is absent.

For the current Taiwu, `attainmentMastered` is available and equals the
successful-breakthrough predicate for the current activation state.
`simplified` is the independent `功法精解` flag. For another explicitly selected
character the player-facing attainment field is unavailable rather than
guessed.

## Helper catalogue maintenance

`POST /api/combat-skills/catalogue-cache/rebuild`

This explicitly named maintenance operation creates or atomically replaces
only the fixed helper-owned derived catalogue cache. It accepts no body, path,
or destination. It cannot modify a save, GameData, the game process, or game
configuration. A current cache is left untouched.

The response returns `Current`, `Rebuilt`, `MissingSources`,
`UnsupportedVersion`, `SourceReadFailed`, or `RebuildFailed`, plus any retained
cache recovery state.

`POST /api/combat-skills/progress-cache/clear`

This explicitly named maintenance operation clears all bounded, helper-owned
current progress snapshots. It accepts no body or path and never reads,
changes, or deletes the configured save. The next atlas read reconstructs the
current snapshot through the normal fingerprinted, read-only save workflow.

The response returns `Cleared`, the number of save snapshots removed, and a
safe failure reason when the helper-owned database could not be cleared.

## Partial data and errors

Successful query endpoints return HTTP 200 even when their typed source status
is stale, rebuilding, unsupported, partial, missing, or failed. Clients should
render those result states directly instead of guessing from an empty list.

Malformed enum values, out-of-range IDs, invalid filters, and paging violations
return an RFC problem response with HTTP 400. Public failure reasons are stable
and exclude local filesystem paths. Request cancellation propagates normally.

Unavailable catalogue/progress fields serialize with a status, null value,
reason, and optional provenance. They never evaluate a throwing unavailable
Domain value getter during JSON serialization.
