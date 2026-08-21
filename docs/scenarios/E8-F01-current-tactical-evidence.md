# E8-F01: Current-version tactical-combat evidence gate

| Field | Value |
|---|---|
| Status | In progress — static and player evidence captured; behavioral verification pending |
| Backlog item | [E8-F01](../roadmap/epic-008/BACKLOG.md#e8-f01--reverify-the-current-gamedata-and-representative-combat-evidence) |
| Inspection date | 2026-08-21 |
| Runtime GameData | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` |
| Sanitized record | [E8-F01-current-tactical-metadata.json](./evidence/E8-F01-current-tactical-metadata.json) |

## Decision

The current-version evidence gate has started, but it does **not** yet authorize
production tactical rules for the installed runtime.

The read-only capture proved that all 19 initial complementary candidate skills
exist in the installed configuration with exact stable identity, category,
grade, element, equipment type, base grid cost, configured timing values,
Direct/Reverse effect IDs, requirement identities and values, and bilingual
Direct/Reverse display text. It also proved that the current save contains all
19 candidates and preserved their active or achievable direction state.

These are static and player-feasibility facts. Unchanged IDs and display text do
not prove current runtime behavior, activation timing semantics, interactions,
or limitations. `VerifiedTacticalCombatRuleSets.HistoricalMagicSound` therefore
remains historical-only, and the installed runtime must continue to return
`UNSUPPORTED_GAME_DATA_RULE_CHAIN` until the remaining behavior evidence is
accepted.

## Read-only method

The capture used two new opt-in guarded integration checks:

1. `Current_candidate_definitions_are_available` read the installed runtime,
   configuration, bilingual combat-skill, special-effect, and legendary-book
   sources twice and compared all eight files before and after.
2. `Current_player_candidate_state_is_repeatable` read one immutable combat
   snapshot twice, compared its save and GameData revisions, and compared the
   save plus the same eight installed sources before and after.

The checks use the repository's read-only configuration source and archive
reader. They do not rebuild the helper catalogue, write a test database, cache
player state, persist an observation, or mutate the save or game.

## Version identity finding

The installed assemblies expose two different version granularities:

| Source | Observed version | Decision |
|---|---|---|
| `GameData.dll` runtime | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` | Exact runtime rule version |
| `GameData.Shared.dll` configuration | `1.0.0` | Insufficient by itself for behavior authorization |

The configuration reader also captures the exact configuration-assembly
fingerprint. Future current-version rules must bind the full runtime product
version and the exact configuration fingerprint; matching the broad `1.0.0`
configuration version is not sufficient.

## Static candidate result

The first bounded set contains 19 of 19 expected definitions with zero import
errors:

| Role group | Skill IDs | Captured static fields |
|---|---|---|
| Suppression and recovery attacks | `599`, `602`, `604`, `616`, `624`, `686` | Category, grade, element, cost, timing, effects, requirements, bilingual text |
| Agility, speed, hit, and distance control | `134`, `147`, `148`, `150`, `151` | Same exact static field set |
| Active defense and counter-pressure | `2`, `289`, `295`, `303` | Same exact static field set |
| Equipped mind and defense support | `252`, `265`, `267`, `280` | Same exact static field set |

The exact expected record identities are retained in
`CurrentTacticalCombatEvidenceIntegrationTests`; complete localized
descriptions are read and checked for presence but are not committed as
mechanical rules.

The seven historical tactical candidates retain their expected current
configuration effect IDs where they overlap this set. That continuity is useful
for investigation, but it does not carry historical behavioral authorization
across the runtime-version boundary.

## Current player result

The captured save contains all 19 candidates; 9 were equipped in the disk-save
revision and none of the 19 was mastered. Direction evidence was:

| State | Skill IDs | Count |
|---|---|---:|
| Active Reverse | `134`, `150`, `280`, `303`, `604`, `616` | 6 |
| Active Direct | `2`, `147`, `148`, `252`, `267`, `289`, `599`, `602` | 8 |
| Not broken through; Reverse available now | `151`, `265` | 2 |
| Not broken through; Direct or Reverse available now | `295`, `624`, `686` | 3 |

This distinction is a hard boundary. A skill whose Reverse breakthrough is
available is not currently Reverse-active. A skill that is currently Direct
also cannot be treated as switchable to Reverse until the required page and
direction evidence is separately projected.

## Capacity-source conflict

The disk snapshot exposes unavailable used-slot values and reconstructed
capacities `6/9/6/10/5`, with eight universal slots allocated `1/3/1/3` across
attack, agility, defense, and assistance. The newer confirmed manual screen
observation remains `6/10/7/9/4`.

The screen observation is authoritative for the current proposed loadout. The
disk values remain useful evidence of the saved revision, but cannot overwrite
the newer screen capacity or invent used-slot totals. E8-F04 must carry this
precedence as typed current-screen/manual observation rather than silently
merging the two sources.

## Unavailable helper sources

The helper-owned SQLite catalogue had non-empty WAL/SHM state during this
capture. The combat-loadout skill correctly refused to open it with immutable
mode, and no checkpoint, rebuild, migration, or cleanup was attempted. The
installed GameData and language sources were used instead.

The standalone skill inspector also failed closed before opening the save. Its
compiler recursively included unrelated generated C# files from the broad
workspace, reproducing the E8-013 failure mode. The repository guarded archive
reader succeeded, and the inspector was not rebuilt or worked around by
modifying its source discovery during this evidence gate.

## Remaining F01 evidence

F01 remains in progress because these facts are not yet independently
authorized for the current runtime:

A one-time read-only reflection probe confirmed that the runtime contains
specialized combat-skill effect classes, but its public surface exposed only
type and member names. That cannot prove numerical behavior, trigger
conditions, direction differences, or interactions. The exploratory failing
probe was therefore removed instead of being retained as a false evidence
gate.

- the behavioral meaning and activation timing of each new Direct/Reverse
  effect identity;
- the current-version interaction between Reverse `604`, its three-layer
  self-lock, and exact feasible Reverse recovery casts;
- active-only defense and agility effects versus equipped passive effects;
- weapon, trick, distance, resource, stance, breath, and backlash requirements
  that cannot be inferred from static requirement IDs alone; and
- exact target/encounter mechanics, which belong to E8-F02 after the F01 rule
  boundary is accepted.

No current-version production rule will be added until the minimum behavioral
set is complete. Unknown candidates stay unsupported rather than receiving a
role from their name or raw description.

## Verification

```powershell
$env:TAIWU_INTEGRATION_CURRENT_TACTICAL_EVIDENCE = '1'
$env:TAIWU_INTEGRATION_SAVE_PATH = '<current-save>'
dotnet test tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj -c Release --no-restore -- --no-progress --filter-class '*CurrentTacticalCombatEvidenceIntegrationTests*'
```

Result: 2 passed, 0 failed, 0 skipped. The static test retained 8 of 8 guarded
files; the player test retained 9 of 9 guarded files, including the save.

No save, game file, installed language resource, helper catalogue, running
process, runtime memory location, or in-game state was modified.
