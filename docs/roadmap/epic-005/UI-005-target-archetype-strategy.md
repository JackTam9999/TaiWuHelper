# UI-005: Compact target archetype and strategy section

## Purpose

E5-008 adds one progressive-disclosure section to the existing combat
recommendation result. It explains which evidence-backed target patterns
apply and what reusable response goals they contribute without creating a
second recommendation engine or repeating the threat panel, skill cards,
manual checklist, battle plan, or loadout comparison.

The section is information-only. Links move focus to existing helper details
or open the existing read-only skill catalogue.

## Page position and information hierarchy

`TargetStrategyPanel` appears once, above the shared Safe/Aggressive policy
control and comparison. Its order is:

1. overall profile/playbook state and snapshot/rule freshness;
2. compact profile facts;
3. all checked archetypes, with matched results first;
4. one composed list of reusable response goals;
5. exact-target adjustments with linked evidence;
6. player-feasibility status and one collapsed, deduplicated counter-option
   list;
7. a boundary reminding the player that the final loadout, manual steps, and
   comparison remain in their established sections below.

Multiple matched archetypes therefore become one profile and one composed
strategy. The UI never creates a recommendation card per archetype.

## Profile and match presentation

Attack-family facts are descriptive context, not proof of pressure or defense.
They use a separate gold-edged group. Verified pressure, resilience, control,
and tempo mechanics use the mechanics group and a separate jade edge. Every
facet also carries a textual evidence state, dimension, source summary, and
source count; meaning never depends on color.

Archetypes are ordered:

1. matched;
2. partial;
3. unsupported;
4. conflicting; and
5. not matched.

Each compact item shows a localized name, textual state, evidence count,
archetype version, and profile-rule version. A native `details` disclosure
lists friendly supporting, missing, contrary, or conflicting fact names.
Typed diagnostic codes and raw evidence references do not appear in the
primary view.

## Goals, links, counters, and gaps

The goal list preserves composer order. Each goal shows localized title,
priority, response timing, and whether exact-target evidence currently makes
it eligible. Threat names link to the existing threat panel and reuse its
selection callback.

Counter names link from goals to one shared `Verified counter options`
disclosure. The shared list contains exactly one card per stable counter,
including:

- read-only catalogue link by skill name;
- required practice direction;
- feasible, inaccessible, infeasible, or unresolved availability text;
- concise typed activation requirements; and
- the exact verified gap when the current player cannot use the option.

Known playbook gaps remain beside their response goal. Player-specific gaps
remain beside the corresponding unique counter. The same gap is not repeated
again as a generic warning.

## Exact-target adjustment and player feasibility

E5-009 keeps two decisions visibly separate:

1. `Exact-target customization` explains how the target changed the reusable
   goals. Retained, elevated, reduced, added, replaced, and unresolved states
   each have concise English and Traditional Chinese action text, reason text,
   and friendly response names.
2. `Player feasibility` explains whether the current character can access and
   fit each surviving verified counter into a legal generated loadout.

Adjustment response and evidence links reuse the existing goal, profile fact,
threat, counter, and skill-detail destinations. Evidence disclosures retain
confirmed, contrary, and incomplete state text plus their source counts. A
reduced broad response therefore does not hide the exact contrary fact or a
source conflict.

An unresolved adjustment or unavailable counter is always described as a
remaining gap, never as completed mitigation. Counter feasibility reasons use
the existing typed access and generation diagnostics rather than a new UI
guess. When the selected proposal has the same skills and generic-slot
allocation as the current loadout, and all manual changes are retains, the
panel explicitly says the final recommendation is unchanged because the
current loadout already satisfies the composed response.

The adjustment area does not repeat skill cards, warnings, manual checklist
items, battle-plan steps, or comparison rows. Counter details remain the one
shared feasibility disclosure introduced by E5-008.

## State ownership

The existing `PageStateNotice` continues to own recommendation loading and
read-failure states, so the page does not add a second spinner or error card.
Once an immutable recommendation exists, `TargetStrategyPanel` renders:

| State | Result |
|---|---|
| Available | One or more matched archetypes and their composed playbook |
| Multi-match | Available state with a count and one shared strategy |
| Partial | Supporting and missing facts; no fabricated goal |
| Unsupported | Version state and no mechanical playbook |
| Conflicting | Conflicting facts and no mechanical playbook claim |
| No match | Checked definitions remain visible; no playbook goal |
| Inaccessible counter | Counter remains named with availability and gap text |

Observation apply/clear rebuilds the whole recommendation view model, so this
section changes atomically with threats, final recommendation, and comparison.
The adjustment explanation has no separate mutable state; applying an
observation replaces the complete mapped strategy and clearing it reproduces
the save-only rendering.

## Localization and identity

`TargetStrategyUiText` is shared by API and Presentation mapping. English and
Traditional Chinese change display text only. Archetype, facet, goal, gap, and
counter stable codes stay in view-model identity fields and links but are never
printed when a friendly name exists. Unknown future codes receive a friendly
localized fallback instead of leaking an untranslated identifier.

## Accessibility and responsive behavior

The panel uses native headings, lists, links, and `details`/`summary`
disclosures. State labels and unresolved-gap messages are text, with `status`
or `note` semantics where appropriate. Keyboard users can follow threat,
counter, and skill-detail links in logical document order.

Layout uses bounded `minmax(0, ...)` tracks and `overflow-wrap: anywhere`.
At 900 CSS pixels the archetype/goal grid becomes one column; at 620 pixels
headers stack and every auto-fit list becomes one column. No fact is removed in
the narrow layout.

## Verification

- Presentation mapper tests cover an available verified result, stable
  bilingual identities/order, unique counter links, inaccessible gaps, and an
  unsupported version with no playbook. They also cover mapped adjustment
  evidence, feasibility reasons, and the unchanged-current-loadout result.
- Component tests cover multi-match, context/mechanics separation, linked
  threats/counters, inaccessible counters, English/Traditional Chinese copy,
  partial, unsupported, conflicting, and no-match output.
- Exact-target component tests cover all six action kinds, exact response and
  evidence links, reduced/conflicting evidence retention, missing-counter
  wording, target-versus-player separation, unchanged output, and atomic
  apply/clear rendering.
- Duplicate guards assert that this component contains no comparison, skill
  card, detailed capacity, or recommendation-policy surface and renders one
  card per unique counter.
- Architecture tests require the one page integration, native disclosures,
  threat linking, helper-local event handler, narrow CSS tracks, overflow
  wrapping, and absence of duplicated recommendation components.
- The shared page-level rendering suite already covers loading and failure
  semantics through `PageStateNotice`.
- Local browser inspection at 1440×1000 and 390×844 confirmed the running
  Chinese page shell has no horizontal overflow. The configured current save
  did not resolve the recorded representative target during this run, so the
  strategy-result matrix remains automated evidence rather than a new live
  target claim.
