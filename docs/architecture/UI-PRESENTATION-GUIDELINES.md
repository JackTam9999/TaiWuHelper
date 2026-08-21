# UI presentation guidelines

| Field | Value |
|---|---|
| Status | Accepted |
| Scope | All TaiWu Helper user interfaces and presentation adapters |
| Applies to | Current and future UI features |
| Last updated | 2026-07-31 |

## Purpose

These are project-wide presentation rules. They apply to every page,
component, warning, error state, copied or printed recommendation, export, and
future user interface. A feature-specific UI document may add stricter rules,
but cannot weaken this guide.

The architectural authority for the safety boundary is
[ADR-0001: Absolute game non-interference](./ADR-0001-absolute-game-non-interference.md).
This guide defines how that invariant must appear and remain enforceable in the
Presentation layer.

## Absolute game non-interference

Every UI is permanently information-only. It may display observations,
analysis, recommendations, and manual instructions, but must never modify
game-owned data or control the running game.

- No UI action may apply, equip, execute, repair, patch, or write a
  recommendation or game state.
- No event handler may write to a save, game directory, game database, process
  memory, or in-game state; send game input; or invoke a game command.
- Checkboxes and selections may change helper-owned UI state only.
- Copying, printing, and helper-owned exports are allowed only outside
  game-owned storage and cannot be consumed as game commands.
- Labels and recovery actions must use recommendation, information-only,
  manual, read, retry, and review language. They must not imply attachment to
  or control of the game.
- A proposed UI feature that requires game modification or automation is
  rejected, not deferred.

## Player-visible identity

Every player-visible game entity must be identified by its localized in-game
name, never by a numeric ID or raw technical reference.

- Characters use their in-game name. Age, consummate level, and a named
  location may disambiguate characters with the same name.
- Skills use their in-game name in cards, setup actions, battle plans,
  requirements, warnings, observation controls, and evidence summaries.
- Locations, weapons, tricks, effects, features, and other game entities use a
  resolved name.
- If a name is unavailable, show a localized unavailable or unnamed label;
  never fall back to the numeric ID.
- Stable references, warning codes, database keys, and numeric IDs may remain
  in Domain, Application, Infrastructure, API contracts, logs, and internal
  helper state, but UI components must not render them.
- Ages, dates, versions, counts, costs, capacities, scores, distances, and
  percentages are values rather than identity keys and may remain numeric.

This rule includes default and expanded content, warnings, errors, empty
states, copied text, printed output, accessibility labels, tooltips, and
exports.

## Localization and terminology

- English and Chinese modes must cover all player-visible labels and generated
  explanations.
- Resolved in-game names remain the primary terminology and are not replaced
  with internal enum, code, or identifier values.
- Missing translations must not expose a technical reference as a fallback.
- Category, practice-direction, timing, severity, and status meanings must be
  expressed in player-facing terms.

## Evidence, warnings, and failures

- Evidence remains traceable internally, while the UI presents named evidence
  summaries rather than raw reference strings.
- Warning and caveat headings are human-readable; raw warning codes are not
  rendered.
- Unknown values remain unknown and are never replaced with guessed values.
- A future state that the player can achieve manually, such as an immediately
  available breakthrough, is labelled as a required step and is never shown
  as an already-active direction or effect.
- Expected and unexpected failures use safe presentation messages. Raw
  exceptions, paths, stack traces, IDs, and third-party diagnostic text are
  logged where appropriate, not displayed to the player.
- Critical uncertainty and its effect on a recommendation remain visible and
  are not hidden only inside a collapsed panel.

## Accessibility and interaction

- All controls support keyboard navigation and visible focus.
- Text carries every meaning conveyed by colour or iconography.
- Dynamic status and failures use appropriate live-region and semantic roles.
- Evidence and conditions do not require hover to access.
- Controls identify their purpose with localized, player-facing names.

## Architecture and testing

- Razor components consume Presentation models, never `GameData` types.
- Presentation models may retain internal IDs for correlation, but components
  use resolved display-name fields.
- xUnit v3 render tests cover important player-visible output in both language
  modes.
- Architecture tests reject raw IDs, warning codes, evidence references,
  mutation-oriented controls, and game-control event paths in UI source.
- NSubstitute is used when a UI test needs an Application use-case substitute.

Code review must apply these rules even when an automated test does not yet
recognize a new rendering or interaction pattern.
