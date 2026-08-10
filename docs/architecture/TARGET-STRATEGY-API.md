# Target-strategy API projection

## Purpose

E5-007 adds one nullable final `targetStrategy` property to the existing
`POST /api/combat-recommendations` response. The server projects the immutable
profile, multi-label archetype matches, composed playbook, exact-target
adjustments, and player counter availability. A client displays the result; it
does not reclassify a target, resolve a playbook, or infer mechanics from text.

The addition is backward-compatible. Existing request and response properties
retain their shapes and meanings, and source consumers can continue to
construct `CombatRecommendationResponse` because the final property is
optional.

## Contract boundaries

The top-level `TargetStrategyResponse` contains five sections:

| Section | Meaning |
|---|---|
| `profile` | Versioned facets, typed values, state, provenance, conflicts, and diagnostics |
| `archetypes` | Every stable definition and its independent match result |
| `playbook` | Matched sources, composed goals/options, requirements, gaps, and conflicts |
| `adjustments` | Typed exact-target decisions and the evidence used for each |
| `counterAvailability` | Current-player feasibility/access state and unresolved gap |

Profile values never collapse missing or contradictory evidence into a false
default. `Confirmed` carries a typed value; `Incomplete` and `Unsupported`
carry an unavailable reason; `Conflicting` carries every typed candidate and
its provenance.

An archetype result retains supporting, missing, excluding, and conflicting
facet references. Only `Matched` definitions contribute playbook sources.
Partial, unsupported, conflicting, and not-matched definitions remain visible
without contributing a confirmed mechanical goal.

Counter options retain their stable code, skill/effect identity, direction,
strength, timing, threat references, and one of six typed requirement shapes:
weapon, trick, range, resource, weapon unlock, or skill activation. Player
availability remains a separate state; an inaccessible verified counter is
not replaced with a similar name or unverified option.

## Stable identity and localization

Stable codes, versions, enums, fingerprints, and evidence references are
language-neutral. The mapper localizes only display fields:

- archetype and response-goal titles;
- known-gap messages;
- target-adjustment explanations; and
- player access and generation diagnostic reasons.

English and Traditional Chinese responses therefore carry the same ordering
and identities. Clients must key links and state by stable identity, never by
localized text.

## Deterministic order

The API preserves the order established below the transport boundary:

1. profile facets by Domain dimension and stable facet identity;
2. archetype definitions by stable identity;
3. source playbooks, composed goals, options, conflicts, and gaps in composer
   order;
4. exact-target adjustments in adjustment-set order; and
5. counter availability in personalization order.

Mapping the same recommendation and language twice produces equivalent Epic 5
JSON. Changing language changes display strings only.

## Information-only and privacy boundary

The public contracts contain logical target, skill, effect, facet, threat,
goal, evidence, and diagnostic identities. They contain no save path, game
path, screenshot path, raw proprietary source text or payload, process handle,
persistence operation, game-data runtime object, snapshot object, or mutation
command. The existing endpoint still has no operation for applying a loadout
or changing the game.

## Representative JSON states

Property names below use the web JSON naming policy. Fragments omit unrelated
fields but retain the state-bearing structure.

### Complete match

```json
{
  "targetStrategy": {
    "profile": {
      "targetCharacterId": 16317,
      "ruleVersion": "1.0.0",
      "facets": [{
        "dimension": "Control",
        "code": "DISTRACTION_MARK_ACCUMULATION",
        "state": "Confirmed",
        "value": {
          "kind": "Presence",
          "code": "DISTRACTION_MARK_ACCUMULATION"
        },
        "evidence": [{
          "sourceKind": "InstalledConfiguration",
          "sourceVersion": "0.0.0.0-alpha.13-test"
        }]
      }]
    },
    "archetypes": [{
      "code": "MIND_RESONANCE_RESET_BASELINE",
      "version": "1.0.0",
      "title": "Mind resonance and defeat-reset chain",
      "state": "Matched"
    }],
    "playbook": {
      "sources": [{
        "archetypeCode": "MIND_RESONANCE_RESET_BASELINE",
        "playbookVersion": "1.0.0"
      }],
      "goals": [{
        "code": "CONTROL_DISTRACTION_MARKS",
        "title": "Control distraction marks",
        "responseTiming": "CombatStartPassive",
        "isEligible": true,
        "threatReferences": ["threat:DISTRACTION_MARK_ACCUMULATION"]
      }]
    }
  }
}
```

