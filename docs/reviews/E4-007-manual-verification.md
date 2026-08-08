# E4-007 manual comparison verification

| Field | Value |
|---|---|
| Status | Passed — two-option trial awaiting product-owner review |
| Evidence date | 2026-08-09 |
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
- the form exposed Safe and Aggressive inputs, while one result-level button
  group controlled the comparison, checklist, and plan;
- each policy header linked to the existing setup checklist and battle plan;
- all five category groups, skill membership/actions, capacity, unavailable
  reasons, 萬用 allocation, and Current provenance were visible;
- only the selected policy's tactical card was present;
- duplicate alternative, condition, and score disclosures were removed because
  those facts already appear in the policy control and comparison;
- threat/risk, requirement/caveat, unsupported-mechanic, evidence-count, and
  policy-local score boundaries rendered in Traditional Chinese; and
- unresolved facts remained outside differences-only filtering; and
- no Balanced/均衡 label appeared anywhere in the rendered page.

## Narrow responsive workflow

At a 760 by 900 CSS-pixel viewport, the same loaded result showed Current plus
the selected Safe policy. Aggressive remained available in the shared policy
button group, but its table column was removed from the visual and accessibility
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
- Safe/Aggressive-only policy buttons with no Balanced text; and
- the existing threat focus when that typed threat remains in the localized
  result.

English headings, policy labels, status announcements, tactical boundaries,
and the information-only notice rendered after the switch. The selected policy
and row filter therefore no longer change as a side effect of language.

## Browser health and cleanup

The local server was deliberately stopped and rebuilt once to apply the state
fix. The tab logged the expected WebSocket disconnect during that interval.
After reload, the final log tail contained successful WebSocket connections
and no later error. The temporary narrow viewport override was reset, the tab
was closed, and the local server was stopped after verification.

## Result

## Whole-page duplication review

The 2026-08-09 review used the same current-save target to inspect the entire
14,300-CSS-pixel result, not only the matrix. The simplified page:

- keeps one detailed information-only notice instead of repeating result and
  matrix badges;
- keeps one result-level policy control instead of tabs plus a select;
- groups matching warning cards while leaving every distinct warning message
  visible;
- groups provenance fields with the same source and capture time;
- renders only the selected tactical card;
- collapses the 25 detailed skill cards because the matrix already shows the
  active loadout and manual actions;
- omits the empty target-threat panel;
- renders one empty battle-plan message instead of five identical phase rows;
  and
- keeps only supporting disclosures not already present in the comparison.

Before the final warning grouping, the live result was approximately 9,200 CSS
pixels, a reduction of about 36%. The final warning-only live reread was blocked
when the running game locked the save, so that last presentation step was
verified by component and architecture tests without bypassing the lock.

Desktop and narrow workflows expose equivalent typed comparison facts in
Traditional Chinese and English. Keyboard-native buttons retain
their expected roles, the layout does not depend on color alone, and every
action remains helper-local or navigational. The review build intentionally
shows only Safe and Aggressive without changing scoring or manufacturing a
different lower-ranked winner. Manual verification passed; the product owner
will review the two-option trial before recording the Epic completion decision.
