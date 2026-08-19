# E6-000: Companion-candidate evidence

| Field | Value |
|---|---|
| Status | Complete |
| Epic | [EPIC-006](../roadmap/epic-006/EPIC.md) |
| Backlog item | [E6-000](../roadmap/epic-006/BACKLOG.md#e6-000--verify-the-candidate-universe-and-select-the-initial-role-matrix) |
| Inspection date | 2026-08-17 |
| Steam application/build | App `838350`, build `24769549` |
| Installed player executable | Unity `2022.3.14f1` player build |
| GameData product version | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` |

## Purpose

Select the smallest trustworthy companion-candidate universe, profile fields,
and initial role matrix before adding Epic 6 Domain contracts.

The inspection answers five questions:

1. Which saved collection establishes a current 同道 roster?
2. Why can the existing target lookup not establish companion eligibility?
3. Which character facts have typed, version-matched meanings suitable for
   role comparison?
4. Which two roles demonstrate objective-specific comparison without guessing
   recruitment, teaching, development, or battle synergy?
5. Can the candidate set be projected once with stable performance and no
   change to the save or installed sources?

## Method

Evidence collection uses three read-only layers:

1. existing production readers and public Application contracts are inspected;
2. public metadata and installed XML documentation from the version-matched
   GameData assemblies are inspected without calling game behavior; and
3. a temporary guarded local probe loads one stable save through the existing
   `TaiwuArchiveReadSession`, projects aggregate candidate facts, repeats the
   projection, and compares source fingerprints before and after.

Only aggregate counts, availability states, timing, and opaque conclusions may
be recorded. Local paths, save hashes, character identifiers, character names,
and proprietary source content are never committed.

The first local archive attempt occurred while the configured save was changing.
`TaiwuArchiveReadSession` rejected the mixed-revision result as designed. A
later stable interval completed two equivalent projections against the same
configured save revision and preserved every guarded source.

## Version and metadata findings

The installed version changed after Epic 5. Epic 6 evidence and future rules
must use the current exact GameData version above; the previous Epic 5 mapping
version does not silently carry forward.

Metadata inspection confirmed these relevant public shapes:

| Owner | Member | Runtime type | Candidate meaning |
|---|---|---|---|
| `CharacterDomain` | `Characters` | `IReadOnlyDictionary<int, Character>` | Broad living-character object store; enumeration only |
| `TaiwuDomain` | `GetGroupCharIds()` | `CharacterSet` | Saved current Taiwu group roster candidate source |
| `TaiwuDomain` | `IsInGroup(int)` | `Boolean` | Domain-level current-group membership cross-check |
| `Character` | `IsInTaiwuGroup()` | `Boolean` | Character-level current-group membership cross-check |
| `CharacterDomain` | `IsCharacterAlive(int)` | `Boolean` | Current living-state eligibility gate |
| `Character` | `GetLocation()` | `Location` | Descriptive current saved area/block when valid |
| `Character` | `GetCurrAge()` | `Int16` | Descriptive current saved age; not a score in the first matrix |
| `Character` | `GetFeatureIds()` | `List<Int16>` | Stable feature identities; display/evidence only until individual mechanics are verified |
| `Character` | `GetBaseMainAttributes()` | Fixed `MainAttributes` buffer of 6 `Int16` values | Saved base main attributes in stable Strength, Dexterity, Concentration, Vitality, Energy, Intelligence order |
| `Character` | `GetBaseCombatSkillQualifications()` | Fixed `CombatSkillShorts` buffer of 14 `Int16` values | Saved base martial qualification by stable combat-discipline index |
| `Character` | `GetCombatSkillQualification(sbyte)` | `Int16` | Current martial qualification; every local call entered unavailable live special-effect modification |
| `Character` | `GetCombatSkillAttainment(sbyte)` | `Int16` | Current martial attainment; every local call entered unavailable live special-effect modification |
| `Character` | `GetBaseLifeSkillQualifications()` | Fixed `LifeSkillShorts` buffer of 16 `Int16` values | Saved base life-skill qualification by stable discipline index |
| `Character` | `GetLifeSkillQualification(sbyte)` | `Int16` | Current life-skill qualification; every local call entered unavailable live special-effect modification |
| `Character` | `GetLifeSkillAttainment(sbyte)` | `Int16` | Current life-skill attainment; every local call entered unavailable live special-effect modification |
| `Character` | `GetLearnedCombatSkills()` | `List<Int16>` | Learned martial identities; learned does not mean equipped or suitable |
| `Character` | `GetEquippedCombatSkills()` | `Int16[]` | Current saved equipped martial identities |
| `Character` | `GetLearnedLifeSkills()` | `List<LifeSkillItem>` | Learned life-skill identities and saved reading state |

Installed XML labels independently identify qualification as `资质` and
attainment as `造诣` for both martial and life-skill domains. Localized labels
confirm presentation meaning only; stable indices and typed values own
identity and evaluation.

## Candidate-universe decision

### Selected first-delivery universe

The first Epic 6 candidate universe is the current saved Taiwu group roster:

1. the character ID appears in `TaiwuDomain.GetGroupCharIds()` and is not the
   Taiwu player character;
2. the character object exists in `CharacterDomain.Characters`;
3. `TaiwuDomain.IsInGroup(id)` and `Character.IsInTaiwuGroup()` agree with the
   roster; and
4. `CharacterDomain.IsCharacterAlive(id)` confirms the character is living.

The saved group set includes the Taiwu character, so exclusion of the player is
an explicit universe rule rather than an assumption about collection shape.
The two membership methods are consistency checks over the saved roster, not
independent sources that may silently override a conflict. A disagreement is a
typed `Conflicting` candidate state. A missing object or unavailable living
state is `Incomplete` or `Unsupported`, never eligible by default.

### Why target lookup is not eligibility

`TaiwuTargetLookupReader` enumerates `CharacterDomain.Characters`, excludes the
Taiwu character, and retains characters with resolvable names. Its contract is
designed to select combat targets, not companions. It does not check current
group membership, living-state consistency, recruitment rules, relationship,
or availability.

Target lookup therefore remains useful for names and descriptive location only
after a stable candidate identity is established. Its membership can never
create an Epic 6 candidate.

### Explicitly excluded universes

The first delivery does not rank:

- every character in `CharacterDomain.Characters`;
- followers returned by general following APIs unless they are also confirmed
  current group members;
- villagers, workers, prisoners, enemies, story templates, temporary
  characters, dead characters, or historical relations;
- characters who might become recruitable after dialogue, travel, favor,
  events, or other future game actions; or
- the Taiwu player character.

Potential recruitment is a separate mechanic requiring exact availability and
interaction evidence. Epic 6 makes no recruitability claim.

## Selected initial role matrix

The completed evidence gate selects two objective families over standalone-
safe saved base qualifications. Both compare verified current roster facts and
neither predicts current modified attainment, development outcome, or success
probability.

| Role | Player-selected dimension | Hard requirements | Candidate comparison facts | Decision |
|---|---|---|---|---|
| `MARTIAL_DISCIPLINE_APTITUDE` | One stable combat-discipline index from the installed 14-entry type catalogue | Confirmed living non-Taiwu current-group member; supported GameData and discipline mapping; base qualification available | Exact saved base martial qualification for the selected discipline; learned/equipped identities remain supporting evidence only | Selected |
| `LIFE_SKILL_DISCIPLINE_APTITUDE` | One stable life-skill discipline index from the installed 16-entry type catalogue | Confirmed living non-Taiwu current-group member; supported GameData and discipline mapping; base qualification available | Exact saved base life-skill qualification for the selected discipline; learned life-skill identities remain supporting evidence only | Selected |

These are separate role families because they use independently typed martial
and life-skill buffers, different installed discipline catalogues, and
different learned-skill contracts. The local probe confirmed every base value
was readable and positive for the representative companion, with variation
across both discipline sets. Synthetic fixtures provide the multi-candidate,
tie, and evidence-state cases that the one-companion local roster cannot.
E6-001 must define comparison and tie semantics. E6-000 does not invent a
weighted or combined universal score.

### Post-delivery capability overview

After the role finder was complete, the player explicitly requested a compact
companion-to-companion overview across the six main attributes, all martial
aptitudes, and all life-skill aptitudes. Metadata inspection confirmed
`MainAttributeType.Count = 6` and the stable buffer order: Strength, Dexterity,
Concentration, Vitality, Energy, and Intelligence (膂力、靈敏、定力、體質、根骨、
悟性).

The resulting version-1 breadth index is a descriptive comparison aid and,
after E6-014, the score for one explicit comprehensive objective. It is not a
universal recommendation:

1. calculate an arithmetic mean only when all 6 saved base main attributes are
   confirmed;
2. calculate separate arithmetic means only when all 14 saved base martial
   aptitudes and all 16 saved base life-skill aptitudes are confirmed;
3. round each category mean to two decimals, then calculate the equal-weight
   mean of those three displayed category values and round it to two decimals;
   and
4. show confirmed coverage and the top three values in each category.

An incomplete, unsupported, stale, or conflicting component makes its category
and the breadth index unavailable. It is never replaced by zero. The overview
does not change a selected-discipline score, rank, tie, shortlist order, or
explanation. Breadth affects order only when the player selects the separate
comprehensive objective.

### Roles rejected from the first matrix

| Candidate role | Decision | Reason |
|---|---|---|
| General combat support | Unsupported | Battle synergy, teammate commands, timing, survivability, and party composition are not established by qualification or learned skills alone |
| Martial-art teacher | Deferred | `CanTeachCombatSkill` and teachable-book APIs exist, but their relationship, interaction, book, and live-context rules have not been independently verified |
| Inheritance value | Deferred to PI-009 | Future growth, age horizon, transferable progress, and inheritance mechanics would mix current facts with speculative development |
| Recruitable prospect | Unsupported | Target enumeration, favor, proximity, or relationship does not prove current recruitability |
| Settlement worker | Deferred to PI-010 | Work availability, buildings, assignments, resources, and villager roles require a separate settlement domain |
| Balanced long-term companion | Rejected | A descriptive saved-base breadth index does not establish long-term potential, objective weights, future growth, or universal suitability |

### 2026-08-19 bounded succession extension

The later product-owner request adds a deliberately narrower current-base
succession comparison without reversing the original inheritance decision:

- the candidate universe is the union of `GetGroupCharIds()` and the already
  verified village-work source `GetVillagersForWork(true, false)`, excluding
  Taiwu and requiring the same object, living, and group-consistency checks;
- the new `SUCCESSION_CANDIDATE_READINESS` objective scores complete capability
  breadth minus exact saved current age, with both components shown; and
- profile mapping and fingerprint schema advance to version `3`, with 108
  typed facts including separate roster and village-work-source membership.

This source does not prove complete village membership. Current age does not
prove remaining lifespan, and the score does not establish inheritance
eligibility, transferable progress, future development, or a recommended
successor. Those mechanics remain deferred to PI-009.

## Evidence-state and precedence decisions

| Question | Decision |
|---|---|
| Candidate identity | Stable saved character ID; localized name is display only |
| Candidate membership | `GetGroupCharIds()` owns the roster; `IsInGroup` and `IsInTaiwuGroup` must agree |
| Living state | `CharacterDomain.IsCharacterAlive` is a hard eligibility gate |
| Missing character object | `Incomplete`; never construct an eligible empty profile |
| Unsupported GameData version | Entire role evaluation is `Unsupported`; no old mapping fallback |
| Current qualification or attainment | Explicitly `Unsupported` in standalone reading because all calls entered `SpecialEffectDomain.ModifyData`; never substitute zero |
| Base versus current value | The initial roles and capability overview use explicitly identified saved base values; they do not label them current modified attributes, qualification, or attainment |
| Feature IDs | Evidence/display only until a separate typed feature rule verifies a mechanical contribution |
| Learned skills | Supporting current-progress evidence; learned membership never proves equipped use, teaching, or role success |
| Location | Descriptive availability evidence only; it does not change rank in the initial matrix |
| Age | Descriptive current fact only; it does not become future-potential or inheritance scoring |

## Performance budget

E6-000 sets these local product budgets for the selected current-group universe:

- cold archive load plus projection and evaluation: at most 30 seconds;
- warm unchanged-revision projection and evaluation: at most 2 seconds; and
- exactly one archive session per complete request, with no per-candidate
  archive reopen.

The final evidence must record observed cold and warm values against these
budgets. A miss blocks completion or requires an explicit product-contract
revision; it cannot be hidden by excluding candidates.

The stable configured-save run completed the cold load and aggregate projection
in 21.598 seconds and the unchanged-revision warm projection in 4 milliseconds.
Both pass. Each complete projection ran inside one archive-session callback;
no candidate triggered another archive open.

## Source and safety results

### Metadata guard

A read-only metadata run fingerprinted five inspected sources: the Steam app
manifest, player executable, `GameData.dll`, `GameData.Shared.dll`, and the
installed `GameData.Shared.xml` documentation used to confirm labels. Length,
last-write time, and SHA-256 were identical before and after inspection.

Result on 2026-08-17: **5 inspected, 5 unchanged**.

### Configured-save rejection

The first guarded archive read ran through the production
`TaiwuArchiveReadSession`. The game process changed the configured save during
the read. The session compared the before and after fingerprint, discarded the
result, and raised the documented changed-revision error.

This is positive safety evidence: Epic 6 must never combine roster or profile
facts from two save revisions.

### Stable configured-save result

The accepted aggregate probe inspected 9,603 broad character objects. The
saved group contained two IDs: the Taiwu character and one non-Taiwu candidate.
The non-Taiwu candidate existed, was living, and agreed across roster, Domain,
and character membership checks. No general following character was reported.

All six base main attributes, 14 base martial qualifications, and 16 base
life-skill qualifications have fixed metadata-backed shapes. The original
guarded role probe confirmed both aptitude buffers were available, positive,
and varied across their respective discipline sets; the later main-attribute
addition is covered by metadata, synthetic mapping, and guarded-call safety
tests without recording representative values.
Learned martial, equipped martial, learned life-skill, feature, and age facts
were readable as supporting evidence. Saved location was unavailable and
therefore remained descriptive missing evidence.

Every current martial qualification, current martial attainment, current
life-skill qualification, and current life-skill attainment call failed at the
standalone live-runtime boundary `SpecialEffectDomain.ModifyData`. No zero or
base-value fallback was accepted.

The warm aggregate projection was semantically equivalent to the cold result.
The configured save, `GameData.dll`, and `GameData.Shared.dll` retained the same
length, last-write time, and SHA-256 before and after both reads: **3 inspected,
3 unchanged**.

No local identifier, name, path, hash, exact qualification value, age, or raw
save content is recorded.

## Representative matrix

| Evidence case | Source shape | Expected product state |
|---|---|---|
| `E6-REP-LOCAL-ELIGIBLE-001` | Living non-Taiwu roster member with agreeing membership checks and both base qualification buffers | `Eligible`; both aptitude roles evaluable |
| `E6-REP-SYN-INELIGIBLE-001` | Roster member with confirmed non-living state | `Ineligible`; no role score |
| `E6-REP-SYN-INCOMPLETE-001` | Roster ID without a current character object | `Incomplete`; no empty profile or role score |
| `E6-REP-SYN-UNSUPPORTED-001` | Different GameData version or only live-modified current getter available | `Unsupported`; no old mapping or zero fallback |
| `E6-REP-SYN-CONFLICT-001` | Roster membership disagrees with Domain or character membership flag | `Conflicting`; retain all evidence and do not rank |
| `E6-REP-SYN-MARTIAL-ORDER-001` | Two eligible candidates with distinct base qualification in one martial discipline | Higher exact qualification orders first for that role only |
| `E6-REP-SYN-LIFE-ORDER-001` | Two eligible candidates with distinct base qualification in one life-skill discipline | Higher exact qualification orders first for that role only |
| `E6-REP-SYN-TIE-001` | Two eligible candidates with equal base qualification for the selected discipline | Explicit tie; stable display order cannot claim merit |

## Resolved decisions

1. The current saved Taiwu group, excluding the Taiwu player, is the complete
   first-delivery candidate universe.
2. Broad character, target, following, relationship, and location membership
   cannot establish eligibility.
3. Roster, Domain membership, character membership, object existence, and
   living state must agree before a candidate is eligible.
4. The installed version exposes 14 martial and 16 life-skill discipline slots.
5. Base qualifications are standalone-safe saved facts; current modified
   qualification and attainment are unsupported in the standalone runtime.
6. Epic 6 starts with martial-discipline and life-skill-discipline aptitude
   roles over one player-selected stable discipline.
7. Learned/equipped skills, features, age, and location remain supporting facts
   and do not change the first role ordering.
8. Teaching, inheritance, recruitment, general combat support, settlement work,
   and a balanced universal score remain outside the first matrix.
9. The cold and warm performance budgets pass.
10. The changed-revision guard and the accepted stable run both preserve the
    one-snapshot boundary; every inspected source remained unchanged.
