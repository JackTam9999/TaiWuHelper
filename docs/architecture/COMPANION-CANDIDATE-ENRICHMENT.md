# Companion-candidate enrichment architecture

| Field | Value |
|---|---|
| Status | Implemented for E6-005 |
| Epic | [EPIC-006](../roadmap/epic-006/EPIC.md) |
| Backlog item | [E6-005](../roadmap/epic-006/BACKLOG.md#e6-005--enrich-candidate-profiles-with-verified-catalogue-and-progress-facts) |
| Snapshot boundary | [Companion-candidate snapshot](./COMPANION-CANDIDATE-SNAPSHOT.md) |
| Evaluation boundary | [Companion role definition and evaluation](./COMPANION-ROLE-EVALUATION.md) |

## Purpose and evidence boundary

E6-005 joins the immutable one-pass candidate snapshot to the existing
version-aware combat-skill catalogue. The join provides supporting definition
and localized display facts for exact saved learned/equipped identities while
preserving the E6-000 source boundary.

Neither approved version-1 role scores learned skills, equipment, mastery,
proficiency, study, activation, or any other progress dimension. Their only
role-evaluable values remain the saved base qualification for the selected
discipline. Enrichment therefore never adds or replaces a `CandidateProfile`
fact and never changes a profile fingerprint.

## Application service

`EnrichCompanionCandidateProfiles` depends only on:

- `ICombatSkillDefinitionSource`, to establish the installed catalogue source
  identity and version; and
- `ICombatSkillCatalogueRepository`, to verify and query the helper-owned
  current catalogue once.

It deliberately does not depend on `ICharacterCombatSkillProgressReader` or
`ReadCharacterCombatSkillAtlas`. Those paths open a configured archive for one
character. Calling either once per candidate would create an N+1 archive
workflow and would introduce unapproved progress facts.

The E6-004 snapshot is the batch progress boundary for the approved saved
membership facts. The service reads its immutable profiles in memory and
performs at most one catalogue query for the whole candidate set.

## Result and candidate states

The result and every candidate keep distinct catalogue states:

| Result state | Candidate state | Meaning |
|---|---|---|
| `Complete` | `Complete` | Current compatible catalogue and complete membership evidence |
| `Partial` | `Partial` | Membership evidence or a referenced definition is incomplete |
| `CatalogueMissing` | `CatalogueMissing` | Helper catalogue or installed sources are absent |
| `CatalogueStale` | `CatalogueStale` | Stored catalogue identity differs from installed sources |
| `CatalogueRebuilding` | `CatalogueRebuilding` | Existing catalogue rebuild gate is active |
| `CatalogueUnsupported` | `CatalogueUnsupported` | Source or snapshot GameData versions are incompatible |
| `CatalogueFailed` | `CatalogueFailed` | Source, corrupt repository, validation, or query failure |

The original `CombatSkillCatalogueStatus` and installed
`CombatSkillCatalogueSourceIdentity` also remain in the result. No state is
collapsed into an empty definition set.

## Membership and progress semantics

For every profile the service resolves these exact E6-000 fields:

- `LearnedMartialSkillIdentities`;
- `EquippedMartialSkillIdentities`; and
- `LearnedLifeSkillIdentities`.

Each aggregate retains `Available`, `Incomplete`, `Unsupported`, `Stale`, or
`Conflicting` evidence state. A confirmed membership fact is usable only when
its configured-save revision matches the snapshot SHA-256 and its source
version matches the profile-mapping version.

Combat entries are the stable union of usable learned and equipped IDs. Each
entry retains separate nullable learned and equipped facts:

- a confirmed collection can prove `true` membership or `false` absence;
- an incomplete, stale, unsupported, or conflicting collection produces no
  Boolean value; and
- equipped-only membership remains two exact facts, not an inferred error or
  proof of mastery.

`DetailedProgressState` is always `NotRequestedByApprovedRole`. The service
does not relabel learned membership as mastery, proficiency, completed study,
breakthrough, activation, simplification, teaching ability, or current combat
contribution. Life-skill and feature identities remain unchanged in the
profile because no verified versioned catalogue or mechanics join is approved
for them in this slice.

## Catalogue compatibility and display data

The existing catalogue-status workflow must report `Current`. Both installed
and stored catalogue GameData versions must exactly equal the candidate
snapshot GameData version before the repository is queried.

For a compatible catalogue, each saved combat-skill identity is joined to at
most one `CombatSkillDefinition`:

- `Available` retains the typed definition, including bilingual names as
  display values;
- `Missing` retains membership and a typed candidate diagnostic; and
- `CatalogueUnavailable` retains membership while withholding definitions.

Localized name text never identifies a skill, matches a candidate, changes
membership, enters a role profile, or affects result ordering and fingerprint.
Changing display text under an equivalent source identity does not change the
semantic enrichment fingerprint.

## Determinism and isolation

Candidate profiles are processed in stable character-ID order and skill joins
in stable numeric skill-ID order. Source profile order, learned/equipped set
order, catalogue query order, and task scheduling cannot change output.

The fingerprint includes snapshot save and mapping identity, catalogue source
identity and fingerprints, typed result/candidate states, original profile
fingerprints, membership values/states, definition availability, and stable
diagnostics. It excludes localized display text, paths, free-form failure
detail, and timestamps.

One candidate's incomplete evidence or missing definition makes that candidate
and the aggregate result partial but does not remove or alter another
candidate. Duplicate catalogue definitions fail the catalogue query safely;
they never select a first entry by enumeration order.

## Verification

Thirteen focused Application test cases cover:

- current exact-version catalogue and membership joins;
- catalogue/snapshot version mismatch;
- missing, corrupt, failed, and stale repositories;
- incomplete, stale, and conflicting membership evidence;
- missing and duplicate definitions and query failure;
- one partial candidate alongside an unaffected candidate;
- deterministic candidate, set, definition, and localization order; and
- preservation of the exact original profile facts and fingerprint.

Architecture tests prove that enrichment has no character-progress reader,
single-character atlas, mutable contract, or second profile-building path and
contains exactly one catalogue query call site.

The guarded production test on 2026-08-17 returned `CatalogueStale` for the
current local helper cache, which predates the newly installed GameData
version. It retained one candidate and 57 saved learned/equipped combat-skill
identities, accepted no stale definitions, produced an equivalent repeated
fingerprint, and preserved SHA-256, length, and last-write time for the save
plus seven installed GameData/language catalogue sources. No cache rebuild or
other write was attempted.
