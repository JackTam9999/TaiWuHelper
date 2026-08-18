# UI-007: Village workforce and building assignment planner

| Field | Value |
|---|---|
| Status | Delivered — final visual confirmation scheduled in E7-011 |
| Epic | [EPIC-007](./EPIC.md) |
| Backlog item | [E7-001](./BACKLOG.md#e7-001--define-workforce-evaluation-comparison-and-ui-semantics) |
| Route | `/village-workforce` |
| Last updated | 2026-08-18 |

## Purpose

Define a compact bilingual decision surface for comparing the current worker
with verified eligible alternatives for one selected settlement assignment.
The page is an information-only planner. It does not assign workers, change
buildings, collect resources, recruit characters, or control the game.

The exact state, score, tie, comparison, and lifecycle rules are defined by the
[village workforce evaluation contract](../../architecture/VILLAGE-WORKFORCE-EVALUATION-CONTRACT.md).

E7-000 selected a shop manager slot as the first assignment target and the
shop-required saved base life-skill qualification as its only ordering
component. `Work objective` remains a scope label, not a productivity claim.
The result must say `Saved base qualification`, never efficiency, output,
percentage, or predicted production. E7-001 finalizes the typed states and
bilingual wording for this boundary.

## Design principles

1. Show the fixed objective, then ask for the shop and manager position before
   evaluating workers.
2. Show the current assignment and concise result summary before alternatives.
3. Put shared scope, component, and evidence limitations once above the result.
4. Keep worker-specific gates and components in collapsed disclosures.
5. Never style one person as universally best or as a recruitable companion.
6. Preserve exact unavailable, unsupported, conflict, and tie states.
7. Show descriptive attributes and aptitudes separately from verified work
   components.
8. Use one DOM whose CSS reflows tables into labelled cards without fact loss.
9. Keep every proposed change visibly manual and outside the game.

## Information architecture

The page contains these regions in DOM and keyboard order:

1. skip link and page heading;
2. information-only and source-boundary notices;
3. fixed objective scope and shop/manager-position controls;
4. inspect/retry action;
5. result heading, snapshot freshness, and shared limitations;
6. current-assignment summary;
7. stable counts and display filters;
8. ranked/tied alternatives;
9. incomplete, unsupported, conflicting, and ineligible workers;
10. optional two-worker comparison;
11. manual reassignment checklist; and
12. evidence, scope, and deferred-mechanics disclosures.

The main result never repeats the same component rule, evidence disclaimer, or
information-only warning inside each worker row.

## Initial load

Before a supported target is selected, the page shows:

- the page purpose;
- the exact supported work-candidate boundary selected by E7-000;
- a statement that aptitude alone does not prove productivity;
- the fixed `Shop manager base aptitude` objective scope;
- a target control populated only from the current immutable target catalogue
  or settlement snapshot boundary;
- a disabled inspect action until required controls are valid; and
- no stale or synthetic worker result.

Selecting controls does not read or write the save. The explicit inspect action
starts one coherent request.

## Wide-screen wireframe

Synthetic labels illustrate structure only.

```text
┌ Village workforce planner · Information only ─────────────────────────────┐
│ Compare one current assignment with verified alternatives.                │
│ The helper will not assign a worker or change the game.                    │
├ Objective ──────────────────────────────────────────────────────────────────┤
│ Objective: Shop manager base aptitude                                      │
│ Shop [Synthetic shop ▼]  Manager position [Position 1 ▼] [Inspect position]│
├ Result · Synthetic target ──────────────────────────────────────────────────┤
│ Snapshot: current · Rules: verified version · Evidence: complete            │
│ Shared limitation: this result applies only to the selected assignment.     │
├ Current assignment ─────────────────────────────────────────────────────────┤
│ Synthetic Worker A · Confirmed current worker                               │
│ Saved base life-skill qualification: 64 points                              │
├ Alternatives ───────────────────────────────────────────────────────────────┤
│ Total 4 · Comparable 2 · Needs review 1 · Ineligible 1                       │
│ Show (●) All (○) Comparable (○) Needs review (○) Ineligible  Name [       ] │
├ Rank │ Worker             │ Base qualification │ State  │ Compare             ┤
│ 1    │ Synthetic Worker B │ 72 points          │ Ranked │ [ ]                │
│ 2    │ Synthetic Worker A │ 64 points          │ Current│ [x]                │
│ —    │ Synthetic Worker C │ Unavailable │ Incomplete  │ [ ]                 │
├ Comparison ─────────────────────────────────────────────────────────────────┤
│ Fact                     │ Worker A (current) │ Worker B (alternative)      │
│ Eligibility              │ Eligible           │ Eligible                    │
│ Required life skill      │ Synthetic skill    │ Synthetic skill             │
│ Saved base qualification │ 64 points          │ 72 points                   │
│ Relative result          │ Lower               │ Higher                      │
├ Manual checklist ───────────────────────────────────────────────────────────┤
│ • Confirm the target and current worker in the game.                        │
│ • Review the verified requirements and unresolved cautions.                │
│ • If desired, make the change manually in the game.                         │
└ No action is sent to the game. ─────────────────────────────────────────────┘
```

## Narrow-screen wireframe — Traditional Chinese

Below 960 CSS pixels, the same facts use heading-led cards.

```text
┌ 村莊人力規劃 · 僅供參考 ───────────────────┐
│ 比較目前指派與有證據支持的替代人選。          │
│ 太吾助手不會指派人員或改變遊戲。              │
├ 目標：商鋪管理基礎資質                        │
│ 商鋪 [範例商鋪 ▼] 管理位置 [位置 1 ▼]        │
│ [檢查位置]                                    │
├ 目前指派                                      │
│ 範例人員甲 · 已確認目前人員                   │
│ 存檔基礎技藝資質：64 點                       │
├ 第 1 名                                        │
│ 範例人員乙                                    │
│ 資質：72 點 · 狀態：已排序                   │
│ [選取作比較] [查看證據]                       │
├ 比較：範例人員甲／範例人員乙                 │
│ 目前人員：64 點                               │
│ 替代人員：72 點                               │
│ 相對結果：替代人員較高                        │
├ 手動檢查清單                                  │
│ • 在遊戲中確認位置與目前人員。                │
│ • 檢查需求與未解決注意事項。                  │
│ • 如有需要，請自行在遊戲中調整。              │
└ 不會向遊戲傳送任何操作。 ─────────────────────┘
```

## Objective and target controls

The single objective renders as a named scope summary rather than a fake
selector. Shop and manager-position controls use stable target identity
internally and only localized display text and position ordinals visibly.
Numeric IDs and raw source keys are never printed.

If only one current target or position is available, it is displayed as a
confirmed value. Controls must reflect actual cardinality; the UI does not
imply options that sources cannot supply.

Changing the draft objective or target leaves the prior result visibly marked
`Previous result` until the player explicitly inspects the new draft. A draft
change does not silently reuse or relabel the old result.

## Result summary and repetition control

The result heading owns information shared by every worker:

- selected objective and target;
- snapshot freshness and source state;
- rule identity/version in friendly form;
- exact saved-base qualification meaning and unit;
- evidence-completeness limitation;
- information-only/manual-action statement; and
- total, comparable, needs-review, and ineligible counts.

Worker rows do not repeat this content. Each row shows only rank/tie, localized
name, current-worker marker, exact work-local result or unavailable state,
evaluation state, concise decisive evidence, and the comparison control.

Detailed requirements, components, provenance, strengths, and limitations live
in one native disclosure per worker. Its collapsed summary reports an exact
passed/total gate count and component-coverage count.

## Current assignment

The current-assignment region always precedes alternatives and shows:

- target identity;
- current worker or explicit incomplete/unavailable state;
- source and freshness state;
- eligibility/evaluation state under the selected objective;
- exact result when supported; and
- a concise warning when current assignment evidence is incomplete or
  conflicting.

`Current` is a factual marker, not a merit badge. The current worker may rank
first, lower, tied, unranked, or ineligible under the selected rules.

## Worker list semantics

At 960 CSS pixels or wider, comparable workers use a semantic table with
column headers and the worker name as the row header. Below 960 pixels, the
same rows become articles or list items with a heading and description list.

Canonical order is:

1. ranked and tied comparable workers;
2. incomplete workers;
3. unsupported workers;
4. conflicting workers; and
5. confirmed ineligible workers.

The current worker appears once in the dedicated current-assignment summary.
The worker list contains alternatives only, avoiding a repeated current row.
Selecting an alternative for comparison pairs it with the current worker by
default, while the immutable result still retains the current evaluation.

No row receives a winner crown, best-person badge, percentage bar, universal
grade, recruitable-companion label, or green/red worth indicator.

## Qualification-result presentation

Every numeric result is headed `Saved base life-skill qualification` with the
target-required discipline and `qualification points` unit. It is never shown
as a percentage, current attainment, efficiency, output, or predicted
production.

The following message remains adjacent to the result heading:

> This result applies only to the selected assignment and verified rule
> version. It is not universal character quality, future potential, or a game
> action.

Traditional Chinese:

> 此結果只適用於所選指派與已驗證規則版本，並非人物的整體價值、未來潛力或遊戲操作。

When the result is unavailable, the UI shows the exact state and friendly
reason. It never renders zero, a blank cell, or an estimated value.

## Descriptive capability context

When supported worker facts are available, the current summary, alternative
rows, and comparison expose three compact averages with explicit coverage:

- six base attributes;
- martial-discipline aptitudes; and
- life-skill-discipline aptitudes.

This context is descriptive. It cannot affect work ordering unless the selected
versioned rule names one exact field. It does not reuse Epic 6's comprehensive
breadth index as a settlement score. Values unrelated to the objective remain
visually separated from verified work components.

## Tie presentation

Equal exact results share one competition rank. Every tied row/card includes:

- the same numeric rank;
- visible `Tied` / `並列` text;
- an accessible label containing the shared rank; and
- no visual emphasis suggesting that stable display order broke the tie.

Stable worker identity orders rendering inside a tie only and is not printed.

## Filters and name query

The status filter is one native radio or pressed-button group in this order:
All, Comparable, Needs review, Ineligible. `Comparable` is the initial filter
and shows at most the top ten alternatives. `Needs review` groups incomplete,
unsupported, and conflicting workers while preserving exact subheadings.

An explicit action expands the comparable set. Any expanded or other filtered
set renders in pages of at most 25 rows, so representative saves do not create
hundreds of table rows at once.

The optional name query filters localized display names only after status
filtering. It never expands the candidate universe or rereads the save.

Filter changes retain focus, preserve immutable counts and comparison
selections, and announce visible/total counts through an `aria-live="polite"`
region.

## Comparison interaction

At most two workers may be selected. Selecting an alternative first
automatically pairs it with the current worker. Selecting the current worker
explicitly remains supported by presentation state even though its repeated
candidate row is omitted.

After two selections:

- remaining unchecked controls are disabled with explanatory text;
- comparison appears after all worker-state groups;
- a polite announcement says comparison is ready; and
- clearing comparison returns focus to the first previous selection when it
  remains visible.

Comparison shows eligibility, hard gates, exact verified components, work-local
result and unit, evidence state, current/proposed marker, and exact relative
outcome. Missing or conflicting values produce an unavailable outcome rather
than a numeric difference.

## Manual reassignment checklist

The checklist appears only when the result can describe a current assignment
and one selected alternative. It is a semantic list of static information,
not checkboxes or interactive completion tracking. Items include:

- confirm the target and current assignment in the game;
- review availability, hard requirements, and whether reassignment is
  currently permitted in the game;
- review resource or dependency cautions supported by evidence;
- review unresolved evidence that could change the decision; and
- make any desired change manually in the game.

The last visible line states that no action was sent to the game. The page has
no `Assign`, `Apply`, `Build`, `Collect`, `Recruit`, or equivalent action.

## Focus and keyboard behavior

Native select, radio, checkbox, button, and disclosure behavior is retained.
Starting a request marks the result busy without moving focus. Success moves
focus to the new result heading because the player explicitly requested it;
failure moves focus to the error summary.

Draft control, filter, name, comparison, disclosure, language, and responsive
layout changes retain focus on the equivalent control whenever it still
exists. No custom roving focus is required.

## Visible state model

| State | Required visible content | Recovery or transition |
|---|---|---|
| Evidence/rule unsupported | Exact limitation and no guessed controls | Await a supported source/rule version |
| No valid draft | Scope explanation and disabled inspect action | Select required objective/target |
| Loading | Busy status and no mixed old/new active result | Atomic success or failure |
| Available result | Objective, target, current assignment, shared limitation, counts, worker groups | Filter, compare, or inspect another target |
| No occupied supported target | Empty target catalogue and no fabricated vacancy | Review another stable save revision |
| Missing current assignment | Exact missing/incomplete evidence state | Review alternatives without claiming current state |
| One comparable worker | Stable rank with no superiority claim beyond scope | Review evidence |
| No comparable workers | Zero count plus honest unranked groups | Review reasons or select another target |
| Explicit tie | Shared rank and visible tied text | Compare tied workers |
| Ineligible worker | Failed hard gate and no result | Review only |
| Incomplete worker | Missing required fact and no zero fallback | Refresh only when source may change |
| Unsupported worker/value | Exact source or rule limitation | Use supported source/version |
| Conflicting worker | Conflicting-source summary and no numeric result | Review conflict manually |
| Filtered result | Active filter, visible count, unchanged full counts | Show all/clear query |
| Comparison ready | Two workers from one result and exact relative outcomes | Clear/change selection |
| Manual checklist unavailable | Exact missing prerequisite | Select comparable current/alternative pair |
| Save changed during read | Discarded result and stable-save retry guidance | Retry after save stabilizes |
| Missing configured save | Configuration guidance without raw path | Configure trusted save and retry |
| Read/calculation failure | Safe error summary without old active result | Retry or correct configuration |
| Draft/result mismatch | Inert `Previous result` label | Submit draft or restore controls |

## Bilingual terminology

E7-001 confirms these first-vertical meanings:

| Contract term | English | Traditional Chinese |
|---|---|---|
| page heading | Village workforce planner | 村莊人力規劃 |
| information only | Information only | 僅供參考 |
| work objective | Shop manager base aptitude | 商鋪管理基礎資質 |
| assignment target | Shop manager position | 商鋪管理位置 |
| saved component | Saved base life-skill qualification | 存檔基礎技藝資質 |
| unit | Qualification points | 資質點數 |
| inspect action | Inspect position | 檢查位置 |
| current assignment | Current assignment | 目前指派 |
| current worker | Current worker | 目前人員 |
| alternative worker | Alternative worker | 替代人員 |
| comparable | Comparable | 可比較 |
| ranked | Ranked | 已排序 |
| tied | Tied | 並列 |
| eligible | Eligible | 符合資格 |
| ineligible | Ineligible | 不符合資格 |
| incomplete | Incomplete | 資料不完整 |
| unsupported | Unsupported | 目前不支援 |
| conflicting | Conflicting | 資料衝突 |
| needs review | Needs review | 需檢查 |
| compare | Compare | 比較 |
| manual checklist | Manual checklist | 手動檢查清單 |
| saved capability context | Saved capability context | 存檔能力資料 |
| previous result | Previous result | 上一次結果 |

Localized worker, target, objective, and component names come from typed
resource identities. If display text is unavailable, the UI uses a localized
unavailable label and never prints raw numeric IDs or stable codes.

## Non-color status cues

Every state uses visible text. Optional icons are supplementary:

| State | Optional icon | Required text |
|---|---:|---|
| Current | `●` | `Current worker` |
| Ranked | `#` | Numeric rank and `Ranked` |
| Tied | `=` | Shared rank and `Tied` |
| Ineligible | `×` | `Ineligible` plus gate reason |
| Incomplete | `?` | `Incomplete` plus missing fact |
| Unsupported | `!` | `Unsupported` plus limitation |
| Conflicting | `⇄` | `Conflicting` plus source summary |

Color never carries eligibility, current state, merit, tie, or evidence meaning
by itself.

## Responsive behavior

The page uses a 960 CSS-pixel content-container boundary:

- at or above 960 pixels, result and comparison tables may align columns;
- below 960 pixels, worker and comparison facts use labelled cards;
- at 620 pixels, controls use one column and full-width actions; and
- names, reasons, and localized labels wrap with `overflow-wrap: anywhere`.

No breakpoint removes counts, limitations, status labels, current-assignment
facts, unavailable reasons, components, evidence, comparison outcomes, or
manual guidance. CSS reflow never changes DOM order or focus.

## Session lifecycle and safety

Draft objective/target, filters, name query, comparison selections, and open
disclosures are helper-owned session state only. None is written to disk or
helper storage. Language and layout changes do not reread the save.

The page has no assign, apply, build, upgrade, demolish, collect, recruit,
dismiss, train, move, equip, dialogue, travel, save-write, process, screenshot,
upload, export, automation, or input-control action. It presents information
for the player to review and act on manually in the game.

## Verification expectations

- Mapper tests cover every snapshot, target, assignment, worker, evaluation,
  comparison, manual-plan, and source state.
- Rendering tests cover objective/target controls, loading, current/no-target,
  ranked, tied, needs-review, ineligible, comparison, checklist, previous-result,
  focus, bilingual, and raw-ID-hiding behavior.
- Localization tests require nonblank English and Traditional Chinese for every
  typed Epic 7 key.
- Architecture tests require the dedicated route and navigation entry, one
  explicit source action, one-DOM responsive parity, and absence of persistence,
  mutation, process, upload, input, or game-control paths.
- Browser review covers wide English and narrow Traditional Chinese synthetic
  states without proprietary save content.

## Delivered implementation

E7-009 delivers the `/village-workforce` route, bilingual navigation, typed
page text, ordinal target and worker labels, a current-assignment summary,
single-DOM responsive candidate and comparison tables, local filters, a
two-worker comparison, progressive worker evidence, and a static manual
checklist. Source identities remain component state and are never rendered.

The implementation and current verification evidence are recorded in
[E7-009 village-workforce UI verification](../../reviews/E7-009-village-workforce-ui.md).
