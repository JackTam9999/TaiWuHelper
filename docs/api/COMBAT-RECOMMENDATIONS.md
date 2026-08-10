# Combat recommendation API

## Endpoint

`POST /api/combat-recommendations`

The endpoint reads the save configured in
`SaveGames:DefaultSaveFilePath`. A client cannot submit a different save path
in this request.

Minimal request:

```json
{
  "targetCharacterId": 16317,
  "objective": "Safe"
}
```

`targetCharacterId` is required and must be positive. `objective` accepts
`Safe`, `Balanced`, or `Aggressive` and defaults to `Balanced`.

## Optional current-screen observation

A client may report a newer loadout visible in the game UI:

```json
{
  "targetCharacterId": 16317,
  "objective": "Balanced",
  "currentScreenObservation": {
    "observedAt": "2026-07-30T13:00:00Z",
    "evidenceReference": "ui:current-screen",
    "equippedSkills": {
      "neigongSkillIds": [],
      "attackSkillIds": [],
      "agilitySkillIds": [],
      "defenseSkillIds": [],
      "assistanceSkillIds": []
    },
    "genericSlotAllocation": {
      "totalSlots": 0,
      "attack": 0,
      "agility": 0,
      "defense": 0,
      "assistance": 0
    },
    "displayedSlotBudgets": {
      "neigong": { "used": 6, "capacity": 6 },
      "attack": { "used": 10, "capacity": 10 },
      "agility": { "used": 8, "capacity": 8 },
      "defense": { "used": 8, "capacity": 8 },
      "assistance": { "used": 2, "capacity": 2 }
    }
  }
}
```

`displayedSlotBudgets` is optional. Supply all five used/capacity pairs when
the current game screen shows exact values; these override configured
capacities whose runtime modifiers cannot be evaluated safely by the
standalone reader.

This observation is helper input only. The snapshot adapter may merge it when
it is newer than the disk save, and the recommendation pipeline then analyzes
that immutable result. The endpoint does not send anything to the game or
change either source.

## Response

The typed response includes:

- snapshot capture time, game-data version, and a shared snapshot reference;
- the current inner-power state name, configured effect description, and
  backlash-on-use element when available;
- analyzed target threats;
- Safe, Balanced, and Aggressive style results from that one snapshot;
- an additive `comparison` object containing Current, Safe, Balanced, and
  Aggressive columns from that same immutable result;
- the requested style;
- component scores;
- selected skill details and evidence-backed reasons;
- manual loadout changes, including a required breakthrough when the save
  proves its exact direction is immediately achievable;
- opening and pre-combat switching steps;
- assumptions, unavailable-data caveats, and known inner-power risks for
  actively cast skills; and
- snapshot, threat-analysis, and generation warnings.

Every threat, candidate, skill, reason, manual change, plan step, caveat, and
warning has a deterministic reference suitable for UI links.

A style with no feasible recommendation returns `hasRecommendation: false`
and a diagnostic. It does not fabricate a loadout.

For each selected skill, `requiresBreakthrough` is separate from
`requiresManualDirectionChange`. A breakthrough prerequisite means the effect
is usable only after the player completes that step; it is not current combat
state.

Each successful style also returns `genericSlots`, containing the total
available generic slots and proposed allocation for attack, agility, defense,
and assistance. The UI checklist shows an allocation step only when the
proposed value differs from the current allocation.

### Loadout comparison

`comparison.columns` is always ordered Current, Safe, Balanced, Aggressive.
Available columns expose five ordered category rows, typed skill membership,
separate direction/breakthrough actions, capacity, effective cost, and 萬用
allocation. Policy columns also expose covered/unresolved threats, conditions,
caveats, evidence, manual-action count, active roles, and policy-local score
components.

Unavailable numeric, membership, direction, name, role, and allocation facts
use an explicit object containing `isAvailable`, nullable `value`, and
`unavailableReason`. An unavailable value is never returned as zero or an
empty feasible loadout.

An infeasible or missing policy remains a column with `status` and
`diagnostic`; its `loadout` and `tacticalSummary` are null. Current provenance
states whether equipped skills, slot budgets, 萬用 allocation, and
legendary-book assignments came from the save or a current-screen observation.

The comparison score notice states that scores rank candidates only inside
their own policy and are not win odds. Clients must not compare totals as a
universal ranking.

The change is backward-compatible and additive. Existing fields retain their
shape. `snapshotReference` remains opaque; clients must compare the full value
and not parse its suffix. See
[the comparison API contract](../architecture/LOADOUT-COMPARISON-API.md) for
the complete projection and versioning rules.

### Target profile and strategy

Successful responses also include an additive `targetStrategy` object. It
projects the exact immutable Epic 5 result rather than asking a client to
classify the target or rebuild a playbook from display text. It contains:

- typed profile facets, values, measurements, evidence state, provenance,
  unavailable reasons, conflicts, and diagnostics;
- every archetype result with stable code/version, match state, and
  supporting, missing, excluding, or conflicting facet references;
- the deterministically composed playbook sources, response goals, threat and
  counter references, timing, typed requirements, conflicts, and known gaps;
- exact-target adjustments and their typed evidence; and
- player-specific counter availability, access issues, generation diagnostics,
  and unresolved gaps.

`code`, version, enum, identity, and evidence-reference fields are stable and
language-neutral. Only `title`, `message`, `reason`, and other explicit display
strings change with the request language. A partial, unsupported, conflicting,
or no-match result remains a typed state and never becomes a fabricated
playbook.

The response preserves Domain/Application order. In particular, facet,
archetype, source-playbook, goal, option, gap, adjustment, and availability
arrays must not be re-sorted by localized text. The contract exposes no save or
game path, screenshot path, proprietary source text, process identity,
persistence command, or game-mutation type.

The property is nullable only for additive source compatibility with older
responses. Recommendations produced by the current Epic 5 pipeline populate
it. See [the target-strategy API contract](../architecture/TARGET-STRATEGY-API.md)
for field rules and complete, partial, unsupported, conflicting, multi-match,
and adjusted examples.

## Errors

Request validation, invalid observation data, missing save files, and invalid
save data return an RFC problem response with HTTP 400. Request cancellation
is propagated instead of being converted into a validation error.

## Information-only guarantee

The endpoint offers only a recommendation POST. It has no route or operation
for applying a loadout, equipping a skill, changing a direction, controlling
combat, or writing game data.
