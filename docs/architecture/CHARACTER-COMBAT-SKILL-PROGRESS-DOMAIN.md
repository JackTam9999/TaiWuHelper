# Character combat-skill progress Domain model

| Field | Value |
|---|---|
| Status | Implemented |
| Backlog item | [E2-004](../roadmap/epic-002/BACKLOG.md#e2-004--define-character-skill-progress-and-study-detail-models) |
| Verified semantics | [Combat-skill progress semantics](./COMBAT-SKILL-PROGRESS-SEMANTICS.md) |
| Static definition boundary | [Combat-skill catalogue Domain model](./COMBAT-SKILL-CATALOGUE-DOMAIN.md) |

## Boundary and identity

`CharacterCombatSkillProgress` is an immutable save-snapshot overlay. Its
identity is the tuple:

`character ID` + `save SHA-256 and read time` + `combat-skill ID`.

It never becomes part of the authoritative static catalogue. A later save read
produces a different overlay even when the character and skill IDs match.
`SaveSnapshotIdentity` validates and normalizes the SHA-256 without retaining a
save path.

## Per-field state and provenance

Character facts use `SkillProgressField<T>`:

| Status | Meaning |
|---|---|
| `Available` | One typed value from one source |
| `Unavailable` | No trustworthy value; a reason is required |
| `Conflicting` | At least two typed observations disagree; both values and sources are retained |

`SkillProgressSource` identifies a save snapshot, current-screen observation,
or verified rule using opaque IDs and a stable field identity. It rejects
filesystem paths. Reading `Value` from unavailable or conflicting data throws,
so callers cannot silently turn uncertainty into `false` or zero.

## Independent progress facts

The overlay exposes these independent properties:

- `Learned`: the exact term verified from
  `GetLearnedCombatSkillByType`; the model does not use “obtained” as a fact;
- `Proficiency`: current value, maximum value, and percentage as three fields;
- `StudyDetails`: ordered read state and active state for each stable detail;
- `Breakthrough`: the existing verified
  `BreakthroughDirectionAvailability` value object;
- `ActiveDirection`: the existing `PracticeDirection` value;
- `AttainmentMastered`: the skill-list `已大成` fact;
- `Simplified`: the separate `功法精解` / slot-reduction fact;
- `Activated`: whether any verified page detail is active;
- `Equipped`: current loadout membership.

There is deliberately no single status enum. A skill may be learned, ready for
breakthrough, activated, unequipped, and have unavailable attainment mastery
at the same time.

## Proficiency

`CombatSkillProficiencyProgress` validates the installed proficiency storage
range `0..999999999`. A known maximum must be `1..999999999`, current cannot
exceed a known maximum, and an available percentage must be `0..100`.

The E2-001 save has no persisted proficiency key and the visible percentage
conversion is not verified. Those two fields therefore remain unavailable in
the golden overlay. The model can hold a percentage when a future adapter has
a verified source, but it never derives one merely because current and maximum
values exist.

## Study details and completeness

`CombatSkillStudyDetailProgress` contains:

- stable detail ID;
- unique display order;
- Outline, Direct, or Reverse group;
- optional localized label with static catalogue provenance;
- `Read` or `NotRead`, wrapped in a progress field so unavailable/conflicting
  is representable;
- independent active-selection boolean.

The verified term is **read**, not the broader inferred term “studied.”
Adapters map the fifteen installed bits defined by E2-002; the Domain model can
also represent a different count from a future version without inventing
missing entries.

`CombatSkillStudySummary` is derived:

- any known `NotRead` detail proves `IsComplete=false`;
- if every known detail is `Read` but at least one detail is unavailable or
  conflicting, completeness is unavailable, not false;
- all details available and `Read` produces `IsComplete=true`;
- zero details produces unavailable completeness.

Unavailable details are counted separately and never added to the not-read
count.

## Verified combination rules

The constructor rejects only combinations proven impossible by E2-002:

- an available active direction must be Direct or Reverse;
- an available active direction is impossible before completed breakthrough;
- when every detail activation is available, aggregate `Activated` must equal
  whether any detail is active;
- duplicate detail IDs and duplicate display orders are invalid.

A completed breakthrough with an unavailable direction remains valid because
an unsupported activation value must not be guessed. Unproven source
disagreements use `Conflicting` and retain both observations.

## Relationship to `CombatSkillSnapshot`

The existing `CombatSkillSnapshot` remains the compact Epic 1 combat-planning
projection. The atlas model reuses its authoritative
`BreakthroughDirectionAvailability`, `PracticeDirection`, and element/grid
value objects rather than creating contradictory alternatives.

Static names, cost, element, and effect references belong to
`CombatSkillDefinition`. Character-specific learned, detail, mastery,
activation, and equipment facts belong to `CharacterCombatSkillProgress`.
Infrastructure must map shared source values once and feed both projections;
the atlas does not parse the legacy snapshot or diagnostic text.

The older `CombatSkillSnapshot.Mastered` source corresponds to simplification,
not `AttainmentMastered`. E2-004 gives the two concepts unambiguous names; a
later adapter slice will populate them from their distinct verified sources.

## Verification

`CharacterCombatSkillProgressTests` cover:

- snapshot-keyed equality;
- learned terminology;
- unavailable and bounded proficiency;
- partial, complete, empty, and unknown detail sets;
- aggregate completeness without false negatives;
- duplicate and immutable detail collections;
- Direct/Reverse breakthrough and unknown direction states;
- independent attainment mastery, simplification, activation, and equipment;
- impossible activation combinations;
- conflicts retaining both source observations;
- invalid save fingerprints.
