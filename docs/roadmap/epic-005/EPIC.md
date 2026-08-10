# EPIC-005: Target archetypes and counter playbooks

| Field | Value |
|---|---|
| Status | In progress |
| Milestone | 5 |
| Target release | TBD |
| Last updated | 2026-08-10 |

## Summary

Turn verified target facts and threats into a reusable, multi-label combat
profile. Match that profile to evidence-backed target archetypes, compose the
corresponding counter playbooks, and then adapt the result to the exact target
and the player's currently feasible 功法 choices.

Epic 5 adds a reusable reasoning layer between target evidence and final
loadout selection:

```text
Target evidence
    -> typed combat profile
    -> zero or more archetype matches
    -> reusable counter playbooks
    -> exact-target adjustments
    -> player-feasible recommendation
```

This extends the immutable snapshots, verified threats, counter rules, and
recommendation engine delivered by
[Epic 1](../epic-001/EPIC.md); the version-aware catalogue from
[Epic 2](../epic-002/EPIC.md); current-screen evidence from
[Epic 3](../epic-003/EPIC.md); and the explanation and comparison surface from
[Epic 4](../epic-004/EPIC.md).

The feature remains information-only. It never changes a target, companion,
loadout, save, game file, running process, or in-game state.

## Context

The current engine can analyze a selected target when exact versioned threat
signatures and counter mappings are already known. Its strongest verified
vertical is the magic-sound, distraction-mark, mind-resonance, and
defeat-reset target used by Epic 1. Adding more targets only as isolated
special cases would duplicate knowledge and make omissions difficult to see.

Targets commonly share tactical characteristics even when their identity and
equipped 功法 differ. Examples include weapon context, high physical pressure,
penetration, repeated attacks, strong physical resilience, poison, mind
pressure, recovery, or threshold resets. These characteristics overlap: one
target may match several at once. Treating them as mutually exclusive classes
would discard relevant evidence and produce brittle recommendations.

Weapon or skill category alone is not a mechanic. A 刀 label does not prove
high physical damage, and a 拳掌 label does not prove high physical defense.
Likewise, a localized name or untyped raw effect description cannot establish
poison, mind-break, recovery, or any other scored behavior. Epic 5 therefore
starts with an evidence gate and preserves `Unknown` whenever the installed
version cannot support a claim.

## Primary user story

> As a player preparing for a selected target, I want the helper to identify
> all verified combat archetypes that apply, start from reusable response
> playbooks, and explain the adjustments made for this exact target and my
> available 功法 so I can understand both the general strategy and the final
> recommendation.

## Supporting user stories

- As a player, I can see that a target belongs to more than one combat
  archetype instead of being forced into one broad class.
- As a player, I can distinguish descriptive context such as weapon family
  from verified tactical mechanics such as penetration or poison pressure.
- As a player, I can see the evidence and freshness behind every matched or
  partially matched archetype.
- As a player, I can understand the baseline response goals shared by similar
  targets.
- As a player, I can see which response goals were changed, elevated, or left
  unresolved for the exact target.
- As a player, I receive only 功法 and directions that remain feasible for the
  current character snapshot.
- As a player, I am told when no verified playbook or accessible counter exists
  instead of receiving an invented generic solution.
- As an API consumer, I receive typed profile, match, playbook, adjustment,
  provenance, and unavailable-state semantics.
- As a bilingual, keyboard, or mobile user, I receive the same facts and
  decision path without relying on color or a wide comparison matrix.

## Goals

1. Define versioned, evidence-backed target combat-profile dimensions.
2. Represent a target as zero or more independent profile facets and
   archetype matches.
3. Separate weapon or attack-family context from damage, defense, control, and
   tempo claims.
4. Reuse the existing typed threat and counter vocabulary rather than
   introducing a competing rules engine.
5. Define counter playbooks as reusable response goals and verified options,
   not fixed universal loadouts.
