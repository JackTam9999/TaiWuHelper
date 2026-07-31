# EPIC-001: Target-specific combat-skill recommendations

| Field | Value |
|---|---|
| Status | Complete |
| Milestone | 1 |
| Target release | TBD |
| Last updated | 2026-08-01 |

## Summary

Build a strictly read-only adviser that recommends a feasible combat-skill
loadout for the current Taiwu character against a selected target. The
recommendation must fit the character's real slot budget, use skills and
practice directions that are actually available, address the target's most
important threats, and explain how the player can configure and use the
loadout manually.

This is only a recommendation and helper system. It is not a mod, trainer,
cheat, bot, treatment, automation, or game-control system. It has no method,
endpoint, port, adapter, workflow, or future intention that can change a save,
game file, game configuration, running game process, runtime memory, or in-game
state.

The first release will use deterministic domain rules. Generative AI may later
help phrase explanations, but it must not decide whether a loadout is legal or
invent unverified game mechanics.

## Problem

Choosing combat skills is not a simple ranking problem:

- A skill consumes one or more category-specific slots.
- Mastery and legendary-book effects may reduce the effective cost.
- Neigong skills can provide specific and generic slots.
- The chosen Neigong combination must fit six base slots and may change all
  four outer capacities and the available generic-slot allocation.
- The character's current inner-power state may improve, weaken, or cause
  backlash when an active skill of a particular element is used.
- Direct, reverse, and neutral practice directions have different effects.
- Some effects exist whenever equipped, while others only apply during an
  active defense or agility skill.
- Skills may require a weapon, trick, distance, Neili allocation, stance,
  breath, unlock state, or other combat condition.
- The on-disk save can be older than the loadout currently visible in-game.
- Some calculated values are unavailable without the complete game runtime.

A recommendation that ignores any of these constraints can be impossible to
equip or ineffective against the selected target.

## Primary user story

> As a player preparing to fight a specific target, I want the helper to
> recommend combat skills I can actually equip, explain which target mechanics
> they counter, and give me an actionable setup and battle plan.

## Goals

1. Observe the player and target state through strictly read-only operations.
2. Identify the target's highest-priority combat threats.
3. Generate only loadouts that satisfy all known hard constraints.
4. Offer safe, balanced, and aggressive recommendation styles.
5. Explain every selected skill, direction, cost, condition, and trade-off.
6. Show the player the manual differences from the current loadout.
7. Make assumptions, stale data, and unavailable runtime calculations visible.

## Non-goals

- Changing saves, game files, game configuration, runtime memory, runtime state,
  or any other game-owned data.
- Treating, controlling, patching, injecting into, hooking, automating, or
  otherwise modifying the game.
- Equipping skills, issuing game input, or executing recommendations on the
  player's behalf.
- Adding any current or future feature that introduces a game-data write path.
- Simulating the complete Taiwu combat engine.
- Supporting every enemy and every special effect in the first release.
- Predicting an exact win probability.
- Treating an LLM response as authoritative game-mechanics data.
- Persisting recommendation history during Milestone 1.

## Product principles

### Correctness before optimization

Hard constraints must be validated before a candidate is scored. An invalid
loadout must never be returned as a recommendation.

### Evidence before inference

Recommendations must reference save data, local game configuration, verified
rules, or a clearly identified user-reported observation. When a value cannot
be obtained, the response must say so rather than estimating it.

### Explainability

Every recommendation must answer:

- Why is this skill included?
- Which target threat does it address?
- Which practice direction is required?
- What is its effective slot cost?
- What conditions must be satisfied in combat?
- What was removed, and why?

### Absolute non-interference

This is a permanent product invariant, not a Milestone 1 preference:

- The helper may open permitted source files for reading, hash their bytes, and
  copy relevant values into immutable helper-owned snapshots.
- The helper must never create, update, delete, repair, convert, re-serialize,
  replace, rename, or overwrite a save or any game-owned file.
- The helper must never call a `GameData` API capable of changing game-owned
  data or live game state.
- The helper must never inject into, hook, patch, attach a debugger or trainer
  to, write to the memory of, or send automated input to the game process.
- The helper must never write into the game installation, save directories, or
  other game-owned storage.
- The API returns information only. Suggested loadout differences and battle
  steps are instructions for the player to consider and perform manually.
- Helper-owned logs, tests, configuration, and optional recommendation history
  must remain outside game-owned storage and must not be consumed as game data.

Where the operating system and library APIs allow it, source files must be
opened with read-only access. Application and Domain interfaces must expose
queries only; no game-data command or mutation abstraction may exist. A feature
that requires game modification is rejected as out of scope rather than
deferred to a later milestone.

## Source-of-truth precedence

When sources disagree, use the following order:

1. Explicit current-screen observations reported by the user.
2. The latest successfully read save snapshot.
3. Local game configuration matching the installed game version.
4. Versioned, verified domain rules.

