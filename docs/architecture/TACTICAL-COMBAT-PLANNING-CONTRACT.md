# Tactical combat planning product and interaction contract

| Field | Value |
|---|---|
| Status | Accepted — product and interaction semantics defined |
| Epic | [EPIC-008](../roadmap/epic-008/EPIC.md) |
| Backlog item | [E8-001](../roadmap/epic-008/BACKLOG.md#e8-001--define-tactical-plan-search-score-and-ui-semantics) |
| Evidence boundary | [E8-000 tactical evidence](../scenarios/E8-000-tactical-combat-evidence.md) |
| UI contract | [UI-008](../roadmap/epic-008/UI-008-tactical-combat-planner.md) |
| Historical rule version | `1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a` |
| Installed version at decision | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` — unsupported |

## Purpose

Define the version-1 user-visible meaning of a tactical result, plan stage,
condition, action, expected purpose, candidate decision, search diagnostic,
policy, score component, limitation, and UI transition before Domain or public
API contracts are implemented.

The first vertical is a synthetic magic-sound scenario pinned to the historical
verified-rule version. Its supported result can contain suppression,
mitigation, and conditional recovery, but its finish state is `FallbackOnly`.
Unchanged configuration IDs in the newer installed version do not authorize
the historical mechanics.

This contract describes conditional information for a player. It does not
simulate a battle, observe the running game, predict damage or victory, execute
a step, change a loadout, write a source, or record step completion.

## Result identity and lifecycle

One immutable tactical result is identified by:

- player and exact-target snapshot fingerprints;
- observation revision and merge result;
- installed source, catalogue, mapping, and verified-rule versions;
- causal-chain and tactical-role rule fingerprints;
- execution-context fingerprint;
- candidate-universe, pruning-rule, and search-bound identities;
- policy and scoring-rule identities;
- selected feasible-loadout fingerprint; and
- compiled plan fingerprint.

Capture time, elapsed measurements, localized display text, viewport, expanded
disclosures, focus, and candidate-page position are diagnostics or
Presentation state and never change semantic identity.

Target, policy, observation, or bound-control changes create a draft. They do
not relabel the displayed result. Only an explicit recommendation request can
atomically replace the current result. Cancellation or failure cannot publish
a mixed chain, score, loadout, and plan. A retained result is visibly inert and
labelled `Previous result` until a coherent replacement succeeds.

## Plan-stage contract

Stages have these stable meanings and canonical order:

| Ordinal | Stable stage | Exact meaning |
|---:|---|---|
| 1 | `Preparation` | Manual checks or proposed changes required before combat |
| 2 | `Opening` | Verified choices or conditions that apply at combat start or before the first relevant target transition |
| 3 | `TargetStateResponse` | A manual response conditional on an observable or manually confirmable target or player state |
| 4 | `Recovery` | A manual route after a verified response cost, lockout, depleted resource, unmet trigger, or failed condition |
| 5 | `Finish` | A manual attack route usable only inside an exact supported finish window |
| 6 | `Fallback` | A separately verified manual branch when a named primary condition is false, unknown, unsupported, conflicting, or infeasible |

An action never moves between stage meanings merely to make the list look
complete. Stage order constrains presentation and plan validation; it is not a
simulated clock and does not claim that every stage occurs once.

One stage can contain multiple ordered conditional steps. Target-state response
and recovery steps may refer to each other through typed branches, but their
canonical stage ordinals do not change. A branch cannot create a cycle that
pretends battle state will eventually advance.

### Supported, omitted, and unsupported stages

| State | Meaning | Presentation |
|---|---|---|
| `Supported` | At least one action in the stage has version-matched evidence and satisfied or explicitly conditional requirements | Show the named stage and supported steps |
| `Omitted` | The stage is not applicable to this result and no user decision depends on it | Render no placeholder action; retain its canonical ordinal so later stages are not renamed |
| `Unsupported` | The stage is relevant, but a required rule or fact is unavailable, incomplete, version-mismatched, or conflicting | Show the named stage, exact limitation, affected branch, and no invented action |

The initial vertical has an unsupported `Finish` action and an available
`Fallback` branch. The visible finish state is `Fallback only`; a generic
attack instruction cannot fill the gap.

## Claim separation

The UI and all generated explanations keep these concepts separate:

| Concept | Exact semantic owner | Must not imply |
|---|---|---|
| Observed state | A save fact, confirmed observation, or player-confirmable live condition with provenance | That a transition occurred or will occur |
| Verified transition | A versioned condition-and-result relationship with timing, evidence, and limitations | A simulated next state, hidden AI choice, frequency, or guaranteed trigger |
| Manual action | Something the player may do in the game after checking its requirements | That the helper sent input, equipped a skill, or completed the action |
| Expected verified purpose | The bounded mechanical purpose supported by the selected transition or role | Damage, survival, victory, or any broader outcome not in evidence |
| Fallback | A separately verified action and the exact condition that selects it | A weaker restatement of an unsupported primary action |
| Unresolved evidence | A missing, incomplete, version-mismatched, or conflicting fact and its effect | `No`, zero, safe, satisfied, or permission to guess |

The primary copy pattern for a conditional step is:

```text
When <observed or manually confirmed condition>, consider <manual action>.
Expected verified purpose: <bounded effect>. If <named requirement is not
satisfied>, use <verified fallback> or leave the branch unresolved.
```

Mechanical verbs such as `interrupts`, `prevents`, `clears`, or `recovers` are
reserved for an exact applicable transition. Display copy otherwise uses
`consider`, `confirm`, `may`, and `expected verified purpose`.

## Evidence and condition states

| Stable state | Exact meaning | Visible label |
|---|---|---|
| `Confirmed` | The required fact is available, version-compatible, and unconflicted | Confirmed / 已確認 |
| `NeedsConfirmation` | The condition is intentionally player-confirmable but is not available in the immutable snapshot | Needs confirmation / 需要確認 |
| `Unsatisfied` | Verified evidence proves the requirement is false for this result | Unsatisfied / 未符合 |
| `Unsupported` | No approved rule or source mapping can decide the fact for this version | Unsupported / 不支援 |
| `Conflicting` | Applicable sources disagree and precedence cannot safely resolve them | Conflicting / 資料衝突 |
| `Unresolved` | A required conclusion cannot yet be selected because evidence is incomplete or another named state blocks it | Unresolved / 未解決 |

Only verified contrary evidence is `Unsatisfied`. Missing data is never false,
an empty set, zero, or a green status. A confirmed observation can replace a
lower-precedence save claim only through the existing observation merge rules;
the tactical layer cannot invent a precedence rule.

## Candidate consideration contract

The candidate universe contains each distinct learned skill direction from the
single tactical snapshot. It is a character-specific planning universe, not a
ranking of the character or every skill in the game.

Each universe member receives exactly one terminal consideration state before
combination search:

| State | Exact meaning | Enters search? |
|---|---|---:|
| `Admitted` | An exact version-matched tactical role applies to the selected chain, every hard gate passes, and no pruning rule removes the option | Yes |
| `Rejected` | Verified evidence proves at least one ordered hard feasibility requirement fails | No |
| `Unsupported` | An exact role, effect, timing, requirement mapping, or source version needed to evaluate the option is not approved | No |
| `Irrelevant` | The option is supported and feasible, but supplies no applicable transition, interaction, recovery, fallback, or supported finish contribution for this exact chain | No |
| `Dominated` | Another option is no worse for every applicable supported contribution and execution requirement, is strictly better on at least one, and has no greater verified cost in the same context | No |

`Dominated` is allowed only when both options have comparable evidence,
requirements, timing, role compatibility, direction state, and version. An
unknown fact prevents the dominance decision. A tie is not dominance. Every
removed option retains the exact pruning rule, dominating option when
applicable, evidence, and limitation.

`Selected` is a later search outcome attached to an admitted option; it is not
a sixth consideration state. Candidate labels describe applicability to this
target and result. They never use `weak`, `bad`, `useless`, or another label
that implies general character quality.

## Ordered search accounting

Diagnostics use these non-overlapping counts:

| Diagnostic | Exact meaning |
|---|---|
| Candidate universe | Distinct learned skill directions examined from the coherent snapshot |
| Role supported | Universe members with an exact applicable version-matched tactical role before hard gates |
| Rejected | Role-supported members removed by verified hard feasibility failures |
| Unsupported | Members lacking an approved role or a required supported fact |
| Irrelevant | Supported feasible members removed by the exact-target relevance rule |
| Dominated | Relevant members removed by a strict comparable-evidence dominance rule |
| Admitted options | Remaining options supplied to bounded combination search |
| Explored combinations | Distinct normalized option combinations whose search node was evaluated |
| Feasible results | Complete loadouts accepted by the existing feasibility validator before result limiting and deduplication |
| Retained results | Canonically distinct feasible results retained after the result limit |

Counts retain units and cannot be silently collapsed into one `considered`
number. The expanded diagnostic accounts for every universe member exactly
once; combination counts are a separate search unit.

### Limits and completion

| Terminator | Exact meaning |
|---|---|
| `OptionLimit` | Relevant non-dominated options exceeded the admitted-option bound before combination traversal |
| `ExplorationLimit` | The maximum distinct combination nodes was reached before exhaustion |
| `TimeLimit` | The monotonic elapsed-time budget expired before exhaustion |
| `ResultLimit` | More canonically distinct feasible results existed than could be retained |
| `Cancelled` | The caller requested cancellation; no active partial result may be published |
| `None` | The eligible normalized search space was exhausted and all feasible results were retained |

The first detected terminator is retained using the fixed precedence above for
diagnostic stability when multiple limits become observable at the same
boundary. `Search complete` is allowed only for `None`. A bounded result says
`Highest-ranked result found within the stated bounds`; it never says `best`,
`optimal`, or `complete`.

Elapsed budget and measured duration are diagnostics, not fingerprint inputs.
Equivalent completed searches have the same identity regardless of duration.
A time-limited search may expose the deterministic results completed before
the boundary only when the Application result is coherently finalized; a
cancelled search exposes no active plan.

### Cache reuse

`Cache hit` means a bounded helper-owned entry with the same complete semantic
key supplied an identical reusable projection during the request. `Cache miss`
means the work ran once and may populate that cache. The diagnostic names the
cache kind and hit/miss count.

Cache reuse never means that a save, catalogue, rule, capacity, or score was
assumed unchanged without its semantic fingerprint. Presentation interactions
cannot turn a miss into a new result or rerun source work. Cache counts and
elapsed values are diagnostics and do not affect ranking or result identity.

## Tactical score semantics

Scoring orders already feasible complete loadouts. It cannot repair a rejected
option, override a hard gate, add a missing step, or turn unsupported evidence
into a numeric penalty.

Version 1 reserves these score components and base policy weights:

| Component | Exact meaning | Safe | Balanced | Aggressive |
|---|---|---:|---:|---:|
| Causal value | Non-duplicated supported contribution to applicable target states and transitions | 28 | 29 | 28 |
| Layered protection | Separately useful mitigation, interaction, or fallback beyond primary causal coverage | 24 | 18 | 10 |
| Timing opportunity | Supported preparation, opening, trigger, and response timing fit | 10 | 16 | 24 |
| Execution reliability | Observability and satisfaction of exact action requirements | 20 | 16 | 12 |
| Recovery cost | Preference for lower verified resource, self-lock, preparation, and recovery burden | 15 | 13 | 8 |
| Finish path | Supported applicable damage channel and finish-window contribution | 3 | 8 | 18 |

Each base weight set totals 100. E8-007 owns the versioned normalization and
scoring formula, but it must preserve these product meanings and published
weights or explicitly version this contract.

Unavailable components have no value, no contribution, and no implicit zero.
For a result, the applied weight is the base weight divided by the sum of base
weights for available components. Both base and applied weights remain
visible. This preserves different Safe, Balanced, and Aggressive priorities
when `FinishPath` is unavailable in the initial vertical.

- **Safe** emphasizes separately useful protection, observable execution, and
  lower recovery burden. It does not mean survival is guaranteed.
- **Balanced** gives the greatest relative base weight to non-duplicated causal
  value while balancing timing, reliability, protection, and cost.
- **Aggressive** emphasizes supported timing opportunities and a finish path
  when one exists. Without finish evidence it remains a timing-oriented policy;
  it does not invent damage or a probability of victory.

One transition receives its full causal value at most once. A second option
covering the same transition has no second full reward. It can receive layered
protection value only when an exact interaction, failure branch, different
timing window, or separately useful mitigation rule proves marginal value.

A self-lock, resource use, preparation burden, or required recovery can lower
the `RecoveryCost` component only through typed inputs. Unknown timing or
requirements remain visible limitations and cannot be silently averaged.

`FinishPath` is available only when version-matched attack, hit or cast
reliability, target defense or resistance, applicable condition, and exact
finish-window evidence all exist. The initial E8-000 vertical therefore
excludes its weight.

Unused capacity is reported as a neutral exact fact. It has no component and
no positive or negative contribution unless a future versioned reserve or
marginal-value rule proves that the capacity changes this exact plan. Duplicate
coverage is never relabelled as reserve value.

Each score disclosure shows component state, raw typed inputs, normalization,
base weight, applied weight, contribution, decisive evidence, and limitation.
The UI uses no stars, difficulty grade, probability, predicted damage, or
progress-style total.

## Evidence placement and progressive disclosure

Facts shared by the whole result appear once in the result header or shared
evidence disclosure:

- target and player snapshot identity summaries;
- source freshness and rule compatibility;
- observation revision and conflict summary;
- search completeness and first terminator;
- fallback-only or finish-supported status;
- information-only and no-game-action limitation; and
- limitations that affect every step.

A plan step repeats only its condition, requirement state, manual action,
expected purpose, branch, step-specific evidence, and step-specific
limitation. Candidate rows retain only their own decision evidence. Repeating
the complete source list in every step is prohibited.

Critical unknowns and conflicts that can invalidate the active branch remain
visible before disclosures. Raw stable IDs, warning codes, paths, hashes, and
technical evidence references remain internal when localized friendly text is
available.

## Interaction and failure contract

| Interaction | Result behavior | Focus and announcement |
|---|---|---|
| Explicit request succeeds | Atomically replace all tactical result parts | Move focus once to the new result heading; politely announce availability and completeness |
| Policy or bound changes | Create a draft; keep prior result inert | Keep focus on the changed control; no recalculation announcement |
| Observation apply, replace, or clear | Invalidate the entire tactical result and request atomic replacement | Keep focus on initiator while busy; announce replacement when coherent |
| Cancellation | Publish no active partial plan | Announce cancellation immediately; keep a safe retry path |
| Expected unsupported/no-candidate state | Publish one typed safe result | Focus result heading and announce its named state |
| Unexpected failure | Keep prior result stale and show a safe error summary | Move focus to error summary; politely announce failure |
| Open/close disclosure | Change Presentation state only | Keep or return focus to its native summary; no result announcement |
| Candidate filter/page | Change visible immutable rows only | Retain focus and politely announce visible/total count |
| Language or viewport change | Remap/reflow the same semantic result | Preserve logical focus; do not reread or replan |

The result region uses a busy state only during an explicit recalculation.
Search progress is not announced continuously. Native buttons, links, and
`details`/`summary` behavior are preferred; no custom timeline keyboard model,
dragging, hover-only evidence, or graph interaction is required.

## Responsive semantic parity

Wide and narrow layouts consume the same Presentation model and render the same
ordered-list structure. Above 960 CSS pixels, condition, manual action, and
purpose may form columns. Below that width they stack in the same order.

Tables may become card-like rows or description lists only when every header
relationship and value remains present. A causal graph can be an optional
non-authoritative enhancement, never the only path to a transition or branch.
The contract requires no graph. At 390 CSS pixels and 200 percent zoom, content
wraps without horizontal document overflow or hidden actions.

## Bilingual terminology

| Stable concept | English | Traditional Chinese |
|---|---|---|
| tactical plan | Tactical plan | 戰術計畫 |
| information only | Information only | 僅供參考 |
| previous result | Previous result | 上一次結果 |
| preparation | Preparation | 戰前準備 |
| opening | Opening | 開場 |
| target-state response | Target-state response | 目標狀態應對 |
| recovery | Recovery | 恢復 |
| finish | Finish | 收尾 |
| fallback | Fallback | 後備方案 |
| observed state | Observed state | 已觀察狀態 |
| verified transition | Verified transition | 已驗證轉換 |
| manual action | Do manually | 手動操作 |
| expected purpose | Expected verified purpose | 已驗證預期用途 |
| confirmed | Confirmed | 已確認 |
| needs confirmation | Needs confirmation | 需要確認 |
| unsatisfied | Unsatisfied | 未符合 |
| unsupported | Unsupported | 不支援 |
| conflicting | Conflicting | 資料衝突 |
| unresolved | Unresolved | 未解決 |
| fallback only | Fallback only | 僅有後備方案 |
| admitted candidate | Admitted | 已納入 |
| rejected candidate | Rejected by feasibility | 未通過可行性檢查 |
| unsupported candidate | Unsupported candidate | 不支援的候選項目 |
| irrelevant candidate | Not relevant to this target chain | 與此目標鏈無關 |
| dominated candidate | Dominated in this exact context | 在此情境中已被支配 |
| candidate universe | Learned directions considered | 已檢視的已學方向 |
| search complete | Search complete | 搜尋完整 |
| search bounded | Search bounded | 搜尋受限 |
| option limit | Option limit | 選項數量限制 |
| exploration limit | Exploration limit | 探索數量限制 |
| time limit | Time limit | 時間限制 |
| result limit | Result limit | 結果數量限制 |
| cancelled | Cancelled | 已取消 |
| cache hit | Reused matching work | 重用相符結果 |
| cache miss | Calculated once | 已計算一次 |
| result within bounds | Highest-ranked result found within the stated bounds | 在所述限制內找到的最高排序結果 |
| component unavailable | Not included in this result | 未納入此結果 |
| finish evidence unavailable | Finish evidence unavailable | 缺少收尾證據 |
| no action sent | No action was sent to the game | 未向遊戲傳送任何操作 |

English and Traditional Chinese must expose equivalent conditions, actions,
purposes, evidence states, limitations, counts, and recovery choices. Stable
codes never become untranslated fallback labels.

## Prohibited implications

No label, button, state, or accessible name may imply:

- execution, equipping, direction changes, allocation, automation, capture,
  game control, or step completion;
- guaranteed safety, survival, damage, victory, success, or recovery;
- an optimal or complete result after a search terminator;
- that an admitted, rejected, unsupported, irrelevant, or dominated decision
  measures general skill or character quality; or
- that an unsupported finish is permission to use generic combat advice.

`Safe` is the proper policy name and may appear only with its disclosed policy
meaning. Generated prose must not use it as a claim that an action or outcome
is safe.

## Acceptance invariants for later slices

E8-002 through E8-013 must preserve these invariants:

1. all six stage identities and their supported/omitted/unsupported states;
2. separation of observed state, verified transition, action, purpose,
   fallback, and unresolved evidence;
3. one terminal candidate consideration state per learned direction;
4. exact non-overlapping search counts, first terminator, and completion rule;
5. published policy meanings, weights, unavailable-component renormalization,
   duplicate-coverage, layering, recovery-cost, finish, and unused-capacity
   rules;
6. one coherent immutable result with atomic observation replacement;
7. semantic parity across languages, widths, keyboard use, and disclosures;
8. shared evidence once and step-specific evidence at the step; and
9. permanent information-only game non-interference.
