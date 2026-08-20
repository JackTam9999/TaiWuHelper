# E8-012 automated verification

| Field | Result |
|---|---|
| Date | 2026-08-21 |
| Scope | Epic 8 evidence fidelity, safety, bounds, determinism, parity, and presentation |
| Decision | E8-012 criteria pass; E8-013 product-owner closure remains required |
| Proprietary data committed | None |

## Focused verification inventory

| Layer | Focused result | Principal evidence |
|---|---:|---|
| Domain tactical namespace | 97 passed | contracts, rules, context, discovery, pruning, all bounds, scoring, plan, fingerprints |
| Application tactical namespace | 25 passed | one snapshot, call counts, cancellation, atomic observation lifecycle, failure states, complete identity parity |
| API and Presentation tactical classes | 34 passed | state preservation, safe public tokens, bilingual typed coverage, semantic rendering |
| Tactical architecture safety | 6 passed | no localized matching, unbounded request, path/payload, file/process/network/screenshot/upload/persistence/game-control capability |
| Guarded local integration | 2 explicit skips in this environment | E8-000/E8-004 require `TAIWU_INTEGRATION_SAVE_PATH`; archived authorized evidence is retained |

## E8-012 criterion matrix

| Criterion | Evidence | Result |
|---|---|---|
| Domain invariants | 97 focused Domain tests and [Domain rule coverage](../testing/DOMAIN-RULE-COVERAGE.md#epic-8-tactical-combat-rules) | Pass |
| One coherent Application result | `RecommendTacticalCombatTests`, including exact work counts and observation replacement | Pass |
| Guarded Infrastructure reads | `TacticalCombatEvidenceIntegrationTests`, `TacticalExecutionContextIntegrationTests`, and [local gate documentation](../testing/LOCAL-GAMEDATA-INTEGRATION-TESTS.md#epic-8-completion-verification) | Pass with explicit local skips |
| API/Presentation state retention | `TacticalCombatApiTests` and `TacticalCombatRenderingTests` cover partial, conflict, unsupported, rejection, pruning groups, every bound, unavailable scores, plans, and lifecycle states | Pass |
| Localization completeness | exhaustive enumeration of every fixed UI key and typed stage/status/policy/finish/condition/candidate/score/terminator/direction/source value | Pass |
| Semantic safety | `TacticalExecutionContextSafetyTests` scans all tactical Domain/Application source and reflects public Domain/API contracts | Pass |
| Repeated identity | `Repeated_and_shuffled_requests_retain_every_semantic_identity` compares chain, context, rule, discovery, search, coverage, score, selected loadout, plan, comparison, order, caches, work counts, and final identity | Pass |
| Shuffled inputs and bounds | canonical-order tests at contract, discovery, search, scoring, and plan layers; exact option/exploration/time/result fixtures | Pass |
| Policy separation | complete-evidence and unavailable-finish fixtures retain three distinct policy fingerprints and limitations | Pass |
| Performance and reuse | E8-000 cold/warm evidence plus deterministic candidate/feasibility cache-count fixtures | Pass |
| Bilingual responsive rendering | [E8-011 browser record](assets/epic-008/E8-011-browser-verification.md) and semantic component tests | Pass |
| Release build and suite | Final command and counts are recorded in the release gate below | Pass |
| Guarded skips | both local tactical tests emit an exact actionable skip when the save path is absent | Pass |
| Epic traceability | the following acceptance matrix links every Epic criterion; product-owner closure is intentionally pending E8-013 | Pass |

## Determinism and parity detail

The end-to-end repeated/shuffled fixture compares every semantic fingerprint
published by the Application result. It also compares feasible-result order,
compiled stage order, legacy scoring candidate order, comparison identity,
cache diagnostics, and one-per-stage work counts. Capture time and elapsed
duration remain diagnostic and are intentionally absent from semantic identity.

The selected tactical plan always retains the unchanged feasible loadout that
was scored. If the existing Epic 4 policy comparison is available, its proposed
skills and universal-slot allocation must match that tactical proposal exactly.
If the legacy comparison is unavailable, it contains only its diagnostic and no
competing proposed loadout; it therefore cannot contradict the tactical plan.
Compiler tests separately prove that every preparation instruction matches the
selected proposal and direction.

## Performance and source evidence

- E8-000 authorized local evidence: cold archive read at most 30 seconds; warm
  unchanged-revision read at most 3 seconds; 7 of 7 guarded sources unchanged.
- Candidate-projection fixture: 2 misses, at least 1 hit, at most 4 accesses.
- Feasibility fixture: 4 canonical misses and 0 false hits.
- Successful Application request: exactly one snapshot, legacy recommendation,
  comparison, rule resolution, context projection, discovery, search, score,
  and plan compilation.
- Current unconfigured environment: the two local tactical tests skip before
  reading and identify the required environment variable without exposing a
  path.

## Epic acceptance traceability

| Epic criterion | Implementation or evidence | State |
|---|---|---|
| E8-000 versioned evidence and representative vertical | [E8-000 evidence](../scenarios/E8-000-tactical-combat-evidence.md) | Verified |
| Typed causal mechanics, not name matching or simulation | `TaiWu.Domain.TacticalCombat`, rule tests, semantic architecture scan | Verified |
| Missing/unsupported/conflicting facts never satisfied | contract, resolver, context, compiler, API, and rendering tests | Verified |
| Complete learned-skill consideration and exact roles | candidate discovery accounting and tests | Verified |
| Existing hard gates remain final authority | candidate discovery plus `CombatSkillCandidateValidator` search fixtures | Verified |
| Deterministic cancellable bounded accountable search | all four bound fixtures, cancellation, shuffled input, coverage fingerprints | Verified |
| Evidence-aware score without double counting or slot reward | scorer duplicate-coverage, unavailable-component, and unused-capacity tests | Verified |
| Three visible and behavioral policies | complete/unavailable policy tests and bilingual UI | Verified |
| Six conditional plan stages | compiler, API canonical ordering, and component ordered list | Verified |
| Information-only plan and parity | selected proposal/preparation/comparison assertions and architecture controls | Verified |
| One immutable result and atomic observations | Application call-count and observation lifecycle tests | Verified |
| API/UI state preservation without private content | controller, mapper, JSON, component, and contract-reflection tests | Verified |
| Bilingual responsive accessible UI | typed localization tests and [browser record](assets/epic-008/E8-011-browser-verification.md) | Verified |
| Automated and representative manual verification | this report, E8-000, and E8-011 browser verification | Verified |
| No victory probability or game control | score, rendering, API, and architecture tests | Verified |
| Product-owner completion decision | E8-013 | Pending product owner |

## Release gate

```powershell
dotnet build TaiWu.slnx -c Release --no-restore -m:1
dotnet test TaiWu.slnx -c Release --no-restore --no-build -- --no-progress
```

| Gate | Result |
|---|---:|
| Release build | 0 warnings, 0 errors |
| Full non-opt-in suite | 1,595 total |
| Passed | 1,578 |
| Failed | 0 |
| Expected guarded-local skips | 17 |

The two Epic 8 local-source tests are among the expected skips and each names
its exact opt-in environment variable. All in-memory, API, Presentation, and
architecture verification ran and passed.
