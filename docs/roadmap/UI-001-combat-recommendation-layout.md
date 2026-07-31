# UI-001: Combat-recommendation result layout

| Field | Value |
|---|---|
| Status | Proposed |
| Epic | [EPIC-001](./EPIC-001-combat-skill-recommendation.md) |
| Milestone | 1 |
| Primary surface | Local browser |
| Last updated | 2026-07-31 |

## Purpose

Present a target-specific combat recommendation as a clear pre-fight briefing.
The player should be able to understand the target's threats, compare the safe,
balanced, and aggressive styles, configure the proposed loadout manually, and
follow a battle plan without interpreting raw API data.

The UI is an information-only presentation adapter. It cannot apply a loadout,
send game input, write game data, or control the running game.

## Technology

Add Blazor Interactive Server components to the existing ASP.NET Core host.
This keeps the application in one local .NET 10 process and does not introduce
a separate Node-based frontend toolchain.

Blazor components and presentation view models belong to the outer
Presentation layer. They may invoke Application use cases and map their
results, but UI state and layout concerns must not enter Domain or Application.

## Primary user flow

1. Open the local TaiWu Helper page.
2. Confirm the configured save snapshot and freshness.
3. Search for and select a target.
4. Select a preferred style and, optionally, a preferred weapon.
5. Request recommendations.
6. Compare safe, balanced, and aggressive results from the same snapshot.
7. Inspect threats, evidence, slot usage, requirements, and warnings.
8. Follow the displayed setup checklist manually in the game.
9. Keep the battle-plan section visible as a reference during combat.

## Desktop layout

The initial design targets a desktop browser at 1280 pixels wide or greater so
it can be used on a second monitor or beside the game.

```text
┌──────────────────────────────────────────────────────────────────────┐
│ TaiWu Helper       INFORMATION ONLY       Save read: 22:15  ✓ Fresh │
├──────────────────────────────────────────────────────────────────────┤
│ Target: [ Search character... ▼ ]  Style: [Safe|Balanced|Aggressive]│
│ Preferred weapon: [ Optional ▼ ]                 [Get recommendation]│
├──────────────────────────────────────────────────────────────────────┤
│ ⚠ Warnings and unavailable information                              │
├───────────────────────┬──────────────────────────────────────────────┤
│ TARGET THREATS        │ RECOMMENDED LOADOUT                          │
│                       │                                              │
│ Critical              │ 內功  3/4 slots                             │
│ • Mind pressure       │ ┌ Skill name · Reverse · Cost 2 · KEEP ┐    │
│ • Opening attack      │ │ Counters mind pressure                │    │
│                       │ └────────────────────────────────────────┘    │
│ Moderate              │                                              │
│ • Range restriction   │ 摧破  5/6 slots                             │
│                       │ 輕靈  2/3 slots                             │
│ Evidence: [Details]   │ 護體  3/3 slots                             │
│                       │ 奇竅  2/4 slots                             │
├───────────────────────┴──────────────────────────────────────────────┤
│ MANUAL SETUP CHECKLIST                                               │
│ □ Add Skill A — direct practice                                      │
│ □ Remove Skill B                                                     │
│ □ Change Skill C to reverse practice                                 │
│ □ Confirm weapon and Neili requirements                              │
│                                                                      │
│ Instructions only: TaiWu Helper cannot perform these steps.          │
├──────────────────────────────────────────────────────────────────────┤
│ BATTLE PLAN                                                          │
│ 1. Before combat: ...                                                │
│ 2. Opening: ...                                                      │
│ 3. When the target activates X: ...                                  │
│ 4. Switch to the alternative defense when: ...                       │
├──────────────────────────────────────────────────────────────────────┤
│ [Alternatives] [Assumptions] [Evidence] [Copy checklist] [Print]     │
└──────────────────────────────────────────────────────────────────────┘
```

## Page regions

### Application header

Display:

- Application name.
- A persistent `Information only` badge.
- Snapshot read time and freshness.
- Game version.
- A link to detailed snapshot metadata.

The badge must not imply that the helper is attached to the game. Freshness
describes only the last source-data read.

### Recommendation controls

Provide:

- Target search by in-game character name with age and named-location context.
- Preferred-style selector: safe, balanced, or aggressive.
- Optional preferred weapon.
- Optional current-screen observations used only for analysis.
- `Get recommendation` and `Refresh read-only snapshot` actions.

The UI must use `recommendation`, `suggestion`, `manual`, and `read-only`
language. It must not use `apply`, `equip automatically`, `fix game`, `patch`,
or other text suggesting game control.

### Status and warnings

Warnings appear above results when:

- The source snapshot may be stale.
- Current-screen observations differ from the save.
- Required values are unavailable.
- A mechanic is unverified.
- A recommendation has conditional requirements.

Warnings must state their effect on the recommendation and must never be hidden
behind a details panel.

### Style comparison

Safe, balanced, and aggressive appear as tabs backed by recommendations
generated from the same immutable snapshot. Each tab displays:

- Threat coverage.
- Survival, reliability, and damage emphasis.
- Number of manual loadout differences.
- Conditional-risk count.

The UI must not display a win probability unless a future verified model can
support it. It may display known-constraint validation and evidence coverage.

### Target-threat panel

Group threats by severity and show:

