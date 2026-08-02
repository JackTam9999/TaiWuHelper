# Local GameData integration tests

M1-024 and E2-002 verify the Infrastructure adapter against a locally installed
Taiwu runtime and fingerprinted golden saves. The suite is opt-in and strictly
read-only.

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

Build output can contain local runtime copies required by GameData. Those files
remain ignored, are never publish items, and must never be committed.
