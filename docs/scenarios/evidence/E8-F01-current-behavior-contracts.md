# E8-F01 current-version candidate behavior contracts

| Field | Value |
|---|---|
| Status | Complete |
| Evidence date | 2026-08-21 |
| Runtime | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` |
| Candidate count | 19 |
| Production support authorized | Yes for the exact E8-F03 role set; live execution remains gated by E8-F04 context |

## Method and boundary

Each candidate was resolved to its concrete current `GameData.dll` combat-skill
effect type. The audit read the declared method bodies for that type and every
combat-skill base type, then reconciled event registration, direction branches,
constants, affected fields, effect-count changes and called combat operations
with the matching Traditional Chinese and English effect records.

`Current_candidate_behavior_contracts_are_version_bound` pins the full type,
base type, inherited behavior chain, ordered method signatures and IL bytes for
all 19 candidates. Any runtime implementation change invalidates the evidence
even when a skill ID, effect ID or localized description remains unchanged.
The test loads a byte copy of the installed assembly for metadata inspection;
it does not instantiate an effect, initialize combat, invoke a handler or
modify a game source.

Localized text was used to label the audited branches. It was not used to
create behavior absent from the code. Exact live values that depend on power,
distance, resources, weapon state or combat state remain inputs to later
feasibility work rather than inferred defaults.

## Authorized candidate facts

| ID | Current player state | Current-version Direct / Reverse behavior | Timing and limitation |
|---:|---|---|---|
| `2` | Direct, equipped | Both directions reduce direct damage by up to the configured maximum, decaying as direct damage is received | Active defense only; other equipped defenses do not stack as simultaneously active |
| `134` | Reverse, equipped | Direct strengthens own mind-pressure effects and lengthens enemy resonance; Reverse strengthens enemy mind reduction and shortens own resonance | Only while this agility skill is active |
| `147` | Direct, not equipped | Direct lowers enemy hit probability at distance at least 5 as distance grows; Reverse does so at distance at most 7 as distance closes | Only while active; exact distance is required |
| `148` | Direct, not equipped | Direct triggers a weapon attack for accumulated enemy advance; Reverse triggers it for accumulated enemy retreat | Only while active; a usable weapon attack remains required |
| `150` | Reverse, equipped | Direct makes own weapon attacks use precision unless another effect changes hit type; Reverse lets enemy weapon attacks be parried unless another effect changes resolution | Only while active; it is an active agility choice, not a passive stack |
| `151` | Not broken through; Reverse is achievable | Direct adds own cast-speed benefit per accumulated movement; Reverse applies enemy cast-speed loss per accumulated movement | Requires the manual Reverse breakthrough before use and only operates while active |
| `252` | Direct, not equipped | When enemy direct damage creates an injury or fatal mark, Direct restores own mobility and Reverse damages enemy mobility | Equipped assistance effect; duplicate injury events are coalesced within one state-machine update |
| `265` | Not broken through; Reverse is achievable | Direct converts charm into increased own mind pressure; Reverse converts charm into increased mind defense, capped by the runtime rule | Requires manual Reverse breakthrough; charm is a live character input |
| `267` | Direct, equipped | Direct shortens own mind-loss mark duration; Reverse lengthens the enemy's | Equipped assistance effect; current Direct does not provide the Reverse enemy debuff |
| `280` | Reverse, not equipped | At distance below 5, Direct increases four offensive hit/mind values and Reverse increases four avoidance/mind-defense values as distance closes | Equipped assistance effect; current distance is required |
| `289` | Direct, not equipped | A successful weapon counter applies stance-recovery loss in Direct or breath-recovery loss in Reverse | Only while this defense is active and a counter actually succeeds |
| `295` | Not broken through; Direct or Reverse is achievable | While defending, both directions prevent critical injury when the vital point is missed; Direct spends defense true-Qi to remove injury marks, Reverse to remove hindrance marks | Requires manual breakthrough and active defense; each removal uses 3 defense true-Qi and increases disorder |
| `303` | Reverse, equipped | Both directions reduce non-vital direct damage; Direct converts neutral tricks into serious flaws or adds neutral tricks, while Reverse converts mind-loss marks into serious acupoints or inflicts mind damage when none exist | Active defense only; non-follow-up neutralization and target-state branches must be observed |
| `599` | Direct, equipped | At the configured power threshold, Direct gains four conversion layers for selected tricks; Reverse grants two chop tricks | Active blade cast; exact power and usable trick/weapon context are required |
| `602` | Direct, equipped | Direct changes leg injury marks into old injuries; Reverse increases leg direct damage and, at the configured power threshold, exhausts enemy mobility and clears its active agility skill | Active blade cast; body-part hit, power and weapon context are required |
| `604` | Reverse, equipped | Direct suppresses Reverse-practice casts and then locks own Reverse casts; Reverse suppresses Direct-practice casts and then locks own Direct casts | Starts during preparation/cast, clears matching active defense/agility, adds 3 lock layers after completion; a Direct `604` layer is removed by each Direct cast and a Reverse `604` layer by each Reverse cast |
| `616` | Reverse, equipped | Direct changes chest/back injury marks into old injuries; Reverse increases chest/back direct damage and, at the configured threshold, worsens one random external injury | Active blade cast; body-part hit, power and injury availability are required |
| `624` | Not broken through; Direct or Reverse is achievable | Direct increases own attack-skill power and Reverse decreases enemy attack-skill power according to achieved power until combat ends | Requires manual breakthrough and an executable active cast; current elemental-backlash state remains a hard feasibility input |
| `686` | Not broken through; Direct or Reverse is achievable | Combat starts with 6 layers: Direct removes own injury marks beyond half defeat, Reverse removes hindrance marks; a qualifying cast restores one layer up to 3 | Requires manual breakthrough and equipment before combat; layers are finite and a refresh cast still needs weapon/trick feasibility |

No row authorizes simultaneous active defenses or agility skills. “Achievable”
means the current read pages support that manual breakthrough direction; it is
not current combat power until the player completes the breakthrough.

## Historical/current comparison

The historical production rule set had exact typed roles for five candidates
in this F01 set. Their current static identity and audited current behavior
remain compatible, but current authorization comes from the new runtime audit,
not from equality alone.

| Role | Historical evidence | Current result | Player-feasibility change |
|---|---|---|---|
| Reverse `604` / `1064` | Cost 3; active suppression; 3-layer Direct lock and Reverse-cast recovery | Effect ID, cost, timing and full suppression/recovery behavior reverified | Still active Reverse and equipped |
| Reverse `686` / `1422` | Cost 2; combat-start finite hindrance-mark removal | Effect ID, cost, six-layer start, threshold and refresh behavior reverified | Still unfinished, but current pages now permit a manual Reverse breakthrough |
| Reverse `134` / `973` | Cost 3; active resonance-duration mitigation | Effect ID, cost, active-only timing and direction branch reverified | Now equipped as active Reverse in the disk revision |
| Direct `267` / `165` | Cost 1; equipped mark-duration mitigation | Effect ID, cost, equipped timing and direction branch reverified | Still Direct and equipped |
| Reverse `624` / `1234` | Cost 1; post-cast enemy attack-power reduction | Effect ID, cost, power-scaled combat-long behavior reverified | Still unfinished; both breakthrough directions are currently achievable |

Historical roles `291` and `611` are not members of the F01 candidate set and
receive no current-version authorization here. The other 14 candidates had no
typed historical production role to compare; their current contracts were
audited independently and remain inputs for E8-F03 rather than silently
promoted historical rules.

## Remaining gates

- E8-F02 has identified one exact target and encounter phase. Candidate
  behavior alone still does not authorize a tactical role.
- E8-F03 converted only the required audited facts into typed tactical roles.
- E8-F04 supplies weapon, trick, distance, resource, backlash, active-role,
  effective-cost and capacity inputs from one coherent source precedence.
- The installed runtime remains `Unsupported` until the minimum vertical owns
  every required target, role and execution rule.
