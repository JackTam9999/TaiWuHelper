# E3-000: Target-observation evidence

| Field | Value |
|---|---|
| Status | In progress — reopened by combat-tooltip evidence |
| Epic | [EPIC-003](../roadmap/epic-003/EPIC.md) |
| Backlog item | [E3-000](../roadmap/epic-003/BACKLOG.md#e3-000--verify-observable-target-loadout-fields) |
| Inspection date | 2026-08-08 |
| GameData product version | `1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a` |
| Language resource | Traditional Chinese (`CNH`), version-bound to the guarded installed resource and GameData build |

## Purpose

Establish exactly which target combat-skill facts the player can reliably
report from the supported game UI. In particular, determine whether the
visible target `運功` screen proves a complete equipped loadout or only a
partial set.

No observation field enters the Epic 3 Domain contract until its visibility,
coverage, identity, and freshness semantics are supported by version-matched
evidence.

## Existing evidence

The following facts were already established by Epic 1 and the read-only
E3-000 inspection:

| Fact | Evidence | Current conclusion |
|---|---|---|
| Prior golden target | Character `16317`, player-visible description `樂器奇書（52歲）` | Retained as historical save evidence; the product owner reports that this encounter is complete and the book has been claimed, so it is no longer the capture scenario |
| Shaolin candidate target | The `少林` story encounter's `16精純老僧` | Rejected as a complete-loadout scenario because `E3-000-CAP-001` shows `秘而不宣`; later combat-tooltip captures prove that some current combat facts can still be observed |
| Prior target loadout | The configured save has no equipped-skill list for target `16317` | Save data cannot prove that target's current equipped skills and is not required for the replacement scenario |
| Prior target learned skills | Target `16317` has versioned learned combat-skill data | Useful fallback evidence, but learned does not mean equipped |
| Combat UI entry | Installed key `LK_HotKeyGroup_Combat_ViewEnemyCharacter` is localized as `查看對方人物` | The game exposes an ordinary player action for inspecting the opponent during combat |
| Character menu entry | Installed key `LK_HotKeyGroup_Map_ShowCharacterMenuEquipCombatSkill` is localized as `打開人物菜單【運功】頁簽` | `運功` is the relevant character-menu page |
| Existing screenshot evidence | The repository previously recorded player-loadout and live-battle captures but no target `運功` capture | Superseded by local-only captures `E3-000-CAP-001` and `E3-000-CAP-002` below |

The installed GameData assembly, shared configuration assembly, Traditional
Chinese UI language file, and configured save were SHA-256 fingerprinted
before and after the read-only E3-000 inspection. All four fingerprints were
unchanged. Machine-specific paths and fingerprint values are not committed.

## Captured evidence

### E3-000-CAP-001 — inaccessible Shaolin story target

| Field | Value |
|---|---|
| Product-owner target description | `少林劇情的16精純老僧` |
| Attachment timestamp (UTC) | `2026-08-07T20:26:10.9350190Z` |
| Opaque evidence reference | `E3-000-CAP-001` |
| PNG SHA-256 | `5E280B3037ED387C9B0058AC8D05E9E0B765B8AF7FDD757A259478BB9AC2C396` |
| Repository distribution | Local-only; image not committed |
| Visible result | The opponent-information surface displays `秘而不宣` and exposes no character details or `運功` page |

This capture is negative evidence, not an empty-loadout observation. It proves
that the representative story target denies observation at the
character-information boundary. Epic 3 must represent this state as
unavailable or unsupported and must never interpret it as zero equipped
skills, empty slots, or a complete loadout. The capture does not expose enough
identity detail to resolve a stable character ID, so its target identity
remains the product-owner-provided scenario description.

### E3-000-ACCESS-001 — encounter access boundary

| Field | Value |
|---|---|
| Product-owner report time (UTC) | `2026-08-07T20:39:31.8552837Z` |
| Opaque evidence reference | `E3-000-ACCESS-001` |
| Reported rule | Opponent `運功` is visible for `切磋武功`; hostile and story characters do not expose the opponent `運功` page |
| Representative corroboration | `E3-000-CAP-001` is an inaccessible story target; `E3-000-CAP-002` is an accessible sparring target |

This access distinction limits complete-loadout observation. A manual current-
screen loadout observation can be collected for a sparring opponent. A hostile
or story target cannot supply complete-loadout or absence evidence through the
`運功` page, but `E3-000-CAP-003` through `E3-000-CAP-005` now prove that the
combat UI may still expose partial current facts. The helper must distinguish
"full loadout unavailable" from "no observable target evidence."

### E3-000-CAP-002 — accessible sparring-opponent loadout

| Field | Value |
|---|---|
| Product-owner scenario description | A `切磋武功` opponent identified as `霍劍嬋` by paired report `E3-000-ID-001` |
| Attachment timestamp (UTC) | `2026-08-07T20:30:07.8305545Z` |
| Opaque evidence reference | `E3-000-CAP-002` |
| Image dimensions | `1409 × 729` |
| PNG SHA-256 | `82FE20CA3FD99F15F5A33EDB86452424912D59356944FE00D95DA55CACC7C4E3` |
| Repository distribution | Local-only; image not committed |
| Visible result | An accessible opponent `運功` screen with all five category rows, equipped skill cards, category capacity counters, a locked slot, skill power, and visible `正`/`逆` markers |

The capture shows `內功 6/6`, `摧破 8/8`, `輕靈 5/5`, `護體 5/6`,
and `奇竅 4/4` together on one screen. Equipped cards are contained within
their category rows, and no paging, scrolling, collapsed-section, or preset
control is visible. The explicit locked tile in the `護體` row distinguishes
an unavailable slot from an omitted off-screen card. The screen therefore
supports a complete observation of the opponent's current displayed combat
loadout for this UI version; it does not support claims about other presets.

The screen itself shows no opponent name, age, location, or stable character
ID. The product owner supplied `霍劍嬋` as the identity of the same sparring
opponent at `2026-08-07T20:35:54.2830509Z`; this paired manual statement is
recorded as `E3-000-ID-001`. A stable character ID was not guessed. The product
flow must resolve and explicitly confirm the reported name through the target
lookup before constructing the typed observation.

### E3-000-CAP-003 — battle-visible inner-art effects

| Field | Value |
|---|---|
| Product-owner report | Some information remains visible when the opponent `運功` page is unavailable |
| Attachment timestamp (UTC) | `2026-08-08T00:21:27.0904234Z` |
| Opaque evidence reference | `E3-000-CAP-003` |
| Image dimensions | `527 × 503` |
| PNG SHA-256 | `DBFCAEBDB287D615E18A60DB7320F114C14F67A223469418EC38F397FA8A166D` |
| Repository distribution | Local-only; image not committed |
| Visible result | A pinned `內功` information panel with three skill names, current power percentages, and active effect text |

The exact guarded bilingual catalogue resolves every visible name uniquely.
Each displayed effect text matches the skill's versioned reverse effect, so
direction is supported by the text rather than inferred from color alone.

| Stable ID | Traditional Chinese | English | Power | Verified visible effect |
|---:|---|---|---:|---|
| 71 | 柴山青囊訣 | Mt. Chai's Geomancy | 146% | Reverse effect 789 |
| 64 | 彰施乃服篇 | Color's Inspiration | 129% | Reverse effect 783 |
| 65 | 五金佳兵篇 | Sword's Making | 127% | Reverse effect 784 |

### E3-000-CAP-004 — battle-visible special-skill effects

| Field | Value |
|---|---|
| Product-owner report | Same partial-information scenario as `E3-000-CAP-003` |
| Attachment timestamp (UTC) | `2026-08-08T00:21:43.8861968Z` |
| Opaque evidence reference | `E3-000-CAP-004` |
| Image dimensions | `534 × 438` |
| PNG SHA-256 | `79AF2216102BF00F26438855AACE37EB8D3B370A918F39DEB853AF91F3DB6F87` |
| Repository distribution | Local-only; image not committed |
| Visible result | A pinned `絕技` information panel with three skill names, current power percentages, and active effect text |

| Stable ID | Traditional Chinese | English | Power | Verified visible effect |
|---:|---|---|---:|---|
| 279 | 四指青膏 | Four Fingered Green Paste | 162% | Reverse effect 903 |
| 275 | 神機陣 | Contrivance Formation | 142% | Reverse effect 899 |
| 274 | 辟甲真鋼十四訣 | Steel Rending | 142% | Reverse effect 898 |

These panels establish six visible active skill effects. They do not show
every loadout category, slot, empty position, preset, or hidden continuation.
They therefore support only partial battle-visible evidence and cannot prove
that an omitted skill is absent. Until GameData behavior separately proves
that every such panel entry must be equipped, the evidence kind must remain
"visible active skill effect" rather than silently becoming complete equipped
membership.

### E3-000-CAP-005 — cropped battle indicators

| Field | Value |
|---|---|
| Product-owner report | Additional indicators are visible in the same combat UI |
| Attachment timestamp (UTC) | `2026-08-08T00:22:54.2403856Z` |
| Opaque evidence reference | `E3-000-CAP-005` |
| Image dimensions | `149 × 118` |
| PNG SHA-256 | `D48761236D15E4701FFD35326873153B76CDD64858625358F5E9B33CE385879C` |
| Repository distribution | Local-only; image not committed |
| Visible result | Four colored indicators showing `2, 2, 3, 3` on each of two rows, plus three status icons and a visible `1` count |

The crop contains no labels, target identity, surrounding control, or tooltip.
The numeric values and icons are observable, but their exact fields and the
meaning of the two rows remain unsupported. Epic 3 must not label or score
them until a wider labeled capture or matching UI tooltip verifies their
semantics.

## Observable-field decision table

| Candidate field | Current status | Required evidence |
|---|---|---|
| Target identity | Available only as paired manual context | `E3-000-CAP-002` contains no identity; `E3-000-ID-001` reports `霍劍嬋`, which must be resolved and confirmed before typed observation construction |
| Character-information accessibility | Context-dependent | Sparring exposes the complete `運功` page; hostile/story denies that page but may expose partial combat information panels |
| Equipped skill name | Verified | `E3-000-CAP-002` visibly labels every equipped card |
| Battle-visible active skill name | Verified for partial evidence | `E3-000-CAP-003` and `E3-000-CAP-004` visibly label six current skill effects |
| Stable skill ID | Not directly visible | Resolve the visible bilingual name through the Epic 2 catalogue and ask for confirmation on ambiguity |
| Skill category | Verified | All five category rows are visibly separated and labeled |
| Battle-tooltip heading | Verified | The new panels visibly distinguish `內功` and `絕技` |
| Complete category coverage | Verified for the current displayed loadout | All category rows and capacity counters appear together with no visible scrolling, paging, or collapsed sections |
| Empty or unavailable slots | Verified for the captured state | `護體 5/6` ends in an explicit locked tile rather than an omitted or off-screen card |
| Practice direction | Verified | Equipped cards visibly expose `正` or `逆`; no `相抵` example is present |
| Battle-visible active effect | Verified for the six captured entries | Each displayed text exactly matches the versioned reverse effect ID in the guarded catalogue |
| Current power | Visible but not yet a typed mechanic | Record the percentage as evidence; do not let it influence scoring until a versioned power contract exists |
| Cropped colored indicators and status icons | Visible, semantics unsupported | A wider labeled capture or individual tooltip is required before naming either numeric row or any icon |
| Loadout preset | Unsupported | The opponent screen exposes no preset identity or preset-selection control; the evidence applies only to the current displayed combat loadout |
| Observation time | Available from helper capture metadata | Record UTC attachment/capture time |
| Evidence reference | Available from helper capture metadata | Record a short opaque label plus SHA-256; do not commit the image by default |

For this UI version, Epic 3 may design complete-current-loadout semantics only
for a confirmed sparring opponent when all five category rows and their
capacity states are captured like `E3-000-CAP-002`. Completeness applies only
to the current displayed combat loadout, never to other presets. A hostile or
story target, `秘而不宣`, missing category rows, cropped screens, or unresolved
target identity must never produce a complete observation or an absence claim.
It may produce a separately typed partial battle-visible observation for facts
actually shown by a version-matched panel. Practice direction is observable
for `正` and `逆`; `相抵` remains unsupported until separately observed.

## Versioned completeness rule

| Field | Rule |
|---|---|
| Rule ID | `TAIWU-CNH-TARGET-LOADOUT-1.0.0-68032f25` |
| Applicable GameData version | Exact match to `1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a` |
| Applicable language/layout | Guarded installed Traditional Chinese (`CNH`) resource |
| Complete-current-loadout requirements | Confirmed sparring context and target identity; accessible opponent `運功`; all five labeled category rows; every row's capacity state; no cropped or hidden continuation |
| Supported direction | Visible `正` and `逆` only; `相抵` remains unsupported |
| Preset semantics | Current displayed combat loadout only; no inference about other presets |
| Unavailable semantics | Hostile/story context or `秘而不宣` means the complete loadout is unavailable, never empty; separately visible battle-tooltip facts remain partial evidence |
| Invalidation | Any GameData version, language-resource fingerprint, or layout change makes completeness unsupported until re-observed |

Later Domain and Application items may consume this rule as evidence for
complete-current-loadout construction. A partial capture remains a partial
observation even when the installed version matches.

## Repeatable capture protocol

Use the game normally; no mod, automation, memory inspection, or save change is
required.

1. Select and explicitly confirm a `切磋武功` opponent, such as the opponent
   reported as `霍劍嬋` in `E3-000-ID-001`. Do not request this workflow for a
   hostile or story target.
2. Pause the battle if the UI permits it.
3. Use `查看對方人物` to open the opponent's character information.
4. Open the opponent's `運功` page.
5. Capture the full loadout page. Because this page does not display target
   identity, pair it with an explicit target selection or identity evidence
   from the same encounter before submission.
6. If any category scrolls, pages, expands, or uses presets, capture every
   state required to see all equipped entries and empty slots.
7. If practice direction appears only in a tooltip/detail panel, capture one
   representative equipped skill with that detail visible.
8. Do not alter the opponent, save, loadout, or game configuration for the
   capture.

The image can be attached to the Codex task. It will be treated as local-only
evidence unless the product owner separately approves distribution. The
repository should normally retain only a sanitized evidence description,
capture time, and SHA-256.

For hostile/story combat-tooltip evidence:

1. Pair the capture with an explicitly confirmed encounter and target.
2. Pin the information panel with the game's displayed `T` action when useful.
3. Capture the complete labeled panel, including its `內功`, `絕技`, or other
   heading, every visible entry, power, and effect text.
4. Treat every submitted panel as partial unless a separate versioned rule
   proves complete coverage.
5. Capture individual tooltips or a wider labeled view for numeric indicators
   and icons; do not infer their semantics from color or shape.

## Resolved decisions

1. The complete opponent loadout is observable in sparring context, not for
   hostile or story targets in the supported UI version.
2. Hostile/story combat may still expose partial active skill names, power,
   and effect text; these facts require a separate provenance and cannot prove
   absence.
3. The full current displayed sparring loadout is observable when all five
   rows and capacity states appear as in `E3-000-CAP-002`.
4. All five categories are visible and clearly labeled.
5. The captured unavailable slot is explicitly locked rather than silently
   omitted; a cropped or missing row still cannot prove absence.
6. No preset identity or selector is visible, so completeness never extends
   beyond the current displayed combat loadout.
7. `正` and `逆` are visible on equipped cards; the six tooltip effects also
   identify reverse direction by exact versioned text; `相抵` is unsupported.
8. Power percentages are visible, but this evidence does not claim that any
   displayed field changes only after combat begins.
9. Target identity is not visible on the loadout screen. Explicit paired
   target selection and confirmation are mandatory.
10. The two rows of colored values and three status icons in `E3-000-CAP-005`
    remain unsupported until their labels are captured.

## Current E3-000 status

E3-000 is complete after the E3-012 scope correction. `E3-000-CAP-001` still proves that the `少林` story
encounter's full `運功` page is unavailable and must never become an empty
loadout. `E3-000-CAP-002` continues to support complete-current-loadout
coverage for sparring. New captures `E3-000-CAP-003` and `E3-000-CAP-004`
prove that hostile/story combat can expose useful partial skill-effect
evidence, while `E3-000-CAP-005` records additional visible but unlabeled
indicators. E3-012 now implements the corrected boundary: complete coverage
remains sparring-only, labelled battle effects use separate partial
provenance, visible power is evidence-only, and the unlabelled indicators are
not represented.
