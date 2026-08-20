# UI-008: Exact-target tactical combat planner

| Field | Value |
|---|---|
| Status | Accepted |
| Epic | [EPIC-008](./EPIC.md) |
| Backlog item | [E8-001](./BACKLOG.md#e8-001--define-tactical-plan-search-score-and-ui-semantics) |
| Implementation | [E8-011](./BACKLOG.md#e8-011--deliver-the-bilingual-accessible-tactical-plan-ui) |
| Route | Existing combat recommendation page (`/`) |
| Last updated | 2026-08-20 |

## Purpose

Define a concise bilingual decision surface for following one evidence-backed,
exact-target tactical plan manually. The surface extends the existing combat
recommendation page; it does not create another target selector, loadout card,
strategy panel, comparison matrix, or recommendation engine.

The primary view answers, in order:

1. Is tactical planning supported for this target and evidence revision?
2. What must the player prepare manually before combat?
3. What is the supported opening?
4. Which observable target or player states trigger a response?
5. How should the player recover from the selected response's verified cost?
6. What supported finish condition or fallback remains?
7. Which evidence gap or search bound could change the plan?

The page is information-only. It never executes a plan, captures the screen,
records input, controls combat, equips a skill, changes direction, allocates a
slot, or writes to the game.

## Placement in the existing workflow

The tactical plan appears after the selected recommendation summary and before
the detailed side-by-side loadout comparison. The existing target selector,
observation workflow, recommendation policy, skill cards, target-strategy
panel, manual loadout changes, and comparison remain authoritative for their
current facts.

The Epic 8 surface consumes one coherent recommendation result. It does not
reread the save, rerun search, or rebuild a plan when the player opens evidence,
changes a disclosure, resizes the viewport, switches language, or reviews a
candidate diagnostic.

Changing the target, policy, observation, or bound controls creates a draft
different from the displayed result. The previous plan remains visibly inert
and labelled `Previous result` until the player explicitly requests the new
recommendation. Old steps are never silently relabelled with new controls.

## Information hierarchy

1. **Tactical-plan status** — target, freshness, policy, completeness, and
   information-only boundary.
2. **Manual plan** — supported preparation, opening, trigger, recovery, finish,
   and fallback stages.
3. **Critical unresolved conditions** — facts that can invalidate or change the
   plan.
4. **Candidate and search summary** — considered, admitted, pruned, explored,
   retained, and limiting-bound counts.
5. **Why this plan** — score components and decisive chain relationships.
6. **Detailed evidence** — target transitions, requirements, provenance,
   candidate decisions, and aggregated diagnostics.

The first viewport should prioritize the status and initial supported steps.
Raw evidence, complete candidate lists, and repeated limitations remain behind
native disclosures.

## Wide layout sketch

```text
┌ Tactical plan ────────────────────────────────────────────────────────┐
│ Exact target · Balanced · Confirmed/partial evidence                 │
│ Information only — carry out every action manually in the game.      │
├ Plan ─────────────────────────────────────────────────────────────────┤
│ 1 Prepare    Condition/status     Manual action            [Evidence]│
│ 2 Open       Condition/status     Manual action            [Evidence]│
│ 3 Respond    When target state…   Manual response          [Evidence]│
│ 4 Recover    After verified cost  Manual recovery/fallback [Evidence]│
│ 5 Finish     Supported window or “Finish evidence unavailable”       │
│ 6 Fallback   If condition fails…  Verified fallback        [Evidence]│
├ Needs confirmation ──────────────────────────────────────────────────┤
│ • Exact unknown/conflicting condition and its effect on the plan.     │
├ Search and selection ────────────────────────────────────────────────┤
│ Considered 84 · admitted 7 · pruned 2 · explored 31/31 · complete    │
│ Why this plan [Show score] · Candidate decisions [Show]               │
└ No action was sent to the game. ─────────────────────────────────────┘
```

## Narrow layout sketch

```text
┌ 戰術計畫 ───────────────────────┐
│ 精確目標 · 均衡 · 部分證據      │
│ 僅供參考，請自行在遊戲中操作。  │
├ 1 戰前準備                     │
│ 條件：已確認／待確認            │
│ 手動操作：……                   │
│ 預期用途：……  [查看證據]       │
├ 2 開場                         │
│ ……                             │
├ 3 條件應對                     │
│ 當目標出現……時，手動……         │
├ 4 恢復                         │
│ 在已驗證代價後……               │
├ 5 收尾                         │
│ 收尾證據不足                   │
├ 6 後備方案                     │
│ 若條件不成立，改用……           │
├ 需要確認                       │
│ • 未知條件及其影響             │
├ 搜尋摘要                       │
│ 已檢視 84 · 納入 7 · 搜尋完整  │
│ [為何選擇此計畫] [候選詳情]     │
└ 未向遊戲傳送任何操作。 ─────────┘
```

The sketches illustrate hierarchy, not verified mechanics. E8-000 leaves the
initial vertical's Finish stage unsupported and its finish state fallback-only;
the UI must show that state rather than invent a placeholder instruction.

## Result header

The tactical-plan heading owns facts shared by all steps:

- selected target display identity;
- recommendation policy;
- save, observation, catalogue, and rule freshness summary;
- plan state: available, partial, unsupported, no candidate, truncated,
  cancelled, or failed;
- whether a finish path is supported or fallback-only;
- search completeness summary; and
- the information-only/manual-action statement.

Rule versions and fingerprints use friendly summaries in the primary view.
Stable codes, version strings, and fingerprint prefixes may appear in the
evidence disclosure for troubleshooting but are not used as untranslated
headings.

## Plan-stage semantics

Plan stages use a semantic ordered list. An omitted stage renders no placeholder
action, while an unsupported relevant stage renders its exact limitation. In
both cases later stages retain their canonical ordinals and meanings.

### Preparation

Preparation contains supported manual checks or changes that occur before
combat, such as exact practice direction, currently achievable breakthrough,
skill equipment, weapon/style availability, inner-power compatibility,
category capacity, or universal-slot allocation.

It reuses the existing manual-loadout plan rather than restating full skill
cards. When all required preparation already matches the current loadout, the
stage says so explicitly.

### Opening

Opening contains only verified initial choices, such as active defense,
agility, resource, distance, or first-use requirements supported by E8-000.
Unknown opening context is displayed as a condition to confirm, not a default
instruction.

### Target-state response

Each response step begins with a condition phrased as an observable or manually
confirmable target/player state. It then states the manual response and the
verified purpose separately.

The UI must not collapse these claims:

- `When`: the condition under which the rule applies;
- `Do manually`: the player action;
- `Expected verified purpose`: what the typed effect supports; and
- `If unavailable`: the unresolved or fallback branch.

### Recovery

Recovery follows a verified self-lock, resource cost, failed condition, or
other execution consequence. A general recommendation to “wait” or “recover”
is not shown unless the underlying recovery condition and option are typed and
version matched.

### Finish

A finish step appears only when the target window, player attack route,
relevant defense or resistance, and required live conditions are supported.
Otherwise the stage displays `Finish evidence unavailable` or `Fallback-only
plan`; it never renders an estimated win chance, predicted damage, or implied
guarantee.

### Fallback

A fallback identifies the condition that failed, remained unknown, or became
infeasible and selects a separately verified manual option. It does not reuse
the primary action with softer wording unless the evidence independently
supports that use.

## Step presentation

Each step exposes these concise facts without opening its disclosure:

- plan-stage label and ordinal;
- condition summary;
- condition state with text and non-color cue;
- manual action;
- expected verified purpose;
- branch state: primary, conditional, fallback, or unresolved; and
- one short limitation when it materially changes execution.

One native disclosure per step contains:

- every typed prerequisite and result;
- applicable target states and transitions;
- exact skill, direction, role, timing, and requirement references;
- provenance and version summary;
- unresolved, unsupported, or conflicting facts; and
- stable reason and evidence references in friendly form.

Checkboxes are not used. A plan step is not a task-tracking item, and the UI
does not record or imply completion.

## State and evidence presentation

The following states always use both text and a non-color cue:

- Confirmed / 已確認;
- Needs confirmation / 需要確認;
- Unsupported / 不支援;
- Conflicting / 資料衝突;
- Unsatisfied / 未符合;
- Fallback / 後備方案; and
- Unresolved / 未解決.

`Unknown` never appears as `No`, `Safe`, a blank value, or a green status.
Conflicting evidence shows the conflict scope and its effect on the plan before
the individual source values.

Shared source freshness, rule compatibility, search completeness, and
information-only limitations appear once in the result header. Step details
contain only evidence specific to that step.

## Candidate and search summary

The collapsed summary reports:

- learned skill directions considered;
- exact tactical-role matches;
- hard-gate rejections;
- irrelevant and dominated removals;
- combinations explored and feasible results retained;
- whether search was complete; and
- the first option, exploration, time, result, or cancellation bound that
  limited completeness.

These are non-overlapping units. `Learned skill directions considered` is the
candidate universe. Candidate decisions account for each universe member once;
combinations explored and feasible results are separate search counts. `Search
complete` is shown only when the normalized eligible space is exhausted and
every distinct feasible result is retained.

The summary never uses `best`, `optimal`, or `complete` after any active bound.
A truncated result may still be displayed as `Highest-ranked result found
within the stated bounds`.

An expanded candidate view groups decisions in this order:

1. selected options;
2. admitted alternatives;
3. rejected by hard feasibility;
4. unsupported tactical role/effect;
5. irrelevant to the verified target chain; and
6. dominated under the exact documented context.

Each group is initially bounded. Additional rows are shown in pages of at most
25 without rereading sources or changing result identity.

## Policy and score explanation

The plan reuses the existing Safe, Balanced, and Aggressive control. Epic 8
does not introduce arbitrary user weights. Their published tactical weights,
component meanings, and unavailable-component renormalization are defined by
[the tactical planning contract](../../architecture/TACTICAL-COMBAT-PLANNING-CONTRACT.md#tactical-score-semantics).

The score disclosure shows each component's:

- localized name and exact meaning;
- available or unavailable state;
- normalized value where supported;
- policy weight and contribution;
- decisive target-chain or execution evidence; and
- limitation.

Unavailable components display `Not included in this result` rather than zero;
their base weight remains explainable, while applied weight and contribution
remain unavailable.
Duplicate target coverage is presented once with any separately verified
layered contribution. Unused slots are described neutrally and do not affect
ranking unless a typed reserve or marginal-value rule applies to the exact
plan.

No progress bar, percentage ring, star rating, difficulty grade, predicted
damage, survival chance, or win probability is shown.

## Observation and refresh behavior

Applying, replacing, or clearing a target observation invalidates the complete
tactical result. The page retains no mixture of an old chain with new steps or
an old score with a new target state.

During recalculation:

- the prior result is inert and labelled `Previous result`;
- the result region is busy;
- focus remains on the initiating control until the request completes;
- success moves focus to the new result heading; and
- failure moves focus to a safe error summary while leaving the previous result
  visibly stale rather than active.

Opening details, changing language, filtering candidate decisions, or resizing
the viewport never refreshes the save or reruns planning.

## Focus and keyboard behavior

Native button and disclosure behavior is retained. Plan stages are headings
inside one ordered list, allowing heading and list navigation without a custom
timeline widget.

After a successful explicit request, focus moves to the tactical-result heading
once. Expanding a step keeps focus on its disclosure summary. Closing it returns
focus to the same summary. Candidate paging and filters retain focus and update
a polite live-region count.

Plan availability, observation replacement, search completion, and errors use
`aria-live="polite"`. Cancellation requested by the player may use an immediate
status message but must not announce every search-progress update.

No drag-and-drop, hover-only content, roving focus, keyboard shortcut, canvas,
or interactive graph is required.

## Responsive behavior

At all widths, the plan uses the same ordered-list DOM. Above 960 CSS pixels,
step condition, action, and purpose may align in columns. Below 960 pixels,
they stack inside each list item in that same order.

Target transitions and candidate decisions use semantic tables only where the
headers remain useful. On narrow screens, CSS presents the same rows as cards
or description lists. No separate mobile data projection is allowed.

Long localized skill names, condition text, and version summaries wrap. The
page has no horizontal document overflow at 390 CSS pixels.

## Visible state model

| State | Required visible content | Recovery or transition |
|---|---|---|
| Tactical rules unsupported | Exact version or evidence limitation; no guessed plan | Use a supported version or await verified rules |
| No selected target | Existing target-selection guidance; no empty timeline | Select a target and request a recommendation |
| Loading | Busy state and inert previous result | Atomic success, cancellation, or failure |
| Complete plan | All supported stages, confirmed conditions, search-complete label | Review steps and evidence |
| Partial plan | Supported stages plus critical gaps and affected branches | Confirm observations or use fallback |
| Unsupported stage | Named stage and exact limitation; no placeholder action | Continue only through supported branches |
| Fallback-only finish | Explicit absence of supported finish evidence | Follow supported fallback without success claim |
| Incomplete context | Exact unknown requirements and their affected steps | Confirm manually or recalculate from new evidence |
| Conflicting context | Conflict summary and no silently selected value | Review sources or replace observation |
| No tactical candidate | Considered/rejected/unsupported counts and reasons | Review feasibility or another target |
| Search truncated | Active bound, explored counts, and no optimality claim | Retry with an allowed bound or use current result cautiously |
| Cancelled | No active mixed partial plan | Request again |
| Observation replaced | Entire old result labelled previous | Await atomic replacement |
| Save changed during read | Discarded result and stable-save retry guidance | Retry after the save stabilizes |
| Missing configured save | Existing safe configuration guidance without raw path | Configure trusted save and retry |
| Calculation failure | Safe error summary and no new active plan | Retry or correct supported inputs |
| Draft/result mismatch | Inert `Previous result` label | Submit draft or restore prior controls |

## Bilingual terminology

These terms are accepted for the version-1 interaction contract:

| Contract term | English | Traditional Chinese |
|---|---|---|
| section heading | Tactical plan | 戰術計畫 |
| information only | Information only | 僅供參考 |
| preparation | Preparation | 戰前準備 |
| opening | Opening | 開場 |
| target-state response | Target-state response | 目標狀態應對 |
| recovery | Recovery | 恢復 |
| finish | Finish | 收尾 |
| fallback | Fallback | 後備方案 |
| manual action | Do manually | 手動操作 |
| expected purpose | Expected verified purpose | 已驗證預期用途 |
| observed state | Observed state | 已觀察狀態 |
| verified transition | Verified transition | 已驗證轉換 |
| needs confirmation | Needs confirmation | 需要確認 |
| unsupported | Unsupported | 不支援 |
| conflicting | Conflicting | 資料衝突 |
| unresolved | Unresolved | 未解決 |
| fallback only | Fallback only | 僅有後備方案 |
| candidate consideration | Candidate consideration | 候選檢視 |
| search complete | Search complete | 搜尋完整 |
| search bounded | Search bounded | 搜尋受限 |
| result found within bounds | Result found within stated bounds | 在所述限制內找到的結果 |
| finish evidence unavailable | Finish evidence unavailable | 缺少收尾證據 |
| no action sent | No action was sent to the game | 未向遊戲傳送任何操作 |

Candidate decisions, search terminators, cache diagnostics, and score-state
terms use the complete bilingual table in
[the tactical planning contract](../../architecture/TACTICAL-COMBAT-PLANNING-CONTRACT.md#bilingual-terminology).

## Content rules

- Use `when`, `if`, `confirm`, `consider`, and `manually` for conditional
  instructions.
- Reserve `causes`, `prevents`, `interrupts`, `recovers`, and similar mechanical
  verbs for exact typed evidence and retain applicable limitations.
- Do not use `will win`, `guaranteed`, `safe`, `optimal`, `best possible`,
  `expected damage`, or percentage-success wording.
- Do not convert an unsupported transition into generic combat advice.
- Do not repeat the full source list in every step.
- Do not expose raw stable codes when localized display text exists.
- Do not describe an unequipped, wrong-direction, infeasible, or pruned skill as
  part of the active plan.
- Do not describe a manual preparation item as already applied.

## Accessibility acceptance

- The result has one programmatic heading and one ordered plan list.
- Stage heading hierarchy remains logical with existing recommendation content.
- Every state has visible text and a non-color cue.
- Step disclosures have descriptive accessible names including stage and state.
- Candidate filters and paging expose visible and total counts.
- Search and observation transitions use restrained live announcements.
- Focus behavior is deterministic for success, cancellation, failure,
  disclosure, paging, language, and responsive changes.
- English and Traditional Chinese expose equivalent names, descriptions,
  conditions, evidence, and limitations.
- At 200 percent zoom and 390 CSS pixels, content reflows without document
  horizontal scrolling or hidden actions.

## Non-interference acceptance

- The section has no execute, equip, apply, change-direction, allocate, capture,
  upload, automate, record-outcome, or control-game action.
- No plan step has a completion checkbox or persisted done state.
- All source reads occur through existing guarded read-only boundaries.
- UI interactions after result creation operate only on immutable presentation
  data unless the player explicitly requests a complete recalculation.
- The final visible line states that no action was sent to the game.

## Verification matrix

E8-011 and E8-013 must verify at least:

- English and Traditional Chinese;
- desktop and 390-by-844 narrow viewport;
- keyboard-only stage and evidence navigation;
- complete, partial, unsupported-stage, fallback-only, incomplete-context, and
  conflicting-context plans;
- no-candidate, complete-search, every truncated-search bound, cancellation,
  observation replacement, stale draft, and safe failure;
- available and unavailable damage/finish components;
- selected, rejected, unsupported, irrelevant, and dominated candidate groups;
- parity between plan, selected loadout, manual preparation, strategy, and
  comparison; and
- absence of horizontal overflow, raw-ID leakage, probability wording,
  completion controls, and game-action controls.
