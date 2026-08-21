# Local GameData integration tests

M1-024, E2-002, and E2-009 verify the Infrastructure adapter against a locally
installed Taiwu runtime and pinned historical golden saves. E2-016 adds a
version-aware catalogue-to-current-save atlas check. The suite is opt-in and
strictly read-only.

E2-006 also verifies the bilingual static catalogue importer without requiring
a save. Enable that assertion in the current shell:

```powershell
$env:TAIWU_INTEGRATION_SKILL_CATALOGUE = '1'
dotnet test tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj --no-restore
```

For a non-default installation, set the trusted runtime locator as well:

```powershell
$env:TAIWU_GAME_DIRECTORY = '<game-directory>'
```

Set the save path only in the current shell:

```powershell
$env:TAIWU_INTEGRATION_SAVE_PATH = '<path-to-local.sav>'
dotnet test tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj --no-restore
```

To run only the E2-016 vertical golden check against any stable current save,
enable both inputs and use the Microsoft Testing Platform method filter:

```powershell
$env:TAIWU_INTEGRATION_SKILL_CATALOGUE = '1'
$env:TAIWU_INTEGRATION_SAVE_PATH = '<path-to-current-local.sav>'
dotnet test tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj --no-restore --filter-method '*Joined_atlas_is_repeatable_and_preserves_all_inspected_sources'
```

Keep the save stable during the run; closing the game is the simplest option.
Unlike the historical tests, this check does not require a pinned save hash.
It imports the installed catalogue into a temporary helper-owned SQLite file,
joins current character progress twice, and compares stable atlas identity,
content, ordering, counts, and status.

If the game is installed somewhere other than the project default, also pass
the local build property:

```powershell
dotnet test tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj --no-restore -p:TaiwuGameDirectory='<game-directory>'
```

When `TAIWU_INTEGRATION_SAVE_PATH` is absent, invalid, or the local GameData
runtime is unavailable, xUnit reports the test as skipped with the missing
prerequisite. No machine-specific path is committed.

The test:

- fingerprints the save and local runtime dependencies using read-only streams;
- reads player `21396` and target `16317` twice in one process;
- verifies the target is the agreed 52-year-old golden target;
- compares fingerprints again in a `finally` block;
- writes no snapshot, hash, save content, or GameData binary to test results.

E2-016 expands that guard to every inspected installed catalogue source and
save dependency. It compares their SHA-256 fingerprints before and after the
import, temporary persistence, and both atlas reads. The temporary SQLite
catalogue is deleted in a `finally` block. The current save path, fingerprint,
and contents are not committed or printed.

E2-002 adds a target-independent golden-skill assertion for stable IDs `40`,
`41`, `361`, `456`, `498`, and `686`. It is guarded by the E2-001 save SHA-256
as well as the environment variable. If the configured save has advanced, the
assertion skips instead of applying stale reading and activation values. Pure
synthetic mapping tests continue to validate all fifteen detail bits and the
breakthrough rules without proprietary fixtures.

E2-006 fingerprints the installed configuration assembly plus the Traditional
Chinese and English combat-skill language files, imports twice, verifies stable
ordering and bilingual golden skill `456`, and compares all three source files
again in a `finally` block. The GameData binary hash is used at runtime but is
not committed or printed by the test.

Pinned historical assertions skip when the configured current save no longer
matches their recorded fingerprint. This is expected and is separate from the
E2-016 current-save check.

The latest commands, counts, coverage map, and non-proprietary local result are
recorded in `docs/reviews/E2-016-automated-verification.md`.

E3-010 adds a current-save target-observation vertical. It deterministically
selects a valid target through the normal lookup, applies one controlled
complete observation twice, clears back to save-only, compares all result
signatures, and fingerprints every inspected source before and after:

```powershell
$env:TAIWU_INTEGRATION_SAVE_PATH = '<path-to-current-local.sav>'
dotnet test tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj -c Release --no-restore -- --filter-class '*TargetObservationReadOnlyIntegrationTests*'
```

The test does not claim that the selected current-save target is the historical
E3-000 sparring opponent. The E3-000 screen and bilingual workflow are verified
separately; this test exercises the read-only current-save integration boundary.

E4-007 extends the same guard with loadout-comparison assertions. It requires
Current plus typed Safe, Balanced, and Aggressive columns, distinguishes
available policy loadouts from infeasible diagnostics, compares repeated
observation comparison facts, and proves clearing returns to the save-only
comparison facts. The existing class-filter command runs both the E3 lifecycle
and E4 comparison assertions in the same single guarded test.

E5-003 extends that vertical again. Target learned-skill snapshots must expose
version-matched configured outer-damage and poison-presence facts, and the
save-only target profile must reproduce the same fingerprint after the
observation is cleared. The profile version gate must remain supported. The
2026-08-10 focused current-save run passed 1/1 in about 29 seconds while the
test's existing `finally` guard confirmed every inspected save, runtime, and
language source unchanged.

