# Village workforce evaluation and comparison contract

| Field | Value |
|---|---|
| Status | Accepted — product and interaction semantics defined |
| Epic | [EPIC-007](../roadmap/epic-007/EPIC.md) |
| Backlog item | [E7-001](../roadmap/epic-007/BACKLOG.md#e7-001--define-workforce-evaluation-comparison-and-ui-semantics) |
| Evidence boundary | [E7-000 village-workforce evidence](../scenarios/E7-000-village-workforce-evidence.md) |
| Supported GameData | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` |

## Purpose

Define the exact version-1 meaning of a work candidate, shop target, manager
slot, current assignment, proposed assignment, hard requirement, score
component, evaluation state, rank, tie, comparison, filter, and manual plan
before Domain or public API contracts are implemented.

The contract compares one verified saved input. It does not estimate current
modified attainment, manager efficiency, revenue, production, capacity,
success probability, future growth, recruitment value, or universal character
quality.

## Result identity and lifecycle

One immutable workforce result is identified by:

- save SHA-256 and capture time;
- exact GameData and source-mapping versions;
- objective identity and rule version;
- assignment-target identity;
- candidate-universe identity and source parameters;
- evaluation and fingerprint-schema versions; and
- the complete unfiltered canonical evaluations.

Language, viewport, status filter, localized-name query, comparison selection,
expanded disclosures, manual-plan visibility, and focus are Presentation state.
They never change target facts, eligibility, components, ranks, ties, ordering,
or result identity.

A new explicit inspect request replaces the result atomically. A changed save
revision, unsupported source version, or read failure produces a typed safe
state and cannot leave an older result labelled current.

## Version-1 identities

| Contract concept | Stable identity | Exact meaning |
|---|---|---|
| Objective | `SHOP_MANAGER_BASE_LIFE_SKILL_QUALIFICATION` | Compare candidates by the shop-required saved base life-skill qualification |
| Target kind | `SHOP_MANAGER_SLOT` | One occupied saved manager position in one existing supported shop building |
| Candidate universe | `TAIWU_WORK_CANDIDATES_V1` | Distinct positive IDs returned by `GetVillagersForWork(true, false)` |
| Requirement | `SUPPORTED_SOURCE_VERSION` | Installed source and mapping versions exactly match the definition |
| Requirement | `SUPPORTED_SHOP_TARGET` | Existing Taiwu-area building is a typed shop with a valid required life-skill discipline |
| Requirement | `ALTERNATIVE_WORK_CANDIDATE` | Proposed worker belongs to the selected work-candidate result |
| Requirement | `CHARACTER_PROFILE_AVAILABLE` | Candidate character and required saved profile fact are present |
| Requirement | `QUALIFICATION_PROVENANCE_MATCH` | Qualification belongs to the same save and source version as the target |
| Component | `REQUIRED_BASE_LIFE_SKILL_QUALIFICATION` | Exact saved base qualification at the target's required discipline index |
| Unit | `BASE_QUALIFICATION_POINT` | One exact saved base qualification point |
| Assignment origin | `CURRENT_SAVE` | Current manager at this target and slot came from the snapshot |
| Assignment origin | `PROPOSED_HELPER` | Alternative selection exists only in the immutable helper result/session |

Stable target identity contains `AreaId`, `BlockId`,
`BuildingBlockIndex`, and `ManagerSlotIndex`. Stable worker identity is the
saved character ID. These values remain internal. Localized names and ordinal
slot labels are display only.

The rule, source mapping, objective, target kind, candidate universe, and
fingerprint schema all begin at version `1`. A different installed GameData
version is unsupported until another evidence gate publishes a new mapping.

## Candidate and assignment states

Candidate-universe state is decided before scoring:

| State | Exact meaning | May be proposed? | May be ranked? |
|---|---|---:|---:|
| `Eligible` | Worker appears in the selected work-candidate result and all required facts agree | Yes | Yes, after requirements pass |
| `CurrentOnly` | Worker occupies the saved slot but is outside the alternative universe | No | No; preserve current evidence separately |
| `Ineligible` | Verified evidence proves an ordered hard requirement fails | No | No |
| `Incomplete` | Required target, worker, assignment, or qualification fact is missing | No | No |
| `Unsupported` | Source, version, mapping, discipline, or runtime path is unsupported | No | No |
| `Conflicting` | Required sources disagree with no safe precedence decision | No | No |

People outside both the candidate result and current assignment are outside
the result rather than emitted as ineligible rows. The broad availability
diagnostic, Taiwu group, target lookup, location, localized name, and general
character enumeration never add a candidate.

Current assignment and proposal are separate immutable contracts:

| Concern | Current assignment | Proposed assignment |
|---|---|---|
| Owner | Configured save snapshot | Helper result/session only |
| Origin | `CURRENT_SAVE` | `PROPOSED_HELPER` |
| Presence | Assigned, incomplete, unsupported, or conflicting | Present only after an eligible alternative is selected |
| Lifetime | Entire snapshot/result | Current result and selection only |
| Persistence | Never helper-written | Never persisted |
| Game effect | Factual read only | None; information only |

Selecting, comparing, clearing, filtering, changing language, or closing the
page cannot mutate the current assignment or mark a proposal complete.

## Ordered hard requirements

The evaluator records every ordered gate needed to explain the outcome and
stops before scoring when a required gate cannot pass:

1. source and rule versions are supported;
2. target identity exists in the same snapshot;
3. target is a typed shop with a required life-skill discipline in the verified
   `0..15` range;
4. manager-slot index identifies an occupied positive character entry in the
   target snapshot;
5. proposed worker is an `Eligible` member of the selected candidate universe;
6. character profile and fixed 16-entry base life-skill buffer are available;
7. the exact required-discipline value is confirmed; and
8. target, worker, value, and rules share the same save and source provenance.

Gate outcomes are `Passed`, `Failed`, `Incomplete`, `Unsupported`, or
`Conflicting`. Only verified contrary evidence is `Failed`. Missing evidence
is never converted to failure, false, or a numeric zero.

For the factual current assignment, gate 5 is descriptive rather than
rewritten: a current worker outside the universe becomes `CurrentOnly`. Its
saved assignment and readable qualification may be shown, but it receives no
alternative rank and cannot become a proposal.

## Component and result semantics

Version 1 has exactly one numeric component:

| Property | Rule |
|---|---|
| Stable component | `REQUIRED_BASE_LIFE_SKILL_QUALIFICATION` |
| Raw value | Exact saved `Int16` base life-skill qualification for the target-required discipline |
| Unit | `BASE_QUALIFICATION_POINT` |
| Direction | Higher exact value orders before lower exact value |
| Normalization | Identity; normalized value equals raw value |
| Weight | `1` |
| Total | The single component contribution |
| Missing behavior | No component and no total; retain exact unranked state |

A confirmed zero is a real value if a future supported save supplies it.
Missing is represented by evidence state, not a sentinel. The result does not
add bands, percentages, bonuses, penalties, averages, hidden tie breakers, or
the Epic 6 comprehensive capability score.

The numeric heading is **Saved base life-skill qualification**. The required
discipline is always named. The result applies only to the selected shop and
manager slot under rule version 1.

It is explicitly not:

- current modified qualification or attainment;
- manager efficiency, revenue, production, progress, capacity, or probability;
- vacancy, work status, personality, feature, dependency, or resource proof;
- a construction, development, farming, collection, or villager-role score;
- recruitment, teaching, training, inheritance, or future potential; or
- universal village or character quality.

These shared limitations appear once in the result summary, not in every
worker row.

## Evaluation, rank, and tie states

| Evaluation state | Requirements | Numeric total | Placement |
|---|---|---:|---|
| `Ranked` | Every gate passes | Required | Ranked alternatives |
| `Tied` | Every gate passes and another worker has the same total | Required | Shared rank group |
| `CurrentOnly` | Factual current worker is outside proposal universe | Optional descriptive value | Current summary and unranked current row |
| `Ineligible` | Verified hard gate fails | Forbidden | Separate ineligible group |
| `Incomplete` | Required fact is missing | Forbidden | Needs-review group |
| `Unsupported` | Required source/rule/value is unsupported | Forbidden | Needs-review group |
| `Conflicting` | Required evidence conflicts | Forbidden | Needs-review group |

Rankable alternatives sort by descending exact total. Equal totals use
competition ranking:

```text
90, 90, 75 -> ranks 1, 1, 3
```

Stable worker ID ascending orders rendering only inside a tie. It never breaks
the tie or appears as merit. Localized name, current-assignment status, source
enumeration order, filters, language, and viewport never alter canonical rank.

The current worker may also appear in the canonical list when it is `Eligible`.
The current-assignment summary establishes saved state; the list establishes
relative position. Details and limitations are not duplicated between them.

## Relative comparison

Exactly two evaluations from the same immutable result may be selected. Their
relative outcome is:

| Outcome | Meaning |
|---|---|
| `Higher` | First worker has higher confirmed target-required base qualification |
| `Lower` | First worker has lower confirmed target-required base qualification |
| `Equal` | Both confirmed values are equal |
| `Unavailable` | At least one required value or eligibility state is incomplete or unsupported |
| `Conflicting` | At least one required fact conflicts |

The comparison shows target and discipline once, then each worker's assignment
origin, candidate/evaluation state, hard gates, exact component, rank, and
unavailable reason. It never turns an exact difference into a percentage or a
claim that changing the assignment improves production.

## Filters and immutable facts

Filters are views over one immutable result:

| Filter | Visible evaluations |
|---|---|
| `All` | Every emitted ranked and unranked evaluation |
| `Comparable` | `Ranked` and `Tied` |
| `NeedsReview` | `Incomplete`, `Unsupported`, and `Conflicting` |
| `Ineligible` | `Ineligible` only |

A localized-name query applies after the status filter. It changes visibility
only. It never expands the universe, resolves identity, rereads the save,
changes counts, reranks workers, or clears hidden comparison selections without
an explicit visible explanation.

## Presentation and repetition contract

The page follows
[UI-007](../roadmap/epic-007/UI-007-village-workforce-planner.md). Its DOM order
is:

1. page heading and information-only notice;
2. fixed objective scope, shop target and manager-slot controls;
3. explicit inspect action and request state;
4. result identity, freshness, exact unit, and shared limitations;
5. current assignment summary;
6. immutable counts, filters, and name query;
7. ranked/tied alternatives, then needs-review and ineligible groups;
8. optional two-worker comparison;
9. static manual guidance; and
10. evidence and deferred-mechanics disclosure.

Shared source version, objective, target, unit, formula limitation,
information-only warning, and deferred-mechanics text appear once per result.
A worker row contains only rank/tie, display identity, current marker, exact
qualification or unavailable state, concise worker-specific decisive evidence,
and comparison control. Worker disclosures contain only that worker's gates,
component provenance, and unavailable reason.

At 960 CSS pixels or wider, rows may use a semantic table. Below 960 pixels,
the same DOM becomes heading-led cards. No fact, state, count, limitation,
comparison outcome, or manual guidance disappears at a breakpoint. Native
controls, visible focus, polite status announcements, and non-color state text
are required.

## Bilingual terminology

| Stable concept | English | Traditional Chinese |
|---|---|---|
| Objective | Shop manager base aptitude | 商鋪管理基礎資質 |
| Target kind | Shop manager position | 商鋪管理位置 |
| Component | Saved base life-skill qualification | 存檔基礎技藝資質 |
| Unit | Qualification points | 資質點數 |
| Required discipline | Required life-skill discipline | 所需技藝類別 |
| Current assignment | Current assignment | 目前指派 |
| Alternative worker | Alternative worker | 替代人員 |
| Current only | Current assignment only | 僅屬目前指派 |
| Information only | Information only | 僅供參考 |
| Manual guidance | Manual review guidance | 手動檢查指引 |

Dynamic target, discipline, and worker display names use typed localization
lookups. Raw IDs, hashes, paths, source keys, exception text, and diagnostic
codes never appear in player-visible copy.

## Manual guidance and safety

Manual guidance is a semantic list, not interactive checkboxes and not stored
completion state. When one current assignment and one eligible alternative can
be reviewed, it says to:

1. confirm the shop and manager position in the game;
2. confirm in the game that reassignment is currently permitted;
3. review the exact saved base qualification and every unresolved limitation;
4. remember that no efficiency or output improvement was calculated; and
5. make any desired change manually in the game.

The final visible statement says no action was sent to the game.

This contract exposes no assignment, building, upgrade, demolition,
collection, recruitment, training, movement, equipment, dialogue, process,
memory, screenshot, upload, export, persistence, automation, input-control, or
save-write capability.
