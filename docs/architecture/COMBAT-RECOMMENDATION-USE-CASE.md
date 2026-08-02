# Combat recommendation use case

## Application boundary

`RecommendCombatLoadout` is the Application-layer entry point for producing a
combat recommendation. Its only external dependency is the query-only
`ICombatSnapshotReader` port.

The use case never references Infrastructure or GameData types. All work after
the snapshot read is delegated to pure Domain services.

## Pipeline

The use case performs these stages in order:

1. create a `CombatSnapshotReadRequest` from save path, target ID, and optional
   helper-owned current-screen loadout observation;
2. read one immutable `CombatSnapshot`;
3. analyze target threats using the exact-version verified threat rules;
4. select verified counter options for the analyzed threat codes and add
   retain-current options;
5. generate bounded, feasibility-validated candidates;
6. score candidates using the requested Safe, Balanced, or Aggressive policy;
7. build manual loadout and battle-plan instructions; and
8. build structured evidence explanations when a plan exists.

The returned `CombatLoadoutRecommendation` retains every intermediate result
needed by later API and presentation layers.

## Cancellation and failures

The caller's cancellation token is:

- checked before reading;
- passed unchanged to `ICombatSnapshotReader`; and
- checked after the read, threat analysis, and candidate generation.

Reader exceptions and cancellation are propagated. Expected absence of a
feasible candidate is represented by diagnostics and an empty manual-plan
result, not by a fabricated recommendation.

## Warning preservation

The exact `CombatSnapshot` returned by the reader is retained. Its source
warnings are exposed unchanged through `SnapshotWarnings`. Threat-analysis and
candidate-generation diagnostics remain in their respective typed results.

## Bounded candidate input

The curated-option maximum is 40. The exploration maximum remains 65,536 and
the emitted-result maximum remains 256, with smaller request defaults. The
larger input envelope allows a full observed configuration and the small
verified counter catalog to enter the bounded search together.

## Non-interference

The use case returns information only. It has no command port, save writer,
process-control dependency, input automation, or method that can apply its
manual instructions.