6. Compose overlapping playbooks deterministically and expose conflicts,
   duplicate coverage, timing pressure, and unresolved gaps.
7. Adapt the composed response to exact target evidence and player feasibility.
8. Explain which recommendation facts came from archetype reuse and which came
   from target-specific adjustment.
9. Deliver at least one existing baseline and three newly verified playbook
   families selected through the evidence gate.
10. Preserve bilingual, accessible, deterministic, and read-only behavior.

## Non-goals

- Assigning every target to one mutually exclusive class.
- Inferring mechanics from a target name, 功法 name, weapon label, category, or
  untyped raw description.
- Statistical clustering, machine learning, automatic rule generation, or
  training from battle outcomes.
- Claiming a probability of victory or a universal numeric target difficulty.
- Comprehensive coverage of every Taiwu target, weapon, damage channel,
  defense, poison, mark, or special effect in one epic.
- Replacing the current feasibility validator, candidate-search bounds,
  policy-score semantics, or manual plan.
- Manufacturing a different lower-ranked loadout merely to make archetypes
  appear distinct.
- Companion selection or development, village management, or library planning.
- Screenshot capture, upload, OCR, or automatic image interpretation.
- Persisting target-profile history, observations, playbook preferences,
  recommendations, or battle outcomes.
- Equipping 功法, changing direction, allocating slots, controlling the game,
  or modifying game-owned state.

## Product principles

### Multi-label, not mutually exclusive

Attack family, pressure, resilience, control, and tempo are independent
dimensions. A target may match several archetypes, and a later exact-target
rule may refine one dimension without replacing the others.

### Facts precede classifications

Classification consumes typed facts with provenance. It does not parse UI
copy or infer tactics from localized names. A fact must identify its source,
version, evidence state, and exact normalized meaning before it can influence
an archetype.

### High and low require documented semantics

Labels such as `HighPhysicalPressure` or `HighPhysicalResilience` require a
versioned threshold, comparison population, or exact mechanic rule documented
by E5-000. An arbitrary UI adjective cannot enter Domain matching or scoring.

### Profile, match, playbook, and recommendation are different claims

- A profile facet states a verified fact about the target.
- An archetype match states that a documented combination of facets applies.
- A playbook states reusable response goals and verified response options.
- A recommendation states what is feasible for this player against this exact
  target now.

The UI and API must not collapse these layers into one unsupported claim.

### Representative targets validate; they do not define

A role-model target is a verification case for an archetype, not its identity.
The archetype definition uses stable typed facts and can match multiple targets
when they provide equivalent evidence.

### Playbooks are not fixed loadouts

A playbook may prioritize survival, counter timing, damage-channel access,
control removal, or another verified response goal. It references existing
threat and counter semantics, but final 功法 selection still passes ownership,
direction, effect, requirement, capacity, and backlash validation.

### The exact target remains authoritative

Current-screen evidence and the latest immutable target snapshot may add,
remove, elevate, or leave unresolved response goals. A broad archetype can
never override a contradictory exact skill, effect, equipment, or observation.

### Unknown is not a negative match

Missing evidence cannot prove that an archetype does not apply. The model must
distinguish `NotMatched` with sufficient contrary evidence from `Unsupported`,
`Incomplete`, and `Conflicting` states.

### Determinism remains mandatory

Identical snapshot, catalogue version, observations, profile rules, archetype
definitions, playbooks, and recommendation inputs must produce identical
facets, matches, composition order, adjustments, diagnostics, and loadouts.

### Game non-interference is permanent

Epic 5 follows
[ADR-0001](../../architecture/ADR-0001-absolute-game-non-interference.md).
Every source read remains guarded and read-only. No profile, playbook, API, or
UI type may become a game command or mutation path.

## Product vocabulary

### Combat-profile dimensions

The first contract separates these dimensions even when E5-000 marks an
individual value unavailable:

