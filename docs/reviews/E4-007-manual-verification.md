# E4-007 manual comparison verification

| Field | Value |
|---|---|
| Status | Passed — two-option trial awaiting product-owner review |
| Evidence date | 2026-08-08 |
| Epic | [EPIC-004](../roadmap/epic-004/EPIC.md) |
| Backlog item | [E4-007](../roadmap/epic-004/BACKLOG.md#e4-007--verify-comparison-safety-parity-and-determinism) |

## Scope and boundary

The Release application was started locally with the current save configured
only in the server process. The workflow used the visible UI and read-only API
path. It did not write a configuration file, save, GameData file, language
resource, screenshot, or observation history.

The separate guarded vertical in
[automated verification](./E4-007-automated-verification.md) supplies the
before/after source fingerprints.

## Traditional Chinese desktop workflow

A uniquely selected current-save regular target produced Current, Safe, and
Aggressive player-facing comparison columns from one successful recommendation
with warnings. The immutable backend result continued to contain the Balanced
policy for API compatibility; the two-option product trial projected it out of
the UI.

Verified visible behavior:

- the persistent information-only notice remained above the result;
- the three visible desktop columns appeared in Current, Safe, Aggressive
  order;
- the recommendation form, style tabs, and comparison selector each exposed
  exactly Safe and Aggressive;
- each policy header linked to the existing setup checklist and battle plan;
- all five category groups, skill membership/actions, capacity, unavailable
  reasons, 萬用 allocation, and Current provenance were visible;
- exactly two policy-local tactical cards were present;
- supporting details exposed one alternative to the selected policy;
- threat/risk, requirement/caveat, unsupported-mechanic, evidence-count, and
  policy-local score boundaries rendered in Traditional Chinese; and
- unresolved facts remained outside differences-only filtering; and
- no Balanced/均衡 label appeared anywhere in the rendered page.

## Narrow responsive workflow

At a 760 by 900 CSS-pixel viewport, the same loaded result showed Current plus
the selected Safe policy. Aggressive was present in the native selector but its
table column and tactical card were removed from the visual and accessibility
layout. Category navigation, differences-only control, live row-count status,
and unresolved-risk content remained visible and operable.

Selecting Aggressive changed the narrow view to Current plus Aggressive,
exposed only the Aggressive tactical card, and kept Balanced absent. No
recommendation reread was required for the policy interaction.

## Language-only interaction regression

An earlier live Chinese-to-English switch exposed a state defect: it returned
the narrow comparison to its default policy and all rows. E4-007 moved filter
ownership to the page and added explicit policy, threat-focus, and filter
restoration after the localized reread.

The corrected Release rerun preserved:

- selected policy `Aggressive`;
- differences-only mode and its row-count status;
- Current plus Aggressive narrow column visibility;
- Aggressive-only tactical-card visibility;
- Safe/Aggressive-only selector options with no Balanced text; and
- the existing threat focus when that typed threat remains in the localized
  result.

English headings, selector labels, status announcements, tactical boundaries,
and the information-only notice rendered after the switch. The selected policy
and row filter therefore no longer change as a side effect of language.

## Browser health and cleanup

The local server was deliberately stopped and rebuilt once to apply the state
fix. The tab logged the expected WebSocket disconnect during that interval.
After reload, the final log tail contained successful WebSocket connections
and no later error. The temporary narrow viewport override was reset, the tab
was closed, and the local server was stopped after verification.

## Result

Desktop and narrow workflows expose equivalent typed comparison facts in
Traditional Chinese and English. Keyboard-native selectors and buttons retain
their expected roles, the layout does not depend on color alone, and every
action remains helper-local or navigational. The review build intentionally
shows only Safe and Aggressive without changing scoring or manufacturing a
different lower-ranked winner. Manual verification passed; the product owner
will review the two-option trial before recording the Epic completion decision.
