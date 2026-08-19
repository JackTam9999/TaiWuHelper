# Companion-candidate source boundary

This document defines the evidence and source boundary being selected by
[E6-000](../roadmap/epic-006/BACKLOG.md#e6-000--verify-the-candidate-universe-and-select-the-initial-role-matrix).
It records which saved facts may enter an Epic 6 candidate profile, which
sources own them, and which tempting interpretations remain unsupported.

The inspected GameData product version is
`1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20`. Every mapping or role rule
derived from this document is invalid for another version until the evidence
gate is repeated.

The first configured-save attempt changed during the guarded archive read, and
the production revision guard rejected that result. A later stable interval
completed two equivalent aggregate projections within budget and preserved
every inspected source.

## Decision

Epic 6 starts from the current saved Taiwu group, not the broad character or
target-lookup universe. A candidate must have consistent saved group
membership, a current character object, and a confirmed living state.

The first role matrix compares one player-selected martial discipline and one
player-selected life-skill discipline. Exact saved base qualification is the
comparison fact. Learned or equipped skill identities remain separate
supporting evidence.

Current qualification and attainment getters are not available in the
standalone archive runtime. Every guarded local call entered
`SpecialEffectDomain.ModifyData`. These fields remain explicitly unsupported;
the helper never substitutes a base value or zero while calling it current.

No first-delivery field proves recruitability, teaching ability, future
development, battle synergy, settlement suitability, or universal companion
quality.

On 2026-08-19, a bounded extension added
`GetVillagersForWork(true, false)` as a second authoritative inclusion source
and introduced a current-base succession pre-screen. The source union does not
prove complete village membership. The formula `capability breadth - current
age` does not prove remaining lifespan, inheritance eligibility, transferable
progress, future growth, or a recommended action.

## Source precedence

| Priority | Source | Permitted ownership |
|---:|---|---|
| 1 | One immutable configured-save revision | Current roster membership, character existence, living state, saved base qualifications, current readable values, learned/equipped identities, age, feature IDs, and location |
| 2 | Exact-version installed GameData metadata and configuration | Field shape, discipline catalogue identity, fixed-buffer length, and typed method contract |
| 3 | Existing helper catalogue and verified progress contracts | Stable martial identities, localized names, progress provenance, and lifecycle state where compatible |
| 4 | Presentation localization | English and Traditional Chinese display text only |

Later sources may enrich a stable fact but cannot replace a conflicting saved
roster or current value silently. A different save fingerprint, GameData
version, catalogue version, or mapping version requires a new candidate result.

## Candidate-universe contract

### Authoritative membership

`TaiwuDomain.GetGroupCharIds()` is the authoritative roster source and
`TaiwuDomain.GetVillagersForWork(true, false)` is the bounded verified
village-work-candidate source. For every ID in their union:

1. the ID must not identify the Taiwu player character;
2. `CharacterDomain.Characters` must contain the current character object;
3. `TaiwuDomain.IsInGroup(id)` must agree with roster inclusion;
4. `Character.IsInTaiwuGroup()` must agree with roster inclusion; and
5. `CharacterDomain.IsCharacterAlive(id)` must be true.

The local roster confirms that `GetGroupCharIds()` includes the Taiwu player,
so the first exclusion is required rather than inferred from collection shape.

The last three checks validate consistency and eligibility. They do not expand
the universe. A character outside both approved sources cannot enter the
shortlist merely because another API describes it as following, friendly,
nearby, visible, named, or potentially interactive.

### Candidate evidence states

The `TaiWu.Domain.CompanionCandidates` contracts preserve these distinctions:

| State | Required source condition | May be ranked? |
|---|---|---:|
| `Eligible` | Approved source entry, current character, roster-consistent group checks, and living state are confirmed | Yes, if the selected role is also evaluable |
| `Ineligible` | A verified hard condition such as living state is false | No |
| `Incomplete` | An approved source entry exists but the character object or required saved fact is absent | No |
| `Unsupported` | The installed version or standalone reader cannot evaluate a required source | No |
| `Conflicting` | Roster and membership checks disagree, or applicable sources retain incompatible facts | No |

Missing evidence never becomes `Ineligible`, and a character cannot become
eligible merely because it can be named or located.

## Implemented Domain profile contract

E6-002 adds presentation-neutral values under
`src/TaiWu.Domain/CompanionCandidates/`:

- `CandidateIdentity` contains only the stable positive saved character ID;
  localized or player-visible names remain outside the profile identity;
- `CandidateProfileFieldIdentity` combines a typed field with a typed martial
  or life-skill discipline identity only where that field requires one;
- `CandidateFactValue` is a closed typed value over Boolean, `Int16`, `Int32`,
  and sorted immutable identity-set shapes rather than an untyped object or
  display string;
- `CandidateProfileFact` has explicit `Confirmed`, `Incomplete`,
  `Unsupported`, `Stale`, and `Conflicting` construction paths;
- `CandidateFactProvenance` and `CandidateEvidenceReference` retain stable
  source identity, source version, source revision, and evidence reference;
- `CandidateConflictValue` retains every candidate value with its own
  provenance and evidence, while `CandidateConflictDecision` records whether
  precedence is unresolved, selected a retained source, or rejected all
  candidates;
- `CandidateProfileSourceVersions` owns the save SHA-256, GameData version,
  profile-mapping version, discipline-catalogue version, and fingerprint-
  schema version; and
- `CandidateProfile` copies, validates, de-duplicates, and canonically sorts
  facts and diagnostics before producing its deterministic fingerprint.

A confirmed fact requires one compatible typed value and provenance. An
incomplete or unsupported fact requires an unavailable reason and cannot carry
a value. A stale fact retains its last observed value and provenance but also
requires the reason it is unusable. A conflicting fact carries no selected
fact value, requires at least two retained candidates, and requires a typed
precedence decision. Therefore missing evidence cannot enter the model as
confirmed numeric zero.

The fingerprint covers stable character identity, candidate-universe state,
all source and rule versions, typed fact semantics, retained conflict values,
precedence decisions, and stable diagnostic identities. It deliberately
excludes localized display text, filesystem paths, reason and diagnostic
detail, and capture timestamps. Stable identity inputs reject path separators
so a local source path cannot accidentally become semantic profile identity.

These contracts depend only on the .NET base class libraries. They have no
reference to Application, Infrastructure, Presentation, persistence,
filesystem, process, reflection, or installed GameData types.

## Source-field matrix

### Identity, eligibility, and descriptive context

| Stable field candidate | Owner and member | Runtime type | Completeness and precedence | Epic 6 decision |
|---|---|---|---|---|
| Character identity | `CharacterDomain.Characters` dictionary key | `Int32` | Available only with a current object; source identity is the save fingerprint | Stable candidate identity; never display text |
| Current roster membership | `TaiwuDomain.GetGroupCharIds()` | `CharacterSet` | Complete for the saved current group under the inspected version | Authoritative candidate-universe inclusion |
| Village-work-candidate membership | `TaiwuDomain.GetVillagersForWork(true, false)` | Character collection | Complete for the bounded verified source call under the inspected version | Authoritative inclusion in this comparison universe only |
| Domain membership check | `TaiwuDomain.IsInGroup(int)` | `Boolean` | Must agree with the roster | Consistency evidence; disagreement conflicts |
| Character membership check | `Character.IsInTaiwuGroup()` | `Boolean` | Must agree with roster and Domain check | Consistency evidence; disagreement conflicts |
| Living state | `CharacterDomain.IsCharacterAlive(int)` | `Boolean` | Required for current-role eligibility | Hard eligibility fact |
| Current age | `Character.GetCurrAge()` | `Int16` | Saved current fact when available | Descriptive context; exact lower-is-better component only for the bounded succession objective |
| Current location | `Character.GetLocation()` | `Location` with area/block IDs | Valid non-negative IDs may be displayed after localization | Descriptive only; no initial scoring or recruitability claim |
| Feature identities | `Character.GetFeatureIds()` | `List<Int16>` | Saved identities; individual mechanics not normalized by E6-000 | Evidence/display only |

### Martial-discipline facts

| Stable field candidate | Owner and member | Runtime type and unit | Completeness | Epic 6 decision |
|---|---|---|---|---|
| Base martial qualification | `Character.GetBaseCombatSkillQualifications()` | Fixed 14-entry `Int16` buffer indexed by installed combat-discipline identity | All 14 values were readable in the guarded local case | Authoritative comparison fact for the martial aptitude role |
| Current martial qualification | `Character.GetCombatSkillQualification(sbyte)` | `Int16` qualification (`资质`) | Every local call entered unavailable `SpecialEffectDomain.ModifyData` | `Unsupported`; never substitute base or zero |
| Current martial attainment | `Character.GetCombatSkillAttainment(sbyte)` | `Int16` attainment (`造诣`) | Every local call entered unavailable `SpecialEffectDomain.ModifyData` | `Unsupported`; no first-role influence |
| Learned martial identities | `Character.GetLearnedCombatSkills()` | `List<Int16>` | Saved learned membership | Supporting fact only; learned does not mean equipped, mastered, teachable, or battle-effective |
| Equipped martial identities | `Character.GetEquippedCombatSkills()` | `Int16[]` | Saved equipped membership | Supporting current-loadout fact only |

### Life-skill-discipline facts

| Stable field candidate | Owner and member | Runtime type and unit | Completeness | Epic 6 decision |
|---|---|---|---|---|
| Base life-skill qualification | `Character.GetBaseLifeSkillQualifications()` | Fixed 16-entry `Int16` buffer indexed by installed life-skill discipline identity | All 16 values were readable in the guarded local case | Authoritative comparison fact for the life-skill aptitude role |
| Current life-skill qualification | `Character.GetLifeSkillQualification(sbyte)` | `Int16` qualification (`资质`) | Every local call entered unavailable `SpecialEffectDomain.ModifyData` | `Unsupported`; never substitute base or zero |
| Current life-skill attainment | `Character.GetLifeSkillAttainment(sbyte)` | `Int16` attainment (`造诣`) | Every local call entered unavailable `SpecialEffectDomain.ModifyData` | `Unsupported`; no first-role influence |
| Learned life-skill identities | `Character.GetLearnedLifeSkills()` | `List<LifeSkillItem>` | Saved identity and reading state | Supporting fact only; no teaching, work, or future-development inference |

## Initial role source contracts

### `MARTIAL_DISCIPLINE_APTITUDE`

The player selects exactly one stable installed combat-discipline identity.
The role requires:

- confirmed `Eligible` candidate state;
- exact supported GameData and discipline mapping versions;
- available saved base martial qualification for the selected discipline.

Learned or equipped martial identities remain explanatory facts. Base
qualification is labelled explicitly and cannot prove current modified
attainment, general combat support, synergy, survival, damage, or success
probability.

### `LIFE_SKILL_DISCIPLINE_APTITUDE`

The player selects exactly one stable installed life-skill-discipline identity.
The role requires:

- confirmed `Eligible` candidate state;
- exact supported GameData and discipline mapping versions;
- available saved base life-skill qualification for the selected discipline.

Learned life-skill identities remain explanatory facts. Base qualification is
labelled explicitly and cannot prove current modified attainment, teaching,
settlement work, production, training efficiency, or future progression.

E6-001 owns score and tie semantics. This source contract permits only the
typed facts; it does not authorize weights, thresholds, or a combined score.

## Unsupported source interpretations

| Tempting interpretation | Why unsupported | Required future evidence |
|---|---|---|
| Every target-lookup entry is a companion candidate | Target lookup enumerates a broad named-character store and omits group eligibility | Exact recruitment or roster contract for the intended expanded universe |
| A following character is a current group member | Following and group APIs are separate | Versioned equivalence rule or explicit product state for followers |
| High qualification means best companion | Qualification is one role-local fact, not universal quality | Selected objective, other required facts, and explicit comparison semantics |
| Learned skill means current combat contribution | Learned does not mean equipped, active, feasible, or synergistic | Typed battle-role and composition rules |
| Feature name implies a bonus | Localized labels are display text and individual feature mechanics are unverified | Stable feature rule with typed effect and version |
| `CanTeach*` proves teaching value | Teaching calls incorporate target and interaction rules not verified by E6-000 | Exact relationship, book, cost, eligibility, and standalone behavior evidence |
| Current age predicts remaining lifespan, inheritance eligibility, or development value | The bounded succession objective may subtract exact current age transparently, but future lifespan, growth, training, and transfer rules remain outside the snapshot | PI-009 evidence and staged-plan contract |
| Location proves recruitability or availability | A saved location is descriptive only | Exact interaction and travel availability rules |
| Life-skill values prove settlement productivity | Building, assignment, resource, and worker formulas are absent | PI-010 settlement evidence |

## One-pass read boundary

On a revision-cache miss, the E6-004 Infrastructure adapter loads one configured
save revision and projects the approved group-roster and village-work-candidate
union inside one `TaiwuArchiveReadSession.ReadAsync` callback. It does not loop over the
existing archive-opening single-character progress reader. The full design and
production evidence are recorded in the
[companion-candidate snapshot architecture](./COMPANION-CANDIDATE-SNAPSHOT.md).

One immutable helper-memory result may be reused after before/after file
revision checks. It is invalidated by path, length, last-write time, GameData,
profile-mapping, or fingerprint-schema changes and is never persisted.

The snapshot records:

- save fingerprint and captured time;
- exact GameData, mapping, and discipline-catalogue versions;
- archive load warning;
- authoritative roster and verified village-work-source IDs plus consistency
  results;
- typed available or unavailable profile facts; and
- sanitized candidate-level and result-level diagnostics.

If the save revision changes before projection completes, the existing session
guard discards the entire result. No candidate from the earlier revision may be
retained.

## Performance and safety gate

The representative local scenario must meet:

- cold complete request at or below 30 seconds;
- warm unchanged-revision request at or below 2 seconds;
- one archive session per request;
- equivalent repeated aggregate result; and
- unchanged save, inspected GameData assemblies, and any installed language or
  configuration sources actually read by the production projection.

Metadata inspection on 2026-08-17 guarded the Steam manifest, player
executable, `GameData.dll`, `GameData.Shared.dll`, and installed XML
documentation: all five were unchanged. The first configured-save probe
correctly rejected a revision that changed while the game was running.

The accepted stable run projected 9,603 broad objects to a two-ID saved roster,
then excluded Taiwu and confirmed one living candidate with agreeing
membership checks. Cold projection completed in 21.598 seconds and warm
unchanged-revision projection in 4 milliseconds. The two aggregate results
were equivalent. The save and two GameData assemblies were unchanged before
and after both reads.

The single local companion verifies the positive source path; documented
synthetic representatives own ineligible, incomplete, unsupported, conflict,
multi-candidate ordering, and tie states without committing local identities or
values.

The original E6-004 production projection emitted one 101-fact complete
profile for that representative. E6-013 profile-mapping version `2` adds six
typed saved base main attributes, making the current complete shape 107 facts.
Negative equipped-array empty-slot sentinels are excluded from the saved
identity set; current qualification and attainment remain explicit unsupported
facts. The original guarded production test completed cold in 20.487 seconds
and warm in 2 milliseconds while the save and two loaded GameData assemblies
remained unchanged.
