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
persistent defeat marks, defeat-mark resets, repeated attacks, penetration,
movement, weapon and trick disruption, range control, practice-direction
suppression, and combat-start effects. They are mechanics, not presentation
strings.

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

The golden target currently has four recognized threats:

| Code | Kind | Severity | Timing |
|---|---|---|---|
| `POSITIVE_MAGIC_SOUND_MIND_DAMAGE` | Mind-damage pressure | High | On skill use |
| `DISTRACTION_MARK_ACCUMULATION` | Distraction-mark accumulation | Critical | On hit |
| `MIND_RESONANCE_CASCADE` | Mind-resonance cascade | Critical | On mark applied |
| `DEFEAT_MARK_RESET_LOOP` | Defeat-mark reset | Critical | At defeat threshold |

The critical cascade records the verified chain:

1. mind-loss damage produces distraction marks;
2. the first mark starts the mind-resonance countdown; and
3. resonance creates repeated pressure and can make new marks persistent.

The target's reset is now verified by two independent sources. Installed
effect `911` states that Reverse 九色玉蝉法 consumes 9 Qiqiao true-Qi at the
defeat condition, clears all injury, hindrance, and critical-injury marks, and
raises the next cost by 9 up to 99. A local-only battle frame shows the target
triggering Reverse 九色玉蟬法 and reporting `消除己之標記`. The screenshot
confirms the live source; the version-matched configuration supplies the exact
threshold and escalating costs. This is therefore a typed Critical threat,
not an unknown warning.

## Deterministic target analysis

`TargetThreatAnalyzer` combines an immutable `CombatSnapshot` with a
versioned `TargetThreatRuleSet`. A rule signature contains an exact skill ID,
Direct or Reverse direction, and raw effect ID. Analysis stops with a warning
when the snapshot GameData version is unavailable or does not exactly match
the rule set.

Candidate skills are traversed in this order:

1. current-screen observed equipped skills in category, visible-slot, and
   stable-ID order;
2. remaining save-equipped skills in category and loadout order;
3. remaining learned-but-unconfirmed skills in ascending skill-ID order.

Every finding retains all matching `TargetThreatSource` values and labels them
as `ObservedEquipped`, `SaveEquipped`, or `LearnedUnconfirmed`, with an opaque
membership evidence reference. The existing `Equipped` and
`LearnedUnequipped` scopes remain available for ranking compatibility. Final
findings are sorted by descending severity, source scope, and ordinal stable
code. Reordering the rules therefore cannot change the result.

An unavailable target loadout does not become an empty equipped loadout. The
analyzer emits `TARGET_EQUIPPED_SKILLS_UNAVAILABLE` and may still report
learned-skill evidence with the weaker `LearnedUnequipped` scope. An applied
partial sparring observation can confirm an equipped subset even while the
full loadout remains unavailable; omitted skills remain learned-unconfirmed.

The golden rules cover all 16 type-13 magic-sound skill IDs and their verified
Direct effect IDs, plus Reverse 九色玉蝉法 (`287`, effect `911`). Assistance
passives are retained in the target learned-skill projection so this reset
cannot disappear merely because the disk save lacks the target's selected
combat loadout. Its ordered output is:

1. `DEFEAT_MARK_RESET_LOOP` — Critical;
2. `DISTRACTION_MARK_ACCUMULATION` — Critical;
3. `MIND_RESONANCE_CASCADE` — Critical; and
4. `POSITIVE_MAGIC_SOUND_MIND_DAMAGE` — High.

Unknown directions, missing effect IDs, changed effect IDs, missing learned
records for equipped skills, and unresolved rule mechanics remain warnings.
The analyzer does not infer an equipped loadout or invoke any game behavior.