The response must identify which sources were used.

## Functional scope

### 1. Typed combat snapshot

Create a structured snapshot independent of the legacy diagnostic lines:

```text
CombatSnapshot
├── Metadata
│   ├── Save hash
│   ├── Read time
│   ├── Game version
│   └── Warnings
├── Player
│   ├── Learned skills
│   ├── Current loadout
│   ├── Slot budget and generic allocation
│   ├── Equipment
│   └── Legendary-book bonuses
└── Target
    ├── Features
    ├── Equipped skills
    ├── Relevant learned skills
    └── Equipment
```

The existing `lines` response remains available as a diagnostic compatibility
endpoint but is not used as the recommendation engine's internal contract.

### 2. Feasibility engine

Validate at least:

- The player owns every recommended skill.
- The required direct or reverse direction is active, or the same exact
  direction is immediately achievable through a verified manual breakthrough
  prerequisite.
- Neutral direction is not treated as either direction-specific effect.
- Effective cost is calculated from actual `GridCost`.
- Mastery reduction applies only when mastery is confirmed.
- Legendary-book cost changes apply only when confirmed.
- Category and generic-slot totals fit the available budget.
- Weapon, trick, range, stance, breath, Neili, and unlock requirements are
  either satisfied or explicitly disclosed.
- Equipped passives, active defense effects, and active agility effects are
  not incorrectly treated as simultaneously active.

### 3. Target threat analysis

Normalize verified target mechanics into threats such as:

- Mind damage and guarding-mind pressure.
- Repeated or high-speed attacks.
- Penetration and resistance pressure.
- Loss or defeat marks.
- Movement restriction.
- Weapon or trick disruption.
- Range control.
- Direct/reverse-effect suppression.
- Required opening or combat-start counters.

Each threat must include severity, evidence, and its source skill or effect.

### 4. Recommendation engine

Generate feasible candidates, then rank them using:

- Threat coverage.
- Survival value.
- Execution reliability.
- Weapon and play-style compatibility.
- Similarity to the current loadout.
- Damage potential.
- Slot opportunity cost.
- Conditional-risk penalties.

Return up to three styles:

- **Safe:** maximize survival and counter coverage.
- **Balanced:** cover critical threats while retaining reliable damage.
- **Aggressive:** accept more risk for a faster victory.

### 5. Actionable explanation

The response must include:

- Recommended skills grouped by category.
- Direction and effective cost for every skill.
- Used and available capacity for every category.
- Generic-slot allocation.
- Skills the player could manually add, remove, retain, change direction, or
  complete an immediately available breakthrough for.
- Primary active defense and agility choices.
- Alternative or switchable skills.
- Suggested opening and combat sequence.
- Assumptions, unavailable data, and warnings.

### 6. Local result UI

Provide a local browser page that presents the recommendation as a pre-fight
briefing. The approved layout is specified in
[UI-001: Combat-recommendation result layout](./UI-001-combat-recommendation-layout.md).

The page must:

- Let the player find a target and choose a preferred recommendation style.
- Compare safe, balanced, and aggressive results from the same snapshot.
- Put critical warnings before the proposed loadout.
- Show target threats beside the skills that counter or mitigate them.
- Display category capacity, generic-slot allocation, practice direction,
  effective cost, activation timing, and conditions.
- Provide an explicitly manual setup checklist and phased battle plan.
- Keep assumptions, alternatives, score contributions, and evidence available
  as supporting detail.
- Keep the information-only boundary visible and provide no control capable of
  applying a recommendation or changing the game.

## Proposed API

```http
POST /api/combat-recommendations
Content-Type: application/json
```

```json
{
  "targetCharacterId": 12345,
  "objective": "balanced",
  "preferredWeaponTemplateId": null,
  "currentLoadoutObservation": null
}
```

High-level response:

```json
{
  "snapshot": {
    "saveHash": "...",
    "readAt": "2026-07-29T00:00:00Z",
    "gameVersion": "...",
    "warnings": []
  },
  "targetThreats": [],
  "requestedStyle": "balanced",
  "recommendations": [
    {
      "style": "balanced",
      "loadout": {
        "genericGridAllocation": {},
        "categories": []
      },
      "manualChanges": [],
      "battlePlan": [],
      "alternatives": []
    }
  ],
  "assumptions": []
}
```

The response returns the available styles together so the UI can compare them
without rereading the save. `requestedStyle` selects the initially displayed
result. Every threat, skill reason, manual change, and battle-plan step must
have stable identifiers or references so the Presentation layer can show their
relationships without parsing explanation text.

## Clean Architecture placement

### Domain

- Combat snapshots and value objects.
- Slot budgets and effective-cost calculations.
- Threats, counters, requirements, and evidence.
- Loadout validation.
- Candidate scoring and recommendation policies.

### Application

