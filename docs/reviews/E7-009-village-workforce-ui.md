# E7-009 village-workforce UI verification

| Field | Value |
|---|---|
| Status | Complete — final manual visual confirmation scheduled in E7-011 |
| Evidence date | 2026-08-18 |
| Epic | [EPIC-007](../roadmap/epic-007/EPIC.md) |
| Backlog item | [E7-009](../roadmap/epic-007/BACKLOG.md#e7-009--deliver-the-bilingual-accessible-village-workforce-ui) |
| UI contract | [UI-007](../roadmap/epic-007/UI-007-village-workforce-planner.md) |

## Delivered surface

The `/village-workforce` route now provides one information-only decision
surface for an occupied shop-manager position. It shows the fixed verified
objective before the target control, performs evaluation only after the
explicit `Inspect position` action, and replaces the active result atomically.

The result places its selected target, stable-snapshot state, rule version,
qualification meaning, source boundary, and shared limitations once above the
worker list. The current assignment is summarized before alternatives. Each
worker row retains only rank, localized display name, exact value or typed
unavailable state, evaluation state, decisive evidence, and comparison control.
Requirements, the single verified component, and redacted provenance live in
one closed native disclosure per worker.

## Identity and repetition controls

The post-review display projection provides optional Traditional Chinese and
English worker names/locations plus building/location/discipline labels. These
values are display-only and cannot enter rule selection, scoring, ordering, or
fingerprints. When a localized value is unavailable, the page falls back to a
localized ordinal label and never renders raw stable references, fingerprints,
local paths, or structural IDs. The name filter operates on the real displayed
worker name.

Shared formula, evidence, and information-only wording is not copied into each
row. The current worker appears once in the current-assignment summary. The
alternative list omits that duplicate row; choosing an alternative pairs it
with the current worker for comparison.

## Interaction and accessibility

The page uses native select, radio, checkbox, button, table, and details
semantics. Shop and manager position are separate selectors, and the position
label includes its current worker. Explicit inspection moves focus to the new
result heading or safe error summary. The initial result renders ten comparable
alternatives; explicit expansion and every other large filter use 25-row pages.
Filter changes announce visible/full counts without changing immutable counts
or ranks. A third comparison checkbox is disabled after two selections, with
visible explanatory text.

Six-attribute, martial-aptitude, and life-skill-aptitude averages render as
descriptive saved context with 6/6, 14/14, and 16/16 coverage. They are excluded
from the verified workforce score, ordering, result identity, and fingerprint.

Filtering, name queries, comparison, language remapping, and responsive reflow
use the already-authoritative immutable result. They cannot call the snapshot
reader or Application finder. A current/alternative comparison may produce a
static manual checklist, but the page contains no reassignment or game action.

At the 960 CSS-pixel container boundary, the same semantic candidate and
comparison tables reflow to labelled cards. No desktop/mobile result tree is
duplicated. At 620 pixels, controls and actions use a single-column layout.

## Automated and runtime evidence

Focused Release tests cover:

- English and Traditional Chinese result facts;
- ranked, tied, current-only, incomplete, unsupported, conflicting, and
  ineligible states with visible non-color labels;
- compact closed evidence disclosures and shared-text non-repetition;
- local filtering, a two-worker comparison, comparison limits, and a manual
  checklist without another source read;
- previous-result inertness and safe bilingual failure guidance;
- initial target discovery without worker evaluation;
- raw-ID and structural-target-label hiding; and
- route, navigation, single-DOM responsive CSS, and forbidden-capability
  architecture checks.

The Release solution build passes with zero warnings and zero errors. The live
localhost route returned HTTP 200 and completed the configured target discovery
read. The read remained bounded to the existing immutable archive session.
The full default Release matrix passed 1,398 tests, skipped 15 explicitly
environment-gated integrations, and failed none.

The in-app browser runtime could not initialize in this workspace because its
bundled RPC dependency was rejected by the local trusted-code-path check. No
alternate browser automation was substituted. E7-011 therefore retains the
final wide English and narrow Traditional Chinese visual confirmation, using
synthetic data only; the rendering and semantic parity gates are already
automated here.

## Safety conclusion

E7-009 adds no persistence, save writing, process access, network/game control,
upload, screenshot, automation, or input-control path. The page remains a
read-only planner whose optional checklist tells the player to verify and make
any desired change manually in the game.