| Dimension | Meaning | Example candidates |
|---|---|---|
| Attack family | Descriptive delivery context | 刀, 劍, 拳掌, 音, 暗器, mixed, unknown |
| Pressure | How the target threatens the player | Physical/internal damage, 破體/破氣, penetration, repeated-hit |
| Resilience | How the target resists or resets progress | Physical defense, internal defense, avoidance, recovery, mark reset |
| Control | Mechanics that constrain the player's plan | Poison, mind-break, distraction marks, movement, range, weapon or trick disruption |
| Tempo | When the response matters | Combat start, burst, sustained attrition, on-hit chain, threshold, unknown |

The examples are discovery candidates, not evidence that all values are
currently readable or mechanically verified.

### Profile-facet evidence states

- `Confirmed`: sufficient version-matched evidence establishes the facet.
- `Incomplete`: some supporting evidence exists but a required field is
  missing.
- `Unsupported`: the current source or GameData version cannot evaluate it.
- `Conflicting`: applicable sources disagree and the conflict policy cannot
  resolve the facet silently.

### Archetype-match states

- `Matched`: every required condition is confirmed and no exclusion applies.
- `Partial`: at least one required condition is confirmed, another remains
  incomplete, and none is contradicted.
- `NotMatched`: sufficient evidence proves that a required condition or
  explicit exclusion fails.
- `Unsupported`: the definition or source version cannot be evaluated.
- `Conflicting`: source disagreement prevents a reliable match decision.

These states describe evidence and rule evaluation, not win probability.

### Counter playbook

A playbook contains:

- a stable archetype identity and version;
- ordered response goals;
- linked typed threats and activation timing;
- verified hard-counter or mitigation references;
- feasibility and equipment requirements;
- composition priority and explicit conflict groups;
- evidence references and known gaps; and
- deterministic fallback behavior when no accessible counter exists.

### Target-specific adjustment

An adjustment states why the final target caused a response goal or candidate
to be:

- retained from the reusable playbook;
- elevated because exact evidence increases its importance;
- reduced because the broad risk is not present;
- added for an exact mechanic outside the matched playbook;
- replaced after feasibility or conflict resolution; or
- left unresolved because evidence or an accessible counter is missing.

## Initial delivery boundary

E5-000 inspected the installed version and selected this final representative
matrix:

1. the already verified mind-damage, distraction, resonance, and defeat-reset
   chain as the baseline reusable playbook;
2. configured outer-damage pressure on a positively identified active attack;
3. positive, unequal base outer/inner resistance values as an exact channel-
   resilience asymmetry; and
4. configured poison application on a positively identified active attack.

The original candidates used `high physical-offense` and `high physical-
resilience` wording. E5-000 found no justified population or threshold for
those adjectives. The selected families use exact mechanic predicates instead
and leave strength, severity, penetration, repeated-hit, recovery, avoidance,
and tempo rankings explicitly unsupported. The evidence boundary and
representative matrix are recorded in
[TARGET-COMBAT-PROFILE.md](../../architecture/TARGET-COMBAT-PROFILE.md) and
[E5-000-target-archetype-evidence.md](../../scenarios/E5-000-target-archetype-evidence.md).

Weapon-family facets such as 刀 or 拳掌 may be delivered as contextual profile
facts, but they do not become independent tactical playbooks unless separate
evidence proves a reusable response.

## Functional scope

### 1. Evidence and representative-target matrix

Inspect the minimum permitted save and installed GameData sources needed to
evaluate candidate profile facets. Record exact fields, units, thresholds,
versions, availability, precedence, and limitations. Select synthetic and
local representative cases without committing proprietary source content.

### 2. Immutable target combat profile

Add presentation-neutral Domain contracts for profile dimensions, facets,
evidence, unavailable reasons, diagnostics, and a deterministic profile
fingerprint. Collections copy into immutable values and reject duplicate or
invalid stable identities.

### 3. Versioned multi-label archetype matching

