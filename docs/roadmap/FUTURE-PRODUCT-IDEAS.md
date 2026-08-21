# Future product ideas

| Field | Value |
|---|---|
| Status | Ongoing discovery |
| Scope | Ongoing product discovery after Epic 8 promotion |
| Related epics | EPIC-001 through EPIC-008 |
| Last updated | 2026-08-21 |

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

**Status:** Promoted to
[EPIC-006](./epic-006/EPIC.md) on 2026-08-17.

The promoted boundary covers evidence-aware role selection and candidate
comparison. Companion development remains PI-009, while settlement work
remains PI-010 rather than becoming a companion-finder role.

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

**Current bounded precursor:** On 2026-08-19, the companion finder gained a
read-only `SUCCESSION_CANDIDATE_READINESS` shortlist over the current group and
the verified village-work candidate source. Its disclosed formula is complete
saved-base capability breadth minus exact current age. This is only an initial
candidate comparison: age is not remaining lifespan, and the source and score
do not prove inheritance eligibility, transferable progress, or future growth.
The development and inheritance mechanics below therefore remain future work.

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

**Status:** Promoted as a bounded first vertical to
[EPIC-007](./epic-007/EPIC.md) on 2026-08-18.

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

### PI-012 — Evidence-backed tactical combat planner

**Status:** Promoted to
[EPIC-008](./epic-008/EPIC.md) on 2026-08-20.

Extend the completed Epic 5 target-archetype and counter-playbook foundation
into an exact-target planner that can explain not only which verified counters
fit, but also how their effects interact and when the player should use them.
This requires a separate epic because Epic 5 deliberately retained the
existing candidate universe, scoring semantics, bounded search, and manual
plan.

The planner should treat a target's combat mechanics as a verified causal
chain rather than a flat collection of independently covered threat codes.
Candidate chain states may include prerequisites, generated combat styles or
marks, active attack/defense/agility state, threshold transitions, repeated
casts, defeat prevention or reset, and a counter's own temporary lockout. A
missing or unverified transition remains explicit and cannot be invented from
a localized skill name or untyped raw effect description.

Candidate discovery should consider the player's complete learned-skill
snapshot, not only the current loadout and manually curated hard-counter list.
A discovered skill may become a recommendation only through verified typed
roles, effects, timing, and requirements, followed by the existing hard checks
for mastery, practice direction, breakthrough availability, expected effect,
effective cost, inner-power compatibility, and category capacity. Useful
roles may include:

- interrupting or suppressing a core cast;
- reducing hit, power, mark duration, resonance, or resource generation;
- preserving mind, movement, or defensive reliability;
- recovering from a counter's self-lock or other execution cost;
- selecting the damage channel that the exact target resists less effectively;
  and
- creating a verified finish window against recovery, defeat prevention, or
  reset mechanics.

The requirement context should carry every available fact needed to decide
whether a paper loadout can actually be executed: equipped and unlocked weapon
types, usable combat styles, current or opening distance, stance and breath,
other verified resources, active defense and agility roles, category budgets,
universal-slot allocation, and legendary-book cost changes. Unknown runtime
facts should produce a manual confirmation or fallback, not a silent empty
context or an optimistic score.

The output should include a conditional, information-only battle plan with:

1. pre-combat preparation and manual direction or breakthrough changes;
2. the opening active defense, agility, resource, and positioning choices;
3. target-state triggers for interrupts, mitigation, switching, and burst;
4. recovery steps after self-lock, resource depletion, or a failed condition;
5. a primary finish condition and evidence-backed fallback; and
6. concise reasons and provenance for every transition.

Safe, Balanced, and Aggressive policies should retain distinct meanings even
when some score evidence is unavailable. Damage potential should be calculated
only from version-matched typed evidence for the player's attack, hit and cast
reliability, the target's relevant defense or resistance, and applicable live
conditions. Threat scoring should account for chains, timing, interactions,
and useful layered protection rather than rewarding duplicate coverage of a
flat code. Slot scoring should represent marginal combat value or a justified
reserve, not automatically reward unused capacity. Unknown components remain
excluded with a visible diagnostic and must not become zero, safety, or
victory-probability claims.

The candidate search must remain deterministic, cancellable, bounded, and
diagnostic. It should report how much of the eligible search space was covered
and which pruning, option, time, or result limit affected the answer. Repeated
snapshot and catalogue work should be reused within one immutable request, and
target-aware pruning should remove demonstrably dominated or irrelevant
options before combination search.

An initial vertical should use one already verified high-value magic-sound
target whose individual skills form an observable chain. Scenario acceptance
should assert tactical invariants rather than one brittle exact loadout: the
plan must suppress the target's core direct-practice casts, mitigate the mark
and resonance path, recover from the chosen suppression counter's self-lock,
respect the player's current inner-power backlash and unavailable directions,
fit the exact displayed budgets, and retain a feasible finish path. Manually
reported battle outcomes may be stored through PI-006 with provenance for
regression review, but a single win must not be treated as proof that one
counter caused the result or used to generate rules automatically.

This remains a read-only planning feature. It must not attach to the game,
inspect runtime memory, capture input, execute combat, equip skills, change
practice direction, allocate slots, write a save, simulate unsupported hidden
mechanics, or claim a probability of victory.

### PI-013 — Current-version complete anti-magic-sound loadout expansion

