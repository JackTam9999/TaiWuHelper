# E8-F03: Current-version tactical role atlas

| Field | Value |
|---|---|
| Status | Complete — exact roles and learned-skill outcomes verified |
| Backlog item | [E8-F03](../roadmap/epic-008/BACKLOG.md#e8-f03--expand-current-version-typed-tactical-role-coverage) |
| Inspection date | 2026-08-21 |
| Runtime GameData | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` |
| Rule fingerprint | `64051C1234CECDFDCE070134FDA0380826154D16C1F171B52B6F7FE1C64ECD5D` |
| Sanitized record | [E8-F03 metadata](./evidence/E8-F03-current-tactical-role-metadata.json) |

## Decision

The exact E8-F02 later phase now resolves a current-version tactical rule set
with 21 typed transitions and 19 exact player roles. A role is selected only
by GameData version, skill ID, practice direction and raw effect ID. Typed
mechanics, timing, purpose, activation kind, requirements, evidence and
limitations are part of the same immutable contract; localized text never
selects or broadens a role.

This authorizes role discovery, not an unconditional loadout recommendation.
Direction, breakthrough, active-role, weapon subtype, usable tricks, distance,
true Qi, stance, breath and inner-power backlash remain hard gates. A missing
live value is `Unknown`, never satisfied.

## Exact role set

| Group | Exact direction/effect roles | Use boundary |
|---|---|---|
| Suppression | `604` Reverse/`1064` | Active attack; applies a three-layer Direct-practice lock |
| Lock recovery | `599` Reverse/`1059`, `602` Reverse/`1062`, `616` Reverse/`1251`, `686` Reverse/`1422` | Active attacks; one layer is removed only after an executable Reverse cast |
| Agility/control | `134` Reverse/`973`, `150` Reverse/`989`, `151` Reverse/`990`, `147` Direct/`260`, `148` Direct/`261` | Active agility and switch-only choices; exact range/manual gates remain |
| Defense | `295` Reverse/`919`, `303` Reverse/`927`, `2` Direct/`1739`, `289` Direct/`187` | Active defense and switch-only choices; only one can be active |
| Equipped support | `267` Direct/`165`, `265` Reverse/`889`, `280` Reverse/`904`, `252` Direct/`150` | Equipped passives; equipment, distance or manual input still applies |
| Opening alternative | `624` Reverse/`1234` | Active opening/persistent attack; pure-Fire backlash remains a hard gate |

The current behavior evidence does not authorize current-version roles for
historical alternatives `291` or `611`, so they remain irrelevant or
unsupported rather than inheriting stale semantics.

## Recovery is conditional

The four recovery attacks carry the exact
`RemoveOwnDirectPracticeLockLayerOnReverseCast` mechanic, but the role label
does not prove that a cast can happen. Each candidate also requires:

- its exact Reverse direction and raw effect;
- Blade subtype `9` for `599`, `602` and `616`, or Whisk subtype `6` for
  `686`;
- the exact stance and breath cost (`60` or `80`);
- manually confirmed usable weapon tricks; and
- no active inner-power backlash for the skill element.

Consequently the guarded current-save atlas admits no recovery cast while
live trick/resource evidence is absent. It does not promise a three-cast
recovery package merely because four typed recovery roles exist.

## Learned-skill atlas outcomes

Every learned skill still produces canonical Direct and Reverse entries. The
current role set distinguishes:

- `Admitted`: an exact role with every captured hard gate satisfied;
- `Rejected`: an exact role whose direction, activation, equipment, distance,
  resource or backlash gate fails;
- `Unsupported`: a known role requested in the wrong direction, an unresolved
  version/effect, or a role whose required live context is not yet known; and
- `Irrelevant`: a learned skill with no role for this exact target goal set.

An irrelevant skill already equipped by the player remains `RetainedOnly` so
discovery does not silently remove the current loadout. Search accounts these
preclassified irrelevant entries as explicit pruning results. Dominance is a
later search proof and is not invented during discovery.

## Current-screen precedence

The guarded integration uses the confirmed current-screen capacities
`6/10/7/9/4`, which supersede the older disk capacities `6/9/6/10/5`. The
screen did not capture used-slot totals, so those totals remain unavailable.
The test also exposed and fixed a current-loadout cost bug: a saved or
screen-observed legendary-book assignment is now evaluated as current state,
not incorrectly passed to the proposed-assignment calculator.

## Verification and boundary

Focused Domain tests pin all 19 roles, 21 transitions, the rule fingerprint,
typed mechanics, use kinds, recovery requirements, manual gates, exact-version
resolution and learned-atlas accounting. One guarded F03 integration check
binds the E8-F02 phase, current save, current screen capacities and full atlas;
it proves deterministic admitted, rejected, unsupported and irrelevant
outcomes while every inspected file retains its hash, length and timestamp.
The Release solution build completed with zero warnings or errors; the full
suite passed 1,598 of 1,621 tests with 23 expected guarded-local skips and no
failures.

E8-F03 completes role authorization. E8-F04 must next project the remaining
live weapon, trick, distance, resource, active-defense and active-agility facts
into one coherent current/proposed execution context before search may claim a
fully executable loadout.
