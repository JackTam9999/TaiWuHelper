# M1-033 Presentation non-interference review

**Result:** Passed  
**Reviewed:** 2026-07-30  
**Scope:** `TaiWuAPI` presentation models, Razor components, controllers,
client helper script, and their Application entry points

## Boundary

TaiWu Helper is an information-only recommendation system. It may read the
configured save and accept helper-local observations, but it must never write
to a save, patch or control the game, automate input, attach to the game
process, or present a manual instruction as an automated action.

The internal method name `ExecuteAsync` means “run an Application use case”.
It is not a user-facing Execute action and neither implementation called by
the UI exposes a mutation port:

- `IFindTargets.ExecuteAsync` performs target lookup through a read-only port.
- `IRecommendCombatLoadout.ExecuteAsync` reads a snapshot and applies pure
  recommendation rules.

## UI event inventory

| UI event | Destination | Effect |
| --- | --- | --- |
| Search target | `SearchTargetsAsync` → `IFindTargets` | Reads the configured save; replaces helper target results |
| Select target | `SelectTarget` | Updates component-local selection only |
| Get recommendation | `GetRecommendationAsync` → `IRecommendCombatLoadout` | Reads a snapshot and calculates an in-memory recommendation |
| Retry/refresh read | `RetryLastReadAsync` | Repeats the previous target or recommendation read |
| Select style | `ShowStyle` | Changes the visible in-memory style |
| Focus/clear threat | `SelectThreat` / callback | Changes helper-local highlighting |
| Checklist toggle | `ManualChecklistState.Toggle` | Changes an in-memory `HashSet` only |
| Copy checklist | `navigator.clipboard.writeText` | Copies generated recommendation instructions to the system clipboard |
| Print recommendation | `window.print` | Opens the browser print flow for rendered helper content |
| Form input/binding | component form objects | Changes helper-local observation and preference values |

There is no export-to-file operation. Copy content contains generated
checklist instructions, not the configured save path, save hash, archive, or
game-owned bytes. Print uses the browser dialog and does not choose or write a
game-owned path.

## Checks performed

- Enumerated every Razor `onclick` and `onchange` handler and traced it to the
  destinations above.
- Scanned presentation C#, Razor, and JavaScript for file writes, destructive
  file operations, archive saves, process control, process-memory access,
  remote-thread injection, operating-system hooks, automated input, and
  Harmony patching.
- Verified the client helper exposes only clipboard copy and browser print;
  it has no network call, browser persistence, file picker, object URL, or
  download operation.
- Verified no user-facing button is named Apply, Equip, Execute, Repair,
  Patch, or Control game.
- Verified refresh/retry invokes the same cancellable read-only target or
  recommendation request and cannot select a different storage destination.
- Verified the UI repeatedly labels the result `Information only`,
  `Instructions only`, and manual configuration/setup.
- Re-ran architecture, presentation, application, domain, and opt-in
  integration test projects.

## Findings

No non-interference violation or open remediation item was found.

Clipboard and printing remain deliberate user-initiated browser operations.
They can copy or print only helper-rendered recommendation content. The helper
does not write those outputs to the game directory and does not send them
back to the game.

This review completes before M1-025 manual in-game verification. M1-025 must
continue to use manual player actions and compare observations without
attaching to, controlling, or modifying the game.
