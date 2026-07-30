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
- analyzed target threats;
- Safe, Balanced, and Aggressive style results from that one snapshot;
- the requested style;
- component scores;
- selected skill details and evidence-backed reasons;
- manual loadout changes;
- opening and pre-combat switching steps;
- assumptions and unavailable-data caveats; and
- snapshot, threat-analysis, and generation warnings.

Every threat, candidate, skill, reason, manual change, plan step, caveat, and
warning has a deterministic reference suitable for UI links.

A style with no feasible recommendation returns `hasRecommendation: false`
and a diagnostic. It does not fabricate a loadout.

## Errors

Request validation, invalid observation data, missing save files, and invalid
save data return an RFC problem response with HTTP 400. Request cancellation
is propagated instead of being converted into a validation error.

## Information-only guarantee

The endpoint offers only a recommendation POST. It has no route or operation
for applying a loadout, equipping a skill, changing a direction, controlling
combat, or writing game data.
