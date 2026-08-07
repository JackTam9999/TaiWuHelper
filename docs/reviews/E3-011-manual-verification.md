# E3-011 workflow validation and completion audit

| Field | Value |
|---|---|
| Status | Awaiting product-owner approval |
| Evidence date | 2026-08-07 |
| Epic | [EPIC-003](../roadmap/epic-003/EPIC.md) |
| Backlog item | [E3-011](../roadmap/epic-003/BACKLOG.md#e3-011--validate-the-workflow-and-close-epic-3) |
| Positive scenario | `E3-000-CAP-002`, `切磋武功`, paired identity `霍劍嬋` |
| Unavailable scenario | `E3-000-CAP-001`, story target displaying `秘而不宣` |

## Decision boundary

The supported game UI exposes the opponent `運功` page for a `切磋武功`
opponent. It does not expose that page for hostile or story characters. The
second case is therefore an explicit unavailable observation state, never an
empty or partial loadout and never a request for the player to report hidden
information.

This distinction is implemented at every layer: the editor disables entry for
hostile/story context, the request and Domain contracts accept only sparring
observations, and tests prove that inaccessible contexts produce no selected
skills. The source evidence and exact version boundary are recorded in
[E3-000](../scenarios/E3-000-target-observation-evidence.md).

## Representative form reproduction

The new
`Complete_review_reproduces_the_E3000_sparring_loadout` presentation-state test
reconstructs the complete current loadout in `E3-000-CAP-002` through the same
typed selections used by the manual form. It confirms:

- sparring encounter context;
- complete-current-loadout coverage;
- 18 resolved stable skill identities;
- category counts of 3 inner arts, 5 attack arts, 4 agility arts, 4 defense
  arts, and 2 assistance arts; and
- optional direction remains absent when it has not been independently
  transcribed from the capture.

The captured screen visibly supports `正` and `逆`, but Epic 3 does not require
the player to report a direction. The reproduction deliberately does not
guess individual markers that were not separately recorded.

## Version-matched bilingual identity check

The Traditional Chinese card labels were compared with the exact guarded CNH
resource and their stable IDs were paired with the corresponding English
resource from the same installed GameData build. No raw display text becomes
a mechanic or a guessed identifier.

| Category | Stable ID | Traditional Chinese | English |
|---|---:|---|---|
| Inner art | 89 | 百邪體大法 | Body of Evils |
| Inner art | 73 | 九鼎功 | The Nine Tripod Dings |
| Inner art | 0 | 沛然訣 | Chant of Abundance |
| Attack | 530 | 黃墳砂 | Graveyard Powder |
| Attack | 529 | 蝕骨爛腸砂 | Bone-corroding Powder |
| Attack | 473 | 分筋錯骨手 | Wicked Clutch |
| Attack | 533 | 血犼砂 | Sanguine Powder |
| Attack | 401 | 摧心掌 | Agonizing Palm |
| Agility | 206 | 遊魂詭步 | Spectral Step |
| Agility | 204 | 狸竄術 | Wildcat Slip |
| Agility | 207 | 鬼犼入地震天法 | Hou's Heaven-shaking Dig |
| Agility | 205 | 墳頭遁 | Graveyard Escape |
| Defense | 320 | 爛泥絕技 | Splattering Mud |
| Defense | 324 | 移魂大法 | Soul Haulage |
| Defense | 322 | 鬼夜哭 | Ghostshriek |
| Defense | 257 | 五鬼搬運法 | Five Ghosts' Burden |
| Assistance | 326 | 十二血童大陣 | Blood Children Formation |
| Assistance | 321 | 亂氣殺 | Qi Disorder |

The loadout screen itself does not display the opponent identity. `霍劍嬋` is
the paired product-owner report `E3-000-ID-001`; no character ID was guessed.
The current save has advanced and no longer uniquely resolves this historical
target, so this audit does not claim a new live application against
`霍劍嬋`. Instead, it combines the recorded UI evidence, the exact bilingual
catalogue mapping, the full typed form reproduction, and E3-010's read-only
current-save vertical against a currently resolvable target.

## Coverage and lifecycle validation

| Scenario | Expected semantics | Result |
|---|---|---|
| Full visible sparring page with five category rows and capacity states | Complete current displayed loadout | Passed by E3-000 evidence and the 18-skill form reproduction |
| Cropped or incomplete sparring evidence | Partial; omitted skills remain unknown | Passed by Domain, merge, UI, threat, and impact tests |
| Hostile/story character without opponent `運功` | Unavailable; not empty or partial | Passed by Domain and editor-state tests |
| Same observation applied repeatedly | Equivalent snapshot, threats, recommendations, impact, and ordering | Passed by [E3-010](./E3-010-automated-verification.md) |
| Observation cleared | Original save-only result, with no retained session observation | Passed by E3-006 and E3-010 lifecycle tests |

## Threat and recommendation evidence

[Threat analysis](../architecture/TARGET-OBSERVATION-THREAT-ANALYSIS.md)
distinguishes observed-equipped, save-equipped, and learned-unconfirmed
sources. A partial observation can add confirmation but cannot remove an
omitted possibility. A version-matched complete observation can replace stale
equipped membership while retaining conflicts.

[Recommendation recalculation](../architecture/TARGET-OBSERVATION-RECOMMENDATION-RECALCULATION.md)
uses only typed verified threats and counters after hard feasibility checks.
[Impact explanation](../architecture/TARGET-OBSERVATION-IMPACT-EXPLANATION.md)
then reports confirmed, added, demoted, removed, unchanged, and unsupported
threats together with recommendation and feasibility changes. E3-007 through
E3-010 contain deterministic positive, unchanged, clear, and unsupported
cases.

## Epic acceptance audit

| Epic criterion | Implementation or evidence | Result |
|---|---|---|
| Versioned observable fields and completeness | E3-000 decision table and completeness rule | Pass |
| Hostile/story observation unavailable | E3-000 captures; Domain/request/editor guards | Pass |
| Stable bilingual identities | E3-003 resolver tests and the table above | Pass |
| Partial and complete coverage separated | E3-001/E3-004 models and merge tests | Pass |
| Time, reference, and provenance retained | [provenance design](../architecture/TARGET-OBSERVATION-PROVENANCE.md) | Pass |
| Stale/conflicting evidence visible and deterministic | E3-002/E3-004/E3-010 tests | Pass |
| Precedence limited to covered fields | E3-004 merger tests | Pass |
| Missing target snapshot skill remains representable | E3-003 resolver tests | Pass |
| Threat sources remain distinguishable | E3-007 and threat-analysis design | Pass |
| Recommendation impact explained | E3-008/E3-009 and impact design | Pass |
| Clear reproduces save-only behavior | E3-006/E3-010 lifecycle tests | Pass |
| Unknown raw effects cannot affect rules | E3-003/E3-007 tests | Pass |
| Observation remains session-bound | E3-006 state and E3-010 safety tests | Pass |
| No path, process, screenshot, or mutation contract | [API design](../architecture/TARGET-OBSERVATION-API.md) and architecture tests | Pass |
| Bilingual accessible UI | [form design](../architecture/TARGET-OBSERVATION-FORM.md) and rendering tests | Pass |
| Required state matrix automated | E3-001 through E3-010 test evidence | Pass |
| Local sources remain byte-for-byte unchanged | E3-000 and E3-010 fingerprints | Pass |
| Product-owner completion decision | This review | Pending |

## Verification commands

Focused editor-state verification:

```powershell
dotnet build tests/TaiWu.API.UnitTests/TaiWu.API.UnitTests.csproj `
  -c Release --no-restore
& tests/TaiWu.API.UnitTests/bin/Release/net10.0/TaiWu.API.UnitTests.exe `
  -class TaiWu.API.UnitTests.Presentation.TargetObservationEditorStateTests `
  -noColor
```

Result: **11 passed; 0 failed; 0 skipped**. Build completed with zero warnings
and zero errors.

Full verification:

```powershell
dotnet build TaiWu.slnx -c Release --no-restore
dotnet test TaiWu.slnx -c Release --no-build --no-restore
```

Result: **833 total; 826 passed; 0 failed; 7 expected opt-in skips**. The
Release build completed with zero warnings and zero errors.

## Explicit future work

The following are not partially implemented in Epic 3:

- screenshot capture, upload, OCR, or assisted image interpretation;
- persisted observation history, retention, deletion, and freshness
  governance;
- hidden-data inspection for hostile or story characters; and
- game input, process, memory, save, or configuration control.

Screenshot assistance requires a separate privacy and accuracy review.
Observation history requires an explicit storage and lifecycle design.

## Product-owner decision

Pending. All technical and evidence criteria are satisfied, subject to the
documented historical-target caveat. The product owner must now approve or
reject Epic 3 completion before the final checkbox and Epic status are closed.
