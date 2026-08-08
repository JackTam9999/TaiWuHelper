# E3-012 battle-visible observation verification

| Field | Value |
|---|---|
| Evidence date | 2026-08-08 |
| Decision | Passed |
| Backlog item | [E3-012](../roadmap/epic-003/BACKLOG.md#e3-012--support-hostilestory-battle-visible-observations) |
| GameData version | `1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a` |
| Evidence | `E3-000-CAP-003`, `E3-000-CAP-004`, `E3-000-CAP-005` |

## Corrected evidence boundary

The full opponent `運功` page remains unavailable for hostile and story
encounters. Their combat UI may nevertheless expose labelled active skill
effects. E3-012 models those entries as partial
`target.visibleActiveEffects` evidence, separate from sparring
`target.equippedSkills` evidence.

The implementation enforces these claims:

- hostile/story coverage can never be complete or establish omitted-skill
  absence;
- applying the observation does not change saved equipped membership;
- catalogue-confirmed name, category, and direction supply stable identity
  and the versioned effect ID;
- raw effect prose cannot become a mechanic;
- visible power is retained in the Domain and API response as evidence only;
- changing visible power on otherwise identical evidence does not change
  threats, feasibility, or scoring; and
- the unlabelled `2/2/3/3` rows and three status icons from
  `E3-000-CAP-005` have no input field or analysis rule.

## Automated matrix

Release build:

```powershell
dotnet build TaiWu.slnx -c Release --no-restore
```

Result: passed with zero warnings and zero errors.

Default matrix:

```powershell
dotnet test TaiWu.slnx -c Release --no-build
```

Result: **843 total; 836 passed; 0 failed; 7 expected opt-in skips**.

The added tests cover Domain coverage and provenance, immutable merge
membership, threat-source scope, power non-scoring, Application resolution
and workflow, API request/response projection, bilingual component rendering,
editor state, and architecture event/safety guards.

Formatting and diff checks:

```powershell
dotnet format TaiWu.slnx whitespace --no-restore --verify-no-changes
git diff --check
```

Result: passed.

## Local read-only vertical

The existing guarded current-save test was rerun with the current local save:

```powershell
$env:TAIWU_INTEGRATION_SAVE_PATH = '<current-local.sav>'
dotnet test `
  tests\TaiWu.Infrastructure.IntegrationTests\TaiWu.Infrastructure.IntegrationTests.csproj `
  -c Release --no-build -- `
  --filter-class '*TargetObservationReadOnlyIntegrationTests*'
```

Result: **1 passed; 0 failed; 0 skipped**.

One first attempt encountered a transient exclusive file lock while reopening
the save for the final fingerprint. It reported no mismatch. A direct
read-only hash succeeded immediately afterwards, and the complete guarded
retry passed. The successful run compared the save, GameData runtime, and
language-resource lengths, timestamps, and SHA-256 values before and after;
all were unchanged. Machine-specific paths and fingerprints are not committed.

## UI and lifecycle

English and Traditional Chinese rendering tests confirm that hostile/story
mode says the full loadout is unavailable, fixes coverage to partial
battle-visible effects, allows catalogue-assisted skill input, and states
that omitted skills remain unknown. The power field explicitly says it is
evidence-only.

Observations remain request/session state. Apply creates a new immutable
helper snapshot; Clear rebuilds the save-only result. There is no screenshot
capture, file upload, history store, game process access, input automation, or
game/save mutation path.

## Conclusion

Every E3-012 acceptance criterion is satisfied. The scope correction is ready
for the E3-011 completion audit and product-owner decision.
