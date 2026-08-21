# E8-F02: Exact current later magic-sound phase

| Field | Value |
|---|---|
| Status | Complete — exact phase and causal boundary verified |
| Backlog item | [E8-F02](../roadmap/epic-008/BACKLOG.md#e8-f02--model-the-exact-later-magic-sound-target-and-encounter-phase) |
| Inspection date | 2026-08-21 |
| Runtime GameData | `1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20` |
| Phase code | `LATER_MAGIC_SOUND_PHASE` |
| Rule fingerprint | `A60918FED854795294AC53FAEFC0E0DFE349E9F67BB30D978C1F47109DBD554D` |
| Sanitized record | [E8-F02 metadata](./evidence/E8-F02-current-later-magic-sound-metadata.json) |

## Decision

The later encounter phase is now bound by the exact story-template identity,
the complete saved equipped loadout, the full runtime product version and exact
configuration/runtime implementation evidence. No localized target name is
used as a mechanical key.

The phase contains 34 equipped skills and all 34 are Direct practice. Reverse
`604` therefore has exact phase-wide applicability when the player's own
direction, weapon, cast and recovery requirements are feasible. This conclusion
comes from the complete loadout, not from generalizing the six magic-sound
attacks.

The target does not learn or equip `287` in this phase. The historical generic
defeat-mark reset assumption is recorded as `NotPresent` and its transition is
`NotApplicable`; it is not silently treated as a verified target mechanic.

## Exact equipped phase signature

| Category | Skill IDs | Count | Direction |
|---|---|---:|---|
| Inner power | `54`–`62` | 9 | Direct |
| Agility | `157`–`165` | 9 | Direct |
| Assistance | `265`, `267`, `268`, `269` | 4 | Direct |
| Defense | `266`, `270`, `271` | 3 | Direct |
| Attack | `440`, `443`, `446`, `726`, `727`, `728`, `729`, `732`, `733` | 9 | Direct |

Every signature also binds its selected raw effect ID in
`VerifiedExactTargetEncounterRuleSets.CurrentLaterMagicSound`; the resolver
returns `WrongPhase` when any equipped signature differs.

## Verified magic-sound chain

The exact Direct magic-sound set and configured mind-damage steps are:

| Skill | Direct effect | Mind-damage step |
|---:|---:|---:|
| `726` | `350` | 20 |
| `727` | `351` | 30 |
| `728` | `352` | 40 |
| `729` | `353` | 50 |
| `732` | `356` | 120 |
| `733` | `357` | 160 |

The typed chain keeps these transitions separate:

1. an exact Direct magic-sound hit applies configured mind pressure;
2. the verified global threshold may add a distraction mark;
3. the first mark starts mind rhythm and later marks reduce its remaining
   count;
4. rhythm reaching zero starts the mind-upheaval cascade; and
5. the cascade can repeat mind pressure.

The immutable snapshot does not predict hit success, threshold progress,
elapsed rhythm, cascade duration or hidden AI selection. Those values remain
manual battle observations.

## Movement, range and speed pressure

The phase separately records the nine equipped agility skills and the relevant
current runtime contracts:

- `157` and equipped assistance `269` can sustain footwork under their exact
  conditions;
- `161` and `165` provide forward distance bursts;
- `164` and `165` add conditional pressure while inside attack range;
- `160` increases target cast speed only while that agility is active; and
- `163` protects or amplifies target quickness only while active.

Only one active agility operates at a time. Current distance, target attack
range and active agility are therefore manual inputs, not simultaneous passive
benefits inferred from the equipped list. The nine inner-power skills are
known, but the active inner-power state is likewise unavailable from the disk
snapshot.

## Typed resolution states

`ExactTargetEncounterPhaseResolver` fails closed with five explicit results:

| Input state | Result |
|---|---|
| Matching runtime, exact template and complete 34-skill signature | `Complete` |
| Missing phase/loadout evidence or partial loadout | `Partial` |
| More than one template identity | `Conflicting` |
| Different template or equipped signature | `WrongPhase` |
| Different runtime product version | `UnsupportedVersion` |

Live mark count, rhythm count, temporary layers, current distance, resources,
active agility and active inner power remain
`ManualObservationRequired`. Base channel resistance is unavailable rather
than interpreted as zero.

## Read-only verification

The guarded integration checks locate the target by exact story template,
read one immutable snapshot, compare all 34 selected direction/effect
signatures, verify the six configured mind-damage steps, and pin 19 exact
runtime implementation identities. The target name and character ID are not
written to repository evidence.

Result: 2 focused integration checks and 7 phase-contract unit tests passed.
The save and every inspected GameData/configuration/language source retained
the same hash, length and timestamp. No effect was instantiated, no combat
handler was invoked, and no save, game, helper-catalogue or runtime state was
modified.

## Downstream boundary

E8-F02 authorizes the exact target facts and transitions only. E8-F03 has since
promoted the necessary F01 behavior contracts into exact typed roles, but it
does not prove that a Reverse `604` recovery package is executable. E8-F04 now
carries the remaining live weapon, trick, distance, resource, backlash and
active-role facts while preserving every absent value as unknown.
