# Future product ideas

| Field | Value |
|---|---|
| Status | Proposed |
| Scope | Ongoing product discovery after Epic 5 selection |
| Related epics | EPIC-001 through EPIC-005 |
| Last updated | 2026-08-10 |

## Purpose

Record potentially valuable product ideas that have not yet been promoted into
an active epic. An idea may extend a completed epic or open a new product area,
but it must receive an explicit epic boundary, acceptance criteria, and
delivery decision before implementation begins.

Every future feature remains subject to
[ADR-0001: Absolute game non-interference](../architecture/ADR-0001-absolute-game-non-interference.md).
The helper may provide information and retain helper-owned metadata, but it
must never change a save, game file, game configuration, running game process,
runtime memory, or in-game state.

## Ideas worth considering

### PI-001 — Verified target observations

**Status:** Promoted to
[EPIC-003](./epic-003/EPIC.md) on 2026-08-07.

Allow the player to supplement an incomplete or stale save snapshot with
information observed on the current game screen or during a battle.

Potential workflow:

1. Read and fingerprint the configured save without changing it.
2. Identify information that the save cannot confirm, such as the target's
   current equipped combat skills.
3. Let the player enter the battle and inspect the target normally.
4. Accept a screenshot or a manual description of the observed information.
5. Resolve displayed Chinese or English names to stable GameData identifiers.
6. Ask the player to confirm any ambiguous match.
7. Recalculate recommendations from the immutable save snapshot plus the
   confirmed observation.

Reloading a save after entering battle may be useful for comparison, but it
must not be assumed to expose runtime-only target selections. The observation
itself remains the relevant evidence.

The helper must not capture game memory, attach to the game, automate
screenshots, inject code, simulate input, or reload a save on the player's
behalf.

### PI-002 — Evidence provenance and confidence

**Status:** Promoted with PI-001 to
[EPIC-003](./epic-003/EPIC.md) on 2026-08-07.

Give every important input, threat, and recommendation an explicit provenance
such as:

- Save-derived.
- Local GameData-derived.
- Observed on the current screen.
- Observed during battle.
- Manually confirmed.
- Inferred from a verified rule.
- Unknown.

Present a confidence status such as `Confirmed`, `Probable`, `Incomplete`, or
`Unsupported`. Confidence must describe evidence completeness, not an
unverified probability of winning.

When sources disagree, retain both values, show their timestamps and
provenance, and apply the documented source-of-truth precedence. Never silently
replace one source with another.

### PI-003 — Side-by-side loadout comparison

**Status:** Promoted to
[EPIC-004](./epic-004/EPIC.md) on 2026-08-08.

Compare:

- The player's current observed loadout.
- The primary recommended loadout.
- One or more alternative loadouts.

The comparison should make category capacity, universal-slot allocation,
practice direction, effective cost, activation conditions, covered threats,
required manual changes, and unresolved risks easy to scan.

This is an explanatory planning surface only. It cannot apply a loadout or
control the game.

### PI-004 — Bilingual martial-art catalogue

**Status:** Promoted to
[EPIC-002](./epic-002/EPIC.md) on 2026-08-02.

Provide a searchable Chinese and English catalogue derived from permitted
local game configuration and language resources. Store the derived catalogue
in a helper-owned, rebuildable SQLite database so that the UI and recommendation
engine can search and join skill data efficiently without repeatedly parsing
all installed resources. Candidate fields include:

- Stable skill identifier.
- Chinese and English names.
- Category, grade, faction, and weapon type.
- Direct, reverse, and neutral practice effects.
- Base and effective slot cost.
- Activation requirements and timing.
- Verified counters, synergies, and evidence.
- GameData and language-resource versions.

The installed game files remain the source of truth for static skill data. The
current save remains the source of truth for learned skills, breakthrough
availability, practice direction, current equipment, and other player state.
Save-derived state may be cached temporarily, but the catalogue must not replace
or override a fresh read.

