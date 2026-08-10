# Target archetype matching

This document defines the versioned, multi-label archetype rule and match
contract delivered by
[E5-002](../roadmap/epic-005/BACKLOG.md#e5-002--define-versioned-multi-label-archetype-rules-and-match-states).
It consumes only the immutable facets defined in
[TARGET-COMBAT-PROFILE.md](TARGET-COMBAT-PROFILE.md). Profile extraction and
snapshot integration belong to E5-003.

## Decision

An archetype is a versioned rule over typed profile facets, not a target class,
target name, weapon-name heuristic, or fixed loadout. Every applicable
definition is evaluated independently. A profile can match several
archetypes, and the same definition can match many profiles.

Unknown is never negative. `NotMatched` requires one of two confirmed facts:

- a required value-equality predicate was contradicted by a confirmed typed
  value; or
- an explicit exclusion predicate was confirmed.

A missing, incomplete, unsupported, or conflicting facet cannot create
`NotMatched` by itself.

## Domain contract

E5-002 adds `TaiWu.Domain.TargetArchetypes` with no Application,
Infrastructure, API, persistence, filesystem, process, or GameData dependency.

| Type | Responsibility |
|---|---|
| `TargetArchetypeIdentity` | Stable non-localized code plus definition version |
| `TargetArchetypeFacetPredicate` | Stable predicate code, facet identity, operator, and optional compatible expected value |
| `TargetArchetypeDefinition` | Applicable profile-rule version, localized-title resource key, required predicates, optional supporting predicates, exclusions, and evidence references |
| `TargetArchetypeMatch` | One definition's typed result against one profile fingerprint, including supporting, missing, excluding, conflicting, and diagnostic facts |
| `TargetArchetypeMatchSet` | Canonically ordered result for every supplied definition |
| `TargetArchetypeMatcher` | Pure deterministic evaluation of immutable profiles and definitions |

Definitions contain no target character ID, localized matching string, raw
GameData value, skill/effect description, loadout, counter, or recommendation.
`LocalizedTitleKey` is only a stable resource lookup key and never participates
in matching.

## Definition invariants

Every definition requires:

- a stable archetype code and version;
- the exact applicable profile-rule version;
- a valid localized-title resource key;
- at least one required predicate;
- explicit supporting-predicate and exclusion collections, which may be empty;
  and
- one or more unique evidence references.

Predicate codes and facet identities are unique across all three roles. A
definition cannot require and exclude the same facet, or evaluate one facet
twice under different role names. Collections are copied and sorted by stable
ordinal keys.

Changing predicate semantics, applicability, or evidence requires a new
definition version. The public definition stable key is
`<ARCHETYPE_CODE>@<VERSION>`.

## Predicate operators

| Operator | Expected value | Confirmed-facet result |
|---|---|---|
| `FacetConfirmed` | Forbidden | Satisfied when the exact facet is confirmed |
| `ValueEquals` | Required and dimension/code compatible | Satisfied when the confirmed typed value equals the expected value; otherwise contradicted |

Neither operator parses localized text or raw descriptions. Predicate identity
uses the facet's stable dimension and code. Typed value equality uses the
immutable measurement/value contract from E5-001.

## Facet evaluation

| Profile fact | Predicate evaluation |
|---|---|
| Exact facet absent | Missing |
| Facet `Incomplete` | Incomplete |
| Facet `Unsupported` | Unsupported |
| Facet `Conflicting` | Conflicting |
| Facet `Confirmed`, `FacetConfirmed` predicate | Satisfied |
| Facet `Confirmed`, matching `ValueEquals` predicate | Satisfied |
| Facet `Confirmed`, different `ValueEquals` value | Contradicted |

The matcher never substitutes a zero, guessed value, nearby version, target
label, or category inference for one of these states.

## Predicate roles

### Required

- Satisfied required predicates become supporting facet references.
- Missing, incomplete, or unsupported required predicates become missing facet
  references with typed diagnostics.
- A contradicted required value becomes an excluding facet reference and is
  sufficient for `NotMatched` when no conflict exists.
- Conflicting required evidence becomes a conflicting facet reference.

### Supporting

Supporting predicates add evidence and explanation but are optional. A
missing, incomplete, unsupported, or contradicted supporting predicate does
not block an otherwise matched definition. A conflicting supporting facet
remains visible and produces `Conflicting`, because the exact profile contains
an unresolved disagreement relevant to the rule.

### Exclusion

- A satisfied exclusion becomes an excluding facet reference and is sufficient
  for `NotMatched` when no conflict exists.
- A confirmed value that contradicts a value-equality exclusion clears that
  exclusion.
- A missing, incomplete, or unsupported exclusion remains unresolved and
  becomes a missing facet reference. It cannot be treated as proof that the
  exclusion is absent.
- A conflicting exclusion becomes a conflicting facet reference.

An unresolved exclusion therefore turns an otherwise supported rule into
`Partial`, not `Matched` or `NotMatched`.

## Match-state resolution

States are selected in this order:

1. A profile-rule version mismatch produces `Unsupported` without evaluating
   facets.
2. Any required, supporting, or exclusion conflict produces `Conflicting` and
   retains every evaluated reference.
3. A confirmed required contradiction or confirmed exclusion produces
   `NotMatched`.
4. All required predicates satisfied, every exclusion cleared, and no blocking
   missing facet produces `Matched`.
5. At least one required predicate satisfied plus a blocking missing required
   or exclusion facet produces `Partial`.
6. No required predicate can be positively established produces
   `Unsupported`.

| State | Required construction facts |
|---|---|
| `Matched` | At least one supporting facet; no missing, excluding, or conflicting facet |
| `Partial` | At least one supporting and one missing facet; no excluding or conflicting facet |
| `NotMatched` | At least one excluding facet backed by confirmed contrary evidence; no unresolved conflict |
| `Unsupported` | No supporting, excluding, or conflicting facet; typed version or availability diagnostic |
| `Conflicting` | At least one conflicting facet; other evaluated references remain visible |

Non-matched results require typed diagnostics. The initial codes distinguish
version mismatch, missing/incomplete/unsupported requirements, required-value
contradiction, confirmed/unresolved exclusion, conflicting evidence, and the
absence of any established required facet.

## Result references

Each result retains canonical immutable arrays for:

- supporting facets;
- missing facets;
- excluding facets;
- conflicting facets; and
- typed diagnostics with an optional predicate and facet reference.

One facet cannot occupy more than one result role. This keeps UI and API
explanations from silently describing the same fact as both support and
contrary evidence.

## Multi-label evaluation

`TargetArchetypeMatcher.Match` evaluates every unique supplied definition and
returns a result even when the state is partial, unsupported, conflicting, or
not matched. It never stops after the first match.

The match set sorts by archetype code and definition version. The convenience
`Matched` collection filters only matched results but does not remove the
other states from the complete set.

This produces both required invariants:

- one profile can match multiple independent definitions; and
- one definition can match multiple profiles without containing either target
  identity.

## Deterministic identity

Definitions use the explicit code/version stable key. A match stable key is an
uppercase SHA-256 over a length-prefixed canonical representation containing:

- definition stable key;
- profile fingerprint;
- match state;
- canonical supporting, missing, excluding, and conflicting facet references;
  and
- canonical diagnostics.

The match-set stable key similarly covers the profile fingerprint and every
canonically ordered match key. Input definition, predicate, evidence, and
facet ordering cannot change the result.

Localized title text, target display text, local paths, timestamps, mutable
references, and fixed recommendations are absent from the identity.

## Version and safety boundary

Only an exact `ApplicableProfileRuleVersion` match permits predicate
evaluation. A nearby or newer version is `Unsupported`; the matcher does not
partially use the definition.

The evaluator is pure Domain code over immutable input. It reads no save,
configuration, language file, process, runtime memory, or UI state and exposes
no mutation or game-control capability.

## Verification

Synthetic Domain tests cover:

- every match state;
- missing, incomplete, unsupported, contradicted, and conflicting facet paths;
- unresolved and confirmed exclusions;
- optional support;
- deterministic ordering and stable keys;
- one profile matching multiple definitions; and
- one definition matching multiple target profiles.
