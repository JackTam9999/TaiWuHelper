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

The catalogue intentionally does not guess a 功法 for the three new profile
families. E5-000 verified their target-side predicates, but did not verify a
corresponding player-side effect and counter rule. Those goals therefore keep
an explicit `NoVerifiedOption` gap until separate evidence is reviewed.

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
| `VerifiedTargetCounterPlaybooks` | Initial baseline plus three evidence-gated families |

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
static catalogue uses `NoVerifiedOption` and `IncompleteEvidence`. E5-006 may
materialize `InaccessibleVerifiedOption` after the existing player-access and
feasibility checks run.

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
| `MIND_RESONANCE_RESET_BASELINE` | Survive mind damage, control distraction marks, break resonance, pressure the defeat-mark reset | Six existing Epic 1 counter rules, attached only to threats they address | Random true-Qi drain is not a guaranteed reset lockout |
| `OUTER_DAMAGE_CONFIGURED` | Prepare for verified configured outer-damage access | None | No verified outer-damage counter |
| `CHANNEL_RESISTANCE_ASYMMETRY` | Preserve access to the lesser-resisted channel | None | No verified channel-access option |
| `POISON_APPLICATION_CONFIGURED` | Prepare for verified configured poison application | None | No verified poison counter |

The last three are useful playbooks even while their option lists are empty:
they preserve a stable mechanical goal, priority, timing, conflict boundary,
evidence, and a truthful reason why the helper cannot yet choose a 功法.

### Baseline goal mapping

| Sequence | Goal | Priority / response timing | Existing typed threat | Verified counters |
|---:|---|---|---|---|
| 10 | `SURVIVE_MIND_DAMAGE_PRESSURE` | Critical / combat-start passive | `POSITIVE_MAGIC_SOUND_MIND_DAMAGE` | Reverse 金猊 suppression; Reverse 伏龍 power reduction |
| 20 | `CONTROL_DISTRACTION_MARKS` | Critical / combat-start passive | `DISTRACTION_MARK_ACCUMULATION` | Reverse 金猊; Reverse 老君; Direct 墨玉; Reverse 伏龍 |
| 30 | `BREAK_MIND_RESONANCE_CASCADE` | Critical / combat-start passive | `MIND_RESONANCE_CASCADE` | Reverse 金猊; Reverse 老君; Reverse 萬花; Direct 墨玉 |
| 40 | `PRESSURE_DEFEAT_MARK_RESET` | Critical / equipped passive | `DEFEAT_MARK_RESET_LOOP` | Reverse 七輪 random true-Qi drain mitigation |

Across those goals, every one of the four existing baseline threats and all six
existing counter rules is retained. Options may appear under multiple goals;
E5-005 composes and deduplicates them by stable counter identity.

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

E5-005 will compose matched playbooks and surface conflicts. E5-006 will pass
their options through the existing access evaluator, slot/capacity feasibility,
bounded candidate generation, scoring, explanation, manual-plan, and
comparison pipeline. An inaccessible option becomes a typed gap; it is never
replaced by a name-similar unverified skill.

## Verification

Focused Domain tests cover:

- all four delivered versioned families;
- exact baseline threat, counter, effect, timing, requirement, and evidence
  linkage;
- the three new typed goals and their explicit no-option gaps;
- exact-version and unknown-archetype resolution;
- construction invariants and inaccessible-option gap references;
- rejection of reconstructed or unregistered counter rules;
- deterministic goal, option, evidence, archetype, and playbook ordering; and
- absence of target identity and complete-loadout fields.
