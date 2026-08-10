# E5-000: Target-archetype evidence

| Field | Value |
|---|---|
| Status | Complete |
| Epic | [EPIC-005](../roadmap/epic-005/EPIC.md) |
| Backlog item | [E5-000](../roadmap/epic-005/BACKLOG.md#e5-000--verify-target-profile-signals-and-select-the-representative-matrix) |
| Inspection date | 2026-08-10 |
| Steam application/build | App `838350`, build `24387008` |
| Installed player executable | Unity `2022.3.14f1` player build |
| GameData product version | `1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a` |
| Language evidence | Traditional Chinese and English installed resources guarded by the current-save integration vertical |

## Purpose

Select the smallest trustworthy set of target-profile facts and
representative playbook families before adding Epic 5 Domain contracts.

The inspection answers four questions:

1. Which saved, configured, and observed fields have typed mechanical
   semantics?
2. Which fields establish active target behavior rather than merely possible
   behavior?
3. Can the intended physical, resilience, and poison families be verified
   without arbitrary `High` or `Low` labels?
4. Which local cases can validate the rules without committing save content,
   character identifiers, localized target names, hashes, or machine paths?

## Method

The evidence collection used four read-only layers:

1. existing production readers and architecture contracts were inspected;
2. public metadata from version-matched GameData assemblies was inspected
   without calling combat behavior;
3. the current save was loaded through the existing read-only archive adapter
   to discover positive equipped-skill and base-character facts; and
4. the normal current-save integration vertical fingerprinted the save,
   runtime assemblies, and installed language resources before and after its
   read.

The representative scanner kept the save SHA-256 open/read policy compatible
with the production guard and compared it before and after discovery. It
recorded only aggregate conclusions in this document. Its temporary source,
local output, local paths, identifiers, names, and hashes are not committed.

## Version-matched type findings

Metadata inspection confirmed these relevant public shapes:

| Owner | Member | Type |
|---|---|---|
| `CombatSkillItem` | `EquipType`, `Type` | `SByte` |
| `CombatSkillItem` | `OuterDamageSteps`, `InnerDamageSteps` | `Int32[]` |
| `CombatSkillItem` | `MindDamageStep` | `Int32` |
| `CombatSkillItem` | `TotalHit`, `Penetrate` | `Int16` |
| `CombatSkillItem` | `Poisons` | Fixed `PoisonsAndLevels` value/level buffers |
| `WeaponItem` | `ItemSubType` | `Int16` |
| `WeaponItem` | `BaseEquipmentAttack`, `BaseEquipmentDefense`, `BasePenetrationFactor` | `Int16` |
| `Character` | `CalcBaseDamageSteps()` | `DamageStepCollection` with outer/inner arrays and mind/fatal integers |
| `Character` | `GetBasePenetrations()` | `OuterAndInnerInts` |
| `Character` | `GetBasePenetrationResists()` | `OuterAndInnerInts` |
| `Character` | `GetBasePoisonResists()` | Fixed `PoisonInts` buffer |

The exact ownership, units, completeness, precedence, and production decision
for each field are in
[TARGET-COMBAT-PROFILE.md](../architecture/TARGET-COMBAT-PROFILE.md).

## Discovery results

The final equipped-only scan evaluated 8,775 current-save target candidates.
It found multiple positive local examples for:

- active equipped attack skills with configured outer-damage steps;
- targets whose two positive base channel-resistance values are unequal;
- active equipped attack skills with non-zero configured poison entries; and
- active skills with configured mind-damage, repeated-hit, and penetration
  candidates.

The last group is discovery evidence only. `TotalHit`, configured penetration,
recovery, avoidance, and live-modified values did not pass the semantic gate
for first-delivery matching.

### Learned is not active

An initial discovery pass over 7,051 candidates used learned attack skills.
It produced implausibly broad poison and mind matches because ordinary targets
can know many attacks they are not currently using. That pass was rejected.

The final rule is explicit: a learned skill can resolve an already identified
equipped skill, but learned membership alone cannot create an active facet,
archetype, threat, or counter playbook.

### Base is not live

The base character getters used for discovery completed in the standalone
archive context. In contrast, live modified penetration and attack-tendency
paths entered `SpecialEffectDomain.ModifyData` and failed because the
standalone reader has no live combat domain.

Epic 5 therefore exposes only specifically verified base facts with explicit
base provenance. Runtime-only values are unavailable. It never catches such a
failure and substitutes zero.

### Zero is not `Low`

Many ordinary saved characters returned zero base values. The inspection did
not establish whether every zero means a true combat value, an uninitialized
state, or an unavailable derived state. Zero therefore cannot prove a `Low`
facet or a negative match in the first rule set.

## Selected delivery matrix

Opaque labels identify local verification cases without publishing the target
identity or source content. Synthetic fixtures reproduce only the minimum
typed values required by each predicate.

| Family | Exact qualifying evidence | Representative verification | Reusable response boundary | Decision |
|---|---|---|---|---|
| `MIND_RESONANCE_BASELINE` | Existing exact magic-sound direction, distraction, resonance, threat, effect, and counter rules | Historical golden scenario in [M1-001](M1-001-golden-target-selection.md) | Preserve the ordered survival, interruption/mitigation, and resonance goals without requiring reset | Baseline selected |
| `DEFEAT_MARK_RESET_OVERLAY` | Existing exact defeat-threshold reset threat, effect, and counter rule | Verified reset evidence in [M1-025](../reviews/M1-025-manual-verification.md) | Compose reset pressure independently when the reset signature is present | Independent overlay selected in E5-011 |
| `OUTER_DAMAGE_CONFIGURED` | Positive active attack binding plus at least one positive configured outer-damage step | `E5-REP-LOCAL-OUTER-001` and a minimal synthetic outer-step fixture | Respond to verified outer-damage access; do not claim high pressure, accuracy, penetration, or dominance | New family selected |
| `CHANNEL_RESISTANCE_ASYMMETRY` | Both base channel-resistance values are positive and unequal | `E5-REP-LOCAL-RESIST-001` and two synthetic fixtures covering either larger channel | Prefer or elevate response options that address the larger resisted channel or preserve access to the lesser-resisted channel, subject to exact counter and player feasibility | New family selected |
| `POISON_APPLICATION_CONFIGURED` | Positive active attack binding plus at least one non-zero configured poison entry | `E5-REP-LOCAL-POISON-001` and a minimal synthetic poison fixture | Add poison-response goals only after exact counter/effect verification; presence alone does not claim type, rate, stacks, severity, or duration | New family selected |

The local cases are role models for tests, not archetype identities. The rule
definitions must contain no local character ID, target name, or output rank.

## Context facets that do not define playbooks

| Candidate | Evidence | Decision |
|---|---|---|
| Weapon subtype such as 刀, 劍, 拳掌, 音, or 暗器 | Positive saved equipment identity joined to the versioned `ItemSubType` code | Retain as attack-family context. Never infer damage, poison, penetration, or defense from it. |
| Skill discipline/category | Versioned `Type` and `EquipType` codes | May restrict which static record is being evaluated. Category alone never matches an archetype. |
| Target and skill localized names | Lookup or installed language resources | Display/confirmation only. Never parse text to obtain mechanics. |

This separation allows a target to be described as, for example, a weapon-
family user while independently matching outer damage, resistance asymmetry,
poison, or the mind/reset baseline.

## Unsupported candidates

| Candidate | Why it did not pass E5-000 | Required future evidence |
|---|---|---|
| `HighPhysicalPressure` | No justified population, threshold, or normalized live value | A versioned population and boundary, or an exact mechanic rule that makes ranking unnecessary |
| `HighPhysicalResilience` / `LowPhysicalResilience` | Zero semantics and a population threshold are unverified | Verified live/base distinction plus documented comparison and boundary behavior |
| Repeated-hit pressure | `TotalHit` exists and varied in discovery, but its production meaning and UI conversion were not independently verified | Versioned mechanic evidence and an exact activation predicate |
| Penetration pressure | Static skill penetration and base/live character penetration have different owners; their combination and unit relationship are not verified | A typed formula/source contract and a standalone-safe current value |
| Recovery/attrition | Candidate fields exist, but live modification, timing, and counter relevance are incomplete | Exact source, unit, activation timing, and counter semantics |
| Avoidance, movement, range, and general tempo | No complete normalized target contract was established | Separate evidence item before any match or score influence |
| Poison severity, type-specific danger, stacks, rate, or duration | Poison configuration proves presence only | Typed slot mapping and application semantics |

These states are `Unsupported` or `Incomplete`, never negative matches.

## Threshold decision

No first-delivery family uses `High` or `Low`.

| Selected family | Rule kind | Population or threshold |
|---|---|---|
| Outer damage | Exact positive configured mechanic | None |
| Channel-resistance asymmetry | Exact comparison of two positive same-target, same-unit base values | No target population; strict inequality only |
| Poison application | Exact non-zero configured mechanic | None |
| Mind/reset baseline | Existing versioned effect and threat rules | Existing exact activation rules |

The representative scanner ranked values only to locate varied local cases.
Those ranks, maxima, and candidate counts are not profile thresholds and may
not enter production rules.

## Source and safety results

### Guarded current-save vertical

Sanitized command:

```powershell
$env:TAIWU_INTEGRATION_SAVE_PATH = '<current-save>'
dotnet test tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj -c Release --no-build --no-restore -- --filter-method "TaiWu.Infrastructure.IntegrationTests.TargetObservationReadOnlyIntegrationTests.Observation_and_comparison_repeat_clear_preserve_sources"
```

Result on 2026-08-10: **1 passed, 0 failed, 0 skipped**. The test's `finally`
guard confirmed identical before/after length, last-write time, and SHA-256 for
the current save, copied GameData runtime dependencies, and installed
Traditional Chinese and English UI/combat-skill resources.

An additional metadata guard fingerprinted 14 inspected sources: the Steam
manifest, player executable, three installed GameData assemblies, eight
Traditional Chinese/English combat-skill, effect, legendary-book, and UI
resources, and the current save. All 14 length, last-write-time, and SHA-256
states were identical before and after the version/type inspection.

### Representative discovery

The equipped-only scanner loaded the current save through the production
archive adapter, evaluated 8,775 candidates, and reported the save SHA-256
unchanged before and after. It created no report, database, snapshot, or game-
owned file.

The scanner was temporary evidence tooling outside the repository. Only this
sanitized conclusion and the opaque representative labels are retained.

### Metadata inspection

The public type inspection loaded the same version-matched assemblies read-
only and enumerated member signatures. No live process, runtime memory,
private field, hook, injection, patch, command, or game input was used.

## Product-contract correction

The candidate wording in the original Epic 5 boundary suggested `high`
physical offense and resilience. E5-000 found no defensible high/low threshold.
The delivery boundary is therefore corrected to exact outer-damage presence,
positive channel-resistance asymmetry, and poison-application presence.

This is a narrower but trustworthy contract. It preserves the user's intended
group-and-role-model workflow while making every match explainable and leaving
unsupported strength, severity, and tempo claims visible.

## Resolved decisions

1. Target profiles are multi-label; weapon context is independent from
   pressure, resilience, control, and tempo.
2. Learned skills never establish current target behavior.
3. Positive equipped membership or applicable current-screen evidence binds a
   static skill definition to the target.
4. Empty or absent saved loadouts do not prove absence.
5. Base character values are explicitly base, not live modified values.
6. Zero base values do not create `Low` or negative matches in the first rule
   set.
7. Names, localized labels, category alone, and raw descriptions are rejected
   as mechanical evidence.
8. The first delivery contains the existing baseline plus three newly
   verifiable exact-mechanic families.
9. Repeated-hit, penetration, recovery, avoidance, and tempo remain
   unsupported until separately verified.
10. Every inspected game-owned source remained byte-for-byte unchanged.
