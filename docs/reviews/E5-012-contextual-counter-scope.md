# E5-012 contextual counter-scope review

| Field | Value |
|---|---|
| Status | Complete — technically ready for product-owner acceptance |
| Evidence date | 2026-08-11 |
| Epic | [EPIC-005](../roadmap/epic-005/EPIC.md) |
| Backlog item | [E5-012](../roadmap/epic-005/BACKLOG.md#e5-012--scope-shared-counters-to-the-selected-target-threats) |

## Review conclusion

Epic 5 now delivers its stated product value: the baseline and three new
evidence-gated families have playable, versioned counter paths; mind/resonance
is reusable without the defeat-reset overlay; exact target evidence changes
the recommendation; and every final option still passes the existing player
feasibility and bounded-generation pipeline.

The completion audit found one remaining cross-family correctness defect. A
shared verified counter was selected through the correct family goal, but the
generated loadout and goal-level API response copied every threat supported by
the underlying rule. An outer-only target using reverse 伏龍刀法 could therefore
claim absent mind/distraction coverage, emit an irrelevant unavailable-threat
caveat, or expose an option threat reference outside its containing goal.

## Refactor

`ComposedTargetCounterOption.ApplicableThreatCodes` now owns the contextual
projection. It intersects the counter rule's complete verified capability with
the threats belonging to selected source goals in stable ordinal order.

- Recommendation generation scopes candidates to currently eligible goals.
- Goal-level API projection scopes option references to the containing goal.
- `CombatLoadoutOption.FromCounterRule` rejects both an empty contextual scope
  and any threat not verified by the counter rule.
- Shared rule capability remains unchanged, so the same counter can still
  serve several families when those goals are actually selected.

## Verification

- Domain: **421/421 passed**. Coverage includes selected-goal intersection,
  full multi-goal capability, and invalid/empty verified scopes.
- Application: **139/139 passed**. An outer-only target with owned reverse
  伏龍刀法 covers only `CONFIGURED_OUTER_DAMAGE_PRESSURE` and produces no
  unavailable detail for absent mind threats.
- API and Presentation: **276/276 passed**. Every goal-level option threat
  reference is a member of the containing goal's threat references.
- Infrastructure unit: **132/132 passed**.
- Architecture: **80/80 passed**.
- Default infrastructure integration: **1 passed, 9 expected opt-in skips**.
- Release build: zero warnings and zero errors.
- Full default Release matrix: **1,058 total; 1,049 passed; 0 failed; 9
  expected opt-in skips**.
- Formatting and diff checks pass.

The current-save environment variable was not present during this refactor,
so the guarded local vertical was not rerun. E5-011 retains the latest 1/1
source-preserving guarded result; E5-012 changes only contextual threat
projection and adds synthetic cross-layer regression coverage for that exact
shape.

## Remaining decision

No unresolved code or evidence finding remains inside Epic 5's defined scope.
The only unchecked acceptance item is the explicit product-owner completion
decision. Broader target families and the companion, village, library, and
resource-management ideas remain separate future epics.
