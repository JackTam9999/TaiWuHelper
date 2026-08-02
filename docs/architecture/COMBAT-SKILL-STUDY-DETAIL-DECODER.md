# Versioned combat-skill study-detail decoder

## Boundary and version selection

`CombatSkillStudyDetailDecoder` is an Infrastructure mapper for the verified
GameData product version
`1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a`. The character-progress reader
selects it only after detecting that exact version. Calling the decoder with
another version returns no detail map, adds the typed
`STUDY_DETAIL_VERSION_UNSUPPORTED` warning, and leaves completeness,
activation, and breakthrough unavailable.

The decoder consumes only the typed `readingState` and `activationState`
values already obtained from the read-only archive. Its stable IDs, groups,
bit positions, wheel order, and localization keys come from the E2-002
[combat-skill progress semantics](./COMBAT-SKILL-PROGRESS-SEMANTICS.md) truth
table.

## Decode truth table

For the supported version, the verified mask is `0x0000..0x7FFF` and the
decoder always emits the 15 defined details in clockwise wheel order.

| Input observation | Detail read state | Detail active state | Aggregate consequence |
|---|---|---|---|
| Reading bit is set | `Read` | Independent | Included in `ReadCount` |
| Reading bit is clear | `NotRead` | Independent | Included in the exact ordered missing-detail list |
| Reading value is negative or has an unknown high bit | Unavailable for every detail | Decoded independently when valid | `AvailableCount=0`; completeness unavailable |
| Activation bit is set | Independent | `true` | Contributes to aggregate activation |
| Activation bit is clear | Independent | `false` | Does not imply not read |
| Activation value is negative or has an unknown high bit | Reading decoded independently when valid | Unavailable for every detail | Activation and breakthrough unavailable; reading completeness is retained |
| Version is unsupported | No details are emitted | No details are emitted | No fabricated map or percentage |

`TotalCount` is the verified definition count. `AvailableCount` is the only
known-state denominator and equals `ReadCount + NotReadCount`; unavailable
details are excluded from both. A known `NotRead` proves incomplete. If all
known details are read but any read state is unavailable, completeness remains
unavailable rather than reporting a misleading complete ratio.

`MissingStudyDetails` contains the exact verified detail objects whose read
state is `NotRead`, including stable ID, group, wheel order, localized label,
and provenance. `UnavailableStudyDetails` remains separate.

## Localized labels

The requested catalogue language flows from the Application atlas request to
the progress reader. Labels are read from the installed selected
`ui_language.txt` resource by localization key. The source is fingerprinted
before and after the read, and each available label carries its language
resource kind, fingerprint identity, and localization-key record identity.

Missing, malformed, or changing language resources never change the save
facts. They produce unavailable label fields and typed warnings without
exposing a local path. Traditional Chinese and English are independent; no
Infrastructure fallback silently changes the requested language.

## Breakthrough consistency

The atlas breakthrough mapper counts the `Read` Direct and Reverse details
from the same decoded collection stored on the progress object. It does not
perform a second raw-bit count. Completed breakthrough still has precedence;
before breakthrough, five read normal pages are required and a direction is
available at three matching pages.

This preserves the E2-002 behavior while preventing the detail map and the
breakthrough badge from disagreeing.

## Verification

Unit tests cover no pages, partial pages, all pages, completed Direct and
Reverse layouts, mixed direction readiness, negative and high-bit malformed
values, independently invalid activation, missing labels, selected-language
provenance, and an unsupported version.

The 2026-08-02 opt-in integration test used save fingerprint
`9C30C00CF1ABD05973435B14B724A0A41A1B0DCD7847A8CA04D4E60E2B53C916`.
Two reads returned the same 506-skill overlay. Skill `456` contained all 15
read details with the five Reverse details active; skill `498` contained the
exact 15-detail missing list. The save, loaded GameData files, and both relevant
language resources had identical fingerprints before and after.
