# Loadout comparison Domain model

## Purpose

E4-001 introduces an immutable, presentation-neutral vocabulary for the
loadout comparison specified by
[the comparison contract](./LOADOUT-COMPARISON-CONTRACT.md). The types live in
`TaiWu.Domain.LoadoutComparisons` and contain no file, GameData, SQLite,
ASP.NET Core, screenshot, process, or persistence dependency.

This slice defines valid comparison data. E4-002 remains responsible for
building it from one `CombatLoadoutRecommendation` and proving manual-plan
parity.

## Aggregate

| Type | Responsibility |
|---|---|
| `LoadoutComparison` | Root that binds logical comparison, snapshot, and target references to typed columns and Current provenance |
| `LoadoutComparisonColumn` | One Current or policy column with a status-valid loadout, tactical summary, or diagnostic |
| `LoadoutComparisonLoadout` | All five ordered category rows and the column's complete 萬用 allocation state |
| `LoadoutComparisonCategoryRow` | One category's capacity and stable-ID-ordered skill cells |
| `LoadoutComparisonSkillCell` | Stable identity, membership, effective cost, and zero or more separate actions |
| `LoadoutComparisonSkillAction` | Required Direct/Reverse direction change or breakthrough with an authoritative reason |
| `LoadoutComparisonCapacitySummary` | Explicit available/unavailable used, capacity, remaining, category contribution, and 萬用 contribution |
| `LoadoutComparisonTacticalSummary` | Policy-local threats, conditions, caveats, active roles, action count, evidence, and score components |
| `LoadoutComparisonBaselineProvenance` | Source, UTC time, and evidence for one Current baseline field |
| `LoadoutComparisonDiagnostic` | Stable failure code, safe summary, and ordered evidence references |

Every caller-supplied collection is copied into an `ImmutableArray<T>`.
Mutating the original list after construction cannot change the comparison.

## Logical references

`LoadoutComparisonReference` is the common value for public comparison,
snapshot, target, diagnostic, threat, condition, caveat, reason, and evidence
identities. It trims its input and rejects:

