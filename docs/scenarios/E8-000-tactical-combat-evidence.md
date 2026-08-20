# E8-000: Exact-target tactical-combat evidence

| Field | Value |
|---|---|
| Status | Complete |
| Epic | [EPIC-008](../roadmap/epic-008/EPIC.md) |
| Backlog item | [E8-000](../roadmap/epic-008/BACKLOG.md#e8-000--verify-tactical-sources-and-select-the-golden-exact-target-vertical) |
| Inspection date | 2026-08-20 |
| Historical verified GameData | `1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a` |
| Installed GameData | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` |
| Sanitized record | [E8-000-golden-tactical-metadata.json](./evidence/E8-000-golden-tactical-metadata.json) |

## Purpose

Select the smallest trustworthy exact-target tactical vertical before adding
causal-chain, tactical-role, search, scoring, or plan contracts. The evidence
gate answers:

1. which already verified target mechanics can form an ordered causal chain;
2. which counter timing, execution cost, and recovery facts are exact;
3. which player execution-context facts are available without live process
   access;
4. whether the installed version may use the existing combat rules; and
5. whether a supported finish path exists for the first delivery.

## Method

The inspection used five read-only layers:

1. the completed Epic 1 and Epic 5 scenario, effect, threat, counter, and
   playbook evidence was reconciled;
2. the helper-owned SQLite catalogue was opened with `mode=ro&immutable=1`
   through the combat-loadout skill's query script;
3. the current save was read through the skill's hidden, hash-checked inspector
   from an isolated temporary source directory;
4. the new `TacticalCombatEvidenceIntegrationTests` projection read the same
   facts twice through `TaiwuArchiveReadSession`, tested cancellation, and
   compared seven game-owned sources before and after; and
5. the installed GameData and bilingual language resources were fingerprinted
   with read-only streams.

The first direct invocation of the legacy inspector could not compile because
the event compiler recursively included unrelated generated and probe source
files in the broad desktop workspace. The identical inspector and reader were
then copied to an isolated temporary source directory and completed normally.
No repository, skill, save, game file, or helper cache was changed to work
around the compiler's source-discovery behavior.

Only versions, public configuration identities, aggregate counts, evidence
states, timing bounds, and sanitized conclusions are committed. No local path,
save hash, character identity, target identity, item identity, raw save content,
game binary, complete language resource, or screenshot is committed.

## Version and source decision

| Source | Observation | Decision |
|---|---|---|
| Production combat rules | Pinned to the historical `68032…` GameData product version | Remain valid only for that exact historical rule version |
| Installed `GameData.dll` | Product version is now `3918…`; binary length and fingerprint differ from the historical evidence | Current installed version is unsupported by the existing production rules |
| Installed `GameData.Shared.dll` | Configuration assembly fingerprint differs from the pre-update catalogue manifest | The existing SQLite catalogue is stale for current-version authorization |
| SQLite catalogue | Schema 4; built 2026-08-05; 946 definitions; 4 warnings; 0 errors; opened immutable | Useful as historical/display evidence only until rebuilt outside this epic workflow |
| Combat-skill language packs | Traditional Chinese and English fingerprints still match the catalogue manifest | Display names and text are unchanged, but text equality does not prove runtime behavior |
| Special-effect language packs | Traditional Chinese and English fingerprints still match the catalogue manifest | Exact effect text may be compared; it cannot bypass the GameData version gate |
| Current save | Each guarded read preserved its exact revision; the save advanced between separate player activity captures | Current player facts are observational, not a pinned golden fixture |

The installed configuration still contains all seven selected counter
definitions with the expected Direct and Reverse effect IDs, all sixteen
Direct-practice magic-sound definitions with their expected effect IDs, and
the Reverse defeat-reset definition with effect `911`. This proves source-shape
continuity, not behavioral compatibility. Production must continue to return a
typed unsupported version instead of silently reusing the historical rules.

## Selected first vertical

The first delivery is a **historical-version synthetic magic-sound tactical
vertical with an explicit fallback-only finish**.

It is derived from the previously verified golden target mechanics without
committing or matching a current character identity. Synthetic fixtures use
only stable skill, effect, threat, state, and evidence identities already
present in the repository. The representative target validates the planning
model; it does not define every magic-sound opponent.

The installed `3918…` version is an explicit unsupported scenario in the first
vertical. Reauthorizing it requires a separate versioned rule decision based on
current behavior evidence, not merely unchanged IDs or localized descriptions.

## Causal-chain boundary

| Sequence | Typed fact or transition | Evidence state | Limitation |
|---:|---|---|---|
| 1 | One of sixteen exact Direct-practice type-13 attack signatures is active | Confirmed for historical rule version | Learned membership alone cannot establish active target use |
| 2 | The active signature creates positive magic-sound mind-damage pressure | Confirmed for historical rule version | Strength, hit probability, and cast frequency are not inferred |
| 3 | Mind-loss accumulation produces distraction marks | Confirmed by the existing golden mechanic evidence | The planner does not simulate accumulation or predict a mark time |
| 4 | The first mark starts the six-count mind-resonance path; later marks reduce it | Confirmed by the existing golden mechanic evidence | Runtime counter value must be observed manually |
| 5 | Zero count enters the verified mind-resonance cascade with repeated mind pressure and persistent marks | Confirmed by the existing golden mechanic evidence | The planner describes the trigger and response, not elapsed-time simulation |
| 6 | Reverse skill `287`, effect `911`, can clear defeat marks at the threshold through escalating Qiqiao true-Qi cost | Confirmed historical overlay | The exact next available reset depends on live target resource state |
| 7 | Reverse skill `604`, effect `1064`, interrupts, clears, and prevents Direct-practice skills during its cast | Confirmed historical hard counter | Requires the exact Reverse direction and a feasible active-attack context |
| 8 | After that cast, the user receives three layers preventing Direct-practice casts | Confirmed execution cost | No unrelated Direct-practice step may appear while a layer remains |
| 9 | Each subsequent Reverse-practice cast removes one layer | Confirmed general recovery transition | The evidence does not select three exact executable recovery skills for every snapshot |
| 10 | Reverse skill `291`, effect `915`, may pressure the reset resource through random true-Qi drain after a damage state | Confirmed mitigation | Random drain is not guaranteed to hit Qiqiao and is not a reset lockout |

The chain intentionally has no automatic target-state advancement, hidden AI
choice, damage calculation, turn prediction, or success probability.

## Counter and recovery boundary

| Role | Exact historical option | Timing | Decision |
|---|---|---|---|
| Core suppression | Reverse `604` / effect `1064` | Active attack | Selected primary response for a confirmed Direct-practice core cast |
| Mark-duration mitigation | Direct `267` / effect `165` | Equipped passive | Selected supporting option when exact direction and equipment are feasible |
| Resonance-duration mitigation | Reverse `134` / effect `973` | Active agility | Selected supporting option only while that agility skill is active |
| Hindrance-mark removal | Reverse `686` / effect `1422` | Combat-start passive | Historical verified option; unavailable when the player lacks the exact completed direction |
| Attack-power reduction | Reverse `624` / effect `1234` | Active attack | Historical mitigation; must still pass direction, element/backlash, and execution gates |
| Reset pressure | Reverse `291` / effect `915` | Equipped passive | Selected mitigation with an explicit random-resource limitation |
| Conditional mark transfer | Reverse `611` / effect `1165` | Active attack plus weapon release | Retained as conditional evidence; not a generic recovery step |

The general recovery transition after Reverse `604` is exact: perform three
otherwise feasible Reverse-practice casts to remove its three layers. The first
vertical does not preselect those three casts. Weapon, style, trick, distance,
resource, active-role, and other live requirements must be satisfied by the
future execution context; otherwise the plan exposes an unresolved recovery
branch.

## Finish-path decision

No guaranteed finish path passed E8-000.

- Random true-Qi drain from Reverse `291` does not guarantee Qiqiao depletion.
- The reset's escalating cost does not reveal the live amount or the exact next
  reset opportunity in a standalone snapshot.
- The current scoring component accepts optional caller-supplied damage rather
  than a versioned attack/hit/defense/cast formula.
- Static attack steps, hit values, penetration, and resistance fields do not by
  themselves prove exact live damage or a defeat window.

The first plan must therefore use `FallbackOnly` finish state. It may explain
how to preserve suppression and apply supported reset pressure, but it may not
claim a primary finish, expected damage, or probability of victory. A later
Epic 8 item may add a finish rule only after separate typed evidence passes the
same version gate.

## Execution-context inventory

| Context fact | Current source status | First-vertical decision |
|---|---|---|
| Learned skill, exact direction, effect IDs, mastery, and breakthrough evidence | Available from the immutable combat snapshot | Required hard-gate input |
| Current equipment and stable weapon subtype | Available from the save for supported items | Required where a rule names a weapon family |
| Current/proposed skill membership | Available, with separate snapshot and proposal origins | Required; never infer active use from equipment alone |
| Configured category capacity and generic-slot allocation | Partially available; effective used cost requires verified cost rules | Preserve unavailable used values and run the existing feasibility validator |
| Legendary-book fixed-cost assignments | Available only when an exact saved assignment exists | No assignment means no fixed-cost discount |
| Inner-power state and configured backlash element | Available from persisted base proportions with runtime modifiers omitted | Required hard gate for active casts; runtime modifiers stay unavailable |
| Active defense and active agility choice | Proposed request context, not current live combat state | Must be selected explicitly for a paper plan |
| Equipped and unlocked weapon sets | Supported by the existing requirement contract when supplied | Unknown sets are not empty sets |
| Trick counts, distance, and typed combat resources | Supported by the existing requirement contract when supplied | Missing values create unknown requirements |
| Usable generated combat styles | No standalone-safe first-vertical projection | Unsupported |
| Live stance, breath, target resource, exact current distance, marks, resonance count, and temporary layers | Runtime-only or manually observable | Manual confirmation or unresolved branch; no process access |
| Hidden AI selection, cast queue, live hit chance, and live damage | Not available | Outside the epic's planning model |

## Guarded local evidence

The opt-in integration run completed with these non-proprietary conclusions:

| Observation | Result |
|---|---:|
| Learned skill records in the captured revision | 715 |
| Equipped skill records across categories | 26 |
| Selected candidate definitions matching exact IDs | 7 of 7 |
| Direct magic-sound definitions matching exact IDs | 16 of 16 |
| Reverse reset definition/effect match | Confirmed |
| Selected candidates known, equipped, and direction-ready | At least 3 in each applicable aggregate |
| Equipped weapon subtype mapping | Complete for the captured equipped weapons |
| Generic-allocation value count | 4 |
| Pre-cancelled read | `OperationCanceledException` observed before projection |
| Cold in-process archive budget | Passed, at most 30 seconds |
| Warm unchanged-revision budget | Passed, at most 3 seconds |
| Guarded files unchanged | 7 of 7 |
| Focused test result | 1 passed, 0 failed, 0 skipped |

The isolated legacy inspector also completed with `saveModified=False`. Its
initial compile/read completed in about 20.4 seconds; a later process-level
read completed in about 13.6 seconds. These timings include starting a separate
GameData host and are not the production archive cache budget.

The save fingerprint changed between two separate successful inspector runs
because the save advanced outside the bounded reads. Each individual run and
the integration test proved its own captured revision unchanged. This is why
current player membership and direction facts are not committed as the golden
scenario.

Verification command:

```powershell
$env:TAIWU_INTEGRATION_SAVE_PATH = '<current-save>'
dotnet test tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj -c Release --no-restore -- --filter-class '*TacticalCombatEvidenceIntegrationTests*'
```

## Representative scenarios

| Scenario | Synthetic evidence | Expected state |
|---|---|---|
| `E8-REP-SYN-HISTORICAL-001` | Historical rule version, complete Direct magic-sound chain, Reverse `604`, exact feasible context | Suppression and mitigation stages available; finish remains fallback-only |
| `E8-REP-SYN-RECOVERY-001` | Reverse `604` completed and three separately feasible Reverse casts exist | Three ordered conditional recovery transitions remove the lock layers |
| `E8-REP-SYN-RECOVERY-GAP-001` | Reverse `604` completed but fewer than three executable Reverse casts are supported | Recovery unresolved; do not schedule Direct-practice actions |
| `E8-REP-SYN-RESET-001` | Reset signature present and Reverse `291` is feasible | Show random-resource mitigation and no guaranteed lockout |
| `E8-REP-SYN-FALLBACK-001` | No typed finish evidence | `FallbackOnly`; no damage or victory claim |
| `E8-REP-SYN-CONTEXT-001` | A required weapon, trick, distance, resource, or active-role fact is unknown | Unknown requirement and manual confirmation branch |
| `E8-REP-SYN-CURRENT-VERSION-001` | Installed `3918…` version with unchanged public IDs | `Unsupported`; never apply the historical rule set |
| `E8-REP-SYN-CONFLICT-001` | Save and confirmed observation disagree on an active target signature | Preserve both sources and return a conflict branch |
| `E8-REP-SYN-TRUNCATED-001` | A later search reaches an option, exploration, time, or result bound | Bounded result with no completeness or optimality claim |

## Resolved decisions

1. Authorize E8-001 and E8-002 against the historical-version synthetic
   vertical and make current-version mismatch a first-class state.
2. Keep the sixteen Direct magic-sound signatures and Reverse reset overlay as
   the exact target-chain boundary; learned target skills alone remain
   insufficient.
3. Use Reverse `604` as the primary suppression transition and represent its
   three-layer Direct-practice lock as an execution cost.
4. Represent recovery as three conditional feasible Reverse casts without
   inventing a universal exact sequence.
5. Preserve mark, resonance, and reset options as distinct mitigation layers;
   do not double-count them as one flat threat.
6. Ship the initial tactical model with a fallback-only finish state until
   typed finish evidence exists.
7. Treat the installed catalogue as stale for authorization and do not rebuild,
   migrate, clear, or modify it inside this workflow.
8. Preserve deterministic, cancellable, bounded, one-snapshot, and byte-for-byte
   non-interference requirements for every later item.

## Deferred evidence

- Current-version behavioral reauthorization for every selected transition and
  tactical role.
- A fresh exact-target equipped loadout for a currently active magic-sound
  opponent.
- Standalone-safe usable combat-style, stance, breath, live distance, target
  resource, mark, resonance, and temporary-layer facts.
- A typed damage, hit/cast reliability, defense/resistance, and finish-window
  formula.
- An exact three-skill recovery sequence for every feasible player snapshot.
- Outcome persistence, causal learning, simulation, probability, screenshot
  interpretation, process access, automation, and game control.

No save, game file, installed language resource, helper catalogue, running
process, runtime memory location, or in-game state was modified.
