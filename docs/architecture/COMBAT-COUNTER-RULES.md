# Evidence-backed combat-counter rules

## Purpose

Counter rules connect verified target threats to skills the player may choose
manually. They describe requirements and expected defensive value. They do not
equip skills, change practice direction, invoke effects, write saves, or
control the game.

## Rule boundary

Every `CombatCounterRule` contains:

- a stable counter code;
- one or more stable target-threat codes;
- HardCounter or Mitigation strength;
- required activation timing;
- one recognized `CombatEffectCatalogEntry`;
- zero or more typed combat requirements; and
- a player-readable rationale.

The catalog entry is the evidence boundary. It supplies the exact GameData
version, skill ID, Direct or Reverse direction, raw effect ID, source text,
source key, and typed mechanics. A counter cannot be constructed from an
unmapped catalog entry with no typed mechanic.

Activation timing is one of:

- `CombatStartPassive`;
- `EquippedPassive`;
- `ActiveAttack`;
- `ActiveDefense`; or
- `ActiveAgility`.

Timing is separate from direction. A skill can be learned in the right
direction but still fail because the proposed context does not equip or
activate it correctly.

## Golden magic-sound rules

| Counter | Strength | Required use | Threat coverage |
|---|---|---|---|
| Reverse 金猊镇魔刀 | HardCounter | Active attack | Positive magic-sound pressure, distraction marks, mind resonance |
| Reverse 老君拂尘功 | Mitigation | Combat-start equipped passive | Distraction marks, mind resonance |
| Reverse 万花听雨式 | Mitigation | Active agility | Mind resonance |
| Direct 墨玉功 | Mitigation | Equipped passive | Distraction marks, mind resonance |
| Reverse 伏龙刀法 | Mitigation | Active attack | Positive magic-sound pressure, distraction marks |
| Reverse 七轮感应法 | Mitigation | Equipped passive | Repeatable defeat-mark reset |

Reverse 金猊 is classified as the hard counter because its verified effect
interrupts, clears, and temporarily prevents enemy Direct-practice skills. The
other rules reduce power, clear marks, shorten dangerous durations, or pressure
the resource behind the reset, so they mitigate the threat without guaranteeing
suppression. In particular, Reverse 七轮感应法 adds a slowly decreasing
random-type true-Qi damage state when the target receives a damage state. It
can drain Qiqiao true-Qi, but the type is random and it first requires the
target to receive a damage state; the helper must never describe it as a
guaranteed reset lockout.

## Player-access evaluation

`CombatCounterAccessEvaluator` evaluates every rule against the immutable
player snapshot and a proposed combat-requirement context:

1. confirm the player learned the skill;
2. confirm its required Direct or Reverse state;
3. confirm the direction-specific effect exists;
4. compare its raw effect ID with the verified catalog entry; and
5. evaluate every activation and combat requirement.

All issues are returned. A counter is accessible only when candidate
eligibility passes, the effect identity matches, and no hard requirement is
rejected. Conditional requirement warnings do not masquerade as failures.

“Accessible” means the player and proposed context can support the rule. It
does not mean the helper changed the current loadout or game. Later candidate
generation must still fit the skill into a feasible proposed loadout.

For the confirmed current direction profile, neutral 金猊 fails the Reverse
requirement and reverse 墨玉 fails the Direct requirement. Reverse 老君,
reverse 萬花, reverse 伏龍, and reverse 七轮 pass direction/effect eligibility
when the proposed context equips the passives and actively runs 萬花. A
separate current inner-power compatibility check may still reject an active
attack such as 伏龍 for a specific inner-power state.

Recommendation options keep this direction check strict by default. The
presence of both raw Direct and Reverse effect IDs does not prove that the
player can change practice direction. A manual direction change may be
proposed only when separate current-player evidence explicitly permits it.

Epic 8 wraps six of these exact rules with causal transitions, narrower
exact-target goals, evidence prerequisites, execution costs, and recovery
limitations. It does not modify or name-match the shared counter catalogue.
See [Versioned tactical transition and skill-role rules](./TACTICAL-COMBAT-RULES.md).
