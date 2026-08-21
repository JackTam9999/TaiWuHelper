# E8-F01: Current-version tactical-combat evidence gate

| Field | Value |
|---|---|
| Status | Complete — current candidate definitions, player state and behavior contracts verified |
| Backlog item | [E8-F01](../roadmap/epic-008/BACKLOG.md#e8-f01--reverify-the-current-gamedata-and-representative-combat-evidence) |
| Inspection date | 2026-08-21 |
| Runtime GameData | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` |
| Sanitized record | [E8-F01-current-tactical-metadata.json](./evidence/E8-F01-current-tactical-metadata.json) |
| Behavior record | [E8-F01 current behavior contracts](./evidence/E8-F01-current-behavior-contracts.md) |

## Decision

The current-version candidate evidence gate is complete. It authorizes the
recorded static, player-feasibility and candidate-behavior facts as inputs to
E8-F02 through E8-F04, but it does **not** by itself authorize a production
tactical rule set for the installed runtime.

The read-only capture proved that all 19 initial complementary candidate skills
exist in the installed configuration with exact stable identity, category,
grade, element, equipment type, base grid cost, configured timing values,
Direct/Reverse effect IDs, requirement identities and values, and bilingual
Direct/Reverse display text. It also proved that the current save contains all
19 candidates and preserved their active or achievable direction state.

The behavior gate separately resolved all 19 concrete runtime effect types and
audited their direction branches, activation events, shared base behavior and
called combat operations. Exact inherited method-chain fingerprints now fail
if implementation changes despite unchanged IDs or text. The existing
`VerifiedTacticalCombatRuleSets.HistoricalMagicSound` remains historical-only,
and the installed runtime must continue to return
`UNSUPPORTED_GAME_DATA_RULE_CHAIN` until the exact target and minimum typed
current-version rule set are both supplied. E8-F02 has since completed the
exact-target gate and E8-F03 has completed the typed-role gate.

## Read-only method

The capture used three opt-in guarded integration checks:

1. `Current_candidate_definitions_are_available` read the installed runtime,
   configuration, bilingual combat-skill, special-effect, and legendary-book
   sources twice and compared all eight files before and after.
2. `Current_player_candidate_state_is_repeatable` read one immutable combat
   snapshot twice, compared its save and GameData revisions, and compared the
   save plus the same eight installed sources before and after.
3. `Current_candidate_behavior_contracts_are_version_bound` loaded a byte copy
   of the installed runtime assembly, inspected metadata and method bodies for
   all 19 exact behavior chains, and preserved the installed assembly before
   and after. It did not instantiate effects or invoke combat handlers.

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

Five of the seven historical production candidates overlap this set. Their
cost, effect identity and behavior were compared field by field in the
[behavior record](./evidence/E8-F01-current-behavior-contracts.md); all five
were reverified against the current runtime rather than authorized from
continuity. Historical candidates `291` and `611` remain outside this gate.

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
the newer screen capacity or invent used-slot totals. E8-F04 now carries this
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

## Behavior result and downstream boundary

The completed behavior audit distinguishes active attacks, active defenses,
active agility skills, equipped assistance effects and combat-start layers. It
also reverified the symmetric Direct/Reverse `604` suppression and three-layer
recovery contract. The sanitized semantic matrix and exact implementation
fingerprints are in the
[behavior record](./evidence/E8-F01-current-behavior-contracts.md).

F01 does not invent the missing execution context. Weapon, trick, distance,
resource, stance, breath, backlash, effective cost and active-role values stay
explicit inputs to E8-F04. Exact target and encounter mechanics belong to
E8-F02, while role selection and production rule authorization belong to
E8-F03. Unknown or unselected behavior stays unsupported.

## Verification

```powershell
$env:TAIWU_INTEGRATION_CURRENT_TACTICAL_EVIDENCE = '1'
$env:TAIWU_INTEGRATION_SAVE_PATH = '<current-save>'
dotnet test tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj -c Release --no-restore -- --no-progress --filter-class '*CurrentTacticalCombatEvidenceIntegrationTests*'
```

Result: 3 passed, 0 failed, 0 skipped. The static test retained 8 of 8 guarded
files; the player test retained 9 of 9 guarded files, including the save; the
behavior test retained the installed runtime assembly unchanged.

No save, game file, installed language resource, helper catalogue, running
process, runtime memory location, or in-game state was modified.
