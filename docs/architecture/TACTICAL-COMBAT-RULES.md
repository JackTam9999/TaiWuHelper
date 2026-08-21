# Versioned tactical transition and skill-role rules

| Field | Value |
|---|---|
| Status | Implemented |
| Epic | [EPIC-008](../roadmap/epic-008/EPIC.md) |
| Backlog item | [E8-003](../roadmap/epic-008/BACKLOG.md#e8-003--define-versioned-causal-transition-and-tactical-role-rules) |
| Domain contract | [Tactical combat Domain](./TACTICAL-COMBAT-DOMAIN.md) |
| Evidence boundary | [E8-000 tactical evidence](../scenarios/E8-000-tactical-combat-evidence.md) |
| Rule version | `TACTICAL_COMBAT_RULES@1.0.0` |
| Supported GameData | Historical `1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a`; current exact-phase `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` |

## Purpose

Encode only the causal relationships and exact skill roles authorized by the
historical E8-000 evidence or the current E8-F01 through E8-F03 evidence. The
rules extend the existing effect catalogue, combat counters, target-threat
taxonomy, and target playbook goals. They do not derive mechanics from names,
descriptions, nearby IDs, archetype similarity, or another GameData version.

The current installed version is supported only by
`CurrentLaterMagicSound`, for the exact E8-F02 phase goals and 19 E8-F03
roles. Historical rules never fall through into that version.

## Rule-set aggregate

`VerifiedTacticalCombatRuleSets.HistoricalMagicSound` contains:

- semantic rule version `1.0.0`;
- one exact supported GameData product version;
- the four selected exact-target goal codes from the existing golden threat
  and playbook vocabulary;
- 14 `TacticalTransitionRule` definitions;
- 7 `TacticalSkillRoleRule` definitions; and
- an uppercase SHA-256 semantic fingerprint.

`VerifiedTacticalCombatRuleSets.CurrentLaterMagicSound` separately contains:

- semantic rule version `1.0.0` and one exact current GameData version;
- six exact target-goal codes, excluding the absent historical reset loop;
- 21 typed transitions and 19 exact skill roles;
- seven explicit use kinds covering passive, active, switching, opening and
  persistent behavior; and
- fingerprint
  `64051C1234CECDFDCE070134FDA0380826154D16C1F171B52B6F7FE1C64ECD5D`.

`TacticalCombatRuleSet` copies, validates, deduplicates, and canonically orders
all definitions. Every child rule must use the rule-set semantic version,
exact supported source-version set, and known target goals. Role transition
references must resolve inside the same set.

## Approved purpose vocabulary

`TacticalRulePurpose` is an allowlist. The initial rule set uses:

- direct magic-sound mind pressure;
- distraction-mark accumulation;
- mind-resonance countdown and cascade;
- defeat-mark reset;
- cast suppression and its Direct-practice self-lock;
- self-lock recovery through a feasible Reverse-practice cast;
- mark and resonance duration reduction;
- hindrance-mark removal;
- enemy attack-power reduction;
- random true-Qi reset-resource pressure; and
- conditional hindrance-mark transfer.

The type also reserves `DamageChannelChoice` and `FinishWindowSupport`, but
E8-000 approved neither for this vertical, so no delivered rule or role uses
them. Their presence in the enum is not evidence of availability.

## Delivered causal transitions

| Relationship | Timing | Exact limitation |
|---|---|---|
| Direct magic cast creates positive mind pressure | During cast | No strength, frequency, or hit inference |
| Mind pressure creates distraction marks | Observed state | No prediction of when a mark appears |
| First mark starts resonance countdown | Observed state | Live count must be confirmed |
| Zero countdown starts resonance cascade | Observed state | No elapsed-time simulation |
| Defeat threshold can trigger reverse reset | Observed state | Live Qiqiao and next reset cost unavailable |
| Reverse `604` suppresses a Direct cast | During cast | Exact Reverse direction and feasible cast required |
| Reverse `604` applies three Direct-lock layers | After cast | No Direct-practice action while a layer remains |
| One feasible Reverse cast removes one lock layer | After manual action | Three exact executable casts are not preselected |
| Reverse `686` removes a hindrance mark | Combat start/threshold | Finite layer pool and threshold required |
| Reverse `134` shortens resonance duration | Active observed state | Applies only while the exact agility is active |
| Direct `267` shortens distraction duration | Before combat | Exact Direct direction and equipment required |
| Reverse `624` reduces attack power | After cast | Reduction depends on achieved effectiveness |
| Reverse `291` pressures random true-Qi | Observed damage state | Random type does not guarantee Qiqiao |
| Reverse `611` transfers hindrance marks | After manual release | Weapon release, durability, and trick cost required |

Transitions contain trigger and resulting fact identities, timing, purpose,
target goals, prerequisites, evidence, and limitation. They expose no apply,
advance, simulate, or prediction operation.

## Delivered exact skill roles

| Skill | Direction/effect | Purpose | Shared counter |
|---:|---|---|---|
| `604` | Reverse / `1064` | Direct-cast suppression with self-lock | `REVERSE_JINNI_SUPPRESSION` |
| `686` | Reverse / `1422` | Hindrance-mark removal | `REVERSE_LAOJUN_MARK_CLEAR` |
| `134` | Reverse / `973` | Resonance-duration reduction | `REVERSE_WANHUA_RESONANCE` |
| `267` | Direct / `165` | Distraction-duration reduction | `DIRECT_MOYU_MARK_DURATION` |
| `624` | Reverse / `1234` | Enemy attack-power reduction | `REVERSE_FULONG_POWER_REDUCTION` |
| `291` | Reverse / `915` | Random true-Qi reset pressure | `REVERSE_QILUN_TRUE_QI_DRAIN` |
| `611` | Reverse / `1165` | Conditional hindrance-mark transfer | None; not a generic counter or recovery step |

Each role owns one exact existing `CombatEffectCatalogEntry`. Construction
requires the complete typed-mechanic set on that entry, not just one matching
mechanic. Skill ID, Direct/Reverse direction, raw effect ID, mechanic set,
timing, purpose, target goals, transition references, prerequisites, evidence,
and limitation all remain typed.

Six roles reuse existing `CombatCounterRule` instances. The tactical wrapper
can expose only selected goal codes already covered by that counter. It also
validates exact effect identity, complete mechanics, and compatible activation
timing. Reverse `611` remains a conditional exact role without being added to
the broad shared counter catalogue.

The current set uses its own exact current effect catalogue and counter set.
It includes Reverse `604`, four conditional Reverse recovery attacks, five
agility/control roles, four active-defense roles, four equipped-passive roles,
and Reverse `624` as an opening/persistent alternative. Weapon subtype,
stance, breath, distance, defense true Qi, active-role, equipped-passive and
manual trick/condition requirements are typed. Manual confirmation always
evaluates `Unknown`; the role's existence is not proof that it is executable.

## Evidence prerequisites

Every rule lists all of its evidence prerequisites. A prerequisite has:

- stable identity;
- `BroadRule` or `ExactTarget` scope;
- required evidence source kind; and
- required `Confirmed` disposition.

Resolution evaluates every prerequisite identity. Confirming one member of a
multi-part chain cannot satisfy the others. For example, the mark transition
requires exact active target signature evidence, the verified direct
magic-sound mechanic, and the verified mind-loss-to-distraction relationship.

Observations distinguish `Confirmed`, `Contrary`, `Absent`, `Incomplete`, and
`Conflicting`. Exact-target `Contrary` evidence for an identity overrides a
broad confirmation of that identity. Exact-target absence alone is not
contrary; a broad verified relationship may remain applicable, while a missing
required exact-target confirmation is `Incomplete`.

Resolution precedence is:

1. `Contrary`;
2. `Conflicting`;
3. `Incomplete`; and
4. `Applicable`.

A role inherits any non-applicable state from each referenced transition, so a
role cannot remain applicable when its causal relationship is contrary,
conflicting, or incomplete.

## Version resolution

`TacticalCombatRuleSet.Resolve` first compares the requested GameData version
using exact ordinal equality. An unsupported version returns
`UnsupportedGameDataVersion` with empty transition and role matches. It does
not examine evidence, fall back to the only known version, or expose historical
rules as suggestions.

For a supported version, unknown target-goal codes and mismatched evidence
versions are invalid requests. Relevant rules are selected only when their
target goals intersect the exact requested goal set.

## Raw description boundary

The existing effect catalogue retains localized names, raw descriptions, and
source references as display evidence. Tactical construction and resolution
never inspect those strings. Rule identity and fingerprint use exact IDs,
direction, typed mechanics, timing, goals, prerequisites, transitions,
versions, and limitations.

Tests replace the display name, raw description, and raw source reference on an
otherwise identical typed effect and prove that the tactical rule-set
fingerprint does not change. Changing typed mechanics, IDs, direction, timing,
goals, evidence, or versions does change or invalidate the rule.

## Construction failures

Construction rejects:

- duplicate transition or role identities;
- duplicate or empty source versions, goals, mechanics, references, or
  prerequisites;
- unknown transition references or target goals;
- invalid enum timing, purpose, source, scope, or evidence disposition;
- a role purpose incompatible with its role kind;
- missing, partial, duplicated, or unrecognized typed effect mechanics;
- a shared counter with another effect, direction, mechanic set, timing, or
  target goal;
- child semantic versions or supported-version sets inconsistent with the
  aggregate; and
- evidence whose GameData or tactical rule version is inconsistent.

## Verification

Focused tests pin every delivered transition, exact role, shared-counter link,
typed mechanic, interaction, limitation, prerequisite, precedence rule,
goal-filtering behavior, raw-text boundary, construction failure, and current-
version unsupported result:

```powershell
dotnet test tests\TaiWu.Domain.UnitTests\TaiWu.Domain.UnitTests.csproj -c Release --no-restore -- --filter-class '*TacticalCombatRuleTests*'
```

The completed verification passed 16 focused tests, all 589 Domain tests, and
all 105 architecture tests with no failures or skips. Domain and Domain-test
formatting checks were clean, and the Release solution build completed with no
warnings or errors.
