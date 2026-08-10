# E5-010 automated verification

| Field | Value |
|---|---|
| Status | Superseded — pre-remediation evidence |
| Evidence date | 2026-08-10 |
| Epic | [EPIC-005](../roadmap/epic-005/EPIC.md) |
| Backlog item | [E5-010](../roadmap/epic-005/BACKLOG.md#e5-010--verify-archetype-reuse-safety-and-determinism) |

> Historical record: an independent review found that the three new families
> were still gap-only despite this matrix passing. E5-011 reopens and
> supersedes the product-readiness conclusion below; see the
> [remediation review](./E5-011-playbook-remediation.md).

## Release and default matrix

Release build:

```powershell
dotnet build TaiWu.slnx --configuration Release --no-restore
```

Result: passed with zero warnings and zero errors.

Default matrix:

```powershell
dotnet test TaiWu.slnx --configuration Release --no-build --no-restore
```

Result: **1,047 total; 1,038 passed; 0 failed; 9 expected opt-in
integration skips**.

| Layer | Result | Epic 5 coverage |
|---|---:|---|
| Domain | 413/413 | Profile/facet evidence, extraction, archetype states, multi-match, playbook catalogue, composition, all six adjustments, conflicts, gaps, and deterministic keys |
| Application | 137/137 | Personalization, feasibility, recommendation/manual-plan/comparison parity, and observation apply/repeat/clear replacement |
| Infrastructure unit | 132/132 | Version-aware, read-only save and catalogue boundaries used by the vertical |
| Infrastructure integration, default | 1 passed, 9 skipped | Non-local test passes; guarded save/GameData tests skip honestly without opt-in paths |
| API and Presentation | 275/275 | Typed state projection, bilingual mapping, compact rendering, interaction lifecycle, and duplicate-element guards |
| Architecture | 80/80 | Layer, bounded-engine, text-matching, filesystem, process, screenshot, persistence, game-control, and mutation-capability boundaries |

Formatting and diff checks:

```powershell
dotnet format TaiWu.slnx --no-restore --verify-no-changes
git diff --check
```

Result: passed on the final verification worktree.

## Synthetic archetype and playbook matrix

The versioned catalogue contains one baseline plus all three E5-000 families.
The tests do not use target IDs as rules.

| Family | Synthetic verification | Result |
|---|---|---|
| `MIND_RESONANCE_RESET_BASELINE` | Existing four-threat baseline, six verified counter rules, requirements, direction, timing, gaps, and feasibility | Pass |
| `OUTER_DAMAGE_CONFIGURED` | Confirmed configured outer-damage facet, reusable goal, multi-match composition, and honest no-counter gap | Pass |
| `CHANNEL_RESISTANCE_ASYMMETRY` | Positive unequal channel resistance, contrary/equal/unavailable inputs, exact channel goal, and honest no-option gap | Pass |
| `POISON_APPLICATION_CONFIGURED` | Confirmed poison application, reusable goal, multi-match composition, and honest no-counter gap | Pass |

The core evidence is in
[profile tests](../../tests/TaiWu.Domain.UnitTests/TargetProfiles/TargetCombatProfileExtractorTests.cs),
[archetype tests](../../tests/TaiWu.Domain.UnitTests/TargetArchetypes/TargetArchetypeMatcherTests.cs),
[playbook tests](../../tests/TaiWu.Domain.UnitTests/TargetPlaybooks/TargetCounterPlaybookTests.cs),
[composition tests](../../tests/TaiWu.Domain.UnitTests/TargetPlaybookComposition/TargetPlaybookComposerTests.cs),
and
[adjustment tests](../../tests/TaiWu.Domain.UnitTests/TargetPlaybookComposition/TargetSpecificPlaybookAdjusterTests.cs).
Synthetic cases prove both directions of reuse: one profile matches outer-damage
and poison playbooks together, while equivalent facts on distinct profiles
resolve the same archetype without character identity.

## Application, API, and Presentation matrix

- [Application recommendation tests](../../tests/TaiWu.Application.UnitTests/CombatRecommendations/RecommendCombatLoadoutTests.cs)
  prove that only confirmed goals contribute and partial archetypes do not
  supply counters.
- [Observation lifecycle tests](../../tests/TaiWu.Application.UnitTests/CombatRecommendations/RecommendCombatLoadoutTargetObservationTests.cs)
  compare profile, match, composition, adjustment, recommendation, and Epic 4
  comparison fingerprints across apply, repeat, and clear.
- [API mapper tests](../../tests/TaiWu.API.UnitTests/Controllers/TargetStrategyResponseMapperTests.cs)
  preserve unavailable, partial, unsupported, conflicting, multi-match, gap,
  feasibility, and all six adjustment states in typed contracts.
- [Presentation rendering tests](../../tests/TaiWu.API.UnitTests/Presentation/TargetStrategyComponentRenderingTests.cs)
  cover English and Traditional Chinese, native semantics, non-colour labels,
  observation replacement, and guards against duplicate strategy panels,
  controls, skill cards, warnings, checklists, and comparison rows.
- [Architecture tests](../../tests/TaiWu.Architecture.Tests/ArchitectureBoundaryTests.cs)
  prevent raw/localized text mechanics, a parallel unbounded engine, paths,
  processes, screenshots, persistence commands, game control, and mutation-
  capable public dependencies.

## Guarded current-save vertical

The guarded observation vertical was extended to compare the complete Epic 5
strategy signature and to require every registered archetype to be evaluated:

```powershell
$env:TAIWU_INTEGRATION_SAVE_PATH = '<current-local.sav>'
dotnet test `
  tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj `
  --configuration Release --no-build --no-restore -- `
  --filter-method `
  'TaiWu.Infrastructure.IntegrationTests.TargetObservationReadOnlyIntegrationTests.Observation_and_comparison_repeat_clear_preserve_sources'
```

Result: **1 passed; 0 failed; 0 skipped** in approximately 25 seconds.

The normalized strategy signature includes the profile fingerprint, complete
archetype match set, composition key and diagnostics, adjustment key, eligible
goals, counter access/feasibility state, gaps, and recommendation diagnostics.
Repeated observation application produced the same signature. Clearing the
observation reproduced the save-only profile, strategy, recommendation, and
comparison signatures.

The current production snapshot supplied these representative facts:

| Local evidence | Result |
|---|---|
| Configured outer-damage family | Matched |
| Configured poison-application family | Matched |
| Outer plus poison multi-match and composition | Exercised |
| Channel-resistance asymmetry | Unsupported because the required positive base channel values were unavailable |
| Mind-resonance/reset baseline | Unsupported because no verified equipped source established the baseline chain |
| Registered-family evaluation | All four definitions evaluated; unsupported families remained explicit |

The `finally` guard confirmed identical length, timestamp, and SHA-256 state
for the save, inspected GameData dependencies, and Traditional Chinese and
English resources. Machine-specific paths, source fingerprints, save content,
and proprietary runtime content are not committed.

## Epic acceptance audit

| Epic 5 criterion | Implementation or evidence | Result |
|---|---|---|
| Versioned profile evidence and unavailable semantics | [Profile contract](../architecture/TARGET-COMBAT-PROFILE.md) and extraction-rule tests | Pass |
| Attack family is separate from mechanics | Profile dimensions, extraction tests, and the compact UI grouping | Pass |
| Multi-label targets and reusable archetypes | Archetype/extractor synthetic cases and guarded outer-plus-poison result | Pass |
| Missing evidence is not a negative or zero | Facet invariants, unsupported/incomplete tests, and guarded channel result | Pass |
| Complete match-state vocabulary | Archetype matcher and API mapper state matrices | Pass |
| Evidence and provenance remain typed | Profile, match, API, and UI evidence references | Pass |
| Localized/raw text cannot become mechanics | Extraction contracts and architecture guards | Pass |
| High/low requires documented semantics | [Evidence gate](../scenarios/E5-000-target-archetype-evidence.md) rejected unsupported adjectives | Pass |
| Baseline plus three new families | [Counter-playbook catalogue](../architecture/TARGET-COUNTER-PLAYBOOKS.md) and four-family tests | Pass |
| Playbooks are goals/options, not fixed loadouts | Playbook contract and catalogue invariants | Pass |
| Deterministic composition and explicit conflicts | [Composition rules](../architecture/TARGET-PLAYBOOK-COMPOSITION.md) and composer tests | Pass |
| Six exact-target adjustment actions | Adjuster, API mapper, and component matrices | Pass |
| Existing feasibility and bounded-search safeguards | [Personalization design](../architecture/TARGET-PLAYBOOK-PERSONALIZATION.md) and Application tests | Pass |
| Inaccessible counters remain gaps | Catalogue, personalization, API, and live outer/poison gap results | Pass |
| Observation apply/clear is atomic | Application lifecycle and guarded signature comparisons | Pass |
| API/UI semantic parity | [API contract](../architecture/TARGET-STRATEGY-API.md), mapper, and rendering tests | Pass |
| One compact, non-duplicated strategy section | Component duplicate guards and [manual verification](./E5-010-manual-verification.md) | Pass |
| Bilingual, responsive, keyboard, and non-colour UI | Rendering tests and manual desktop/narrow review | Pass |
| Identical inputs are deterministic | Reordering tests plus guarded repeat/apply/clear signatures | Pass |
| Full required automated state matrix | 1,047-test Release matrix above | Pass |
| Game-owned sources remain unchanged | Guarded current-save `finally` fingerprints | Pass |
| No mutation or expansive capability | Architecture tests and read-only integration boundary | Pass |
| Product-owner completion decision | Awaiting explicit decision | Awaiting |

## Explicit future work

Statistical clustering, persisted observations/recommendations/outcomes,
screenshot assistance, outcome learning, broader target coverage, companion
selection/development, village management, and library/book planning remain in
[future product ideas](../roadmap/FUTURE-PRODUCT-IDEAS.md). None is partially
implemented by Epic 5.

## Decision boundary

This was the pre-remediation conclusion. It is retained for audit history and
is superseded by E5-011.
