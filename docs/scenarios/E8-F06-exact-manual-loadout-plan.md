# E8-F06: Exact manual loadout and execution plan

| Field | Value |
|---|---|
| Status | Complete — selected packages compile to reproducible manual instructions |
| Backlog item | [E8-F06](../roadmap/epic-008/BACKLOG.md#e8-f06--compile-an-exact-loadout-and-manual-execution-plan) |
| Inspection date | 2026-08-21 |
| Runtime GameData | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` |
| Rule fingerprint | `64051C1234CECDFDCE070134FDA0380826154D16C1F171B52B6F7FE1C64ECD5D` |
| Selected-loadout schema | `TACTICAL_SELECTED_LOADOUT_V2` |
| Sanitized record | [E8-F06 metadata](./evidence/E8-F06-exact-manual-loadout-plan-metadata.json) |

## Exact selected loadout

The compiled plan now contains a typed selected-loadout manifest rather than
only a candidate key. It exposes:

- used and available capacity for all five skill categories, including each
  category's universal-slot contribution;
- the complete universal-slot allocation;
- every selected skill's category, exact direction, effective cost, tactical
  role, supported use kinds, scoring eligibility and limitation;
- one main active attack, one primary active defense, one primary active
  agility skill, equipped passives, ordinary active attacks and switch-only
  backups; and
- admitted but unselected candidates as optional alternatives, never as part
  of the selected package.

The selected-loadout fingerprint now binds the package semantics, scored
candidate content, manifest, exact skill directions, universal allocation and
legendary assignments. The compiled result also binds the same context,
search, score, candidate diagnostics, preparation comparison and plan
fingerprints, so independently changed artifacts cannot be presented as one
coherent result.

## Manual preparation comparison

The current-versus-selected comparison compiles to ordered information-only
checks. It lists exact skill removals and additions, direction changes,
required breakthrough/book-page completion, all five category totals, the
complete universal allocation, each legendary slot reference, required weapon
subtypes, trick counts, distance, resources, active roles and manually
confirmed conditions.

Every step remains a manual instruction. The UI repeats that no action was sent
to the game; no check box, game command, save write or optimistic completion
state is introduced.

## Package-aware battle sequence

The six-stage plan keeps only version-supported actions:

1. preparation checks reproduce the exact selected manifest;
2. opening-use roles appear in Opening even when their persistent transition
   continues later;
3. exact current Reverse `604` remains a target-cast response;
4. a complete recovery package emits three ordered cast steps and may repeat
   the same admitted Reverse recovery skill once per lock layer;
5. alternate equipped defense and agility roles appear only as explicit switch
   actions, not simultaneous primary effects; and
6. reset, finish and fallback actions appear only when their separately typed
   transitions and proofs exist.

An unresolved Reverse `604` recovery package produces an unsupported Recovery
stage with its stable package reason and no invented cast. Missing trigger or
live state remains a manual check whose failed or unknown branch leads to the
verified fallback when available, otherwise to an unresolved stop.

Historical plan compilation retains its previous three-preselected-cast
behavior because it has no exact current package contract.

## Bilingual presentation

English and Traditional Chinese now render the same selected skill names,
directions, effective costs, category totals, universal allocation, recovery
cast counts, role classifications, optional alternatives and manual changes.
Numeric internal skill IDs remain in the machine contract but are not used as
player-visible identity; the UI follows the localized/display skill name.

The existing bilingual condition, requirement, evidence, limitation and
information-only wording remains attached to each plan step. The new role
classification vocabulary is exhaustively covered in both languages.

## Verification

The current package/compiler fixture proves exact capacity, role separation,
weapon and manual checks, three repeated Reverse `686` recovery casts, defense
and agility switch steps, optional alternatives, omitted reset, unsupported
finish and coherent fingerprints. The bilingual rendering fixture proves that
the same names, costs and totals appear in English and Traditional Chinese.

The Release build completed with zero warnings and errors. The full suite
passed 1,612 of 1,636 tests with 24 expected guarded-local skips and no
failures. No save, GameData, helper cache or game runtime state was modified.

