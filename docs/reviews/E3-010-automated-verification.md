# E3-010 automated verification

**Evidence date:** 2026-08-07

**Decision:** Passed

E3-010 verifies that the same save, catalogue, verified rules, and manual
target observation produce deterministic in-memory results; clearing the
observation reproduces save-only behavior; and no inspected game-owned source
changes.

## Source identity

| Source | Verified identity |
|---|---|
| Epic 3 implementation baseline | `d794ea5` (`feat(ui): explain target observation impact`) |
| GameData product version | `1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a` |
| Observable-loadout rule | `TAIWU-CNH-TARGET-LOADOUT-1.0.0-68032f25` |
| Catalogue importer | Version `1` |
| E3-000 positive scenario | Visible `切磋武功` opponent loadout, paired identity `霍劍嬋` |
| E3-000 unavailable scenario | Story/hostile opponent, represented as unavailable and never empty |

The current save path and SHA-256 values are deliberately not recorded. The
local integration test compares them in memory only.

## Automated matrix

Release build:

```powershell
dotnet build TaiWu.slnx -c Release --no-restore
```

Result: passed with zero warnings and zero errors.

Default matrix:

```powershell
dotnet test TaiWu.slnx -c Release --no-build
```

Result: **832 total; 825 passed; 0 failed; 7 expected opt-in skips**.

Current-save and installed-catalogue matrix:

```powershell
$env:TAIWU_INTEGRATION_SAVE_PATH = '<current-local.sav>'
$env:TAIWU_INTEGRATION_SKILL_CATALOGUE = '1'
$env:TAIWU_GAME_DIRECTORY = '<installed-game-directory>'
dotnet test TaiWu.slnx -c Release --no-build
```

Result: **832 total; 828 passed; 0 failed; 4 expected historical-fingerprint
skips**. The skipped golden assertions belong to older pinned save states and
correctly declined to apply their stale facts to the current save.

All Domain, Application, Infrastructure, API, Presentation, architecture, and
integration projects passed.

## Determinism and lifecycle

`RecommendCombatLoadoutTargetObservationTests` now compares a stable full
result signature covering:

- merged target snapshot identity, learned-skill order, directions, and
  equipped category order;
- typed threats and their source order;
- generated candidate and ranked recommendation order across all policies;
- observation-impact threats, recommendation changes, unsupported evidence,
  conflicts, and timestamps.

Two executions of the same request are equivalent. A separate lifecycle test
applies an observation and then issues the original save-only request. The
cleared result has no observation or impact object and exactly reproduces the
initial save-only signature.

The same test verifies that each request reads the current catalogue through
the existing query ports and never calls `ReplaceAsync`. Observation state
therefore cannot rebuild, clear, or otherwise change catalogue/cache state.

## Architecture and non-interference

`TargetObservationSafetyTests` verifies that:

- target-observation runtime code contains no file-write, SQLite, browser
  persistence, process-control, hook, input automation, or observation-history
  API;
- Infrastructure defines no target-observation persistence type;
- UI observation state has no mutable static session store; and
- observation resolution depends on query ports only and cannot call catalogue
  rebuild or cache-clear operations.

The full architecture suite passed **78/78**.

## Current-save vertical verification

`TargetObservationReadOnlyIntegrationTests` performs the following guarded
workflow against the configured current save:

1. Discover a valid current target through the normal bilingual target lookup.
2. Build the save-only recommendation.
3. Apply the same controlled complete-current-loadout observation twice.
4. Compare snapshots, threats, recommendations, impacts, conflicts, and
   ordering.
5. Clear the observation by rebuilding the save-only request and compare it
   with the initial result.
6. Recompute every guarded SHA-256 in a `finally` block.

The test passed. All inspected save, GameData runtime, and language-resource
lengths, timestamps, and SHA-256 fingerprints were unchanged. The observation
was an immutable helper value only; no observation history or cache row was
created.

## Bilingual E3-000 comparison

The English and Traditional Chinese rendered-component checks were manually
compared with the E3-000 decision table:

| E3-000 rule | Verified UI behavior |
|---|---|
| Only sparring exposes the supported opponent loadout | Both languages tell the player to use the visible `切磋武功` loadout |
| Story/hostile targets expose no opponent loadout | Both languages show an explicit unavailable state and request no hidden input |
| Current screen may be complete or partial | Coverage is explicit; partial observations retain a remaining-unknown warning |
| Target identity is paired context, not read from the loadout screen | A save-only recommendation and confirmed target selection are required before entry |
| Visible names resolve through the catalogue | The form searches active-language names and requires a stable confirmed match |
| `正`/`逆` may be reported only when visible | Direction is optional and restricted to supported visible values |
| Observation evidence is not win probability | Both impact renderings state that confidence is provenance, not win probability |
| No screenshots or game control | There is no file input, upload, capture, process, or automation control |

The two dedicated bilingual impact renderings and all target-observation form
renderings passed. A background live-host check was also attempted, but the
available in-app browser blocked loopback URLs and no desktop-browser
connection was available. This browser-client restriction did not affect the
server, component rendering, API, or local vertical results and is not treated
as a product failure.

## Conclusion

Every E3-010 acceptance criterion is satisfied. The feature remains
deterministic, session-bound, explicitly clearable, bilingual, and byte-for-byte
non-interfering with all inspected game-owned sources.
