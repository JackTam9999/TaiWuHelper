# Target-threat taxonomy

## Purpose

The threat taxonomy converts verified target mechanics into stable Domain
concepts that later analysis, counter selection, scoring, API, and UI layers
can share. It records what is known and why; it never changes the target,
player, save, or game runtime.

## Threat model

Every `TargetThreat` contains:

- a stable uppercase code;
- a typed `TargetThreatKind`;
- severity;
- a player-readable title and explanation;
- activation timing; and
- one or more evidence records.

Evidence is mandatory. Each `TargetThreatEvidence` retains a source reference,
summary, confidence, and optional raw skill and effect IDs. An unavailable
source must not be replaced with a likely skill or nearby effect.

The initial threat kinds cover mind damage, distraction marks, mind resonance,
persistent defeat marks, repeated attacks, penetration, movement, weapon and
trick disruption, range control, practice-direction suppression, and
combat-start effects. They are mechanics, not presentation strings.

## Severity scale

Severity has a stable ascending order:

| Severity | Meaning |
|---|---|
| Informational | Relevant evidence that does not materially endanger the objective by itself |
| Moderate | Reduces reliability or warrants mitigation, but does not normally defeat the objective alone |
| High | Creates severe pressure or can disable the intended plan if left unanswered |
| Critical | Can directly defeat the stated objective or requires an opening/hard counter |

Severity represents impact on the selected objective, not a claimed win
probability. Later analysis may use the numeric order for deterministic
ranking, but it must retain the evidence and explanation.

## Activation timing

The taxonomy distinguishes:

- always active;
- combat start;
- skill use;
- hit;
- mark application;
- threshold; and
- unknown timing.

This allows later recommendation and UI work to distinguish opening counters
from active defenses and conditional mitigations.

## Unknown mechanics

`UnknownTargetMechanic` retains its description, evidence reference, and
optional raw skill/effect identity. `TargetThreatTaxonomy.Normalize` emits an
`UNRECOGNIZED_TARGET_MECHANIC` warning for every unknown mechanic and does not
create a typed threat for it.

This is intentional. A warning remains visible to users and later analysis
without giving unsupported mechanics a severity, counter, or score.

## Golden magic-sound taxonomy

The golden target currently has three recognized threats:

| Code | Kind | Severity | Timing |
|---|---|---|---|
| `POSITIVE_MAGIC_SOUND_MIND_DAMAGE` | Mind-damage pressure | High | On skill use |
| `DISTRACTION_MARK_ACCUMULATION` | Distraction-mark accumulation | Critical | On hit |
| `MIND_RESONANCE_CASCADE` | Mind-resonance cascade | Critical | On mark applied |

The critical cascade records the verified chain:

1. mind-loss damage produces distraction marks;
2. the first mark starts the mind-resonance countdown; and
3. resonance creates repeated pressure and can make new marks persistent.

The player's observed reset at 36 defeat marks resembles reverse 九色玉蟬法,
but the current snapshot does not prove that the target equipped that source
effect. It therefore remains an unknown-mechanic warning until new target
evidence confirms it.

## Deterministic target analysis

`TargetThreatAnalyzer` combines an immutable `CombatSnapshot` with a
versioned `TargetThreatRuleSet`. A rule signature contains an exact skill ID,
Direct or Reverse direction, and raw effect ID. Analysis stops with a warning
when the snapshot GameData version is unavailable or does not exactly match
the rule set.

Candidate skills are traversed in this order:

1. equipped skills in category and loadout order;
2. remaining learned skills in ascending skill-ID order.

Every finding retains all matching `TargetThreatSource` values and labels them
as `Equipped` or `LearnedUnequipped`. Final findings are sorted by descending
severity, source scope, and ordinal stable code. Reordering the rules therefore
cannot change the result.

An unavailable target loadout does not become an empty equipped loadout. The
analyzer emits `TARGET_EQUIPPED_SKILLS_UNAVAILABLE` and may still report
learned-skill evidence with the weaker `LearnedUnequipped` scope.

The golden rules cover all 16 type-13 magic-sound skill IDs and their verified
Direct effect IDs. The current target snapshot contains eight matching Direct
skills (`719`, `721`, `722`, `724`, `725`, `727`, `731`, and `733`) but no
equipped-skill evidence. Its ordered output is:

1. `DISTRACTION_MARK_ACCUMULATION` — Critical;
2. `MIND_RESONANCE_CASCADE` — Critical; and
3. `POSITIVE_MAGIC_SOUND_MIND_DAMAGE` — High.

Unknown directions, missing effect IDs, changed effect IDs, missing learned
records for equipped skills, and unresolved rule mechanics remain warnings.
The analyzer does not infer an equipped loadout or invoke any game behavior.