Clean Architecture placement should keep catalogue models and query ports in
the Domain/Application boundary, with importing and SQLite persistence in
Infrastructure. Recommendation calculations may use only verified typed effect
rules; retaining raw effect text in the local catalogue does not by itself make
an effect safe to interpret or score.

The generated database must live outside game-owned storage, be excluded from
Git, and be invalidated or rebuilt when the relevant GameData or language
resource version changes. The project must not commit or distribute a
pre-populated catalogue, proprietary game binaries, complete game resources,
extracted artwork, or other unlicensed content. Each installation builds its
own catalogue from the player's locally installed resources.

### PI-005 — Shareable recommendation card

Export a compact, bilingual summary that the player can keep visible while
playing or share for discussion. It could include:

- Target identity and observation freshness.
- Selected recommendation style.
- Recommended loadout and manual changes.
- Key threat-counter relationships.
- Capacity and practice-direction summary.
- Confidence, assumptions, and missing evidence.
- Save fingerprint prefix and GameData version.

Exports are helper-owned presentation artifacts. They must be written outside
game-owned directories and must not be consumable as game commands or
configuration.

### PI-006 — Version-aware helper database

Use SQLite for helper-owned observations and derived data only when persistence
provides a clear workflow or performance benefit.

Possible records include:

- Save fingerprints and observation timestamps.
- GameData and language-resource versions.
- Target observations and their provenance.
- Confirmed name-to-identifier mappings.
- A rebuildable local skill catalogue derived from installed configuration and
  language resources, as described by PI-004.
- Previous recommendation summaries.
- Player feedback and manually reported outcomes.

The save and installed GameData remain authoritative source material. Cached
records must be invalidated or marked stale when their save fingerprint,
GameData version, language version, or relevant verified rule changes.

Do not store complete saves, proprietary binaries, unnecessary raw game
content, or any data intended to be written back into the game. A generated
catalogue database is a local cache and must never be committed or shipped as
application content.

### PI-007 — Target archetypes and counter playbooks

**Status:** Promoted to
[EPIC-005](./epic-005/EPIC.md) on 2026-08-10.

Scale the existing verified target-specific recommendation pipeline by
classifying each target into one or more evidence-backed combat archetypes.
Classification is multi-label rather than mutually exclusive: a target may be
blade-oriented, apply high physical-damage pressure, use poison, and have a
defeat-threshold reset at the same time.

Candidate profile dimensions include:

- attack or weapon family;
- physical/internal damage, 破體/破氣, penetration, or repeated-hit pressure;
- physical or internal defense, avoidance, recovery, and reset mechanics;
- poison, mind-break, distraction-mark, movement, range, weapon, and trick
  disruption; and
- opening burst, sustained attrition, threshold triggers, and other combat
  tempo characteristics.

Each archetype defines a reusable counter playbook containing response goals,
verified counter or mitigation candidates, timing, requirements, evidence, and
known gaps. A playbook is not a fixed loadout and does not claim that one
representative target defines every member of the archetype.

The final recommendation combines all matched playbooks, resolves conflicts
and capacity pressure, filters against the player's learned skills and current
practice directions, and then applies target-specific adjustments from the
target's actual skills, effects, equipment, observations, and unresolved
evidence. The UI should distinguish the reusable archetype response from the
adjustments made for this exact target.

Initial classification must use explicit, versioned, verified rules. It must
not infer mechanics from a skill name, weapon label, or untyped raw effect
description. Unknown fields remain unknown. Statistical clustering, automatic
training, and win-probability claims remain outside the initial epic.

### PI-008 — Companion role and candidate finder

Find suitable 同道 candidates for a player-selected role or objective rather
than claiming that one character is universally best. Possible objectives
include a combat role, teaching or inheritance value, a particular life-skill
role, settlement work, or a balanced long-term candidate.

The comparison should explain the relevant attributes, learned skills,
features, availability, evidence freshness, missing data, and tradeoffs used
for the selected role. It may rank only fields supported by version-matched
save or GameData evidence and must not automate recruitment, dialogue, travel,
or party changes.

### PI-009 — Companion development planner