- `CreateCombatSnapshot`.
- `AnalyzeTargetThreats`.
- `RecommendCombatLoadout`.
- Query-only ports for save reading and game knowledge.
- Optional persistence ports for helper-owned recommendation metadata only.

### Infrastructure

- Strictly read-only `GameData` snapshot adapter.
- Local configuration and effect catalog.
- Version detection.
- SQLite repositories only for helper-owned history, caching, or feedback when
  introduced. The database must be outside game-owned storage and can never be
  used to write data back into the game.

### Presentation

- Target selection and search.
- Recommendation request and response DTOs.
- Current-screen observation input.
- Validation and problem responses.
- Information-only endpoints; no command endpoint may alter game state.
- Blazor Interactive Server components hosted by the existing ASP.NET Core
  application.
- Presentation view models mapped from Application results.
- Local pre-fight briefing and manual setup checklist.
- xUnit v3 presentation-state and render-level tests, using NSubstitute where a
  use case substitute is needed.

## Milestone acceptance criteria

- [x] A player can select the agreed golden target.
- [x] The service opens source data through read-only operations and leaves
      saves and all other game-owned data byte-for-byte unchanged.
- [x] No Domain, Application, Infrastructure, or API contract exposes a
      game-data or game-state mutation operation.
- [x] The helper does not attach to, inject into, hook, patch, automate, or
      write to the running game.
- [x] The target's critical threats are returned with evidence.
- [x] Every recommended skill is owned by the player.
- [x] Every required direction is available and correctly interpreted.
- [x] Actual effective cost is used for each skill.
- [x] All category and generic-slot totals are valid.
- [x] Weapon and combat conditions are disclosed.
- [x] The response identifies primary, alternative, and switchable skills.
- [x] Exact manual add, remove, retain, direction-change, and verified
      breakthrough-prerequisite suggestions are returned for the player to
      perform.
- [x] A practical opening and battle sequence is included.
- [x] Safe, balanced, and aggressive results are deterministic for the same
      snapshot and request.
- [x] A local browser page presents all available styles from the same
      snapshot.
- [x] Critical warnings, target threats, skill reasons, capacity, direction,
      effective cost, timing, requirements, and evidence are visible.
- [x] The page provides a manual setup checklist and never provides an apply,
      equip, execute, or game-control operation.
- [x] Initial, loading, success, warning, empty, and failure states are
      implemented and keyboard accessible.
- [x] Unsupported runtime calculations are marked unavailable, not inferred.
- [x] Domain, Application, and Presentation behavior is covered by xUnit v3
      tests.
- [x] The end-to-end recommendation workflow and final player-adjusted loadout
      are verified by a real in-game victory; Reverse 七轮感应法 was separately
      validated, while attributing individual contributions to the recorded
      victory remains a future improvement.

## Completion decision

The product owner accepted Epic 1 as complete on 2026-08-01. The final target
victory used a manually adjusted loadout, legendary-book effects, and all
available beneficial pills. Reverse 七轮感应法 was not used. Therefore Epic
completion establishes a usable, strictly read-only recommendation vertical
slice and a successful player outcome; it does not claim that every proposed
counter caused the victory or that consumable and legendary-book contributions
were isolated. Reverse 七轮感应法 was separately validated in-game after the
recorded victory, but it was not used to produce that particular outcome. Those
measurements remain later improvements.

## Success measures

- 100% of returned loadouts pass the feasibility validator.
- 100% of helper operations leave every game-owned file and runtime state
  unchanged.
- 0 game-data or game-state mutation operations exist in the architecture or
  public API.
- 100% of selected skills include a reason and evidence.
- Identical snapshots and requests produce identical recommendations.
- The golden-target recommendation can be equipped exactly as returned.
- No known target-critical mechanic is omitted without a warning.

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| Game updates change costs or effects | Record game version and invalidate verified data after updates |
| Save is older than the current screen | Accept user-reported observations for analysis only and report snapshot time/hash |
| A proposed feature requires changing game data | Reject it as outside the permanent product boundary |
| A library exposes both read and write operations | Wrap only the minimum query surface and never expose mutation-capable objects |
| SpecialEffect runtime is incomplete | Use stored/configured values and mark calculated values unavailable |
| Effect text is difficult to normalize | Begin with curated rules for one golden target |
| Search space becomes too large | Apply hard filters before scoring and cap candidate combinations |
| Recommendation appears arbitrary | Include evidence and score contributions for every choice |
| Scope expands to every enemy | Complete and validate one vertical slice before generalizing |

## Release strategy

1. Select one golden target and one primary objective.
2. Implement the complete vertical slice for that target.
3. Add the local pre-fight briefing and verify its information hierarchy,
   accessibility, and non-interference language.
4. Have the player manually compare the result with a verified in-game
   loadout; the helper remains disconnected from game control.
5. Generalize target threats and counter rules one mechanic at a time.
6. Add persistence and feedback only after deterministic recommendations are
   reliable.
