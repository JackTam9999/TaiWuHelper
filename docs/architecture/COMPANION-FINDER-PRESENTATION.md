# Companion finder Presentation architecture

| Field | Value |
|---|---|
| Status | Implemented for E6-010 |
| Epic | [EPIC-006](../roadmap/epic-006/EPIC.md) |
| Backlog item | [E6-010](../roadmap/epic-006/BACKLOG.md#e6-010--deliver-the-bilingual-accessible-companion-finder-ui) |
| UI contract | [UI-006 companion finder](../roadmap/epic-006/UI-006-companion-candidate-finder.md) |
| Application source | [Companion finder Application](./COMPANION-FINDER-APPLICATION.md) |
| API mapping | [Companion candidates API](../api/COMPANION-CANDIDATES.md) |

## Purpose and boundary

The `/companions` Blazor page presents one information-only workflow over the
immutable E6-008 finder result. The player selects one martial or life-skill
discipline, explicitly starts one read, inspects every candidate state, filters
the existing shortlist, and optionally compares two entries from that same
result.

Presentation cannot recruit, dismiss, train, move, equip, assign, persist,
upload, export, automate input, control a process, or mutate the game. The page
does not accept a save path or character ID and never reads a save on page load,
role change, discipline change, filter, name query, comparison, language
change, or responsive reflow.

## One evaluation path

`CompanionFinderViewModelMapper` maps the exact authoritative
`CompanionFinderResult`. It first uses the existing API response mapper so UI
and API expose the same typed states and localized evidence, then shapes those
values for semantic Razor markup. It does not call a role evaluator, ranking
builder, shortlist builder, merit comparer, source reader, or catalogue
repository.

Comparison is the only additional projection. It calls
`CompanionRoleComparisonBuilder.Compare` with the two exact entries already
owned by the immutable shortlist. It neither rereads a source nor reconstructs
a score, rank, gate, component, or evidence state. Unavailable or conflicting
values remain non-numeric and produce no invented difference.

## Read and interaction lifecycle

Page initialization reads only the installed bilingual discipline labels. The
save-reading use case is behind the enabled `Find candidates` button and is
called once per explicit submission. A changed draft marks an existing result
as `Previous result` and makes it inert; the old result never appears current
under a different objective.

The helper session owns:

- draft role and discipline;
- active status filter and localized-name query;
- at most two selected comparison identities; and
- focus targets for success, failure, and comparison clearing.

This state is in memory only. It does not enter a finder fingerprint or cross a
persistence boundary. Language changes remap the retained result and bilingual
display descriptors without another source call. Filters change only visible
references and leave unfiltered counts, scores, ranks, ties, evidence, and the
authoritative fingerprint unchanged.

## Display data and localization

All fixed UI copy is keyed by `CompanionFinderUiTextKey` and has complete
English and Traditional Chinese values. Discipline, candidate, and location
names come from bilingual descriptors captured by typed read-only sources.
Presentation uses localized unavailable labels when text is absent and never
prints a raw character ID, discipline index, stable code, source path, or
exception message.

Display descriptors are outside `CandidateProfile` and every semantic
fingerprint. They cannot decide candidate inclusion, eligibility, score, rank,
tie order, comparison, or evidence state.

## State and accessibility model

The route renders explicit states for initial selection, loading, complete,
partial, empty, cancellation, configured-save absence, unsupported source,
changed revision, and safe read failure. Candidate sections retain ranked,
tied, incomplete, unsupported, conflicting, and ineligible labels with visible
reasons. Missing scores render `Unavailable`, never zero or blank.

The page uses native radios, select, text input, checkboxes, buttons, and
details. Result tables use scoped column and row headers. The same DOM changes
to heading-led cards below the 960-pixel finder-container boundary; there is no
duplicated desktop/mobile content. Live regions announce loading, visible
counts, and comparison readiness. An explicit request focuses the result or
error heading, while clearing a comparison returns focus to the first prior
selection when it remains visible.

Every status has required text, so color and optional symbols are only
supplementary. Names, reasons, and bilingual labels wrap at arbitrary points.
The 620-pixel control layout stacks into one column without whole-page
horizontal overflow.

## Verification

Presentation mapper tests cover bilingual role and discipline mapping, every
candidate and source state, score warnings, counts, filtering, comparison,
missing display text, and ID hiding. Rendered-component tests cover native
semantics, focus targets, state copy, tie cues, responsive single-DOM markup,
and absent mutation actions. Architecture tests enforce the route, navigation,
one explicit read action, no second evaluation path, no raw-ID rendering, and
no persistence, process, screenshot, upload, input, or game-control capability.

The [E6-010 browser review](../reviews/E6-010-companion-finder-ui.md) records
the live route check plus synthetic English desktop and Traditional Chinese
narrow captures. Synthetic fixture values are review-only and contain no real
save identity or content.
