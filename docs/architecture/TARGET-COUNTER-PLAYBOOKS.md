# Versioned target counter playbooks

This document defines the reusable counter-playbook catalogue delivered by
[E5-004](../roadmap/epic-005/BACKLOG.md#e5-004--define-the-verified-counter-playbook-catalogue).
It connects the typed archetype matches from
[TARGET-ARCHETYPE-MATCHING.md](TARGET-ARCHETYPE-MATCHING.md) to the verified
threat, effect, counter, timing, and requirement contracts from Epic 1.

## Decision

A counter playbook is an ordered set of response goals. It is neither a target
classification nor a complete 功法 loadout. A goal states what must be answered
and why. A playable option exists only when it wraps the exact
`CombatCounterRule` registered by the catalogue for the exact GameData
version.

E5-011 closed the original target-side-only gap with exact, read-only catalogue
evidence. The three new families now reference reviewed effects and counter
rules for 逆練伏龍刀法, both directions of 錯倒陰陽拂塵, and both directions
of 五黃辟毒術. Ownership, breakthrough, direction, active role, and capacity
remain player-specific feasibility checks rather than catalogue assumptions.

## Domain contract

The `TaiWu.Domain.TargetPlaybooks` namespace contains:

| Type | Responsibility |
|---|---|
| `TargetCounterPlaybookIdentity` | Stable archetype identity plus independently versioned playbook identity |
| `TargetCounterPlaybook` | Canonically ordered reusable goals and playbook evidence |
| `TargetCounterPlaybookGoal` | Ordered priority, response timing, typed facets/threats, options, conflict groups, evidence, and known gaps |
| `TargetCounterPlaybookOption` | Exact typed `CombatCounterRule`, effect, activation timing, requirements, conflict groups, and effect evidence |
| `TargetCounterPlaybookGap` | Typed missing, inaccessible, or incomplete response with evidence and an optional exact counter reference |
| `TargetCounterPlaybookCatalog` | Exact-version archetype, reviewed-counter, and playbook registry |
| `TargetCounterPlaybookResolution` | `Resolved`, `UnsupportedGameDataVersion`, or `ArchetypeNotFound` result |
| `VerifiedTargetCounterPlaybooks` | Mind baseline, independent reset overlay, and three evidence-gated families |

No playbook contract contains a target character ID, target name, current
player identity, or fixed complete loadout.

## Evidence and construction boundary

Every mechanical goal must contain at least one of:

- an immutable `TargetProfileFacetIdentity`; or
- an existing typed `TargetThreat`.

Every playable option must contain a `CombatCounterRule`. That rule already
owns its stable counter code, typed threat codes, strength, activation timing,
recognized `CombatEffectCatalogEntry`, practice direction, requirements, and
rationale. The option constructor has no display-name, raw-description, skill
ID, or effect-ID-only path.

The catalogue receives reviewed `CombatCounterRuleSet` instances and checks:

1. every rule set uses the catalogue's exact GameData version;
2. registered rule codes are unique;
3. each option references the exact registered rule instance; and
4. each option addresses at least one typed threat on its owning goal.

This means reconstructing a look-alike rule, copying raw effect text, parsing a
localized skill name, or supplying a nearby effect ID cannot create a playable
option.

## Goal and gap invariants

A goal owns:

- a stable code and explicit non-negative sequence;
- `Critical`, `High`, `Normal`, or `Fallback` priority;
- existing `CombatCounterActivationTiming` semantics;
- canonically ordered profile facets and typed threats;
- canonically ordered verified options;
- potential conflict-group codes;
- evidence references; and
- canonically ordered known gaps.

A goal without a verified option must contain at least one explicit gap. The
gap kinds are:

| Kind | Meaning |
|---|---|
| `NoVerifiedOption` | Target-side evidence exists, but no reviewed playable response exists |
| `InaccessibleVerifiedOption` | A reviewed option exists but the current player/context cannot use it |
| `IncompleteEvidence` | A response exists but a known limitation prevents a stronger claim |

An inaccessible-option gap must name an option on the same goal. The initial
static catalogue retains `IncompleteEvidence` only for the non-guaranteed reset
lockout. E5-006 materializes `InaccessibleVerifiedOption` after the existing
player-access and feasibility checks run.

## Exact version identities

The initial catalogue uses:

- GameData `1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a`;
- profile rule `E5.PROFILE.1`;
- archetype definition version `1.0.0`; and
- playbook version `1.0.0`.

The public playbook key is
`<ARCHETYPE_CODE>@<ARCHETYPE_VERSION>/PLAYBOOK@<PLAYBOOK_VERSION>`.
An observed GameData mismatch returns `UnsupportedGameDataVersion`; the
catalogue never falls forward to a nearby definition or returns a playbook
without its exact version gate.

## Initial catalogue

| Archetype | Reusable goal | Verified playable options | Explicit gap |
|---|---|---|---|
| `MIND_RESONANCE_BASELINE` | Survive mind damage, control distraction marks, break resonance | Five existing Epic 1 counter rules, attached only to threats they address | None |
| `DEFEAT_MARK_RESET_OVERLAY` | Pressure the defeat-mark reset independently of mind/resonance | Reverse 七輪 random true-Qi drain mitigation | Random true-Qi drain is not a guaranteed reset lockout |
| `OUTER_DAMAGE_CONFIGURED` | Prepare for verified configured outer-damage access | Reverse 伏龍 reduces all enemy 摧破 power for the battle | Current player may not own or have completed the required reverse direction |
| `CHANNEL_RESISTANCE_ASYMMETRY` | Attack through the lesser-resisted channel | Direct 錯倒 routes outer injury through inner resistance; Reverse 錯倒 routes inner injury through outer resistance | Exact resistance measurements replace the wrong direction before feasibility |
| `POISON_APPLICATION_CONFIGURED` | Actively defend against verified configured poison application | Direct 五黃 prevents direct poison and reduces corresponding poison; Reverse 五黃 prevents direct poison and reflects it | The effect applies while 五黃 is the active defense |

### Baseline goal mapping

| Sequence | Goal | Priority / response timing | Existing typed threat | Verified counters |
|---:|---|---|---|---|
| 10 | `SURVIVE_MIND_DAMAGE_PRESSURE` | Critical / combat-start passive | `POSITIVE_MAGIC_SOUND_MIND_DAMAGE` | Reverse 金猊 suppression; Reverse 伏龍 power reduction |
| 20 | `CONTROL_DISTRACTION_MARKS` | Critical / combat-start passive | `DISTRACTION_MARK_ACCUMULATION` | Reverse 金猊; Reverse 老君; Direct 墨玉; Reverse 伏龍 |
| 30 | `BREAK_MIND_RESONANCE_CASCADE` | Critical / combat-start passive | `MIND_RESONANCE_CASCADE` | Reverse 金猊; Reverse 老君; Reverse 萬花; Direct 墨玉 |
| 40 | `PRESSURE_DEFEAT_MARK_RESET` | Critical / equipped passive | `DEFEAT_MARK_RESET_LOOP` | Reverse 七輪 random true-Qi drain mitigation |

The first three goals belong to `MIND_RESONANCE_BASELINE`; sequence 40 belongs
to the independent `DEFEAT_MARK_RESET_OVERLAY`. Across both playbooks, every
one of the four existing threats and all six existing counter rules is
retained. A magic-sound target without reset can therefore reuse the mind
responses without falsely requiring the reset signature. Options may appear
under multiple goals.
The composer in
[TARGET-PLAYBOOK-COMPOSITION.md](TARGET-PLAYBOOK-COMPOSITION.md) deduplicates
them globally by stable counter identity while retaining every goal reference.

## Deterministic ordering

Source declaration order cannot change exposed order:

- catalogue archetypes and playbooks sort by stable identity;
- playbook goals sort by sequence, priority, then code;
- options sort by hard counter before mitigation, canonical activation timing,
  then counter code; and
- facets, threats, conflict groups, evidence, and gaps sort by ordinal stable
  identity.

Goal, option, and gap stable identities are their reviewed stable codes.
Content keys additionally retain typed mechanics, timing, evidence, and
conflict data for later deterministic composition checks.

## Recommendation boundary

Catalogue resolution says only that a reusable playbook is verified for the
matched archetype and GameData version. It does not say that the current player
owns, can reverse/direct-practice, can equip, can activate, or can fit an
option.

E5-005 composes matched playbooks, surfaces conflicts, and applies exact-target
adjustments. E5-006 passes only exact-target-confirmed options from that
composition through the existing access evaluator, slot/capacity feasibility,
bounded candidate generation, scoring, explanation, manual-plan, and
comparison pipeline. An inaccessible option becomes a typed gap; it is never
replaced by a name-similar unverified skill. See
[TARGET-PLAYBOOK-PERSONALIZATION.md](./TARGET-PLAYBOOK-PERSONALIZATION.md).

## Verification

Focused Domain tests cover:

- all five delivered versioned definitions and playbooks;
- exact baseline threat, counter, effect, timing, requirement, and evidence
  linkage;
- exact playable options for all three new families;
- resistance-direction replacement from exact outer/inner measurements;
- independent mind/resonance and defeat-reset matching;
- exact-version and unknown-archetype resolution;
- construction invariants and inaccessible-option gap references;
- rejection of reconstructed or unregistered counter rules;
- deterministic goal, option, evidence, archetype, and playbook ordering; and
- absence of target identity and complete-loadout fields.
