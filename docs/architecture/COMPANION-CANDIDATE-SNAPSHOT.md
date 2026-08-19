# Companion-candidate snapshot architecture

| Field | Value |
|---|---|
| Status | Implemented for E6-004 and extended by E6-013 and the 2026-08-19 succession slice |
| Epic | [EPIC-006](../roadmap/epic-006/EPIC.md) |
| Backlog items | [E6-004](../roadmap/epic-006/BACKLOG.md#e6-004--project-a-one-pass-read-only-candidate-snapshot), [E6-013](../roadmap/epic-006/BACKLOG.md#e6-013--add-a-transparent-companion-capability-overview) |
| Source decision | [Companion-candidate source boundary](./COMPANION-CANDIDATE-SOURCES.md) |
| Profile contract | [Companion role definition and evaluation](./COMPANION-ROLE-EVALUATION.md) |
| Enrichment | [Companion-candidate enrichment](./COMPANION-CANDIDATE-ENRICHMENT.md) |

## Purpose

E6-004 projects the approved candidate universe and every raw profile fact
from one immutable configured-save revision. The current universe is the union
of the Taiwu group roster and the verified village-work-candidate source. It adds
one path-free Application read port and one configured-path-only
Infrastructure adapter over the existing guarded archive session.

The adapter does not enrich catalogue facts, evaluate roles, or rank
candidates. E6-010 extends the same one-pass archive projection with optional
bilingual display descriptors for candidate name and location; those values
remain outside role profiles and every semantic identity.

## Application port

`ICompanionCandidateSnapshotReader` extends the existing
`IReadOnlyGameDataSource` marker and exposes only:

```text
ReadAsync(CompanionCandidateSnapshotReadRequest, CancellationToken)
```

`CompanionCandidateSnapshotReadRequest.Current` has no public instance
properties. In particular, callers cannot provide a save path, game path,
character ID, raw GameData value, command, process, or control input.

The immutable result status is one of:

| Status | Meaning | Snapshot present? |
|---|---|---:|
| `Complete` | Every approved group or village-work candidate and source fact was projected under one revision | Yes |
| `Partial` | A candidate or optional raw fact has an explicit omission, incomplete state, or mapping diagnostic | Yes |
| `SaveUnavailable` | Trusted configuration is absent or its configured file is missing | No |
| `UnsupportedVersion` | Installed GameData does not match the verified mapping | No |
| `ChangedRevision` | The session guard discarded a projection because the save changed | No |
| `ReadFailed` | The configured archive could not be read safely | No |

Success and failure payloads are mutually exclusive. Failure identities and
messages are sanitized and never contain exception or filesystem detail.

## Immutable snapshot

`CompanionCandidateSnapshot` retains:

- UTC capture time;
- save SHA-256;
- exact GameData, profile-mapping, discipline-catalogue, and fingerprint-
  schema versions;
- candidate profiles in stable character-ID order;
- optional bilingual candidate display descriptors keyed by character ID;
- typed candidate omissions;
- typed expected standalone-runtime warnings; and
- typed candidate or result diagnostics.

Every profile must carry the same `CandidateProfileSourceVersions` value as
the snapshot. Constructors defensively copy, sort, reject nulls, reject
duplicate candidate and diagnostic identities, and reject mixed source
revisions. No local path crosses the port.

Display descriptors are accepted only for identities present in the snapshot.
They are presentation context, not profile facts: they cannot affect universe
membership, eligibility, evaluation, ranking, tie order, comparison, or any
profile, shortlist, or finder fingerprint.

## Trusted source and one-pass session

`TaiwuCompanionCandidateSnapshotReader` resolves only
`SaveGames:DefaultSaveFilePath` through
`ITaiwuSaveFilePathProvider`. It cannot accept a caller path. It verifies the
installed GameData version before opening the archive.

A cache miss contains exactly one call to
`TaiwuArchiveReadSession.ReadAsync`. Inside that one callback, the projection:

1. records the guarded save SHA-256 and source versions;
2. obtains Taiwu ID, the authoritative saved group roster, and
   `GetVillagersForWork(true, false)`;
3. rejects duplicate or invalid identities in either source explicitly;
4. excludes the Taiwu player identity;
5. unions and enumerates the remaining IDs in stable numeric order;
6. reads each current object and all approved raw facts;
7. resolves optional Chinese and English name and location text inside the same
   archive callback;
8. maps each candidate independently to an immutable Domain profile; and
9. returns one atomic snapshot.

The adapter never invokes the existing single-character atlas reader, never
opens an archive inside the candidate loop, and never persists candidate data.
The session's process-wide lock and before/after source guard apply to the
whole aggregate projection. `TaiwuArchiveChangedException` is a typed internal
session failure so the adapter can return `ChangedRevision` without matching
exception text.

Because the group-plus-village projection is materially larger than the
original one-companion roster, the singleton adapter retains one immutable
snapshot result in helper process memory. A later request may reuse it only
when configured path, file length, last-write time, GameData version, profile
mapping, and fingerprint schema all still match, with the file revision checked
again before returning. Misses and changed revisions use the guarded archive
path above. The cache is neither persisted nor written to the save, and access
is serialized so concurrent misses cannot publish mixed revisions.

## Display descriptor isolation

Candidate and location labels use the existing version-aware game-text
resolver while the guarded archive projection is already open. Chinese and
English are captured together so changing helper language never rereads the
save. Raw `Name_` and `SurName_` tokens, numeric IDs, paths, and source text do
not become fallback display values; unavailable labels remain a typed display
condition for Presentation to localize safely.

`TaiwuCompanionDisciplineDisplaySource` separately reads the installed 14
martial and 16 life-skill language entries. It exposes stable typed discipline
identities plus bilingual labels and a `Complete`, `Partial`, or `Unavailable`
state. It never accepts a caller path and never enters the candidate archive
session. Discipline text is also display-only and excluded from role and
finder fingerprints.

## Candidate-universe mapping

The production projection uses only these verified sources:

- `TaiwuDomain.GetGroupCharIds()` for authoritative inclusion;
- `TaiwuDomain.GetVillagersForWork(true, false)` for the bounded village-work
  candidate set;
- `CharacterDomain.TryGetElement_Objects()` for current object existence;
- `TaiwuDomain.IsInGroup()` and `Character.IsInTaiwuGroup()` for consistency;
  and
- `CharacterDomain.IsCharacterAlive()` for living eligibility.

The Domain universe result is:

| Source result | Universe state |
|---|---|
| Object and all membership/living facts present, group checks agree with roster inclusion, at least one approved source includes the candidate, living true | `Eligible` |
| Same, with living false | `Ineligible` |
| Object or required check unavailable, or candidate-level source read failed | `Incomplete` |
| Either membership check disagrees with roster inclusion | `Conflicting` |

`GetVillagersForWork(true, false)` is evidence of inclusion in this comparison
universe only. It does not establish complete village membership, succession
eligibility, inheritance mechanics, remaining lifespan, or future growth. No
name, age, location, target-lookup membership, following state, or learned
skill independently decides candidate inclusion or eligibility.

## Raw profile projection

A complete profile-mapping version-3 profile contains 108 typed facts:

- roster, village-work-candidate, Domain membership, character membership,
  and living state;
- current age, area ID, block ID, and feature identities;
- learned and equipped martial identities and learned life-skill identities;
- 6 saved base main attributes from one fixed buffer;
- 14 saved base martial qualifications;
- 16 saved base life-skill qualifications; and
- explicit unsupported current qualification and attainment facts for all 30
  disciplines.

Saved base arrays must match the verified 6-, 14-, and 16-entry shapes. Main
attributes retain the fixed Strength, Dexterity, Concentration, Vitality,
Energy, and Intelligence identities. A missing entry is `Incomplete`; it is
not numeric zero. Current modified attributes, qualification, and attainment
are never substituted for these saved-base values. Current modified
qualification and attainment remain `Unsupported` with the standalone-runtime
reason from E6-000 and are never called by this projection.

All confirmed saved facts use configured-save provenance with profile-mapping
version `3`, fingerprint-schema version `3`, and the same guarded save SHA-256
as the profile. Unsupported modified facts use exact installed-GameData
provenance. Evidence references are stable, path-free field identities.

The installed equipped-skill array includes negative empty-slot sentinels.
The production mapper filters those non-identities before creating the typed
identity set; it does not turn an empty slot into missing evidence. Duplicate
or negative values reaching another identity-set boundary produce an explicit
incomplete fact rather than being fabricated or allowed to invalidate another
candidate.

## Partial isolation and cancellation

A missing current object becomes an incomplete profile. A recoverable
candidate read failure becomes an incomplete profile with a stable diagnostic.
If a raw candidate cannot be mapped safely at all, its positive roster ID and
typed omission remain in the snapshot. Processing continues for unrelated
candidates.

The reader observes cancellation before source resolution, before archive
projection, for every roster entry, and during both discipline-buffer loops.
Cancellation propagates and prevents the session's final accepted result.

Roster, raw identity-set, fact, profile, omission, warning, and diagnostic
enumeration order cannot affect stable output order or fingerprints.

## Safety boundary

Architecture tests verify that the Application request is path-free and the
public contracts expose no GameData, archive, Infrastructure, file, stream, or
process type. Source tests verify exactly one archive-session call and reject
file-write, destructive-file, database-persistence, network, process-control,
native-hook, automated-input, Harmony, and archive-save APIs in this feature.

The adapter reads the configured save and loaded GameData state only. It has
no write, cache, export, process, network, upload, screenshot, input, game-
control, recruitment, equipment, movement, or assignment path.

## Verification

Synthetic tests cover immutable Application contracts, mixed revisions,
duplicates, valid profiles, display-identity validation, bilingual discipline
resources, missing characters, living and membership states, short
qualification buffers, invalid identity sets, deterministic mapping,
dependency injection, unavailable configuration, cancellation/session guards,
and architecture safety.

The opt-in guarded production test on 2026-08-17 used the same representative
configured save as E6-000 and passed with:

- status `Complete`;
- one non-Taiwu current-group profile;
- 101 facts and no omissions under the original profile-mapping version `1`;
- one expected standalone-runtime warning;
- equivalent repeated profile fingerprints and save identity;
- cold production read `20.487` seconds against a 30-second budget;
- warm unchanged-revision read `2` milliseconds against a 2-second budget;
  and
- unchanged SHA-256, length, and last-write time for the save, `GameData.dll`,
  and `GameData.Shared.dll`.

The repository stores no local path, save identity, candidate identity, or
candidate value from that run.

After E6-013 added the six saved base main attributes, the focused
Release integration class again passed all 3 guarded companion scenarios with
zero skips. That snapshot scenario validated the 107-fact version-2 profile
shape, all six confirmed typed main attributes, and repeated
fingerprints; the before/after guard again covered the save and every installed
source used by the companion workflow. The class completed in `32.759` seconds.
No local identity, value, path, or fingerprint is recorded.

E6-005 consumes this snapshot in memory. It joins only the confirmed learned
and equipped identity sets to a compatible helper catalogue and never reopens
the archive per candidate or modifies the current profiles. E6-013
retains the same one-pass read boundary: `GetBaseMainAttributes()` is called
once per candidate and the six values are copied before mapping.
