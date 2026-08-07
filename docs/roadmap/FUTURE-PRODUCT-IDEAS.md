# Future product ideas

| Field | Value |
|---|---|
| Status | Proposed |
| Scope | Post-Milestone 1 discovery |
| Related epic | None |
| Last updated | 2026-07-31 |

## Purpose

Record potentially valuable product ideas discovered while reviewing other
Taiwu community tools. These ideas are deliberately outside
[EPIC-001](./epic-001/EPIC.md) and its Milestone 1
backlog. An idea must receive its own epic, acceptance criteria, and delivery
decision before implementation begins.

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

## Suggested promotion order

This was the original suggested promotion order after Milestone 1. The product
owner selected the catalogue and character skill atlas for Epic 2 on
2026-08-02, then selected target observations plus evidence provenance for
Epic 3 on 2026-08-07. The ordering is retained as discovery context:

1. Verified target observations and evidence provenance — promoted to
   [EPIC-003](./epic-003/EPIC.md).
2. Side-by-side loadout comparison.
3. Bilingual martial-art catalogue — promoted to
   [EPIC-002](./epic-002/EPIC.md).
4. Shareable recommendation card.
5. Version-aware observation and result persistence.

The two evidence ideas promoted into Epic 3 provide the most direct improvement
to recommendation correctness. The remaining ideas primarily improve
discovery, presentation, sharing, and repeat-use performance.

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
- What SQLite retention and deletion controls should the player have?
- Which ideas justify separate epics rather than remaining small enhancements?
