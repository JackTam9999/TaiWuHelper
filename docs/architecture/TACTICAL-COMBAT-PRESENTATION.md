# Tactical combat Presentation

## Scope

The Epic 8 Presentation layer adds one information-only tactical plan to the
existing combat recommendation result. `CombatRecommendation.razor` invokes
`IRecommendTacticalCombat` once per explicit recommendation request, then maps
the legacy recommendation and tactical response from that same Application
result. The UI never reads a second snapshot or starts a second tactical
workflow while rendering.

The presentation boundary consists of:

- `TacticalCombatViewModelMapper`, which converts typed API contracts into
  immutable display models without exposing stable mechanical identities;
- `TacticalCombatUiText`, which exhaustively maps typed UI, stage, status,
  policy, finish, condition, candidate, score, terminator, direction, and
  evidence-source values to English and Traditional Chinese; and
- `TacticalCombatPlan.razor`, which renders status, the ordered plan, gaps,
  search accounting, scores, candidate decisions, and evidence.

## Coherent result lifecycle

The page retains one complete tactical presentation result. Target, policy,
current-player-observation, and target-observation draft changes mark that
result as previous and inert until the user explicitly recalculates. Loading
keeps the previous result visible without implying that it matches the draft.
Cancellation and failure have dedicated visible states.

The existing target-observation form cannot express the typed Epic 8 tactical
facts without guessing. Applying or replacing such an observation therefore
clears the tactical result atomically and shows the observation-replaced state;
clearing it returns to the normal coherent recommendation workflow. No text or
legacy observation is inferred into tactical evidence.

## Information and accessibility boundaries

The component uses one ordered list in canonical stage order and native
`details`/`summary` disclosures. Every condition has visible text and a symbol,
unknown or conflicting evidence never looks confirmed, and unavailable score
components are excluded rather than rendered as zero. Candidate lists are
grouped by typed terminal decision and page after 25 items.

There are no completion controls or game-mutation actions. The final line
always states that no action was sent to the game. Focus returns to the
tactical-plan heading after a successful recalculation, while the existing
page controls remain the only request inputs.

## Responsive rendering

The desktop and narrow layouts use the same DOM. Container rules collapse step
and score grids before the narrow-page media query stacks result metadata and
headings. Long evidence identities use `overflow-wrap`, so the 390-by-844 test
fixture has no document or component horizontal overflow.

## Verification

`TacticalCombatRenderingTests` covers both languages, every response status,
fallback-only plans, unavailable score components, all search terminators,
candidate grouping and paging, stale/loading/cancelled/failure lifecycle
states, localization completeness, semantic markup, page orchestration, and
responsive CSS. The browser record is
[E8-011 browser verification](../reviews/assets/epic-008/E8-011-browser-verification.md).
