# E8-F07: Current golden anti-magic-sound vertical

| Field | Value |
|---|---|
| Status | Complete — current-version package verified end to end |
| Backlog item | [E8-F07](../roadmap/epic-008/BACKLOG.md#e8-f07--verify-the-current-version-golden-anti-magic-sound-vertical) |
| Inspection date | 2026-08-22 |
| Runtime GameData | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` |
| Rule fingerprint | `64051C1234CECDFDCE070134FDA0380826154D16C1F171B52B6F7FE1C64ECD5D` |
| Search bound | 8 options, 256 combinations, result ceiling 256 |
| Sanitized record | [E8-F07 metadata](./evidence/E8-F07-current-golden-vertical-metadata.json) |

## Result

The sanitized current-version recommendation now succeeds only with the exact
current later-phase target chain and its complete authorized evidence set.
Removing that evidence produces the typed `TACTICAL_EVIDENCE_PARTIAL` result
and no compiled plan; an unsupported GameData version remains an unsupported
chain. Current rules are not borrowed from the historical version.

The manually audited reference package is:

| Category | Exact role |
|---|---|
| Attack | Reverse 金猊鎮魔刀 for Direct-practice suppression; Reverse 羅剎刀法 repeated for the three recovery casts |
| Agility | Direct 鐵橋功 as the active long-range control; Reverse 五鬼步 as a switch-only movement backup |
| Defense | Reverse 即身成佛 as the active defense; Reverse 鬼降大法 as a switch-only mind-mark backup |
| Assistance | Direct 墨玉功 for distraction-duration mitigation; Reverse 冰清玉潔 for a separate mind-defense layer |

The sanitized fixture and guarded installed-save projection both use
`0/4/2/6/2` of the five category capacities. They remain within the acceptance
ceiling of `6/9/7/8/4` for a screen-observed `6/10/7/9/4` context.
The active defense and agility backups remain equipped alternatives and are not
scored as simultaneously active effects.

## Why 羅剎刀法 is the recovery choice

The installed-save evidence rejects Reverse 老君拂塵功 with the exact hard gate
`INNER_POWER_BACKLASH_ON_USE`. The planner therefore does not recommend it
merely because its recovery effect is verified. Reverse 羅剎刀法 passes the
same direction, raw-effect, execution, cost, capacity and backlash gates, and
one feasible recovery role may be repeated for all three lock layers.

This distinction is retained in the guarded integration test: Reverse 老君拂塵
功 must remain infeasible while the eight-skill reference package is admitted.
Changing the inner-power state may change that future result, but the current
plan does not invent such a change.

## Determinism and typed failure paths

The current Application vertical repeats the request and reverses learned-skill
order, target goals, rule observations and layering proofs. Context, rule,
candidate decisions, pruning, coverage, score, selected loadout, comparison,
plan and final identity remain identical. The manually audited package is
present in the complete 256-combination search and every reference candidate
has an admitted terminal decision.

The new current fixture covers complete and partial evidence. Existing focused
fixtures continue to pin conflicting and wrong-phase evidence, unsupported
versions, missing execution context, no candidate, all four search bounds,
truncation and cancellation. Historical repeated/shuffled fixtures retain
their historical GameData identity and deterministic artifacts.

API mapping, English and Traditional Chinese rendering, localization
exhaustiveness and architecture safety remain covered by the exact selected
loadout tests introduced in E8-F06 and by the full solution regression.

## Read-only guarded verification

The opt-in current-version integration read the installed GameData catalogue,
language sources and save through the existing read-only readers. SHA-256,
length and last-write time for all nine guarded files were captured before and
after the search and remained equal. No path, hash, character identity, save
content or proprietary description is committed in the sanitized record.

The guarded test completed all 256 combinations and passed with three Reverse
羅剎刀法 recovery casts and the explicit 老君拂塵功 backlash rejection. The
Release build completed with zero warnings and errors. The full suite passed
1,614 of 1,639 tests with 25 expected guarded-local skips and no failures; the
F07 guarded integration also passed separately against the installed sources.
