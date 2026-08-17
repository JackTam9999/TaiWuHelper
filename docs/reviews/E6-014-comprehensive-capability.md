# E6-014 comprehensive base-capability objective review

| Field | Result |
|---|---|
| Backlog | [E6-014](../roadmap/epic-006/BACKLOG.md#e6-014--make-comprehensive-base-capability-a-selectable-objective) |
| Objective identity | `COMPREHENSIVE_BASE_CAPABILITY` version `1` |
| Aggregate identity | `Capability/0` |
| Candidate boundary | Current saved non-Taiwu group only; no village expansion |
| Review date | 2026-08-17 |
| Result | Complete |

## Delivered behavior

Role discovery now exposes three objectives. Martial and life-skill aptitude
retain their required discipline select and exact saved-base scoring. The new
comprehensive objective requires no discipline choice and orders only
complete, comparable candidates by the existing version-1 breadth index.

The breadth total is the equal-weight mean of these three rounded saved-base
category averages:

1. six main attributes;
2. 14 martial aptitudes; and
3. 16 life-skill aptitudes.

Every one of the 36 typed profile facts must be confirmed and have configured-
save provenance matching the profile revision and mapping version. Missing,
incomplete, unsupported, stale, conflicting, mismatched, or out-of-range
evidence produces no component, total, or rank. A confirmed numeric zero
remains distinct from unavailable evidence.

The main result table directly shows breadth plus all three category averages.
The established two-candidate capability table remains available for detailed
coverage and top-value review. The adjacent copy states that breadth is a
saved-base description, not future potential, universal suitability, success
probability, or an action recommendation.

## Cross-layer evidence

- Domain tests cover complete score arithmetic, descending rank, exact source
  compatibility, missing evidence, and safe range rejection.
- Application tests run the aggregate request through the authoritative
  snapshot, ranking, shortlist, filter, and comparison path.
- API tests verify discovery metadata, aggregate role context, breadth total,
  derived score fact, three category averages, and comparison.
- Presentation and rendered-component tests prove no discipline label is
  required and every candidate row exposes the direct summary without a second
  client-side ordering path.
- The guarded companion integration scenario now repeats all three objectives
  and checks the same before/after source fingerprints when a local save is
  explicitly configured.

## Responsive browser review

The bilingual synthetic fixtures contain no real save identity or value:

- [desktop interaction fixture](./fixtures/E6-014-comprehensive-capability.html);
- [390-pixel wrapper](./fixtures/E6-014-comprehensive-capability-narrow.html).

The desktop review confirmed three objective radios, automatic removal and
restoration of the discipline control, direct row summaries, and no horizontal
overflow at a 1,280-pixel viewport. The 390-pixel review used an independent
375-pixel iframe content viewport after browser chrome: document client,
scroll, and body widths were all `375`; mobile labels were visible and all
three averages remained readable in the candidate card.

## Automated verification

- Release solution build: zero warnings and zero errors.
- Default Release matrix: `1,269` total, `1,257` passed, `12` guarded local
  scenarios skipped because no integration save was configured, zero failed.
- Focused aggregate Domain, Application, API, rendering, and architecture
  coverage passed.
- `dotnet format --verify-no-changes --no-restore` passed.

The previously recorded E6-013 statement remains true for martial and
life-skill objectives: their separate capability overview cannot change their
score or order. E6-014 deliberately uses that complete breadth only when the
player selects the comprehensive objective itself.
