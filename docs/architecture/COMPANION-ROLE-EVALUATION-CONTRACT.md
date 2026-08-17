# Companion role evaluation and shortlist contract

| Field | Value |
|---|---|
| Status | Accepted — Domain rules implemented; shortlist planned |
| Epic | [EPIC-006](../roadmap/epic-006/EPIC.md) |
| Backlog item | [E6-001](../roadmap/epic-006/BACKLOG.md#e6-001--define-role-evaluation-shortlist-and-ui-semantics) |
| Evidence boundary | [Companion-candidate sources](./COMPANION-CANDIDATE-SOURCES.md) |
| Supported GameData | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` |

## Purpose

Define the exact meaning of an Epic 6 role, candidate state, hard requirement,
score component, rank, tie, shortlist, filter, comparison, evidence indicator,
and result lifecycle before Domain or public API contracts are implemented.

The contract deliberately starts small. Each delivered role compares one exact
saved base-qualification value for one player-selected discipline. It does not
estimate current modified attainment, future development, battle contribution,
teaching, recruitment, settlement productivity, or universal character value.

## Result boundary

One complete finder result is identified by:

- save SHA-256 and captured UTC time;
- exact GameData version;
- candidate-profile mapping version;
- role-definition identity and version;
- selected stable discipline identity;
- evaluation-rule and fingerprint-schema versions; and
- the unfiltered canonical candidate evaluations.

Language, viewport, name query, status filter, comparison selection, expanded
details, and focus state are Presentation concerns. They never change result
identity, score facts, rank, ties, or canonical ordering.

## Initial role catalogue

Both first definitions use role version `1`, candidate-profile mapping version
`1`, and evaluation-rule version `1` for the exact supported GameData version.
A different installed version produces an unsupported role result until a new
evidence gate approves an explicit mapping.

| Stable role identity | Purpose | Discipline identity | Authoritative score fact |
|---|---|---|---|
| `MARTIAL_DISCIPLINE_APTITUDE` | Compare current companions by saved base aptitude in one martial discipline | One installed combat-discipline type in the verified range `0..13` | `BASE_MARTIAL_QUALIFICATION` |
| `LIFE_SKILL_DISCIPLINE_APTITUDE` | Compare current companions by saved base aptitude in one life-skill discipline | One installed life-skill type in the verified range `0..15` | `BASE_LIFE_SKILL_QUALIFICATION` |

Stable discipline identity is its domain plus exact installed type value. The
Domain identity is not a localized name. The UI resolves the current installed
English or Traditional Chinese discipline name and never prints a raw type
value as player-visible identity.

## Candidate and eligibility states

Candidate-universe evaluation occurs before role evaluation:

| State | Exact meaning | Role score permitted? |
|---|---|---:|
| `Eligible` | Non-Taiwu roster member has a current character object, agreeing Domain and character group membership, and confirmed living state | Yes, if role requirements pass |
| `Ineligible` | Sufficient verified evidence proves a hard universe rule fails, such as confirmed non-living state | No |
| `Incomplete` | A roster ID exists but a required saved fact or current object is missing | No |
| `Unsupported` | Installed source or mapping version cannot evaluate a required universe fact | No |
| `Conflicting` | Roster, Domain membership, character membership, object, or living-state evidence disagrees without a safe precedence decision | No |

Characters outside the saved group roster are outside the first candidate
universe. They are not emitted as thousands of `Ineligible` target-lookup
entries. The Taiwu player is an explicit universe exclusion, not a candidate
with a poor score.

## Role hard requirements

The evaluator applies these ordered gates and stops before scoring on the first
non-passing state while retaining all already-known evidence and diagnostics:

1. candidate state is `Eligible`;
2. role identity and version are known;
3. installed GameData, profile mapping, and evaluation versions exactly match
   the role definition;
4. the selected discipline belongs to the role's verified domain and range;
5. the candidate profile contains exactly one confirmed saved base-
   qualification fact for that discipline; and
6. the fact provenance matches the same save and supported source version as
   the profile.

Gate outcomes are `Passed`, `Failed`, `Incomplete`, `Unsupported`, or
`Conflicting`. Only `Failed` with sufficient contrary evidence may create an
ineligible role result. Missing, unavailable, or conflicting evidence never
becomes a failed requirement.

## Score semantics

### One transparent component

Each first role has exactly one component:

| Property | Rule |
|---|---|
| Stable component identity | Role-specific base-qualification field identity |
| Raw value | Exact saved `Int16` value for the selected discipline |
| Unit | `BASE_QUALIFICATION_POINT` |
| Direction | Higher exact value ranks before lower exact value |
| Normalization | Identity: normalized value equals raw value |
| Weight | `1` |
| Contribution | normalized value multiplied by `1` |
| Total score | the single contribution |
| Missing behavior | No component and no total; evaluation is unranked |

Zero, if supplied as a confirmed saved value by a future representative, is a
real value rather than missing evidence. Missing is carried by evidence state,
not by a numeric sentinel. The evaluator does not invent percentages,
population bands, `High`/`Low` labels, bonuses, penalties, or cross-discipline
normalization.

The score is role-local. A martial value cannot be compared with a life-skill
value, and two different selected disciplines do not form one leaderboard.

### Prohibited score claims

The total is not:

- current modified qualification or current attainment;
- chance of success, win probability, efficiency, or production output;
- teaching, recruitment, inheritance, or growth potential;
- a prediction of future skill acquisition;
- universal companion quality; or
- comparable across role or discipline identities.

API documentation and visible UI copy must state this limitation adjacent to
score details.

## Evaluation states

| State | Requirements | Score | Shortlist placement |
|---|---|---|---|
| `Ranked` | Candidate and every role gate pass | Required | Ranked list |
| `Tied` | Same as `Ranked`, with another candidate having the same total | Required | Shared rank group |
| `Ineligible` | Verified universe or role hard requirement fails | Forbidden | Separate unranked section |
| `Incomplete` | Required candidate or score evidence is missing | Forbidden | Separate needs-review section |
| `Unsupported` | Required source, role, discipline, or mapping version is unsupported | Forbidden | Separate needs-review section |
| `Conflicting` | Required evidence conflicts | Forbidden | Separate needs-review section |

An evaluation retains the candidate state, every hard-gate outcome, score
component when available, evidence references, stable unavailable or conflict
reason, diagnostics, role identity, discipline identity, versions, and
deterministic fingerprint.

## Ranking and tie rules

Rankable evaluations are ordered by descending total score. Equal totals form
one explicit tie group. Competition ranking is used:

```text
scores 90, 90, 75 -> ranks 1, 1, 3
```

Candidate stable ID ascending provides deterministic canonical order only
inside an equal-score tie group. It never breaks the tie, changes the shared
rank, or appears as a merit explanation. Localized name, location, source
enumeration order, request order, and UI language never affect canonical
ranking.

The first release returns every rankable current-group candidate rather than
silently truncating to a top-N list. A later bounded display may collapse lower
rows visually only if every row remains reachable and total counts stay
visible.

## Relative strengths, weaknesses, and tradeoffs

The first roles do not assign subjective `Strong` or `Weak` bands. A two-
candidate comparison may state only exact relative outcomes for the selected
role:

- `Advantage`: candidate has a higher confirmed base qualification;
- `Disadvantage`: candidate has a lower confirmed base qualification;
- `Equal`: confirmed values are equal;
- `Unavailable`: at least one required value is incomplete or unsupported; or
- `Conflicting`: at least one required value conflicts.

The exact values and evidence states remain visible. Supporting learned or
equipped skill counts, features, age, and location may appear as neutral facts
but cannot be labelled strengths, weaknesses, or score explanations in the
first role definitions.

A material tradeoff exists only when comparing separate finder results for
different roles or disciplines. The first UI does not combine those results or
claim that one candidate is balanced. The player may run another role request
and review it separately.

## Shortlist contract

One immutable shortlist contains:

- complete result identity and snapshot freshness;
- selected role and discipline identity;
- canonical ranked/tied evaluations;
- separate ineligible, incomplete, unsupported, and conflicting evaluations;
- counts for every state and total considered candidates;
- score limitation and evidence-completeness diagnostics; and
- deterministic shortlist fingerprint.

There is no hidden candidate, alternate evaluation policy, recommendation
style, or secondary score. A candidate cannot appear in more than one state
collection.

## Filter semantics

Filters create a view over one immutable shortlist. They never rebuild a
profile or evaluation.

The first visible status filter has these mutually exclusive values:

| Filter | Visible evaluations |
|---|---|
| `All` | Every emitted ranked and unranked evaluation |
| `Ranked` | `Ranked` and `Tied` |
| `NeedsReview` | `Incomplete`, `Unsupported`, and `Conflicting` |
| `Ineligible` | `Ineligible` only |

An optional localized-name query may further narrow the visible view after
status filtering. Name matching affects visibility only; it does not resolve
identity, change ordering, or become evidence. Blank query means no name
filter. The UI always exposes unfiltered state counts and the active filtered
count.

No first-release control changes sorting, weights, thresholds, normalization,
or tie breaking.

## Candidate comparison contract

The player may select exactly two evaluations from the same immutable finder
result. They therefore share save, GameData, role, discipline, and rule
identity.

The comparison contains:

- candidate display identities resolved separately from stable IDs;
- candidate and evaluation state;
- each ordered hard requirement and its outcome;
- exact base qualification and evidence state;
- score component, total, rank, and tie group when available;
- the exact relative outcome listed above;
- neutral supporting facts and unavailable reasons; and
- diagnostics that materially affect comparability.

Comparing ranked with unranked evidence is permitted for review, but the model
must say `Unavailable` or `Conflicting` rather than invent a score difference.
Selecting a comparison never reruns or changes the shortlist.

## Evidence presentation contract

Internal models retain stable field, source, rule, evidence, reason, and
diagnostic codes. Presentation maps them to localized, player-facing summaries.

Critical eligibility, hard-gate, missing-score, conflict, and unsupported-
version evidence is visible in the primary result. Supporting provenance may
use a native disclosure. Raw IDs, filesystem paths, hashes, exception text,
`SpecialEffectDomain`, and stable diagnostic codes never appear in player-
visible copy.

Evidence confidence describes completeness and compatibility only. It is not a
statistical confidence score.

## Loading, replacement, and failure

Finder requests replace results atomically:

1. selecting a role or discipline changes helper-owned draft input only;
2. explicit `Find candidates` starts one complete request;
3. the active result becomes busy and cannot be mixed with new facts;
4. success replaces candidate profiles, evaluations, shortlist, and comparison
   atomically; and
5. failure exposes a typed safe state without presenting the previous result as
   current.

If the configured save changes during reading, the result is discarded and the
UI asks the player to retry after the save is stable. Language, status filter,
name query, comparison selection, disclosure, or viewport change never rereads
the save.

## Interaction and responsive contract

The dedicated `/companions` page follows
[UI-006](../roadmap/epic-006/UI-006-companion-candidate-finder.md).

At a finder-container width of 960 CSS pixels or more, ranked results use a
semantic table or aligned list and the two-candidate comparison may use paired
columns. Below 960 pixels, each candidate becomes a heading-led card and
comparison facts stack candidate A then candidate B under each shared fact.
Responsive changes preserve all facts, order, state labels, counts, and
evidence.

The DOM and keyboard order is:

1. page heading and information-only notice;
2. role and discipline controls;
3. find/retry action and result status;
4. unfiltered summary counts;
5. status and name filters;
6. ranked evaluations in canonical order;
7. needs-review and ineligible sections;
8. comparison selection and comparison facts; and
9. evidence/limitation details.

Native controls retain expected keyboard behavior. Focus remains on role,
discipline, and filter controls after local changes. A user-initiated finder
success moves focus to the result heading; failure moves focus to the error
summary. A polite live region announces result counts, active filter counts,
and comparison readiness. No interaction requires hover, drag, or color.

## Safety boundary

The contract has no candidate recruitment, dismissal, training, movement,
equipment, party, dialogue, travel, settlement, save-writing, process,
screenshot, upload, persistence, export, automation, or input-control action.

The UI uses `Find`, `compare`, `review`, and `information only` language. It
never uses `apply`, `recruit now`, `assign`, `train`, or another verb implying
that TaiWu Helper controls the game.
