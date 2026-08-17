# UI-006: Companion role and candidate finder

| Field | Value |
|---|---|
| Status | Accepted design — implemented and verified |
| Epic | [EPIC-006](./EPIC.md) |
| Backlog items | [E6-001](./BACKLOG.md#e6-001--define-role-evaluation-shortlist-and-ui-semantics), [E6-010](./BACKLOG.md#e6-010--deliver-the-bilingual-accessible-companion-finder-ui), [E6-013](./BACKLOG.md#e6-013--add-a-transparent-companion-capability-overview), [E6-014](./BACKLOG.md#e6-014--make-comprehensive-base-capability-a-selectable-objective) |
| Primary surface | Dedicated local Blazor page at `/companions` |
| Last updated | 2026-08-17 |

## Purpose

Provide one information-only page where the player selects a martial aptitude,
life-skill aptitude, or comprehensive base-capability objective; reads one
coherent configured-save snapshot; reviews the deterministic shortlist; filters
visible states; and compares two candidates without turning any role-local
score into universal character quality.

The data and state rules are defined by the
[companion role evaluation contract](../../architecture/COMPANION-ROLE-EVALUATION-CONTRACT.md).
This document fixes visual hierarchy, responsive behavior, bilingual copy,
keyboard order, focus behavior, and user-visible states.

## Navigation and page boundary

The primary navigation adds `Companion finder` after `Skill atlas`. Its route is
`/companions`. The page does not appear inside combat recommendations because
candidate role selection has a different snapshot, objective, and lifecycle.

One persistent notice states that the helper reads the configured save and
cannot recruit, train, move, equip, assign, or otherwise change a companion or
the game.

## Information hierarchy

The page renders in this order:

1. page heading and persistent information-only notice;
2. concise explanation of the current-group-only candidate boundary;
3. role family control;
4. role-specific discipline control when required;
5. explicit `Find candidates` action;
6. active result heading with snapshot freshness and score limitation;
7. unfiltered state counts;
8. status filter and optional localized-name query;
9. ranked/tied candidate list;
10. needs-review and ineligible candidate sections;
11. two-candidate comparison controls, capability overview, and role result;
    and
12. evidence, unsupported-current-value, and scope limitations.

Eligibility, unsupported versions, conflicts, missing score evidence, and the
role-local score warning are primary text. They are never available only by
hover, color, icon, or a collapsed disclosure.

## Role controls

### Role family

Three radio buttons or a single-labelled radio group appear in stable order:

1. `Comprehensive base capability`;
2. `Martial discipline aptitude`; and
3. `Life-skill discipline aptitude`.

`Comprehensive base capability` is selected by default, so the initial page
can search the complete current companion group without requiring a discipline
choice.

Changing role updates the discipline options and clears any draft discipline
that is invalid for the new role. It does not read the save, retain a stale
result as current, or compare scores across roles.

### Discipline

One native labelled select lists every installed verified discipline in stable
type order using localized in-game names. The raw type index never appears.
Martial role exposes 14 verified entries; life-skill role exposes 16.

The comprehensive objective hides this select and uses its fixed internal
aggregate identity. Its find action requires only that objective selection.

For the two discipline objectives, the first option is `Choose a discipline`
and `Find candidates` remains disabled until both role and discipline are
valid. Disabled state is conveyed by native semantics and explanatory text,
not color alone.

### Find action

`Find candidates` starts the only archive-reading action. It is a read-only
query, not a recruitment or recommendation command. During loading, the button
is disabled and labelled with busy state through the surrounding status region.

Changing draft role or discipline after a result exists labels the old result
`Previous result` and makes it inert until a new explicit request succeeds.
The page never shows old results under new draft controls as if they match.

## Synthetic desktop wireframe — English

The example is synthetic and does not represent a real character, save, name,
qualification, or rank.

```text
┌ Companion finder ─ INFORMATION ONLY ─────────────────────────────────────┐
│ Reads the configured save. TaiWu Helper cannot recruit or change anyone. │
│ Boundary: saved group roster excluding Taiwu; evidence sets eligibility. │
├ Role: (●) Martial discipline aptitude  (○) Life-skill discipline aptitude│
│ Discipline: [ Synthetic martial discipline ▼ ]  [ Find candidates ]      │
├ Results — Synthetic martial discipline ─ Snapshot current ───────────────┤
│ Score = saved base qualification for this discipline only.               │
│ Not current attainment, success probability, or universal companion rank.│
│ Considered 4 · Eligible 4 · Ranked 1 · Tied 2 · Needs review 1          │
│ Show: (●) All (○) Ranked (○) Needs review (○) Ineligible  Name: [      ] │
├ Rank │ Candidate          │ Base qualification │ State       │ Compare ┤
│ 1    │ Synthetic Person A │ 90                 │ Ranked      │ [ ]     │
│ 2=   │ Synthetic Person B │ 75                 │ Tied        │ [x]     │
│ 2=   │ Synthetic Person C │ 75                 │ Tied        │ [x]     │
│ —    │ Synthetic Person D │ Unavailable        │ Incomplete  │ [ ]     │
├ Compare — Synthetic Person B and Synthetic Person C ─────────────────────┤
│ Capability overview (descriptive only; does not change role rank)        │
│ Breadth index     │ 48.29                │ 49.26                         │
│ Six attributes    │ 54.50 · 6/6          │ 55.50 · 6/6                  │
│ Martial aptitudes │ 50.86 · 14/14        │ 51.79 · 14/14                │
│ Life aptitudes    │ 39.50 · 16/16        │ 40.50 · 16/16                │
│ Eligibility       │ Eligible            │ Eligible                       │
│ Base qualification│ 75                  │ 75                             │
│ Relative result   │ Equal               │ Equal                          │
│ Evidence          │ Saved base value    │ Saved base value               │
└ Review and make any choice manually in the game. ────────────────────────┘
```

The `2=` notation is supplemented by the visible word `Tied` and an accessible
label such as `Tied at rank 2`. It is never the only tie cue.

## Synthetic narrow-screen wireframe — Traditional Chinese

Below 960 CSS pixels, the same facts use heading-led cards and stacked
comparison rows.

```text
┌ 同道人選比較 ─ 僅供參考 ───────────────────┐
│ 讀取已設定的存檔；太吾助手不會招募或改變人物。 │
│ 人選範圍：目前太吾隊伍中仍在世的同道。          │
├ 目標                                             │
│ (●) 武學資質  (○) 技藝資質                     │
│ 類別：[範例武學類別 ▼]                          │
│ [查找人選]                                       │
├ 結果：範例武學類別                              │
│ 分數只代表此類別的存檔基礎資質。                │
│ 並非目前造詣、成功機率或人物的整體排名。        │
│ 共 4 · 已排序 3 · 需檢查 1 · 不符合資格 0       │
│ 顯示：[全部] [已排序] [需檢查] [不符合資格]     │
├ 第 1 名                                            │
│ 範例人物甲                                       │
│ 基礎資質：90 · 狀態：已排序                     │
│ [選取作比較]                                     │
├ 並列第 2 名                                        │
│ 範例人物乙                                       │
│ 基礎資質：75 · 狀態：並列                       │
│ [已選取作比較]                                   │
├ 比較：範例人物乙／範例人物丙                    │
│ 基礎資質                                         │
│   人物乙：75                                     │
│   人物丙：75                                     │
│ 相對結果：相同                                   │
└ 請自行在遊戲中檢查並作出選擇。 ─────────────────┘
```

Synthetic names and values never enter production fixtures as real save
content.

## Candidate list semantics

At 960 CSS pixels or wider, the ranked section uses a semantic table with:

- `scope="col"` column headers;
- candidate name as the row header;
- visible rank or tie text;
- exact saved base qualification or localized unavailable state;
- ranking state text, with the exact evaluation state retained for comparison;
- concise evidence summary; and
- one labelled comparison-selection checkbox.

At narrower widths, each candidate uses an article or list item with a heading
for the localized character name and a description list for the same facts.
CSS changes layout only; DOM order and fact parity remain stable.

Ranked and tied candidates appear first in canonical order. `Needs review`
groups incomplete, unsupported, and conflicting candidates under separate
state subheadings. `Ineligible` is last. Sections with zero entries show their
count in the summary and may omit an empty body.

`Eligible` is the exact candidate-universe state, not a synonym for ranked or
tied. An eligible candidate whose role evidence is incomplete remains in the
eligible count and the needs-review section without receiving a score.
The visible boundary includes the saved non-Taiwu group roster; membership and
living-state agreement are hard eligibility evidence, so a confirmed nonliving
roster member remains visible as ineligible rather than disappearing from the
candidate universe.

Requirement evidence preserves each gate's stable identity, ordered requirement
kind, typed field when applicable, exact outcome, reason identity, and localized
explanation. The UI labels repeated field gates by requirement purpose so fact
confirmation and provenance compatibility remain distinguishable.

Candidate rows keep this evidence in a native disclosure that is collapsed by
default. Its summary reports the exact passed/total requirement count; expanding
one candidate reveals strengths, material limitations, and every typed gate.
The role-wide score limitation appears once above the table rather than being
repeated inside every ranked candidate.

No candidate receives a winner crown, best-person badge, percentage bar,
quality grade, green/red worth indicator, or cross-role comparison.

## Score presentation

Every score value is headed `Saved base qualification` or `存檔基礎資質`.
The raw number has no percent sign or progress bar.

This message remains adjacent to the result heading and score details:

> Scores compare saved base qualification within this selected discipline
> only. They are not current attainment, success probability, or universal
> companion quality.

Traditional Chinese:

> 分數只比較所選類別的存檔基礎資質，並非目前造詣、成功機率或人物的整體價值。

When the score is unavailable, the UI shows the candidate's exact state and a
friendly reason. It never renders `0`, a blank score cell, a disabled rank, or
an estimated value.

## Tie presentation

Equal exact scores share one competition rank. Every tied row/card includes:

- the same numeric rank;
- visible `Tied` / `並列` text;
- an accessible label containing the shared rank; and
- no visual emphasis suggesting that stable display order broke the tie.

Candidate ID is used internally only to make rendering deterministic inside a
tie group. The UI never prints the ID or explains one tied candidate as better.

## Filters and name query

The status filter is one native radio group or single pressed-button group in
this order: All, Ranked, Needs review, Ineligible. It maps exactly to the
contract states.

The optional name query matches localized display names only after status
filtering. It is labelled `Filter visible names`, not `Find character`, because
it does not expand the candidate universe. Clearing the query restores the same
immutable shortlist.

After any filter change:

- focus remains on the changed control;
- the result and all unfiltered state counts remain unchanged;
- an `aria-live="polite"` region announces visible and total counts; and
- comparison selections remain only when both candidates are still in the
  immutable result, even if one is temporarily filtered from view.

No filter changes score, rank, tie, evidence, or result fingerprint.

## Comparison interaction

Each candidate has one checkbox labelled with the localized character name.
At most two may be selected. After the second selection:

- remaining unchecked controls are disabled with explanatory text;
- the comparison region appears after all candidate-state sections;
- a polite announcement says comparison is ready; and
- a `Clear comparison` button removes both selections and returns focus to the
  first previously selected candidate control when it remains visible.

Unchecking one selected candidate preserves the other and re-enables the
remaining controls. Selecting or clearing comparison never rereads the save.

The comparison uses a semantic table on wide screens and one shared-fact
heading followed by candidate A and B values on narrow screens. It shows
eligibility, hard gates, exact qualification, evidence state, score/rank when
available, neutral supporting facts, and the contract's exact relative outcome.

Before the role-specific facts, a separate `Capability overview` table shows
the equal-category breadth index and the three saved-base category averages:
six main attributes, 14 martial aptitudes, and 16 life-skill aptitudes. Each
category shows confirmed/expected coverage and up to three highest localized
values. For martial and life-skill objectives, a visible limitation says this
overview is descriptive, equally weighted, and cannot change the selected-role
score, rank, or recommendation. It is not styled as a winner, grade,
probability, or universal ranking.

When `Comprehensive base capability` is the selected objective, breadth is the
explicit role-local score. The main candidate list directly shows breadth and
all three category averages; the same comparison table remains available.

If either value is unavailable or conflicting, the result says so and omits a
numeric difference. It never treats missing evidence as an advantage.

## Focus and keyboard behavior

The keyboard and DOM order is:

1. skip link and page heading;
2. information-only and candidate-boundary notices;
3. role radio group;
4. discipline select when the objective requires it;
5. find/retry button;
6. result heading and status summary;
7. status filter and name query;
8. candidate rows/cards in canonical order;
9. needs-review and ineligible sections;
10. comparison region and clear button; and
11. evidence and scope disclosures.

Native radios, select, checkboxes, buttons, and disclosures retain expected
Tab, arrow, Space, Enter, and Escape behavior where applicable. No custom
roving focus is required.

Starting a request places busy state on the result region without moving focus.
Success moves focus to the new result heading because the user explicitly
requested it. Failure moves focus to the error summary. Local role, discipline,
filter, name, comparison, disclosure, language, or viewport changes retain
focus on the equivalent control whenever it still exists.

## Visible state model

| State | Required visible content | Recovery or transition |
|---|---|---|
| No role selected | Candidate-boundary explanation and disabled find action | Select an objective, plus a discipline when required |
| Valid draft input | Objective purpose and enabled find action | Find candidates |
| Loading | Busy status, information-only notice, no mixed old/new active result | Atomic success or failure |
| Available ranked result | Objective, optional discipline, score warning, counts, ranked/tied rows | Filter, compare, or request another objective |
| One eligible candidate | Rank 1 with no claim that comparison proved superiority | Review or run another discipline |
| No eligible candidate | Zero eligible count and honest universe-state sections | Review reasons or refresh stable save |
| Explicit tie | Shared rank plus visible tied text | Compare tied candidates |
| Ineligible candidate | Failed hard gate and evidence summary; no score | Review only |
| Incomplete candidate | Missing required fact and no zero fallback | Refresh only when source may become available |
| Unsupported version/value | Supported-version or standalone limitation and no score | Update evidence mapping or use supported source |
| Conflicting candidate | Conflicting source summary and no score | Review source conflict; no automatic precedence |
| Filtered result | Active filter, visible count, unchanged unfiltered counts | Show all or clear name query |
| Comparison ready | Two candidates from one result and exact relative state | Clear or change selection |
| Save changed during read | Discarded result and stable-save retry guidance | Retry after save stabilizes |
| Missing configured save | Configuration guidance without raw path | Configure trusted save and retry |
| Partial snapshot | Exact partial source status and affected-candidate guidance | Review unranked entries or retry a stable read |
| Catalogue missing or installed sources missing | Exact missing state and source-specific guidance | Restore trusted sources or rebuild, then retry |
| Catalogue stale or rebuilding | Exact stale/rebuilding state, never old values | Refresh or wait for rebuild, then retry |
| Catalogue unsupported, unreadable, unavailable, or corrupt | Exact typed failure and distinct recovery guidance | Correct the named source/catalogue condition before retrying |
| Read/calculation failure | Safe error summary with no exception or old active result | Retry or correct configuration |
| Previous draft/result mismatch | Inert `Previous result` label | Submit the new draft or restore matching controls |

## Bilingual terminology

All visible copy is resource-backed. These terms define the intended meaning:

| Contract term | English | Traditional Chinese |
|---|---|---|
| page heading | Companion finder | 同道人選比較 |
| information only | Information only | 僅供參考 |
| martial role | Martial discipline aptitude | 武學資質 |
| life-skill role | Life-skill discipline aptitude | 技藝資質 |
| comprehensive role | Comprehensive base capability | 綜合基礎能力 |
| discipline | Discipline | 類別 |
| find action | Find candidates | 查找人選 |
| saved-group boundary | Saved group roster excluding Taiwu; evidence determines eligibility | 存檔隊伍名冊不含太吾本人；證據決定資格 |
| saved base qualification | Saved base qualification | 存檔基礎資質 |
| ranked | Ranked | 已排序 |
| tied | Tied | 並列 |
| eligible | Eligible | 符合資格 |
| ineligible | Ineligible | 不符合資格 |
| incomplete | Incomplete | 資料不完整 |
| unsupported | Unsupported | 目前不支援 |
| conflicting | Conflicting | 資料衝突 |
| needs review | Needs review | 需檢查 |
| compare | Compare | 比較 |
| capability overview | Capability overview | 能力概覽 |
| breadth index | Breadth index | 廣度指數 |
| six-attribute average | Six base attributes | 六項基礎屬性 |
| martial-aptitude average | Martial aptitudes | 武學資質 |
| life-skill-aptitude average | Life-skill aptitudes | 技藝資質 |
| confirmed coverage | Confirmed coverage | 已確認覆蓋 |
| top values | Top values | 最高項目 |
| equal | Equal | 相同 |
| advantage | Higher for this role | 此目標較高 |
| disadvantage | Lower for this role | 此目標較低 |
| previous result | Previous result | 上一次結果 |

Localized candidate and discipline names come from installed resources. If a
name is unavailable, the UI uses a localized unnamed/unavailable label and
never prints a numeric ID or stable code.

## Non-color status cues

Every state uses visible text. Optional icons are supplementary and hidden from
assistive technology when the same label follows:

| State | Optional icon | Required text |
|---|---:|---|
| Ranked | `#` | Numeric rank and `Ranked` |
| Tied | `=` | Shared rank and `Tied` |
| Ineligible | `×` | `Ineligible` plus hard-gate reason |
| Incomplete | `?` | `Incomplete` plus missing fact |
| Unsupported | `!` | `Unsupported` plus limitation |
| Conflicting | `⇄` | `Conflicting` plus source summary |

Color may reinforce grouping but never carries eligibility, merit, tie, or
evidence meaning by itself.

## Responsive behavior

The page uses a 960 CSS-pixel finder-container boundary rather than viewport-
only assumptions:

- at or above 960 pixels, result and comparison tables may use aligned columns;
- below 960 pixels, candidates become cards and comparison facts stack under
  shared headings;
- at 620 pixels, controls use one column and full-width buttons; and
- names, reasons, and localized labels wrap with `overflow-wrap: anywhere`.

No breakpoint removes counts, score warnings, state labels, unavailable
reasons, evidence, supporting facts, or comparison outcomes. CSS reflow never
changes DOM order or moves keyboard focus.

## Session lifecycle and safety

Draft role, discipline, filters, name query, selected comparison candidates,
and expanded disclosures are helper-owned session state only. None is written
to disk or helper storage. A language or responsive-layout change does not
reread the save.

The page has no recruit, dismiss, train, move, equip, assign, dialogue, travel,
settlement, save-write, process, screenshot, upload, export, automation, or
input-control action. It gives information for the player to review and act on
manually in the game.

## Verification evidence

- Presentation mapper tests cover every candidate and evaluation state, exact
  universe eligibility, typed snapshot/enrichment/catalogue status, score
  warning, identifiable typed hard gates, ties, filters, counts, comparison
  outcomes, missing display values, and raw-ID hiding.
- Rendered-component tests cover native role/discipline controls, loading,
  stable-save retry, one-candidate, empty, tied, needs-review, ineligible,
  comparison, collapsed candidate evidence, previous-result, focus-target, and
  bilingual states.
- Architecture tests require the dedicated route and navigation entry, one
  explicit source action, the single-DOM responsive layout, and absence of a
  Presentation evaluation, persistence, process, upload, input, or game-control
  path.
- Browser review found no console errors and verified the live `/companions`
  route before any save read. The synthetic 1,440 by 900 English result had
  equal document scroll and client widths. The 390 by 844 Traditional Chinese
  result had equal 375-pixel content widths and exposed the same semantic table
  facts as labelled cards.
- The captures contain only synthetic names, locations, values, timestamp, and
  ranks:
  [English desktop result](../../reviews/assets/epic-006/companion-finder-en-desktop.png),
  [Traditional Chinese narrow result](../../reviews/assets/epic-006/companion-finder-zh-narrow.png),
  and [Traditional Chinese candidate cards](../../reviews/assets/epic-006/companion-finder-zh-narrow-candidates.png).
- Full browser observations and artifact provenance are recorded in the
  [E6-010 review](../../reviews/E6-010-companion-finder-ui.md).
