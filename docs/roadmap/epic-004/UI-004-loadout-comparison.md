# UI-004: Loadout comparison layout and interaction

| Field | Value |
|---|---|
| Status | Implemented through E4-005 |
| Epic | [EPIC-004](./EPIC.md) |
| Backlog items | [E4-000](./BACKLOG.md#e4-000--define-comparison-semantics-and-ui-states), [E4-004](./BACKLOG.md#e4-004--build-the-desktop-comparison-matrix), [E4-005](./BACKLOG.md#e4-005--add-narrow-screen-bilingual-and-keyboard-interaction) |
| Primary surface | Existing local Blazor recommendation page |
| Last updated | 2026-08-08 |

## Purpose

Add one comparison section to the existing combat-recommendation workflow so
the player can compare Current with the Safe, Balanced, and Aggressive policy
winners without reconstructing changes across separate tabs.

The data and state rules are defined by the
[loadout comparison contract](../../architecture/LOADOUT-COMPARISON-CONTRACT.md).
This document fixes their visual hierarchy, responsive behavior, bilingual
copy, keyboard order, focus behavior, and user-visible states.

## Information hierarchy

The section renders in this order:

1. `Loadout comparison` heading and information-only notice.
2. Target, snapshot, GameData/catalogue, and Current provenance summary.
3. Existing snapshot/observation warnings.
4. Narrow-mode policy selector when applicable.
5. `All rows` / `Differences only` filter and category navigation.
6. Current/policy comparison matrix grouped into all five categories.
7. Capacity and 萬用-allocation summaries for each category/column.
8. Policy tactical summaries, conditions, caveats, and unresolved risks.
9. Change-state legend and evidence details.
10. A policy-specific route to the existing setup checklist and battle plan.

Warnings, policy diagnostics, unavailable reasons, and required manual actions
are primary text. They are never available only through hover, color, or a
collapsed evidence panel.

## Synthetic desktop wireframe — English

This wireframe uses synthetic names and values. It does not represent a real
save, target, or recommendation.

```text
┌ Loadout comparison ─ INFORMATION ONLY ─ Training target ─ Snapshot S-42 ──┐
│ Current provenance: loadout from current screen; capacity from save      │
│ ⚠ Assistance used slots unavailable: one skill cost is unavailable.    │
├ Filter: (●) All rows  (○) Differences only   Categories: All 內 摧 輕 護 奇 ┤
│ Category / skill    │ Current      │ Safe         │ Balanced     │ Aggressive │
├ Neigong ─ capacity   │ 4/6, 萬用0    │ 4/6, 萬用1    │ 5/6, 萬用0    │ 5/6, 萬用0  │
│ Synthetic Inner A  │ Present      │ ✓ Retained   │ ✓ Retained    │ ✓ Retained │
├ Attack ─ capacity    │ 4/5          │ 5/6          │ 4/5           │ 5/5        │
│ Synthetic Strike B │ Present      │ − Removed    │ ✓ Retained    │ ⇄ Direction:│
│                      │              │              │               │   Reverse   │
│ Synthetic Strike C │ Absent       │ + Added      │ + Added       │ + Added    │
│                      │              │ ⇄ Reverse    │ ◇ Breakthrough│             │
├ Agility / Defense / Assistance follow the same row semantics          ┤
│ Safe: covers T-01; unresolved T-03; 4 manual actions  [View plan]   │
│ Balanced: no feasible proposal — required condition unavailable       │
│ Aggressive: covers T-01; critical caveat remains       [View plan]   │
├ Legend: ✓ Retained  + Added  − Removed  ⇄ Direction  ◇ Breakthrough ┤
│ Scores rank candidates only inside each policy; they are not win odds. │
└ TaiWu Helper cannot equip, redirect, or break through skills. ─────┘
```

The infeasible Balanced example deliberately remains a column. It is not
rendered as an empty loadout.

## Synthetic narrow-screen wireframe — Traditional Chinese

Below 1280 CSS pixels, the same facts use Current plus one selected policy.
The synthetic example selects Safe.

```text
┌ 運功比較 ─ 僅供參考 ─ 訓練目標 ─ 快照 S-42 ─┐
│ 目前配置來源：畫面觀察；格數來源：存檔             │
│ ⚠ 奇竅已用格數無法取得：一項功法成本無法取得。 │
│ 比較方案：[ 安全 ▼ ]   顯示：[所有] [僅顯示差異] │
├ 摘要：目前 ↔ 安全                                  ┤
│ 內功 格數       │ 目前 4/6、萬用 0 │ 安全 4/6、萬用 1  │
│ 範例內功甲      │ 已裝備          │ ✓ 保留             │
│ 摗破 格數       │ 目前 4/5         │ 安全 5/6          │
│ 範例摗破乙      │ 已裝備          │ − 移除             │
│ 範例摗破丙      │ 未裝備          │ + 加入；⇄ 改為逆練 │
├ 風險：已覆蓋 T-01；T-03 尚未解決                 ┤
│ [前往安全方案的手動設置清單與戰鬥計畫]          │
├ 圖例：✓ 保留  + 加入  − 移除  ⇄ 改變正逆練  ◇ 突破 ┤
│ 分數僅用於各方案內排序，並非獲勝機率。             │
└ 太吾助手不會裝備、改變正逆練或進行突破。 ───────┘
```

## Matrix semantics

The matrix uses a semantic table at desktop widths. The first header cell
labels the category/skill axis. Column headers use `scope="col"`; category
headers use `scope="rowgroup"`; skill names use `scope="row"`. Capacity is a
category summary row, not a skill with a synthetic ID.

Each skill cell renders, in order:

1. membership text;
2. required direction action, if any;
3. required breakthrough action, if any;
4. unavailable reason, if any; and
5. an accessible evidence/detail control when supporting facts exist.

The accessible cell label combines the localized skill name, category, column,
membership, and all actions. Color and icons reinforce but never replace the
text.

In narrow mode the implementation may retain a two-column table or use a
heading/description-list layout. It must preserve equivalent heading
relationships, reading order, labels, actions, capacity, and diagnostics.

## Controls and keyboard behavior

### Policy selector

The policy selector is visible in narrow mode and remains available as a
compact "focus policy" control on desktop when it selects the checklist/battle
plan destination. It is a native select or radio group with Safe, Balanced,
and Aggressive in that order.

The initial policy is the requested policy when feasible; otherwise the first
feasible policy; otherwise Safe. Arrow keys change a radio selection according
to platform conventions. A native select uses its normal keyboard behavior.

After selection:

- focus remains on the selector;
- the matrix shows Current plus the selected policy in narrow mode;
- differences-only rows are recalculated for that policy;
- warnings, filter, category position, and expanded details remain intact;
- an `aria-live="polite"` message announces the selected policy and visible
  skill-row count; and
- the setup-checklist link changes to that same policy.

### Row filter

`All rows` and `Differences only` form one radio group or one pressed toggle.
The state is stated in visible text and exposed programmatically. Focus remains
on the control after change and an `aria-live="polite"` message announces the
active filter and row count.

The exact visibility rule is defined in the architecture contract. Capacity
changes, 萬用 allocation, warnings, diagnostics, conditions, caveats, and
unresolved critical risk do not disappear when unchanged skill rows are
hidden.

### Category navigation

Category links occur in canonical order: All, 內功, 摗破, 輕靈, 護體,
奇竅. They move focus to the selected category heading without changing
filter state. A skip link moves directly to the first visible matrix heading.

### Evidence and plan links

Evidence uses native `details`/`summary` or buttons with `aria-expanded` and a
controlled region. Closing a detail returns focus to its trigger. A policy's
`View setup checklist and battle plan` link selects the existing matching
policy view and moves focus to its checklist heading. It never executes a
manual instruction.

## Focus order

The DOM and keyboard order is:

1. comparison heading and summary;
2. warnings and observation diagnostics;
3. policy selector in narrow mode;
4. row filter;
5. category navigation/skip link;
6. matrix headers and row details in visual order;
7. tactical summaries and unresolved risks;
8. legend and evidence disclosures; and
9. setup-checklist/battle-plan link.

CSS reflow must not create a visual order that disagrees with DOM order. No
interaction requires hover or drag.

When the user requests a new recommendation, the surface exposes a busy state
and, on success, moves focus to the new comparison heading. On failure, focus
moves to the error summary. A viewport-only desktop/narrow transition never
moves focus. Language changes retain focus on the equivalent control when it
still exists.

## Visible state model

| State | Matrix | Required visible content | Recovery/transition |
|---|---|---|---|
| No recommendation | Absent | Target-selection guidance | Request recommendation |
| Loading | Busy placeholder; no mixed old/new columns | Loading text and information-only notice | Atomic success or failure |
| Four feasible policies | Current plus all policies on desktop | Provenance, capacities, rows, summaries, legend, plan links | Filter, inspect, or rebuild |
| Partly infeasible | Diagnostic occupies each affected policy column | Non-blank policy diagnostic; no fake empty proposal | Select another policy or change inputs |
| All policies infeasible | Current plus three diagnostics | Generation/scoring diagnostics and known Current facts | Change inputs or refresh |
| Value unavailable | Affected fact says unavailable | Localized reason near the fact | Inspect evidence or refresh if applicable |
| Player observation applied | Rebuilt matrix | Current-screen provenance on replaced player fields | Clear observation |
| Player observation stale/rejected | Save-based rebuilt matrix | Warning and rejection reason | Supply newer observation or continue with save |
| Target observation applied | Rebuilt matrix | Observation status/impact and one coherent set of columns | Clear observation |
| Observation cleared | Rebuilt save-only matrix | Cleared confirmation; no observed labels remain | Continue or apply new observation |
| Differences only | Filtered skill rows | Active-filter label and row-count announcement | Show all rows |
| Narrow mode | Current plus selected policy | Policy selector and equivalent facts | Select policy or widen viewport |
| Read/calculation failure | Absent | Error summary without raw exception/path | Retry or correct configuration |

The loading and failure states cannot display old columns as though they
belong to a new target or observation. If the previous result remains on the
page for continuity, it must be inert and explicitly labelled as the previous
result, outside the active comparison region.

## Bilingual terminology

All new visible text is resource-backed. These terms define the intended
meaning; final copy may be refined without changing the state vocabulary.

| Contract term | English | Traditional Chinese |
|---|---|---|
| comparison heading | Loadout comparison | 運功比較 |
| Current | Current | 目前 |
| Safe | Safe | 安全 |
| Balanced | Balanced | 均衡 |
| Aggressive | Aggressive | 進攻 |
| Retained | Retained | 保留 |
| Added | Added | 加入 |
| Removed | Removed | 移除 |
| direction action | Change to Direct/Reverse practice | 改為正練/逆練 |
| breakthrough action | Complete Direct/Reverse breakthrough | 完成正練/逆練突破 |
| unavailable | Unavailable | 無法取得 |
| all rows | All rows | 所有 |
| differences only | Differences only | 僅顯示差異 |
| current-screen source | Current-screen observation | 目前畫面觀察 |
| save source | Save | 存檔 |
| information only | Information only | 僅供參考 |

Unavailable reasons, warning summaries, diagnostics, category names, action
directions, evidence labels, tactical headings, and screen-reader-only labels
must all have complete English and Traditional Chinese resources. Long names
and reasons wrap; they are not ellipsized when that would hide an action or
reason.

## Score presentation

Policy scores appear only inside that policy's tactical detail, headed
`Ranking within Safe`, `Ranking within Balanced`, or `Ranking within
Aggressive` and the corresponding Traditional Chinese label. Components show
their weight, available value, explanation, and evidence.

There is no cross-policy score bar, podium, winner badge, best-score highlight,
percentage sign suggesting probability, or default sorting by score. The
following message remains adjacent to score details:

> Scores rank candidates only inside each policy; they are not win odds.

Traditional Chinese:

> 分數僅用於各方案內排序，並非獲勝機率。

## Non-color status cues

Every status has visible text. Icons are supplementary and receive hidden or
equivalent accessible labels so they are not announced twice:

| Status | Suggested icon | Required text |
|---|---:|---|
| Retained | ✓ | Retained / 保留 |
| Added | + | Added / 加入 |
| Removed | − | Removed / 移除 |
| Direction change | ⇄ | Full required direction text |
| Breakthrough | ◇ | Full required breakthrough text |
| Unavailable | ? | Unavailable plus reason |
| Warning/unresolved | ! | Warning or unresolved-risk text |

Color may distinguish states but is never their only signal. The legend is
always reachable and remains visible in differences-only mode.

## Session lifecycle

The comparison is rebuilt atomically after target selection, recommendation
input change, player-observation apply/clear, target-observation apply/clear,
or snapshot refresh. Filter, selected policy, focus target, and expanded
details are temporary UI state.

No comparison, filter, selection, evidence expansion, or history is written to
disk or helper storage. A language or responsive-layout change does not reread
the save and does not produce a new recommendation.

## Explicitly out of scope

- Applying, equipping, removing, redirecting, or breaking through a skill.
- Reallocating 萬用 slots in the game.
- Screenshot capture, upload, OCR, or image interpretation.
- Persisted comparison history or preferences.
- Exporting or sharing comparison cards.
- Battle-outcome collection or feedback training.
- Comparing different snapshots, targets, saves, or catalogue versions as one
  simultaneous result.
- Exposing lower-ranked candidates beyond the three policy winners.

The persistent notice is:

> TaiWu Helper cannot equip, redirect, or break through skills. Follow these
> instructions manually in the game.

Traditional Chinese:

> 太吾助手不會裝備、改變正逆練或進行突破。請在遊戲中手動按照指示操作。

## E4-000 acceptance mapping

| Acceptance criterion | Decision/evidence |
|---|---|
| One-result column boundary | Architecture contract: Comparison boundary and Columns |
| Existing manual semantics and composites | Architecture contract: Skill membership and actions |
| Stable identity | Architecture contract: Stable identity and ordering |
| Available/unavailable numeric rules | Architecture contract: Numeric and capacity semantics |
| Infeasible policies remain diagnostic | Architecture contract and both wireframes |
| Scores are not probabilities | Score presentation |
| Desktop/narrow modes | Both synthetic wireframes and Responsive contract |
| Keyboard, headings, legend, focus, non-color cues | Matrix semantics, Controls, Focus order, and Non-color status cues |
| Bilingual representative state | English desktop and Traditional Chinese narrow wireframes |
| Excluded capabilities | Explicitly out of scope |

## E4-005 implementation and verification

The implemented matrix follows this contract with these concrete mechanics:

- a native Safe/Balanced/Aggressive selector shares
  `RecommendationSelectionState` with the existing checklist and battle plan;
- the requested policy is used when feasible, otherwise the first feasible
  policy is selected, otherwise Safe;
- below 1280 CSS pixels, Current and the selected policy remain visible while
  the other policy columns are removed from visual and accessibility layout;
- selected-policy difference classes re-evaluate narrow row visibility without
  changing the immutable comparison or losing the all/differences filter;
- canonical category links and a skip link target focusable row-group
  headings;
- cell labels combine localized skill/category, policy, membership, values,
  actions, and unavailable information;
- polite live regions announce policy, filter, and desktop/narrow row counts;
  and
- long names, diagnostics, and action reasons wrap without ellipsis.

Verification on 2026-08-08:

- API/presentation tests passed 254/254, including English and Traditional
  Chinese feasible, partially infeasible, unchanged, changed, unavailable,
  selected-policy, filter-preservation, and accessible-markup states;
- architecture tests passed 79/79, including the read-only/helper-local event
  allow-list and visible-identifier protections;
- a 760 × 900 in-app-browser pass confirmed the page's narrow reflow, logical
  focusable order, bilingual controls, and no browser console errors;
- the local development configuration had no save path, so matrix-specific
  responsive facts were verified with the synthetic component renderer and a
  stylesheet/semantic-markup audit rather than a live save; and
- the viewport override was reset after verification.