- blank values;
- whitespace inside the identity;
- `/` or `\` path separators;
- `..` traversal-like content;
- Windows drive prefixes; and
- values longer than 128 characters.

The value may contain a short namespace separator such as
`snapshot:AB12` or `threat:mind-pressure`. It compares by normalized value.
Display text is stored separately and never acts as identity.

## Root and column invariants

`LoadoutComparison` requires:

- non-null comparison, snapshot, and target logical references;
- exactly one `Current` column;
- no more than one `Safe`, `Balanced`, or `Aggressive` column;
- canonical Current, Safe, Balanced, Aggressive order for the columns that
  exist; and
- unique, canonically ordered baseline provenance fields.

Missing policy columns are valid Domain state. E4-002 will normally emit all
three and use an Unavailable diagnostic if a style result itself is absent.
Keeping the root tolerant of missing policies lets constructor tests and
future versioned readers distinguish missing input from a fabricated proposal.

The column status matrix is enforced at construction:

| Column | Status | Loadout | Tactical summary | Diagnostic |
|---|---|---:|---:|---:|
| Current | Available | Required | Forbidden | Forbidden |
| Policy | Available | Required | Required | Forbidden |
| Policy | Infeasible | Forbidden | Forbidden | Required |
| Policy | Unavailable | Forbidden | Forbidden | Required |

Current cannot be infeasible. An infeasible or unavailable policy cannot
carry an empty or otherwise feasible-looking proposed loadout.

## Category and skill invariants

Every `LoadoutComparisonLoadout` contains exactly one row for every
`SkillCategory` in Domain order: Neigong, Attack, Agility, Defense, and
Assistance. Rows cannot be omitted, duplicated, or reordered.

A category row:

- rejects null skill cells;
- rejects a skill whose identity names another category;
- rejects duplicate `(SkillCategory, SkillId)` identities; and
- requires ascending stable skill-ID order.

The stable identity rejects negative skill IDs. Localized names do not appear
in the Domain identity and therefore cannot affect equality or ordering.

## Membership and composite actions

`LoadoutComparisonMembership` contains `Present`, `Retained`, `Added`, and
`Removed`. Availability is orthogonal: a
`LoadoutComparisonValue<LoadoutComparisonMembership>` may instead be
Unavailable with a reason.

Column construction enforces the vocabulary boundary:

- Current cells may contain only `Present` or unavailable membership and may
  not contain policy actions.
- Policy cells may contain `Retained`, `Added`, `Removed`, or unavailable
  membership and may not contain `Present`.

`LoadoutComparisonSkillActionKind` separately represents
`DirectionChangeRequired` and `BreakthroughRequired`. Each action requires a
verified Direct or Reverse direction and a `LoadoutComparisonReason`. Neutral
is rejected. Actions are unique and canonically ordered.

This allows, without collapsing:

- Added plus direction change;
- Added plus breakthrough;
- Retained plus direction change; and
- a future authoritative composite containing both distinct action kinds.

An unavailable membership cannot claim a manual action. E4-002 maps each
available membership and action back to exactly one existing
`ManualLoadoutChange`.

## Available and unavailable facts

`LoadoutComparisonValue<T>` has two construction paths:

- `Available(value)` rejects null; and
- `Unavailable(reason)` rejects a blank reason.

Reading `Value` while unavailable throws. This forces builders, API mappers,
and Presentation code to branch on `IsAvailable` instead of interpreting a
default value as evidence. An available zero remains distinct from an
unavailable number.

`LoadoutComparisonCapacitySummary` additionally enforces:

- available used, capacity, remaining, and 萬用 contribution values are
  non-negative;
- category contribution may be negative because verified Neigong effects may
  reduce one category;
- used cannot exceed capacity when both are available; and
- remaining equals capacity minus used when all three are available.

An unavailable used value can coexist with available capacity. Remaining
then remains independently unavailable with its own retained reason.

Effective skill cost is positive when available. The entire
`GenericSlotAllocation` is one available/unavailable value so a partial
allocation cannot be mistaken for a complete proposal.

## Provenance

`LoadoutComparisonBaselineField` identifies the Current facts whose sources
matter to the player:

1. equipped skills;
2. 萬用 allocation;
3. slot budgets; and
4. legendary-book cost assignments.

Each present provenance entry retains its `SnapshotDataSource`, normalized UTC
capture time, and opaque evidence reference. The root forbids duplicate or
misordered fields. A builder can therefore represent save-only, observed, or
mixed baselines without assigning one misleading source to every fact.

## Tactical summary

The policy-only tactical summary keeps independent typed facts for:

- manual-action count;
- primary active defense and agility identities;
- covered and unresolved threat references;
- condition and caveat references;
- evidence references; and
- policy-local score components.

Unavailable action counts and active roles use `LoadoutComparisonValue<T>` and
must retain reasons. Logical-reference collections are duplicate-free and
ordinally ordered. The same threat cannot be both covered and unresolved in
one summary. Score components are unique and ordered by existing
`RecommendationScoreComponentKind`; each retains weight, an
available/unavailable score, explanation, and evidence reference.

No type compares score totals across policies or represents win probability.
E4-006 will populate these facts only from the existing style result.

## Diagnostics

`LoadoutComparisonDiagnostic` separates a stable logical code from safe
human-readable summary text and zero or more ordered evidence references. A
diagnostic summary must not contain a local path, exception dump, or raw
Infrastructure error. API projection remains responsible for localization and
public error-detail policy.

## Verification

`LoadoutComparisonModelTests` covers:

- exact Current and unique policy columns;
- missing policy columns;
- canonical ordering;
- duplicate and mismatched skill identities;
- membership/action separation and composites;
- infeasible-policy diagnostics;
- path-safe reference equality;
- available/unavailable values and numeric arithmetic;
- immutable collection copies; and
- typed provenance uniqueness and ordering.

The focused verification command is:

```powershell
dotnet test tests\TaiWu.Domain.UnitTests\TaiWu.Domain.UnitTests.csproj --no-restore
```
