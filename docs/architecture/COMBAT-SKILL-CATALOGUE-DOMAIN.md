# Combat-skill catalogue Domain model

| Field | Value |
|---|---|
| Status | Implemented |
| Backlog item | [E2-003](../roadmap/epic-002/BACKLOG.md#e2-003--define-static-combat-skill-catalogue-models) |
| Progress semantics | [Combat-skill progress semantics](./COMBAT-SKILL-PROGRESS-SEMANTICS.md) |

## Boundary

`TaiWu.Domain.CombatSkills` represents immutable, installed combat-skill
definitions. It has no GameData, SQLite, HTTP, Presentation, filesystem-path,
or character-save dependency. Infrastructure may translate installed records
into these types; Application may query and join them with character progress.

A definition is static and is never authoritative for whether a character has
learned, read, activated, broken through, simplified, or equipped a skill.

## Identity and equality

`CombatSkillDefinition.SkillId` is the stable installed combat-skill ID and the
definition's identity. Two definitions with the same ID compare equal even if
one was imported from a newer source manifest. Version and field comparison
belongs to catalogue lifecycle logic, not entity identity.

Negative IDs are invalid. Importers must surface malformed records as import
diagnostics rather than constructing a plausible replacement ID.

## Field availability

Every source-derived optional value uses `CatalogueField<T>`:

| Status | Meaning |
|---|---|
| `Available` | A typed value and its `CatalogueSourceReference` are present |
| `Unavailable` | The source did not provide a usable value; a reason is required |
| `Unsupported` | The source provided a value the detected importer cannot map; a reason and source are required |

Reading `Value` for a non-available field throws. Callers must branch on
`Status` or `IsAvailable`; an unavailable boolean or number cannot silently
become `false` or zero.

`CatalogueSourceReference` contains only a source kind and opaque source/record
identities such as `gamedata:<version>` and `combat-skill:<id>`. It rejects
path separators and traversal tokens. Local paths and Infrastructure objects
never cross the Domain boundary.

## Bilingual names

`CombatSkillLocalizedNames` accepts at most one
`LocalizedCombatSkillName` per `CatalogueLanguage`. Traditional Chinese and
English values are independent and each carries its own language-resource
source.

Resolution is deterministic:

1. return the requested language when present;
2. otherwise return the other supported language;
3. otherwise return an unavailable field.

The resolved value retains its actual language and source, so Presentation can
identify a fallback without guessing from the text.

## Typed definition fields

`CombatSkillDefinition` contains:

- `CombatSkillDiscipline`: the fourteen installed disciplines from Neigong
  through Music;
- `CombatSkillGrade`: installed grade index `0..8`;
- `CombatSkillFactionId`: non-negative stable faction/organization ID;
- `CombatSkillElement`: the existing six verified element values;
- `CombatSkillEquipmentType`: Neigong, Attack, Agility, Defense, or Assistance;
- `CombatSkillGridCost`: a positive configured base cost;
- `SkillSlotContribution`: four typed specific-grid contributions plus a
  non-negative generic-grid contribution;
- typed requirement IDs and numeric requirement values;
- non-negative preparation, breath/stance, and cast timing values;
- typed Direct, Reverse, and optional Neutral effect IDs;
- raw localized descriptions that are explicitly never verified mechanics;
- one opaque definition source record.

An unknown enum cannot be placed in an `Available` field. The importer must
use `Unsupported` with the raw-value diagnostic. Invalid grade, cost, timing,
effect ID, faction ID, duplicate name language, and duplicate requirement ID
fail construction.

## Typed mechanics versus display text

`CombatSkillRequirementDefinition`, `CombatSkillTimingDefinition`, and
`CombatSkillEffectReferences` are typed facts. Each value retains provenance
and availability.

`RawCombatSkillDescription` is display evidence only. Its
`IsVerifiedMechanic` value is always false. A raw effect or requirement string
may be shown by the catalogue UI but cannot become a recommendation rule,
feasibility fact, counter, or score without a separate verified Domain rule.

## Immutability

Definition requirements, localized names, and raw descriptions are copied to
immutable arrays during construction. Mutating an importer-owned input list
after construction cannot change the definition. Source references, localized
names, typed IDs, field wrappers, timing, effects, and descriptions expose only
read-only state.

## Verification

`CombatSkillDefinitionTests` cover:

- typed field construction and validation;
- stable-ID equality and hashing;
- bilingual preference and fallback;
- duplicate-language rejection;
- unavailable and unsupported values;
- invalid grades, costs, timing, and unknown enums;
- duplicate requirement rejection;
- defensive collection copies;
- the unverified status of raw text;
- rejection of path-like source record identities.

The existing architecture test also guarantees the Domain assembly has no
reference to Infrastructure, API, or GameData assemblies.
