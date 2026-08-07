# E2-017 manual verification

**Evidence date:** 2026-08-02

**Last updated:** 2026-08-07

**Decision:** Awaiting product-owner approval

The completed catalogue and atlas agree with the recorded installed version,
the two agreed in-game captures, and the verified save decoder. The configured
save has advanced since the historical captures, so current-save results are
validated at their own freshness boundary instead of being compared with stale
raw expectations.

## Evidence identity

| Evidence | Identity | Result |
|---|---|---|
| Installed GameData | Product version `1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a` | Matches the verified importer/decoder boundary |
| Traditional Chinese skill language | SHA-256 `9932B589389DF643981A3CB6E6E8DFFD9B7B1FC814BBA30ACD34C6C18CF1CFF4` | 946 localized definitions imported |
| English skill language | SHA-256 `F89C3B8AD7DEFE0E6E587EA4F1E109E983817B3F609C34946379FC82314D5229` | 946 localized definitions imported |
| In-game skill list capture | SHA-256 `BC016080C3139737C43AAC227F1BFBA5BB504D198822D89FBBBBA3D7F3C43F32` | Local file still matches E2-001 |
| `黑血蠱降` detail capture | SHA-256 `5A8BFC6B3A863D5258C52BBB4BC36960A5C1540F7F4B7D7231B40E7C06572097` | Local file still matches E2-001 |
| Historical golden save | SHA-256 recorded in E2-001 | Superseded; guarded tests skip instead of applying stale facts |
| Current configured save | SHA-256 compared in memory and deliberately not recorded | Two atlas reads and before/after guards passed |

No screenshot, save, local path, GameData binary, generated catalogue, or
proprietary resource was added to the repository.

## Game-to-helper comparison

| Observation | Game evidence | Helper/decoder evidence | Result |
|---|---|---|---|
| Catalogue population | Installed configuration contains 946 combat skills | Import and vertical atlas both report 946 unique stable definitions | Match |
| Bilingual identity | Golden IDs use installed Traditional Chinese labels | All six golden IDs resolve in both languages; skill `456` is `黑血蠱降` / `Corruptive Gu Infection` | Match |
| Skill `40` | List shows Reverse and `已大成` | Historical save has a supported activation state for which `IsBrokenOut` and attainment are both true; simplification remains false | Exact verified match |
| Skill `41` | List shows Direct and `已大成` | Historical save has a supported activation state for which `IsBrokenOut` and attainment are both true | Exact verified match |
| Skill `361` | List shows `已取得` | Learned-collection membership is present with sparse reading state | Match |
| Skill `456` study wheel | Fifteen labels are visible; orange sectors are `用`, `奇`, `巧`, `化`, `絕`; centre shows `50%` | Reading mask contains all 15 pages; activation mask contains exactly those five Reverse pages | Exact detail/activation match |
| Skill `456` completion | Orange sectors could be mistaken for the only studied pages | Decoder keeps reading and activation independent and reports 15/15 read; the five orange sectors are active, not unread/read status | Explained semantic distinction |
| Skill `456` percentage | Newer screen visibly reports `50%` | E2-F06 identifies this as final `Power`; the older standalone save lacks the live calculation context, so it remains unavailable instead of inferred | Explained source freshness and runtime boundary |
| Skill `498` | No separate detail screenshot was required by E2-001 | Versioned golden read reports learned, 0/15 read, and the exact ordered 15-detail missing list | Verified decoder evidence; no visual claim invented |
| Skill `686` | Prior verified evidence records incomplete breakthrough | Decoder reports immediate Direct readiness only | Match |

The helper’s accessible Common/Direct/Reverse lists contain the same fifteen
stable details as the wheel. The verified clockwise order begins with `解`,
`異`, `獨`, continues through Direct, Reverse, then `承`, `合`. The five orange
wheel sectors exactly equal activation mask `0x7C00`.

## Search and presentation

- Traditional Chinese query `黑血蠱降` and English query
  `Corruptive Gu Infection` resolve stable skill ID `456`.
- Each language tab now renders only its selected-language skill name and raw
  descriptions. The detail page no longer combines Chinese and English text.
