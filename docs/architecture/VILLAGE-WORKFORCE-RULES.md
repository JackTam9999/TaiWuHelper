# Village workforce rule catalogue

| Field | Value |
|---|---|
| Status | Implemented — verified versioned rule definition |
| Epic | [EPIC-007](../roadmap/epic-007/EPIC.md) |
| Backlog item | [E7-004](../roadmap/epic-007/BACKLOG.md#e7-004--define-versioned-assignment-and-work-objective-rules) |
| Product contract | [Village workforce evaluation contract](./VILLAGE-WORKFORCE-EVALUATION-CONTRACT.md) |
| Snapshot input | [Village workforce snapshot](./VILLAGE-WORKFORCE-SNAPSHOT.md) |
| Supported GameData | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` |

## Purpose

Define the only rule that E7-005 may evaluate for the first village-workforce
vertical. The rule compares an alternative worker for one occupied shop
manager position using the exact saved base life-skill qualification required
by that shop.

The catalogue does not infer a shop's output, current modified attainment,
availability, capacity or vacancy. It has no localized-name or raw-label
lookup and cannot choose a rule from display text.

## Stable rule identity and versions

| Property | Verified value |
|---|---|
| Rule identity | `SHOP_MANAGER_REQUIRED_BASE_LIFE_SKILL_QUALIFICATION` |
| Rule semantic version | `1.0.0` |
| Objective | `ShopManagerBaseLifeSkillQualification`, objective version `1` |
| Target kind | `ShopManagerSlot` |
| GameData version | Exact supported installed product version above |
| Snapshot mapping version | `1` |
| Candidate-universe version | `1` |
| Fingerprint schema version | `2` |

`WorkforceRuleVersion` enforces `MAJOR.MINOR.PATCH` Semantic Versioning syntax,
including valid optional prerelease and build identifiers. Short forms such as
`1`, leading-zero core numbers and leading-zero numeric prerelease identifiers
are rejected.

`VerifiedVillageWorkforceRules.Resolve` compares each version using ordinal
equality. It does not choose a nearest, older or default rule. A mismatch
returns one of these typed states with no rule payload:

- `UnsupportedObjectiveVersion`;
- `UnsupportedGameDataVersion`;
- `UnsupportedMappingVersion`;
- `UnsupportedCandidateUniverseVersion`;
- `UnsupportedFingerprintSchemaVersion`; or
- `UnsupportedTargetKind`.

## Ordered hard requirements

The verified rule contains every approved gate exactly once and evaluates
them in this order:

| Order | Requirement | Required evidence |
|---:|---|---|
| 1 | `SupportedSourceVersion` | Exact snapshot source-version tuple |
| 2 | `SupportedShopTarget` | Typed occupied shop target with a valid required discipline |
| 3 | `AlternativeWorkCandidate` | Confirmed candidate-universe membership fact |
| 4 | `CharacterProfileAvailable` | Confirmed target-discipline base-qualification fact |
| 5 | `QualificationProvenanceMatch` | That same fact belongs to the snapshot revision and source mapping |

Requirement identity, evidence kind and source fact form a validated tuple.
The rule rejects an unknown field, a membership gate pointed at qualification,
or a qualification gate pointed at Boolean membership.

The distinctions from the evidence gate are deliberate:

- **membership** is the exact `GetVillagersForWork(true, false)` result and is
  represented by `AlternativeWorkCandidate`;
- **profile availability** is whether the exact required saved fact can be
  read and is represented by `CharacterProfileAvailable`;
- **target compatibility** is a typed occupied shop-manager target and is
  represented by `SupportedShopTarget`;
- broad work-availability diagnostics were not verified as eligibility and do
  not produce a gate; and
- no explicit vacant manager position was established, so vacancy is neither
  inferred from collection capacity nor represented as a version-1 rule.

This first rule is replacement-only. It cannot rank a proposal for an invented
vacancy.

## Numeric component

Version `1.0.0` has exactly one component:

| Property | Rule value |
|---|---|
| Identity | `RequiredBaseLifeSkillQualification` plus target discipline |
| Source fact | Exact `BaseLifeSkillQualification` at that discipline |
| Raw type | Saved `Int16` |
| Normalization | `Identity` |
| Unit | `BaseQualificationPoint` |
| Direction | `HigherIsBetter` |
| Weight | `1` |
| Explanation identity | `REQUIRED_BASE_LIFE_SKILL_QUALIFICATION_EXACT_VALUE` |

The component constructor rejects a different discipline, membership source,
unknown source field, non-identity normalization, mismatched or unknown unit,
different direction, or any weight other than one. The rule definition rejects
duplicate component identities and duplicate source fields.

Six-attribute summaries, martial-discipline summaries, whole-life-skill
summaries and the Epic 6 comprehensive base-capability score remain descriptive
companion evidence. None appears in this rule, its fingerprint or its future
workforce result.

## Limitations

The definition carries stable limitation identities for localization by a
later Presentation slice:

- `SAVED_BASE_QUALIFICATION_ONLY`;
- `NO_EFFICIENCY_OUTPUT_OR_REVENUE`; and
- `OCCUPIED_SHOP_REPLACEMENT_ONLY`.

Limitations are mandatory, immutable, canonically ordered and unique. They are
shared result scope, not repeated worker evidence.

## Definition validation and fingerprint

`WorkforceRuleDefinition` rejects:

- null or duplicate requirement, component or limitation entries;
- duplicate requirement orders;
- missing or extra approved hard requirements;
- anything other than the one verified numeric component;
- profile/provenance gates that do not reference the component's exact fact;
- an unknown target kind; and
- a rule without a limitation.

Its SHA-256 fingerprint covers rule identity and semantic version, objective,
supported source tuple, target kind, ordered requirements, component contract
and sorted limitations. Re-resolving the same discipline and versions produces
the same definition fingerprint.

## Dependency and safety boundary

Rules live entirely in `TaiWu.Domain.VillageWorkforce`. They contain no
GameData, filesystem, database, network, process, UI, localization or mutation
type. Resolving a rule cannot read the save or issue an assignment. E7-005 may
consume only a resolved definition and one immutable E7-003 snapshot.

## Verification

Focused tests pin the delivered rule and every supported-version boundary.
They also cover semantic versions, typed unsupported resolution, duplicate
identity rejection, invalid weights and units, and unsupported source fields.

```powershell
dotnet test tests\TaiWu.Domain.UnitTests\TaiWu.Domain.UnitTests.csproj -c Release --no-build -- --filter-class TaiWu.Domain.UnitTests.VillageWorkforce.VillageWorkforceRuleTests
```
