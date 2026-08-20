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

## Optional tactical planning

Add `tacticalPlanning` to request the Epic 8 read-only tactical projection.
The client still cannot supply a save path, player identity, mechanics, score,
loadout, or plan. The configured snapshot supplies the player and proposal
baseline. The request supplies only exact observations and bounded-search
controls:

```json
{
  "targetCharacterId": 16317,
  "objective": "Balanced",
  "tacticalPlanning": {
    "observations": [
      {
        "identity": "TARGET_DIRECT_MAGIC_SIGNATURE_ACTIVE",
        "scope": "ExactTarget",
        "source": "ConfirmedObservation",
        "disposition": "Confirmed",
        "evidenceIdentity": "OBSERVED_TARGET_DIRECT_MAGIC",
        "scopeIdentity": "EXACT_TARGET"
      },
      {
        "identity": "MAGIC_SOUND_DIRECT_EFFECT_VERIFIED",
        "scope": "BroadRule",
        "source": "InstalledConfiguration",
        "disposition": "Confirmed",
        "evidenceIdentity": "INSTALLED_RULE_EVIDENCE",
        "scopeIdentity": "BROAD_RULE"
      }
    ],
    "bounds": {
      "maximumOptions": 16,
      "maximumExploredCombinations": 65536,
      "maximumElapsedMilliseconds": 2000,
      "maximumResults": 256
    }
  }
}
```

Observation identity, scope, and source triples must belong to the published
version-1 tactical rule set. Unknown identities, numeric enum tokens,
duplicates, malformed observations, and bounds outside the Domain limits are
rejected with a safe HTTP 400 problem. Evidence and scope identities are
stable, language-neutral tokens; they are not display copy. An empty
`observations` array is valid and produces an honest partial-evidence result.

The current-screen player-loadout observation can be combined with tactical
planning. The older target-skill observation workflow is a separate merge
mode and cannot be combined in the same request; the endpoint returns
`INCOMPATIBLE_OBSERVATION_MODES` rather than mixing revisions.

### Tactical response

The additive `tacticalPlanning` response contains:

- typed status, stable reason, semantic identity, and snapshot summary;
- rule-version status plus every canonical transition and role match;
- current and proposed execution facts with available, incomplete,
  unsupported, or conflicting state and typed values;
- both Direct and Reverse consideration for every learned skill, including all
  hard gates, evidence, support, admission, and terminal decision;
- proved pruning, every retained feasible result, declared bounds, first
  terminator, exact coverage counts, elapsed diagnostic, and cache diagnostics;
- policy-local component inputs, availability, normalized values, applied
  weights, contributions, limitations, and neutral unused capacity;
- the selected non-empty loadout with category skills, capacity, and 萬用
  allocation; and
- preparation, opening, trigger, recovery, finish, and fallback stages with
  typed branches, requirements, evidence, and unsupported states.

Arrays retain Domain/Application canonical order. Clients must not re-sort
stages, candidates, gates, components, or evidence by localized text. Tactical
contracts contain no localized mechanical claims; a UI may add display text
without replacing the returned identities or enums.

Representative state fragments follow. Fields are abbreviated only in this
documentation; the HTTP response retains the complete typed structures.

Complete result:

```json
{
  "status": "Success",
  "reasonIdentity": "TACTICAL_PLAN_COMPILED",
  "hasTacticalPlan": true,
  "search": { "isComplete": true, "isOptimal": false },
  "plan": {
    "finishDisposition": "Unsupported",
    "stages": [
      { "stage": "Preparation", "state": "Supported" },
      { "stage": "Opening", "state": "Supported" },
      { "stage": "Trigger", "state": "Supported" },
      { "stage": "Recovery", "state": "Unsupported" },
      { "stage": "Finish", "state": "Unsupported" },
      { "stage": "Fallback", "state": "Unsupported" }
    ]
  }
}
```

Partial or conflicting evidence returns HTTP 206 and retains the unresolved
chain and gate states:

```json
{
  "status": "PartialEvidence",
  "reasonIdentity": "TACTICAL_EVIDENCE_PARTIAL",
  "targetChain": {
    "transitions": [
      {
        "identity": "DIRECT_MAGIC_CAST_CREATES_MIND_PRESSURE",
        "applicability": "Incomplete",
        "unmetEvidence": ["TARGET_DIRECT_MAGIC_SIGNATURE_ACTIVE"]
      }
    ]
  }
}
```

An unsupported installed GameData version remains an HTTP 200 typed result so
legacy recommendation and comparison clients still work:

```json
{
  "status": "UnsupportedChain",
  "reasonIdentity": "UNSUPPORTED_GAME_DATA_RULE_CHAIN",
  "hasTacticalPlan": false,
  "targetChain": {
    "status": "UnsupportedGameDataVersion",
    "transitions": [],
    "roles": []
  }
}
```

A bounded result returns HTTP 206, never claims optimality, and may contain a
coherent plan found before the bound:

```json
{
  "status": "SearchTruncated",
  "reasonIdentity": "BOUNDED_SEARCH_TRUNCATED",
  "search": {
    "isComplete": false,
    "isOptimal": false,
    "coverage": { "firstTerminator": "ExplorationLimit" }
  }
}
```

When separately verified recovery or finish proofs exist, the same stage
shape can expose `finishDisposition: "FallbackOnly"`; the Finish stage remains
`Unsupported` while the Fallback stage is `Supported`. This state never means
predicted victory or damage. The historical public fixture currently has no
approved finish proof and therefore reports `Unsupported`.

```json
{
  "plan": {
    "finishDisposition": "FallbackOnly",
    "stages": [
      { "stage": "Finish", "state": "Unsupported" },
      { "stage": "Fallback", "state": "Supported" }
    ]
  }
}
```

Applying an observation replaces the entire semantic result. Repeating the
same observation produces the same identity; replacing it changes chain,
candidate, score, plan, and identity together; clearing back to the empty set
reproduces the empty-set identity for the same snapshot. Capture time and
elapsed milliseconds remain diagnostics and do not alter semantic identity.

Caller cancellation is propagated and publishes no response body or partial
plan. Expected tactical source/evidence/context problems use safe HTTP 400
problem codes. Rule/search/scoring/planning and unexpected boundary failures
use safe HTTP 500 problems without exception text, local paths, or proprietary
payloads.

## Errors

Request validation, invalid observation data, missing save files, and invalid
save data return an RFC problem response with HTTP 400. Request cancellation
is propagated instead of being converted into a validation error.

## Information-only guarantee

The endpoint offers only a recommendation POST. It has no route or operation
for applying a loadout, equipping a skill, changing a direction, controlling
combat, or writing game data.
