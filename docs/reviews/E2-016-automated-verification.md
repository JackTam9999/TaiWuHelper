# E2-016 automated verification

**Date:** 2026-08-02

**Result:** Passed

Epic 2 now has automated coverage from immutable catalogue definitions through
installed import, guarded SQLite persistence, current-save progress, API and
presentation states. The default suite remains independent of proprietary
data, while the opt-in vertical test accepts the explicitly configured stable
current save.

## Coverage map

| Layer | Required behavior | Primary automated evidence |
|---|---|---|
| Domain | Definition identity/provenance, typed progress, study detail, completeness | `CombatSkillDefinitionTests`, `CharacterCombatSkillProgressTests` |
| Application | Freshness lifecycle, joins, filters, language fallback, status propagation, failures | `CombatSkillCatalogueUseCaseTests` |
| Infrastructure | Mapping/source preservation, transactions, path guards, invalidation, corruption recovery, deterministic rebuild | `CombatSkillDefinitionMapperTests`, `TaiwuCombatSkillDefinitionSourceTests`, `SqliteCombatSkillCatalogueStoreTests`, `CatalogueStoragePathProviderTests`, `CombatSkillProgressMappingTests`, `ReadOnlyFileFingerprintTests` |
| API | Catalogue, detail, refresh, and character-atlas endpoints plus status mappings | `CombatSkillsControllerTests` |
| Presentation | Search/filter states, progress badges, detail/study states, accessible semantics, recommendation links | `SkillCatalogueRenderingTests`, `SkillDetailRenderingTests`, `RecommendationComponentRenderingTests` |
| Architecture | Inward dependency boundaries and forbidden game-owned writes/process control | `ArchitectureBoundaryTests` |
| Local integration | Repeatable bilingual import, temporary SQLite rebuild, current-save join, stable ordering/content, source preservation | `LocalGameDataIntegrationTests` |

## Reproducible commands and results

Default suite, with both integration variables absent:

```powershell
Remove-Item Env:TAIWU_INTEGRATION_SKILL_CATALOGUE -ErrorAction SilentlyContinue
Remove-Item Env:TAIWU_INTEGRATION_SAVE_PATH -ErrorAction SilentlyContinue
dotnet test TaiWu.slnx --no-restore --verbosity minimal
```

Result: **616 total, 611 passed, 0 failed, 5 skipped**. All five skips were
documented opt-in local-data checks; no proprietary save was required.

Installed catalogue suite, with no save configured:

```powershell
$env:TAIWU_INTEGRATION_SKILL_CATALOGUE = '1'
Remove-Item Env:TAIWU_INTEGRATION_SAVE_PATH -ErrorAction SilentlyContinue
dotnet test TaiWu.slnx --no-restore --verbosity minimal
```

Result: **616 total, 612 passed, 0 failed, 4 skipped**. The bilingual installed
catalogue import passed; only save-dependent checks skipped.

Current-save vertical check:

```powershell
$env:TAIWU_INTEGRATION_SKILL_CATALOGUE = '1'
$env:TAIWU_INTEGRATION_SAVE_PATH = '<path-to-current-local.sav>'
dotnet test tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj --no-restore --filter-method '*Joined_atlas_is_repeatable_and_preserves_all_inspected_sources'
```

Result: **1 total, 1 passed, 0 failed, 0 skipped**. The complete local
integration project against the same current save also passed with **6 total,
3 passed, 0 failed, 3 skipped**; its three skips were historical assertions
whose pinned save fingerprints no longer matched the current save.

## Non-proprietary local evidence

| Item | Observed result |
|---|---|
| Installed GameData product version | `1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a` |
| Catalogue importer version | `1` |
| Imported definitions | `946`, unique and ordered by stable skill ID |
| Traditional Chinese definitions | `946`; language SHA-256 `9932B589389DF643981A3CB6E6E8DFFD9B7B1FC814BBA30ACD34C6C18CF1CFF4` |
| English definitions | `946`; language SHA-256 `F89C3B8AD7DEFE0E6E587EA4F1E109E983817B3F609C34946379FC82314D5229` |
| Representative bilingual definition | Skill `456`: `黑血蠱降` / `Corruptive Gu Infection` |
| Current-save atlas | `946` joined matches; two reads returned identical first-page entry signatures and ordering |
| Installed and save sources | All runtime SHA-256 fingerprints matched before and after |
| Current save fingerprint | Computed and compared across both reads; value and path deliberately not recorded |
| GameData binary fingerprint | Computed and compared before/after; value deliberately not committed |

The integration test created only a temporary helper-owned SQLite catalogue,
then removed it. It did not write to the game installation, save, save
directory, or any running game process.
