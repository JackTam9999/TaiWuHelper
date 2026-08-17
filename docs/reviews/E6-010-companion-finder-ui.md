# E6-010 companion finder UI verification

| Field | Value |
|---|---|
| Status | Complete |
| Evidence date | 2026-08-17 |
| Epic | [EPIC-006](../roadmap/epic-006/EPIC.md) |
| Backlog items | [E6-010](../roadmap/epic-006/BACKLOG.md#e6-010--deliver-the-bilingual-accessible-companion-finder-ui), [E6-013](../roadmap/epic-006/BACKLOG.md#e6-013--add-a-transparent-companion-capability-overview) |
| UI contract | [UI-006](../roadmap/epic-006/UI-006-companion-candidate-finder.md) |

## Scope and data hygiene

The live localhost `/companions` route was inspected only before its explicit
save-reading action. The route loaded installed bilingual discipline labels but
did not read or display candidate save content. The result review used the
committed [synthetic fixture](./fixtures/E6-010-companion-finder.html), whose
banner states that every name, location, value, timestamp, and rank is
invented.

No real character identity, candidate value, save fingerprint, source
fingerprint, proprietary source text, or local machine path is present in this
report or its captures.

> **Closure correction:** the PNG captures below predate the independent E6-012
> reviews and retain the former rankability-based eligible tile plus the former
> living-only boundary wording. They also predate the compact candidate-evidence
> disclosure. They remain historical layout evidence only.
> The corrected synthetic fixture reports all four candidate-universe-eligible
> profiles and describes the full saved non-Taiwu roster universe, while current
> mapper and Razor rendering tests prove exact eligibility, evaluation/gate
> semantics, typed partial/catalogue states, and the separate capability
> overview added by E6-013.

## Live route verification

The live page returned its expected title and `/companions` route. Its initial
accessibility snapshot exposed:

- skip link, banner, primary navigation, language group, and main landmark;
- information-only and candidate-universe-boundary notes;
- one labelled role-family group with two native radios;
- one native labelled discipline select; and
- one disabled native Find button with visible enabling guidance.

Selecting the martial role did not read the save. It enabled the select and
showed the 14 installed localized martial disciplines in stable type order,
with raw type indexes absent from visible labels. The browser console recorded
no errors. At the live page's 1,280-pixel viewport, document scroll and client
widths were equal, so the initial route introduced no whole-page horizontal
overflow.

## Synthetic English desktop result

At 1,440 by 900 CSS pixels, the result shell used wide semantic tables and
showed the selected role, discipline, current-snapshot label, exact score
limitation, all nine unfiltered counts, filters, tied rows, unavailable state,
and two-candidate comparison without a universal-quality claim.

The inspected synthetic document contained six native radios, four native
comparison checkboxes, three semantic tables, and the expected heading order.
Its document scroll width equalled its client width. Visible tie text and an
accessible shared-rank label supplemented the numeric competition rank.

A post-review compactness pass keeps the comparison row visible while moving
each candidate's strengths, limitations, and exact typed gates into a native
`details` disclosure that is closed by default. The visible summary gives only
the passed/total requirement count. The role-wide score limitation appears once
above the table instead of repeating for every ranked candidate. At the desktop
breakpoint, the three-candidate Traditional Chinese ranked table measured 384
CSS pixels high, with a 1,425-pixel client width matching its scroll width.
Opening and closing the English example exposed the complete evidence without
changing the candidate comparison facts.

![Synthetic English desktop result](./assets/epic-006/companion-finder-en-desktop.png)

## Synthetic Traditional Chinese narrow result

At 390 by 844 CSS pixels, the content viewport was 375 pixels and both
`scrollWidth` and `clientWidth` were 375. The same result used the accepted
Traditional Chinese terminology, retained the score warning and all counts,
and exposed 30 mobile fact labels from the same DOM tables. No duplicated
desktop/mobile candidate tree was introduced.

![Synthetic Traditional Chinese narrow result](./assets/epic-006/companion-finder-zh-narrow.png)

The candidate section reflowed each table row into a heading-led card. Rank,
candidate, location, saved base qualification, explicit state, evidence, and
comparison control remained visible in their original DOM order; long labels
wrapped without widening the document.

![Synthetic Traditional Chinese candidate cards](./assets/epic-006/companion-finder-zh-narrow-candidates.png)

## E6-013 capability-overview review

The corrected synthetic fixture now places a separate capability table before
the existing role-evidence table. At the default 1,280 by 720 viewport, its
breadth index, six-attribute average, martial-aptitude average, life-skill-
aptitude average, exact 6/6, 14/14, and 16/16 coverage, and top-three values
were readable in aligned columns. The equal-weight saved-base limitation was
visible directly above the table, with no winner color, grade, or universal-
recommendation language.

At 390 by 844 CSS pixels, the content viewport and document scroll width were
both 375 pixels. The same five-row semantic table reflowed into labelled cards;
Traditional Chinese values and top-value details wrapped without clipping or
horizontal overflow. The following role evidence remained in its own table.
The browser console contained no errors.

## Interaction and state guarantees

Automated Presentation tests exercise loading, cancellation, configured-save
absence, changed revision, unsupported source, partial, empty, ranked, tied,
ineligible, incomplete, unsupported, stale, and conflicting states. They also
prove that filters preserve unfiltered counts and ranks, two selections use the
existing immutable shortlist, unavailable comparison values have no numeric
difference, and language remapping preserves stable identities.

Capability tests additionally prove formula/version transparency, complete-
category requirements, explicit unavailable states, bilingual labels, top-
three values, raw-ID hiding, and that the overview cannot change role score,
rank, order, or comparison outcome.

Rendered semantic tests verify native controls, scoped headers, accessible
names, live regions, focus targets, visible non-color cues, previous-result
inertness, complete bilingual copy, responsive single-DOM classes, and raw-ID
hiding. Architecture checks forbid a Presentation evaluation path and any
save-write, persistence, process, screenshot, upload, input-automation, or
game-control dependency.

The closure correction additionally renders the production Razor result for
every supported snapshot/enrichment/catalogue state and checks distinct
bilingual recovery guidance. It also proves that universe eligibility remains
independent of rankability in both API and Presentation summaries.

## Automated result

The current post-review non-integration matrix passed 1,248 tests:

| Project | Passed |
|---|---:|
| Domain | 496 |
| Application | 178 |
| Infrastructure | 146 |
| Architecture | 95 |
| API and Presentation | 333 |

The complete solution build passed with zero warnings and zero errors.
Formatter verification, Markdown link validation, leak/safety scans, and
`git diff --check` also passed before the backlog commit.

## Conclusion

E6-010 satisfies the accepted UI contract. The page is bilingual,
keyboard-native, evidence-preserving, responsive without fact duplication,
compact by default with full candidate evidence available on demand, and
information-only. It delegates the only save read to the existing
Application use case and does not introduce a second evaluation or mutation
path.