### Partial and unsupported

```json
{
  "partial": {
    "profileFacet": {
      "state": "Incomplete",
      "unavailableReason": { "code": "REQUIRED_SOURCE_UNAVAILABLE" }
    },
    "archetype": {
      "code": "MIND_RESONANCE_RESET_BASELINE",
      "state": "Partial",
      "missingFacets": [{ "code": "DEFEAT_MARK_RESET" }]
    },
    "playbookSources": []
  },
  "unsupported": {
    "profileDiagnostic": {
      "code": "UNSUPPORTED_GAME_DATA_VERSION",
      "severity": "Error"
    },
    "archetypeState": "Unsupported",
    "playbookSources": []
  }
}
```

### Conflicting evidence

```json
{
  "profileFacet": {
    "code": "OUTER_DAMAGE_CONFIGURED",
    "state": "Conflicting",
    "value": null,
    "conflictCandidates": [
      { "value": { "kind": "Measurements", "code": "A" } },
      { "value": { "kind": "Measurements", "code": "B" } }
    ],
    "unavailableReason": { "code": "SOURCE_CONFLICT" }
  },
  "archetype": {
    "code": "OUTER_DAMAGE_CONFIGURED",
    "state": "Conflicting",
    "conflictingFacets": [{ "code": "OUTER_DAMAGE_CONFIGURED" }]
  }
}
```

### Multi-match composition

```json
{
  "archetypes": [
    { "code": "CHANNEL_RESISTANCE_ASYMMETRY", "state": "Matched" },
    { "code": "MIND_RESONANCE_RESET_BASELINE", "state": "Matched" },
    { "code": "OUTER_DAMAGE_CONFIGURED", "state": "Matched" },
    { "code": "POISON_APPLICATION_CONFIGURED", "state": "Matched" }
  ],
  "playbook": {
    "sources": [
      { "archetypeCode": "CHANNEL_RESISTANCE_ASYMMETRY" },
      { "archetypeCode": "MIND_RESONANCE_RESET_BASELINE" },
      { "archetypeCode": "OUTER_DAMAGE_CONFIGURED" },
      { "archetypeCode": "POISON_APPLICATION_CONFIGURED" }
    ],
    "goals": [
      { "code": "SURVIVE_MIND_DAMAGE_PRESSURE", "isEligible": true },
      { "code": "PREPARE_FOR_OUTER_DAMAGE", "isEligible": true },
      { "code": "EXPLOIT_LESS_RESISTED_CHANNEL", "isEligible": true },
      { "code": "MITIGATE_CONFIGURED_POISON_APPLICATION", "isEligible": true }
    ]
  }
}
```

### Adjusted and player-filtered

```json
{
  "adjustments": {
    "items": [
      {
        "ruleCode": "AUTOMATIC_GOAL_CONTROL_DISTRACTION_MARKS",
        "action": "Retained",
        "originalResponse": {
          "kind": "Goal",
          "code": "CONTROL_DISTRACTION_MARKS"
        },
        "reasonCode": "EXACT_TARGET_SUPPORTS_RESPONSE",
        "reason": "Exact target evidence supports this response."
      },
      {
        "ruleCode": "AUTOMATIC_GAP_NO_GUARANTEED_RESET_LOCKOUT",
        "action": "Unresolved",
        "originalResponse": {
          "kind": "Gap",
          "code": "NO_GUARANTEED_RESET_LOCKOUT"
        }
      }
    ]
  },
  "counterAvailability": [
    {
      "counterCode": "REVERSE_JINNI_SUPPRESSION",
      "state": "Feasible",
      "gap": null
    },
    {
      "counterCode": "REVERSE_QILUN_TRUE_QI_DRAIN",
      "state": "Inaccessible",
      "accessIssues": [{ "code": "SkillNotLearned" }],
      "gap": {
        "kind": "InaccessibleVerifiedOption",
        "relatedCounterCode": "REVERSE_QILUN_TRUE_QI_DRAIN"
      }
    }
  ]
}
```

The same adjusted response in Traditional Chinese keeps every code and enum
unchanged while translating fields such as `reason` and the gap `message`.
