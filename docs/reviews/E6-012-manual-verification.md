# E6-012 representative companion-finder verification

| Field | Value |
|---|---|
| Status | Awaiting product-owner decision |
| Evidence date | 2026-08-17 |
| Epic | [EPIC-006](../roadmap/epic-006/EPIC.md) |
| Backlog item | [E6-012](../roadmap/epic-006/BACKLOG.md#e6-012--validate-representative-roles-and-close-epic-6) |
| Automated evidence | [E6-011 verification](./E6-011-automated-verification.md) |
| UI evidence | [E6-010 review](./E6-010-companion-finder-ui.md) |

## Scope and privacy boundary

The representative review used the configured local save through the Release
localhost API and the committed synthetic UI fixture. The local responses were
checked in memory and reduced to typed pass/fail assertions. No candidate name,
character ID, location, exact qualification, score, rank, save path, save
fingerprint, source fingerprint, or proprietary raw value was printed or
committed.

The live review did not send a recruitment, training, movement, equipment,
assignment, persistence, export, screenshot, input, process-control, or game
command. The temporary localhost server was stopped after the checks.

## Representative role review

Role discovery returned exactly the two verified version-1 roles in English
and Traditional Chinese. Four authoritative finder responses were reviewed:

| Role | English | Traditional Chinese | Exact scored field | Result |
|---|---:|---:|---|---|
| Martial discipline aptitude | Passed | Passed | `BaseMartialQualification` for the selected martial discipline | Authoritative and ranked |
| Life-skill discipline aptitude | Passed | Passed | `BaseLifeSkillQualification` for the selected life-skill discipline | Authoritative and ranked |

For every retained candidate, the API exposed hard-gate evidence. Every ranked
candidate had one exact role-local component in the requested typed domain and
a non-null total. Candidate count matched the unfiltered shortlist total.
Neither role reused the other domain's field or claimed current attainment,
success probability, teaching ability, recruitment, future potential, or
universal character quality.

English and Traditional Chinese responses retained identical candidate
references, evaluation/ranking states, competition ranks, role-local totals,
hard-gate outcomes, raw component values, contributions, and authoritative
finder fingerprints. Only localized presentation text changed. All four
responses shared the same stable save revision during the review.

## Candidate universe independence

Manual source inspection reconfirmed that the companion projection starts from
`TaiwuDomain.GetGroupCharIds()`, excludes the Taiwu player, requires a current
character object, and cross-checks `TaiwuDomain.IsInGroup`,
`Character.IsInTaiwuGroup`, and `CharacterDomain.IsCharacterAlive` before a
candidate is eligible.

`TaiwuTargetLookupReader` separately enumerates the broad
`CharacterDomain.Characters` collection for name/ID search. It is not a
dependency of the companion reader, finder, evaluator, API mapper, or UI.
Target-lookup membership therefore cannot create or qualify a companion
candidate.

## Cross-layer parity

The reviewed live chain was:

```text
guarded configured-save snapshot
  -> immutable candidate profiles and display descriptors
  -> catalogue enrichment over those exact profile references
  -> one verified role evaluation and shortlist
  -> one Application result and fingerprint
  -> API response mapper
  -> Presentation mapper over that same response/result
```

Domain and Application tests prove hard gates precede scoring, missing or
conflicting evidence has no numeric fallback, ties retain competition rank,
and repeated/reordered inputs preserve fingerprints. API and Presentation
tests prove every typed state, component, explanation, unavailable reason,
filter, and comparison remains derived from the immutable result without
re-ranking.

An independent closure review found that the original Presentation summary
had labelled only ranked/tied candidates as `Eligible`. The corrected contract
now counts profiles whose exact `CandidateUniverseState` is `Eligible`, so an
eligible candidate with incomplete role evidence remains eligible while still
being visibly unranked. A separate mixed-universe API case prevents the count
from regressing to rankability or `total - ineligible`.

The same correction pass carries the typed snapshot-read status plus exact
enrichment and catalogue statuses into the Presentation model. Missing source
packs, missing local catalogue, stale, rebuilding, unsupported, source-read
failure, repository failure, corrupt, and candidate-partial states each render
distinct actionable English and Traditional Chinese guidance. Component
rendering exercises every supported combination through the real Razor result
component rather than a static copy of the notice markup.

The synthetic nine-candidate UI matrix keeps incomplete, unsupported, stale,
conflicting, and ineligible candidates visibly unranked. It displays `Unavailable`
instead of zero and shows the exact friendly evidence reason. The two tied
candidates retain one shared rank and an explicit bilingual tie cue.

## Bilingual, responsive, and keyboard review

The [E6-010 browser review](./E6-010-companion-finder-ui.md) covers the live
pre-read route and synthetic result at English desktop and Traditional Chinese
narrow widths. Native radios, select, text input, checkboxes, buttons, details,
landmarks, headings, scoped tables, live regions, and focus targets are present.
The narrow layout reflows the same table DOM into labelled cards and preserves
all facts, order, and non-color cues without document-level horizontal
overflow.

Post-read result behavior additionally passes executable Razor component
rendering for exact eligibility counts, all typed source/catalogue states,
filters, comparison, previous-result inertness, and both languages. This
retains the privacy boundary while testing the production component and mapper.

## Performance and non-interference

The live representative API review observed:

- cold guarded archive read and projection: `27.929` seconds;
- repeated unchanged-revision martial read: `3` milliseconds;
- first unchanged-revision life-skill read: `1` millisecond; and
- repeated unchanged-revision life-skill read: `1` millisecond.

These pass the E6-000 limits of 30 seconds cold and 2 seconds warm. The E6-011
Release integration class independently fingerprints the configured save,
runtime assemblies, catalogue configuration, and combat, special-effect,
legendary-book, discipline, candidate-name, and candidate-map language sources
before and after all three read scenarios. Its expanded version completed in
about 24 seconds in each of two repeated Release runs. Every inspected file,
including both language variants of `Name`, `MapState`, `MapArea`, and
`MapBlock`, retained the same length, last-write time, and SHA-256.

## Deferred mechanics and future backlog

The first delivery intentionally supports only martial- and life-skill-
discipline saved base aptitude. Remaining mechanics are explicit discovery or
future-product candidates in
[Future product ideas](../roadmap/FUTURE-PRODUCT-IDEAS.md#epic-6-deferred-companion-mechanics):

- current modified qualification and attainment under live special effects;
- general combat-support synergy;
- teaching eligibility and teachable content;
- recruitment availability and interaction requirements;
- inheritance and other future-development value; and
- settlement/work assignment.

None is partially implemented or inferred from labels, learned skills,
location, age, or raw text.

Both delivered roles intentionally use one evidence-backed saved-base-
qualification component. Multi-component synergy and development tradeoffs
remain future evidence work rather than invented Epic 6 dimensions; the
generic comparison model is not presented as proof of an undelivered role.

## Independent review correction result

The three P1 findings from the independent Epic 6 closure review are resolved:

1. exact candidate-universe eligibility is counted independently of ranking;
2. Presentation retains and renders exact snapshot, enrichment, and catalogue
   states with bilingual recovery guidance; and
3. every candidate-display name/map language pack is included in all relevant
   before/after non-interference guards.

The corrected Release matrix passes 1,238 non-integration tests with zero
failures and the guarded local class passes 3 of 3. The solution build has zero
warnings and errors; formatter and repository validation also pass.

## Product-owner decision

All technical and representative E6-012 checks pass. Epic 6 remains in
progress until the product owner explicitly accepts or rejects completion.