- The 門派 filter is a responsive circular picker populated from the current
  catalogue. It forms two rows at desktop width and reflows without horizontal
  scrolling on narrow screens. Each mark uses the active-language faction
  initial instead of game artwork. Its text color comes from the installed
  faction's main inner-power element and its outer ring from primary alignment;
  localized element and alignment labels keep the meaning non-color-only.
  Factions with no alignment use a white ring and retain an explicit
  unavailable label. Faction remains a filter, while results are grouped by
  combat-skill category and ordered from lower to higher installed grade.
- Completed breakthroughs place one circled `正` or `逆` marker before the
  active-language skill name. Ready breakthroughs place `突破` followed by
  only the verified available `正`/`逆` choices before the name; those choices
  are never presented as already active.
- Collapsed skill results use the first character of the active-language skill
  name inside a circular mark instead of game artwork. Circle and skill-name
  color follow the verified
  nine-grade sequence from grey through red; numeric grade remains in the
  accessible summary rather than a repeated visible badge. Primary progress
  status remains visible, and opening a skill expands its facts across the
  result row while retaining the stable-ID detail link.
- Wide category rows use nine columns. A completed breakthrough with an active
  circled `正`/`逆` direction omits the repeated collapsed `已突破` label, while
  expanded facts retain it. Static catalogue entries without save progress are
  shown as `未取得` and remain available through the learned-state filter.
- The learned/all/not-learned control is now a primary filter. Each displayed
  category reports learned and not-learned counts, and the compact grade
  legend labels all nine colors numerically from low to high in either active
  language.
- Live E2-013 validation exercised the current 946-definition atlas in both
  languages at desktop and mobile sizes without horizontal overflow.
- Live E2-014 validation showed static and current facts separately, all 15
  read details, five active Reverse details, provenance, partial-data warnings,
  loading state, and translated labels.
- Recommendation cards link by stable ID. Catalogue current, missing, stale,
  and rebuilding states cannot suppress or alter an Epic 1 recommendation
  because recommendation Domain/Application code has no catalogue dependency.

## Freshness, rebuild, and recovery

| Controlled condition | Evidence | Result |
|---|---|---|
| Any source-identity component changes | `Every_source_identity_input_invalidates_the_catalogue` | Stale, never falsely current |
| Save advances beyond pinned golden | Historical integration fingerprint gates plus current-save vertical test | Stale historical facts skip; current snapshot reads normally |
| Missing/stale/corrupt catalogue | Lifecycle and SQLite recovery tests | Deterministic rebuild |
| Rebuild fails or is interrupted | Transaction rollback/concurrent-reader/recovery tests | Complete old catalogue retained only as explicitly stale |
| Helper path overlaps game/save path | Storage path-guard and architecture tests | Rejected before write |
| Progress cache exceeds eight save paths | SQLite retention test | Oldest derived path snapshot is removed transactionally |
| Player clears progress cache | Application, SQLite, API, and UI tests | Only helper-owned derived snapshots are removed; the save is not opened or changed |
| Full current-save vertical read | E2-016 local integration | All inspected game/save fingerprints unchanged |

Only the validated helper-owned catalogue database, its named rebuild file,
and the bounded current-progress cache can be written. Game files, saves,
directories, runtime processes, and memory remain outside the write boundary.

## Epic acceptance audit

All 26 milestone criteria in EPIC-002 have implementation and evidence across
E2-000 through E2-019. After the latest cache-governance and atlas UI work, the
final E2-F06 automated run passed **691 total: 686 passed, 0 failed, 5
documented opt-in skips**. Installed-catalogue plus current-save verification
passed **691 total: 688 passed, 0 failed, 3 stale historical-fingerprint
skips**, including the current-save vertical check.

E2-F06 subsequently verified the remaining semantics. Current-Taiwu `已大成`
is the same successful-breakthrough predicate already present in the save, and
the centre percentage is final `Power`, not a proficiency conversion. Only the
calculated current/maximum power values remain unavailable from a standalone
save, with reasons, because the live special-effect context is absent.

## Product-owner decision

Pending: record the product-owner Epic 2 completion decision. E2-F06 no longer
requires additional semantic evidence.
