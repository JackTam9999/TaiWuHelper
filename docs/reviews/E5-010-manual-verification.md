# E5-010 manual target-strategy verification

| Field | Value |
|---|---|
| Status | Superseded — pre-remediation evidence |
| Evidence date | 2026-08-10 |
| Epic | [EPIC-005](../roadmap/epic-005/EPIC.md) |
| Backlog item | [E5-010](../roadmap/epic-005/BACKLOG.md#e5-010--verify-archetype-reuse-safety-and-determinism) |

> Historical record: this review correctly observed that the two matched new
> families produced zero counter options. The later readiness conclusion was
> reopened by independent review. See the
> [E5-011 remediation review](./E5-011-playbook-remediation.md).

## Scope and safety boundary

The Release application was started locally with the current save configured
only in the server process. The visible recommendation workflow and read-only
API were exercised without writing a configuration file, observation history,
save, GameData file, language resource, or screenshot artifact.

The representative is recorded only as `E5-REP-LOCAL-MULTI-001`. Its character
identity, machine path, source fingerprints, save content, and proprietary data
are intentionally absent from committed evidence. The separate
[automated verification](./E5-010-automated-verification.md) supplies guarded
before/after fingerprints.

## Traditional Chinese desktop workflow

At the default 1280 by 720 CSS-pixel viewport, the representative current-save
target produced a successful recommendation with warnings and one grouped
target-strategy panel.

Verified visible behavior:

- attack-family context appeared separately from verified combat mechanics;
- configured outer damage and configured poison both matched, while channel
  resistance and mind-resonance/reset remained visibly unsupported;
- the two matched archetypes were composed into one reusable strategy, not two
  recommendation results;
- two reusable response goals appeared with explicit no-verified-counter gaps;
- exact-target customization followed the reusable goals and showed two
  `Unresolved` plus two `Retained` decisions with non-colour text labels;
- unresolved responses explicitly said they were not completed mitigation;
- player feasibility appeared afterward and honestly reported that no verified
  counter entered feasibility filtering;
- evidence disclosures and links targeted the existing profile, match, goal,
  gap, and response details;
- the strategy section did not repeat full skill cards, warning cards, the
  manual checklist, or comparison rows; and
- the page retained one policy control, one strategy panel, one adjustment
  section, and one feasibility section.

Desktop layout metrics reported equal document scroll and client widths, so
the page introduced no horizontal viewport overflow.

## Narrow responsive workflow

At a 390 by 844 CSS-pixel viewport, the same result stacked profile facets,
archetype states, response goals, adjustment cards, and feasibility in the
same semantic order. The content viewport was 375 CSS pixels and, after the
review fix, both `scrollWidth` and `clientWidth` were 375.

The review found and corrected two whole-page containment defects:

- a long unavailable-data warning could widen its grid item; and
- English navigation labels retained their intrinsic width instead of sharing
  the mobile navigation row.

The final narrow rerun preserved internal horizontal scrolling only for the
existing comparison tables. Warning text wrapped inside its card, all four
navigation items shared the row, and the document itself did not overflow.

## English workflow

The language switch preserved the selected target and produced the same typed
strategy in English. The panel headings were:

1. `Reusable response strategy`;
2. `Attack-family context`;
3. `Verified combat mechanics`;
4. `Target patterns checked`;
5. `Reusable response goals`;
6. `Exact-target customization`; and
7. `Player feasibility`.

The four decisions rendered as `Unresolved`, `Unresolved`, `Retained`, and
`Retained`. Stable identity codes stayed out of the primary copy where display
text existed. The English 390 by 844 result also reported equal document and
client widths and exactly one strategy, adjustment, and feasibility section.

## Accessibility and duplication review

- Native landmarks, headings, lists, links, buttons, checkboxes, status, note,
  and disclosure semantics appeared in the accessibility snapshot.
- Matched, unsupported, unresolved, and retained states used explicit text and
  did not depend on colour.
- The narrow workflow preserved the same facts and order as desktop.
- The page did not introduce a third policy option or another recommendation
  selector.
- Full loadout, warning, checklist, battle-plan, and comparison content remained
  owned by their existing sections.
- The responsive fix changed containment only; it did not alter evidence,
  matching, playbook, adjustment, feasibility, scoring, or recommendation
  behavior.

## Local representative coverage

The production snapshot exercised the outer-damage and poison families
together. Sampled current-save targets did not establish the required channel-
resistance values or the verified mind-resonance/reset chain, so those families
were recorded as unsupported locally rather than claimed from names or nearby
effects. Their complete behavior remains covered by the synthetic matrix.

## Browser health and cleanup

The local server initially ran from the wrong content root, so the Razor root
returned an empty 500 response while API routes remained healthy. It was
restarted through the project entry point, after which the page returned 200
and the UI workflow completed normally. The temporary viewport override was
reset, the automation tab was released, and the local server was stopped.

## Product-owner decision

The UI workflow passed, but its zero-counter product result was not sufficient
for completion. This pre-remediation conclusion is superseded by E5-011.