Define archetypes as versioned rules over typed facets and existing threats.
Evaluate every applicable definition independently. Preserve matched, partial,
not-matched, unsupported, and conflicting results in stable order.

### 4. Counter-playbook catalogue

Represent reusable response goals by referencing existing typed threat,
counter, effect, timing, and requirement semantics. The first catalogue covers
the baseline plus the three evidence-gated families. Raw descriptions may be
shown as evidence but cannot become playable counter rules by themselves.

### 5. Playbook composition

Compose all matched playbooks, deduplicate identical response goals and
counters, retain stronger or earlier timing where rules permit, and surface
true conflicts.

### 6. Exact-target adjustment and player personalization

Use current target threats, skills, effects, equipment, observations, and
unresolved evidence to create explicit target-specific adjustments. Then
filter composed and adjusted options through the current player snapshot,
feasibility validator, bounded candidate generator, and existing
recommendation pipeline from one immutable input boundary. Applying or
clearing an Epic 3 observation rebuilds the profile, matches, playbooks,
adjustments, recommendation, and Epic 4 comparison atomically.

### 7. Typed API projection

Expose profile facets, evidence states, archetype matches, playbook goals,
composition conflicts, target adjustments, diagnostics, and stable references
without exposing local paths, proprietary raw content, or mutation-capable
types.

### 8. Bilingual responsive UI

Add a compact target-profile and strategy section to the existing
recommendation page. It shows dominant matched archetypes first, groups partial
or unsupported matches as supporting evidence, and explains reusable response
goals before exact-target adjustments. It does not add another policy selector
or duplicate the full loadout matrix.

### 9. Verification and lifecycle

Cover Domain, Application, API, Presentation, Infrastructure boundaries, and
guarded local reads. Synthetic fixtures prove that one archetype can match
multiple targets and one target can match multiple archetypes. Repeated and
observation apply/clear runs prove deterministic replacement and source
non-interference.

## User-visible states

The workflow must present these states explicitly:

- profile loading;
- profile available with no verified archetype match;
- one matched archetype;
- multiple matched archetypes;
- partial archetype match;
- archetype not matched with sufficient evidence;
- unsupported GameData or profile rule version;
- conflicting profile evidence;
- playbook available with accessible counters;
- playbook available with inaccessible or infeasible counters;
- overlapping playbooks composed without conflict;
- playbook composition conflict;
- exact-target adjustment applied;
- unresolved exact-target mechanic;
- recommendation completed with archetype evidence;
- recommendation unavailable or failed; and
- observation applied or cleared with the whole result rebuilt.

## Epic acceptance criteria

- [x] Versioned evidence defines every profile field, threshold, unit,
      precedence rule, and unavailable state used by Epic 5.
- [x] Weapon or attack family remains separate from damage, defense, control,
      and tempo mechanics.
- [x] One target can match multiple archetypes and one archetype can match
      multiple targets.
- [x] Missing evidence never becomes a negative match or a zero value.
- [x] Match states distinguish matched, partial, not matched, unsupported, and
      conflicting outcomes.
- [x] Every matched facet and archetype links to typed evidence and source
      provenance.
- [x] Localized names and raw effect descriptions never become stable identity
      or scored mechanics.
- [x] High/low labels use documented, versioned semantics rather than arbitrary
      UI thresholds.
- [x] The first catalogue contains the verified baseline plus three newly
      verified playbook families.
- [x] Every playbook expresses response goals and verified options rather than
      a fixed universal loadout.
- [x] Playbook composition deterministically deduplicates shared coverage and
      exposes true timing, requirement, or capacity conflicts.
- [x] Exact-target evidence can retain, elevate, reduce, add, replace, or leave
      unresolved a playbook response with an explicit reason.
- [x] Final 功法 selections still pass ownership, direction, effect, requirement,
      capacity, backlash, and bounded-search safeguards.
- [x] An inaccessible counter remains a visible gap and is never replaced by
      an invented effect.