Create a staged, information-only development plan for an existing or selected
同道. Start from a chosen future role, identify the gap between current and
desired capabilities, and suggest evidence-backed priorities for skills,
training, equipment, and other verified development resources.

The plan must distinguish directly observed progress, save-derived state,
verified opportunities, and speculative or unavailable data. It should expose
conflicts between multiple desired roles instead of combining them into an
impossible universal build. It must never perform training or modify a
character, party, save, or game state.

### PI-010 — Village workforce and building management

Provide a read-only settlement planning surface that connects villagers,
roles, buildings, current assignments, resource constraints, and uncovered
work. Recommendations should be objective-specific, such as production,
recovery, training support, or balanced operation, and should explain why a
villager is suitable for a particular assignment.

The current save remains authoritative for people, buildings, assignments,
and resources. The helper may produce a manual reassignment checklist, but it
must not automate work assignment, construction, collection, or any other
in-game operation.

### PI-011 — Library and book planning

Build a library and study-planning view from book, page, ownership, condition,
and location data only where those fields can be read and interpreted with
version-matched evidence. Help the player identify relevant holdings, missing
or incomplete material, duplicates, study priorities, and connections to
player or companion development goals.

Acquisition sources, repair behavior, reading requirements, and progression
effects must be separately verified before they become recommendations. This
idea may begin as a bounded slice of PI-010, but it should become its own epic
if its inventory, study, acquisition, or progression rules require a distinct
domain model.

## Suggested promotion order

### Completed promotions

The product owner promoted and completed these discovery ideas:

1. Bilingual martial-art catalogue — promoted to
   [EPIC-002](./epic-002/EPIC.md) on 2026-08-02.
2. Verified target observations and evidence provenance — promoted to
   [EPIC-003](./epic-003/EPIC.md) on 2026-08-07.
3. Side-by-side loadout comparison — promoted to
   [EPIC-004](./epic-004/EPIC.md) on 2026-08-08 and completed with the approved
   two-option design on 2026-08-10.

### Active promotion

Target archetypes and counter playbooks were promoted to
[EPIC-005](./epic-005/EPIC.md) on 2026-08-10. Epic 5 reuses the completed
threat, evidence, recommendation, and comparison foundations while expanding
target coverage.

### Current candidates after Epic 5 selection

1. Companion role and candidate finder.
2. Companion development planner.
3. Village workforce and building management.
4. Library and book planning, initially assessed as a village-management slice.
5. Version-aware observation, recommendation, and outcome persistence.
6. Shareable recommendation card, which may remain a smaller enhancement
   because copy and print foundations already exist.

The companion and settlement ideas intentionally remain separate. They may
share character, skill, evidence, and comparison primitives, but each needs a
different objective model and should not be combined into one unbounded
optimizer epic.

## Discovery questions

- Which target details can be reliably observed in the current game UI?
- Which observations require screenshots, and which are practical to enter
  manually?
- Should screenshot interpretation be automatic, assisted, or manual-first?
- What evidence is sufficient to change a target mechanic from `Unknown` to
  `Confirmed`?
- How should conflicting observations from different battles be represented?
- Which extracted catalogue fields may be safely distributed?
- Which raw effect fields are useful for local display, and which effects need
  verified typed rules before the recommendation engine may use them?
- Which independent profile dimensions distinguish reusable target archetypes
  without forcing a target into one mutually exclusive group?
- Which representative targets and counter evidence are sufficient to validate
  the first archetype playbooks?
- How should overlapping playbooks resolve conflicting counters, timing, and
  category or universal-slot pressure?
- Which player-selected roles make companion comparison useful, and which
  character fields are reliable enough to rank for each role?
- Which development opportunities can be represented as verified steps rather
  than speculative advice?
- Which village assignments, buildings, resource constraints, and worker
  capabilities are available from the current save?
- Does library planning share enough of the village model to remain one slice,
  or does it require a separate inventory and progression domain?
- What SQLite retention and deletion controls should the player have?
- Which ideas justify separate epics rather than remaining small enhancements?