**Status:** Planned as a non-blocking post-Epic 8 follow-up in
[E8-F01 through E8-F07](./epic-008/BACKLOG.md#planned-follow-up-backlog-after-epic-8).

Extend Epic 8's historical representative tactical vertical to the newer
installed GameData and one exact later magic-sound encounter. The expansion
should close the concrete gap between a narrow verified hard-counter list and a
coherent full loadout across inner power, attack, agility, defense, Qiqiao, and
universal-slot allocation.

The evidence gate comes first. The target's exact encounter phase,
Direct-practice coverage, marks, resonance, reset, movement, range, and speed
pressure must be independently reverified for the installed version. The
planner may then add typed roles for complementary suppression-recovery
attacks, footwork and distance control, active-defense choices, and equipped
mind-protection passives. Localized descriptions and unchanged IDs alone do
not authorize current-version mechanics.

Candidate search should operate over every learned skill with an exact verified
role, not only a curated counter whitelist. Coupled constraints must keep
Reverse `604` together with three executable Reverse-practice recovery casts,
respect mutually exclusive active defense/agility effects, enforce weapon,
distance, trick, direction, backlash, cost, and capacity requirements, and
report why every unsupported or pruned skill was excluded.

Acceptance should use a sanitized current-version scenario and tactical
invariants rather than one hard-coded answer. A manually audited reference
loadout must remain feasible and discoverable, while the planner may select an
evidence-equivalent alternative with a better disclosed score. All output
remains read-only, information-only, bounded, deterministic, and explicit
about unsupported live facts.

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
4. Target archetypes and counter playbooks — promoted to
   [EPIC-005](./epic-005/EPIC.md) on 2026-08-10 and completed after remediation
   and product-owner approval on 2026-08-11.
5. Companion role and candidate finder — promoted to
   [EPIC-006](./epic-006/EPIC.md) on 2026-08-17 and completed after evidence,
   remediation, comprehensive-capability follow-up, and product-owner approval
   on 2026-08-18.
6. Village workforce and building assignment planner — promoted to
   [EPIC-007](./epic-007/EPIC.md) on 2026-08-18 and completed after evidence,
   implementation, corrective review, representative verification, and
   product-owner approval on 2026-08-20.
7. Evidence-backed exact-target tactical combat planner — promoted to
   [EPIC-008](./epic-008/EPIC.md) on 2026-08-20 and completed after its
   historical evidence gate, implementation, guarded representative checks,
   independent closure review, and product-owner approval on 2026-08-21.

### Current candidates after Epic 8 selection

1. Current-version complete anti-magic-sound loadout expansion; Epic 8,
   E8-F01, and E8-F02 are complete, with typed role coverage continuing in
   E8-F03.
2. Companion development planner.
3. Library and book planning, assessed after the Epic 7 source boundary rather
   than assumed to share its first assignment vertical.
4. Version-aware observation, recommendation, and outcome persistence.
5. Shareable recommendation card, which may remain a smaller enhancement
   because copy and print foundations already exist.

The companion and settlement ideas intentionally remain separate. They may
share character, skill, evidence, and comparison primitives, but each needs a
different objective model and should not be combined into one unbounded
optimizer epic.

### Epic 6 deferred companion mechanics

Epic 6 deliberately leaves these as evidence-gated future candidates rather
than partial role rules:

| Candidate | Future evidence needed | Current boundary |
|---|---|---|
| Current modified qualification and attainment | Standalone-safe or explicitly live special-effect context with versioned provenance | Saved base aptitude only |
| General combat-support role | Party synergy, timing, teammate commands, survivability, and composition mechanics | Qualification and learned skills do not prove support value |
| Martial-art teaching role | Relationship, interaction, teachability, book/content, and live-context rules | No teaching claim |
| Recruitable-prospect role | Recruitment availability, relationship thresholds, dialogue/event, travel, and party-capacity rules | Current group members only |
| Inheritance or long-term potential | Verified growth, transfer, age-horizon, and development rules | Continue under PI-009 |
| Settlement/work role | Villager availability, assignment, building, resource, and output rules | Continue under PI-010 |

Names, locations, age, learned skills, raw descriptions, and nearby APIs remain
insufficient evidence for any of these mechanics. A future promotion must
define a separate source decision and objective contract before implementation.

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
- What is the smallest verified combat-state model that can represent target
  skill chains, marks, active roles, thresholds, resource flow, and temporary
  self-lock without pretending to be a complete combat simulator?
- Which typed effect and role evidence is sufficient to admit a learned skill
  that has no manually curated hard-counter rule into candidate discovery?
- Which version-matched attack, defense, resistance, hit, timing, and resource
  facts are sufficient for a useful damage or execution score?
- When should unused category capacity count as a justified reserve, and when
  should it count as missed marginal combat value?
- How should manually reported wins, losses, trigger timings, and failure causes
  improve regression scenarios without creating automatic causal claims or
  unsupported win-probability models?
- Which development opportunities can be represented as verified steps rather
  than speculative advice?
- Which village assignments, buildings, resource constraints, and worker
  capabilities are available from the current save?
- Does library planning share enough of the village model to remain one slice,
  or does it require a separate inventory and progression domain?
- What SQLite retention and deletion controls should the player have?
- Which ideas justify separate epics rather than remaining small enhancements?