- [x] Applying or clearing target observations atomically rebuilds profile,
      matches, playbooks, adjustments, recommendation, and comparison.
- [x] API and UI expose equivalent typed archetype and playbook semantics.
- [x] The UI adds one compact strategy explanation without restoring duplicate
      recommendation controls or policy results.
- [x] Traditional Chinese and English layouts are complete, responsive,
      keyboard accessible, and do not rely on color alone.
- [x] Identical evidence and rules produce identical ordering, fingerprints,
      diagnostics, adjustments, and recommendations.
- [x] Automated tests cover matched, multi-match, partial, not-matched,
      unsupported, conflicting, inaccessible-counter, composition-conflict,
      adjusted, observation-applied, and cleared states.
- [x] Guarded local verification proves every inspected save, GameData, and
      language source remains byte-for-byte unchanged.
- [x] No file, process, screenshot, automation, persistence, or game-control
      capability is introduced.
- [ ] The product owner records the Epic 5 completion decision.

## Success measures

- A player can explain a target through several independent verified traits
  instead of one opaque class.
- A player can distinguish a reusable archetype response from the changes made
  for the selected target.
- Synthetic fixtures prove that the same playbook is reusable across targets
  and that overlapping playbooks compose deterministically.
- The local representative matrix validates every delivered family supported
  by the current installed sources.
- No weapon label, localized name, raw description, or missing value creates a
  tactical claim.
- Every recommended counter remains feasible for the current player or is
  explicitly reported as inaccessible.
- Repeated identical inputs and observation apply/clear cycles are stable.
- No Epic 5 operation changes game-owned bytes or runtime state.

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| Targets are forced into one broad stereotype | Use independent dimensions and evaluate every archetype |
| A weapon family is mistaken for damage or defense | Keep context and mechanics as separate facets with separate evidence |
| “High” uses an arbitrary threshold | Require a versioned threshold, population, or exact mechanic rule in E5-000 |
| A representative target becomes the archetype definition | Define by stable facts and use representatives only as verification cases |
| Raw text silently becomes a scored mechanic | Permit only typed, version-matched rule inputs; retain raw text as evidence only |
| Overlapping playbooks duplicate or contradict counters | Use stable identities, deterministic composition, explicit conflict groups, and diagnostics |
| A generic playbook overrides exact target evidence | Apply exact-target adjustment after matching and retain its reason |
| Playbooks bypass feasibility | Route every final option through existing hard filters and bounded search |
| Missing target data appears safe | Preserve incomplete, unsupported, and conflicting states instead of `NotMatched` |
| The UI becomes another long duplicate result | Add a compact strategy layer and reuse existing threat, loadout, and comparison details |
| Scope expands to every target or management system | Enforce the initial family matrix and keep PI-008 through PI-011 outside Epic 5 |
| Feature drifts toward game control | Enforce ADR-0001 in contracts, architecture tests, and UI language |

## Completion decision

The original E5-010 technical completion claim was reopened after an
independent review found that all three new families identified a target
problem but supplied no playable 功法. E5-011 closes that gap with exact
version-gated outer-damage, channel-routing, and poison counters; splits the
reusable mind/resonance response from the defeat-reset overlay; and wires
reviewed exact-target replacement into production. The
[pre-remediation automated](../../reviews/E5-010-automated-verification.md),
[pre-remediation manual](../../reviews/E5-010-manual-verification.md), and
[remediation](../../reviews/E5-011-playbook-remediation.md) reviews retain the
full audit trail. Epic 5 remains `In progress` until the product owner reviews
the remediated result and records the final completion decision.

## Delivery reference

Implementation order and item-level evidence are tracked in
[the Epic 5 backlog](./BACKLOG.md).

PI-007 was promoted into this epic from
[future product ideas](../FUTURE-PRODUCT-IDEAS.md#pi-007--target-archetypes-and-counter-playbooks)
on 2026-08-10.
