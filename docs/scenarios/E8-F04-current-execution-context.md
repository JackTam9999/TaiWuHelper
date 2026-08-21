# E8-F04: Coherent current and proposed execution context

| Field | Value |
|---|---|
| Status | Complete — current, observed and proposed facts remain separate |
| Backlog item | [E8-F04](../roadmap/epic-008/BACKLOG.md#e8-f04--project-a-complete-current-and-proposed-execution-context) |
| Inspection date | 2026-08-21 |
| Runtime GameData | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` |
| Rule fingerprint | `64051C1234CECDFDCE070134FDA0380826154D16C1F171B52B6F7FE1C64ECD5D` |
| Context schema | `TACTICAL_EXECUTION_CONTEXT_V2` |
| Sanitized record | [E8-F04 metadata](./evidence/E8-F04-current-execution-context-metadata.json) |

## Decision

The application now selects the tactical rule set by exact GameData product
version and projects one immutable context from one combat snapshot read. The
current installed version resolves `CurrentLaterMagicSound`; another version
does not receive historical or nearest-version rules.

The context has two distinct fact sets:

- `Current` contains save/current-screen facts plus an optional explicitly
  confirmed newer manual execution observation; and
- `Proposed` contains only the supplied plan, while fixed inner-power and
  legendary-slot mechanics are retained from the same snapshot.

No missing collection becomes an empty collection. Empty weapons, tricks,
styles, resources or assignments mean the caller explicitly supplied an empty
set; a missing fact stays `Unknown` with its origin and evidence identity.

## Fact coverage

Both current and proposed facts now carry:

- equipped and unlocked weapon subtype IDs;
- usable combat-style IDs and typed trick counts;
- current or opening distance;
- stance, breath and typed resources, including defense true Qi;
- active defense and active agility skill IDs;
- exact inner-power mechanics and backlash element;
- category slot budgets and universal-slot allocation;
- legendary-book cost slots and assignments; and
- the complete equipped-skill identity set.

Stance and breath are projected from their typed resource entries, so the
dedicated values and the general resource collection cannot silently disagree.
A supplied partial resource collection remains available as supplied; an
absent or unavailable individual amount stays unknown.

## Evidence precedence and coherence

The precedence boundary is field-level and never unions collections:

1. a newer current-screen `PlayerLoadoutObservation` replaces the complete
   displayed loadout/budget/allocation field;
2. a confirmed newer `TacticalExecutionObservation` replaces only the live
   fields it explicitly contains;
3. unobserved stable fields fall back to the same disk snapshot; and
4. runtime-only fields with no observation remain `Unknown`.

Observation time is checked in the Application boundary and retained only as
diagnostic metadata; it is not part of the pure Domain contract or semantic
fingerprint. A stale observation is rejected. If no save timestamp is
available, the observation must explicitly confirm that it is newer.

An observed active defense or agility must also appear in the current loadout
from the same revision. Otherwise the fact is `Conflicting`, not accepted.
Current and proposed facts never overwrite one another.

## Representative current boundary

The guarded current-save check applies the newer displayed capacities
`6/10/7/9/4`, superseding disk capacities `6/9/6/10/5`. The screen evidence
does not contain used-slot totals, so all five used values remain unavailable.
The current snapshot still supplies equipped weapons, equipped skills,
universal allocation, legendary-book state and inner-power mechanics.

Current tricks/styles, distance, stance, breath, resources, active defense and
active agility were not present in the immutable evidence and remain unknown.
The test does not invent them.

## Representative proposal boundary

The same guarded test supplies a deterministic proposal with weapon subtype
`9`, unlocked subtypes `6` and `9`, opening distance `5`, stance/breath `100`,
defense true Qi `3`, active defense `2`, active agility `134`, the displayed
budgets and the current universal allocation. Empty trick/style collections
are explicit proposal inputs. These values prove proposal transport and
separation; they are not claims about the live battle state.

Alternative budget and universal-slot allocations are also covered by Domain
tests. Direction availability remains a separate learned/breakthrough fact,
and candidate discovery still applies exact direction, effect, execution,
capacity and backlash gates.

## Read-only verification

Focused Domain and Application tests cover complete and partial observations,
manual/save precedence, stale and unconfirmed observations, current/proposed
separation, exact current rule selection, trick/style/resource transport,
active-role conflict detection, alternative allocations, deterministic
fingerprints, cancellation and one snapshot read.

Two guarded local checks cover the generic current-version context and the
exact E8-F04 target/proposal context. Every guarded save, GameData,
configuration and language file retained the same hash, length and timestamp.
No screenshot capture, OCR, save write, runtime effect invocation or game
control was added.

The Release solution build completed with zero warnings or errors. The full
suite passed 1,605 of 1,629 tests with 24 expected guarded-local skips and no
failures.

E8-F04 completes the execution-context boundary. E8-F05 can now search
cross-category packages using these facts, while unresolved live requirements
continue to block unconditional recommendations.
