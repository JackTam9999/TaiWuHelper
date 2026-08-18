# Village workforce evaluator

| Field | Value |
|---|---|
| Status | Implemented — deterministic target-specific evaluation |
| Epic | [EPIC-007](../roadmap/epic-007/EPIC.md) |
| Backlog item | [E7-005](../roadmap/epic-007/BACKLOG.md#e7-005--evaluate-worker-eligibility-and-deterministic-suitability) |
| Rule input | [Village workforce rules](./VILLAGE-WORKFORCE-RULES.md) |
| Snapshot input | [Village workforce snapshot](./VILLAGE-WORKFORCE-SNAPSHOT.md) |

## Purpose

Evaluate every profile in one immutable snapshot for one occupied shop-manager
target using one resolved rule. The evaluator produces target-specific hard
gate outcomes, an optional exact qualification component, typed unrankable
states, exact ties and a canonical evaluation-set fingerprint.

It does not read GameData or the save, resolve display labels, calculate rank
numbers, select a proposed assignment, or estimate output. Those concerns
remain in their assigned Infrastructure, shortlist, Application and
Presentation slices.

## Inputs and result identity

`VillageWorkforceEvaluator.Evaluate` requires:

- one `VillageWorkforceSnapshot`;
- one `ShopManagerTargetIdentity` that exists in that snapshot; and
- one `WorkforceRuleDefinition`.

The result identity contains the snapshot fingerprint, objective, semantic
rule version and target. `VillageWorkforceEvaluationSet` additionally retains
the exact immutable rule definition, its fingerprint, its limitations, and the
selected target's current worker. It contains every snapshot worker exactly
once in stable character-ID order.

The set rejects nulls, duplicate workers, cross-result evaluations and a
result that drops the selected target's current worker.

## Gate-first evaluation

The evaluator constructs all five typed requirements before considering a
numeric component:

1. the snapshot source tuple exactly matches the rule;
2. the occupied shop target, current assignment and required discipline match
   the rule;
3. candidate-universe membership is confirmed true;
4. the exact required qualification fact is confirmed; and
5. that fact has matching configured-save revision and mapping provenance.

Outcomes preserve `Passed`, `Failed`, `Incomplete`, `Unsupported` and
`Conflicting`. Unavailable facts retain their exact reason identity;
conflicting facts retain their canonically ordered typed conflict values and
provenance. State precedence is conflict, unsupported source/shape, incomplete
evidence, then verified failure. A high qualification cannot override an
earlier failed gate.

A confirmed false candidate-membership fact is a verified failure. If the
profile is `CurrentOnly`, the factual saved qualification may remain visible
as a descriptive value, but the evaluation is never rankable. Missing, stale,
unsupported or conflicting qualification evidence never becomes zero.

## Component and formula

Only `Ranked`, provisional tie candidates, and `CurrentOnly` descriptive rows
may carry the component. It retains:

| Property | Value |
|---|---|
| Raw value | Exact saved `Int16` required-discipline qualification |
| Normalized value | Raw value unchanged |
| Weight | `1` |
| Contribution | Normalized value unchanged |
| Unit | `BaseQualificationPoint` |
| Evidence | Canonical source evidence plus direct fact provenance |
| Explanation identity | `REQUIRED_BASE_LIFE_SKILL_QUALIFICATION_EXACT_VALUE` |

An unrankable incomplete, unsupported, conflicting or ineligible evaluation
has no component and no result value. This static shape prevents a missing
fact from being rendered as a low score.

## Evaluation and tie states

| State | Exact evaluator meaning |
|---|---|
| `Ranked` | All five gates pass and no other rankable worker has the exact total |
| `Tied` | All five gates pass and at least one other rankable worker has the exact total |
| `CurrentOnly` | Saved current-only profile is outside the proposal universe; optional descriptive value only |
| `Ineligible` | A verified candidate gate fails |
| `Incomplete` | A required fact is missing or stale |
| `Unsupported` | Source, target, rule shape or fact state is unsupported |
| `Conflicting` | Required evidence or provenance conflicts |

Tie detection groups exact decimal result values. It does not use a tolerance,
localized name, current-worker marker, source enumeration position or worker
ID as merit. Stable worker ID orders the immutable collection only.

Competition rank numbers are deliberately deferred to E7-006. E7-005 marks
the semantic tie without inventing a tie breaker.

## Provenance and evidence

Source and target gates retain a derived-rule evidence reference. Target
evidence also retains the snapshot's shop evidence and current-assignment
provenance. Fact gates retain the fact's canonical evidence and a direct value
provenance reference when present.

Qualification provenance passes only when it is configured-save evidence with
the snapshot mapping version and exact save SHA-256 revision. Snapshot
construction already rejects mixed GameData/save evidence; the evaluator still
records this target-specific gate rather than assuming it silently.

Reason and explanation identities are stable non-localized tokens. Later
layers must map them to typed bilingual text and must not expose the raw token.

## Determinism

`VillageWorkforceEvaluationSet` fingerprints:

- immutable result identity;
- full rule-definition fingerprint;
- selected target's current worker; and
- every canonical worker-evaluation fingerprint.

The snapshot and evaluation constructors canonicalize their inputs. Reordering
otherwise identical worker profiles does not change the evaluation set,
states, exact ties or fingerprint. Changing an evaluation fact changes the
worker and result fingerprints.

## Verification

Focused tests cover ranked, exact tie, failed eligibility with a high value,
missing evidence without zero, unsupported and conflicting facts,
current-only descriptive values, formula/evidence preservation, source and
target mismatches, canonical fingerprints and duplicate rejection.

```powershell
dotnet test tests\TaiWu.Domain.UnitTests\TaiWu.Domain.UnitTests.csproj -c Release --no-build -- --filter-class TaiWu.Domain.UnitTests.VillageWorkforce.VillageWorkforceEvaluatorTests
```