Build output can contain local runtime copies required by GameData. Those files
remain ignored, are never publish items, and must never be committed.

## Epic 6 companion finder

E6-011 verifies the snapshot, enrichment, and complete two-role finder against
the current configured save without recording its path or contents:

```powershell
$env:TAIWU_INTEGRATION_SAVE_PATH = '<path-to-current-local.sav>'
dotnet test tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj -c Release --no-restore -- --filter-class '*CompanionCandidateSnapshotIntegrationTests*'
```

The three tests enforce the 30-second cold and 2-second warm budgets, repeat
both verified role families, and compare the save, runtime, catalogue, combat,
special-effect, legendary-book, discipline-language, and bilingual candidate
name/map source fingerprints before and after. They never print a path, hash,
candidate identity, or exact candidate value. The non-proprietary outcome is
recorded in
[`E6-011-automated-verification.md`](../reviews/E6-011-automated-verification.md).

## Epic 8 tactical evidence gate

E8-000 captures the minimum current local metadata needed to establish the
tactical-planner boundary without authorizing mechanics from a mismatched
runtime version:

```powershell
$env:TAIWU_INTEGRATION_SAVE_PATH = '<path-to-current-local.sav>'
dotnet test tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj -c Release --no-restore -- --filter-class '*TacticalCombatEvidenceIntegrationTests*'
```

The test requires the current installed GameData build recorded by the E8-000
evidence, captures the save plus six installed runtime and language sources,
and compares all seven files before and after. It performs repeated in-process
projections, checks cancellation, enforces a 30-second cold and 3-second warm
budget, and records only aggregate facts. Paths, save hashes, character
identities, and proprietary content are never printed or committed.

The immutable helper catalogue predates the installed GameData build. The test
therefore proves source stability and execution-context availability only. The
synthetic tactical scenario remains pinned to the older verified-rule version,
and production must return an unsupported result for the current build.

### Epic 8 completion verification

E8-012 retains two opt-in guarded checks:

```powershell
$env:TAIWU_INTEGRATION_SAVE_PATH = '<path-to-current-local.sav>'
dotnet test tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj -c Release --no-restore -- --filter-class '*TacticalCombatEvidenceIntegrationTests*' --filter-class '*TacticalExecutionContextIntegrationTests*'
```

Without the environment variable, both tests record explicit E8-000/E8-004
skip reasons and perform no source read. With an authorized local save, they
verify guarded repeatable reads, cancellation, stable projection, seven-source
non-interference, and the exact unsupported-version result for the current
runtime. They never print or commit the save path, hashes, identities, or raw
content.

The retained E8-000 representative evidence records a passing 30-second cold
budget and 3-second warm unchanged-revision budget, with 7 of 7 guarded files
unchanged. These are the production archive-read budgets. The separate 20.4-
second and 13.6-second isolated inspector timings include process startup and
are not used as cache budgets.

In-memory bounded-search verification records two candidate-projection cache
misses with at least one reuse hit and at most four total accesses for the
two-candidate fixture. The feasibility cache records four canonical misses and
zero false hits. Application work counts remain exactly one snapshot read, rule
resolution, context projection, discovery, search, score, and plan compilation
per successful request.

## Post-Epic 8 current-version tactical evidence gate

E8-F01 adds an explicit opt-in check for the broader current-version candidate
set without rebuilding or querying a helper catalogue:

```powershell
$env:TAIWU_INTEGRATION_CURRENT_TACTICAL_EVIDENCE = '1'
$env:TAIWU_INTEGRATION_SAVE_PATH = '<path-to-current-local.sav>'
dotnet test tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj -c Release --no-restore -- --no-progress --filter-class '*CurrentTacticalCombatEvidenceIntegrationTests*'
```

The first test reads 19 exact installed definitions twice and checks runtime
and configuration version identity, configuration fingerprint, static fields,
effect IDs, requirement IDs/values, and bilingual Direct/Reverse text. It
guards the runtime assembly, configuration assembly, and six language sources.

The second test reads the current player snapshot twice and checks the same 19
learned candidates, their active or achievable directions, mastery, equipment,
and the disk capacity/allocation state. It adds the save to the guarded source
set.

The third test loads a byte copy of the installed runtime and pins the concrete
effect type, base type, inherited combat-skill behavior chain, ordered method
signatures, and method-body bytes for all 19 candidates. It does not instantiate
effects or invoke combat handlers. Candidate behavior authorization therefore
depends on exact current code rather than raw descriptions or unchanged IDs;
the installed runtime still remains unsupported until E8-F02 and E8-F03 supply
the exact target and typed minimum rule set.