- Threat name and short explanation.
- Source target skill or effect.
- Activation timing.
- Evidence status.
- The recommended counter or mitigation.

Selecting a threat highlights the skills and battle-plan steps that address it.

### Recommended-loadout panel

Group skills using the in-game categories:

- 內功.
- 摧破.
- 輕靈.
- 護體.
- 奇竅.

Each category displays used capacity, available capacity, and generic-slot
allocation. A skill card displays:

- Chinese in-game skill name.
- Category.
- Direct, reverse, or neutral practice direction.
- Actual grid cost and effective cost.
- Manual-change status: add, retain, or change direction.
- Countered threats and recommendation reason.
- Passive, active-defense, or active-agility timing.
- Weapon, range, stance, breath, Neili, unlock, or other requirements.

Colour may reinforce categories and statuses, but text and icons must carry the
same meaning without relying on colour.

### Manual setup checklist

Show an ordered, copyable checklist containing:

- Skills for the player to add manually.
- Skills for the player to remove manually.
- Skills to retain.
- Practice-direction changes.
- Generic-slot allocation.
- Weapon and Neili checks.

Checklist state is temporary helper UI state only. Checking an item does not
communicate with or change the game.

The section always displays:

> Instructions only: TaiWu Helper cannot perform these steps.

There is no `Apply`, `Equip`, `Execute`, or game-control button.

### Battle plan

Present concise phases:

1. Before combat.
2. Opening actions.
3. Normal execution.
4. Trigger-based reactions.
5. Defense or agility switching conditions.

Every step links back to its threat, skill, condition, or evidence.

### Supporting detail

Alternatives, assumptions, score contributions, and detailed evidence use
collapsed panels below the primary result. These panels remain available for
verification without overwhelming the default view.

Safe helper-owned actions are limited to copying, printing, and exporting the
recommendation. Exports must remain outside game-owned storage and cannot be
consumed as game commands.

## Responsive behaviour

- At 1280 pixels and above, show threats and loadout side by side.
- Below 1280 pixels, stack threats above the loadout.
- Keep warnings and the information-only boundary visible at every size.
- Keep the preferred-style selector reachable while reviewing long results.
- Do not require hover to reveal conditions or evidence.

## Page states

The page must define:

- Initial state with target-selection guidance.
- Loading state while the snapshot and recommendations are calculated.
- Successful result.
- Successful result with warnings.
- No matching target.
- Ambiguous target.
- Invalid or unavailable save configuration.
- Unsupported game version or mechanic.
- Unexpected read or calculation failure.

Failures use clear recovery actions such as correcting configuration or
retrying a read. They never offer repair or modification of game data.

## Presentation component structure

```text
TaiWuAPI/
├── Components/
│   ├── Pages/
│   │   └── CombatRecommendation.razor
│   └── Recommendations/
│       ├── TargetSelector.razor
│       ├── RecommendationSummary.razor
│       ├── ThreatPanel.razor
│       ├── LoadoutCategory.razor
│       ├── SkillCard.razor
│       ├── CapacityBar.razor
│       ├── ManualChecklist.razor
│       ├── BattlePlan.razor
│       └── EvidencePanel.razor
└── Presentation/
    └── RecommendationViewModels.cs
```

Exact component boundaries may change during implementation, but components
must consume presentation models rather than `GameData` types.

## Accessibility and terminology

This layout inherits the project-wide
[UI presentation guidelines](../architecture/UI-PRESENTATION-GUIDELINES.md),
including absolute game non-interference and the requirement to use localized
entity names instead of IDs or raw technical references.

- Support keyboard navigation for all controls and panels.
- Maintain readable contrast.
- Do not communicate severity, direction, or status by colour alone.
- Use visible focus states.
- Associate warnings and validation messages with their inputs.
- Use Chinese in-game names as the primary skill terminology.
- Keep identifiers internal while explanatory text uses localized names.

## Testing

Use xUnit v3 for:

- Mapping Application results into presentation view models.
- Style selection and comparison state.
- Capacity and skill-card display rules.
- Loading, warning, empty, and error states.
- The manual checklist's helper-only behaviour.
- Tests that prevent mutation-oriented labels or actions from entering the UI.

Use NSubstitute where a presentation service or component needs an Application
use case substitute. Render-level tests must confirm the important information
hierarchy and non-interference message.

## Acceptance criteria

- [ ] The page runs locally in the existing .NET host.
- [ ] A player can find a target and request recommendations.
- [ ] Safe, balanced, and aggressive results share one snapshot and can be
      compared without rereading game data.
- [ ] Critical warnings appear before the loadout.
- [ ] Threats visibly link to recommended counters.
- [ ] All skill cards show direction, effective cost, reason, timing, and
      requirements.
- [ ] All category capacities and generic-slot allocations are visible.
- [ ] The player receives a manual setup checklist and phased battle plan.
- [ ] Assumptions, alternatives, and evidence are inspectable.
- [ ] Loading, empty, warning, and failure states are implemented.
- [ ] The layout is usable with keyboard navigation and without colour.
- [ ] The information-only boundary remains visible.
- [ ] No control can apply a recommendation or modify or control the game.
- [ ] Every player-visible game entity uses its localized name; no numeric ID,
      warning code, or raw evidence reference is rendered.
