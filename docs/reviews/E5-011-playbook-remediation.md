# E5-011 playbook-family remediation

| Field | Value |
|---|---|
| Status | Complete — awaiting product-owner review |
| Evidence date | 2026-08-10 |
| Epic | [EPIC-005](../roadmap/epic-005/EPIC.md) |
| Backlog item | [E5-011](../roadmap/epic-005/BACKLOG.md#e5-011--deliver-playable-family-counters-and-reusable-overlays) |

## Why E5-010 was reopened

Independent review accepted the architecture and safety boundary but rejected
the product-readiness claim. The three new families were gap-only, the
mind/resonance baseline unnecessarily required a separate defeat-reset
signature, production had no reviewed replacement rules, and a dormant
multi-evidence matcher accepted `any` required state instead of `all`.

## Delivered counter paths

All entries are exact GameData-version-gated effects and typed counter rules.
Display names and raw descriptions do not create mechanical claims.

| Family | Reviewed option | Purpose |
|---|---|---|
| Configured outer damage | Reverse 伏龍刀法 | Reduce all enemy 摧破 power for the battle |
| Configured poison | Direct 五黃辟毒術 | While actively defending, prevent direct poison and reduce the enemy's corresponding poison |
| Configured poison | Reverse 五黃辟毒術 | While actively defending, prevent direct poison and reflect applied poison to the enemy |
| Outer resistance higher than inner | Direct 錯倒陰陽拂塵 | Route own direct outer injury through the target's lower inner resistance |
| Inner resistance higher than outer | Reverse 錯倒陰陽拂塵 | Route own direct inner injury through the target's lower outer resistance |

The mind/distraction/resonance response is now one reusable playbook. Defeat-
mark reset is an independent overlay, so absence of reset evidence no longer
blocks otherwise applicable mind counters.

## Exact-target customization

Production now receives the reviewed channel-resistance adjustment rules. A
confirmed `outer > inner` relation replaces the reverse channel route with the
direct route; confirmed `inner > outer` does the inverse. Required evidence is
matched per identity, so every required identity must carry the required
state. Unknown, incomplete, unsupported, contrary, or conflicting evidence
cannot trigger a replacement.

Both directions remain visible in feasibility reporting. Before bounded
loadout generation, options are grouped by skill and one direction is selected
deterministically, preferring current-player accessibility and then verified
strength/stability. This prevents duplicate-skill alternatives without hiding
direction-specific access evidence.

## Read-only evidence

The project-provided read-only catalogue inspection reported a healthy schema
4 catalogue for GameData version `1.0.0`. It confirmed exact direct/reverse
effects and costs for 五黃辟毒術 and 錯倒陰陽拂塵, plus the existing reverse
伏龍刀法 effect. A read-only current-save inspection confirmed reverse
五黃辟毒術 is learned and breakthrough-active, while the other two reviewed
skills are currently inaccessible. No save, GameData, cache, language, or
runtime source was modified. Machine paths, source fingerprints, save content,
and proprietary runtime content are intentionally absent from this record.

## Verification

- Domain tests cover exact effects, counter rules, five playbooks, split reset
  matching, deterministic composition, production replacement, and all-state
  multi-evidence matching.
- Application tests prove a confirmed poison target selects an owned reverse
  五黃辟毒術 candidate through the existing feasibility and bounded generator.
- API and Presentation tests preserve the split identities and bilingual
  strategy states.
- The guarded current-save vertical evaluates every registered family,
  repeats/apply/clear deterministically, and preserves every inspected source.
- The local Traditional Chinese strategy workflow renders one available panel,
  one verified outer counter card, one adjustment section, and one feasibility
  section without document overflow after rebuilding the remediated
  application. The obsolete outer-counter gap is absent.
- The Release build passed with zero warnings and errors. The default matrix
  passed **1,052 total; 1,043 passed; 0 failed; 9 expected opt-in skips**.
  Domain passed 417/417 and Application passed 138/138.
- The guarded current-save vertical passed 1/1 in about 29 seconds and retained
  its source-preservation guard.
- `dotnet format TaiWu.slnx --no-restore` and `git diff --check` pass on the
  final worktree.

## Decision boundary

The independent review's four actionable findings are resolved. Epic 5 remains
open for the product owner's review of the remediated behavior and explicit
completion decision.
