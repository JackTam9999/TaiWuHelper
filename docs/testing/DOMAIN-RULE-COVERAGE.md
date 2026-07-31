# Domain rule test coverage

This matrix traces the hard recommendation constraints to executable xUnit v3
tests. Domain tests use only immutable in-memory snapshots and do not require
the installed game, a save file, or proprietary GameData binaries.

| Constraint | Positive coverage | Negative and boundary coverage |
| --- | --- | --- |
| Learned-skill ownership | `Known_skill_without_special_requirements_is_accepted` | `Unknown_skill_is_rejected_instead_of_throwing` |
| Required mastery | `Required_mastery_accepts_mastered_skill` | `Required_mastery_rejects_unmastered_skill`; unavailable mastery is rejected explicitly |
| Practice direction and effect availability | Direct, reverse, explicit manual direction-change, and exact immediately achievable breakthrough cases are accepted | Neutral activation, opposite direction, wrong-direction breakthrough, unavailable breakthrough, and unavailable direct/reverse effects are rejected |
| GameData direction adapter | Completed Direct and Reverse values map by GameData meaning; read-page bits map the exact directions an available breakthrough can produce | `None`, `NotInited`, unknown values, and incomplete breakthrough cannot be mistaken for an active Reverse effect; potential direction is never counted as active direction |
| Verified effect identity | `Matching_verified_effect_is_accepted` | `Changed_verified_effect_is_rejected_before_combinations` |
| Weapon, trick, range, resource, unlock, and activation requirements | `All_supported_requirement_types_can_be_satisfied` | `Every_supported_hard_requirement_has_a_rejection_case`; unknown hard values are also rejected |
| Active defense/agility uniqueness | The supported-requirements case accepts one active skill per role | Conflicting active-role options and a skill assigned to both roles are rejected |
| Candidate and proposal membership | `Valid_proposal_produces_accepted_only_loadout` | Missing candidate, unselected candidate, rejected candidate, duplicate option, and duplicate equipped skill cases are rejected |
| Requirement context and generic-slot total | Valid proposals retain their matching context and allocation | Context mismatch and generic-allocation mismatch are rejected |
| Category slot budgets | `Exact_capacity_is_valid_and_one_over_is_rejected` accepts exact capacity for Neigong 6 and Attack/Agility/Defense/Assistance 2 | The same theory rejects one over capacity in every category; unavailable cost remains explicitly unavailable |
| Neigong combination optimization | Current Neigong is retained when sufficient; a learned provider is selected within the six-slot budget when required; generic slots are assigned to actual deficits | Over-budget or capacity-insufficient combinations cannot reach scoring |
| Inner-power compatibility | Active skills receive mapped power and requirement adjustments; an equipped Pure Yang Neigong is not treated as a cast | An actively used skill matching the backlash element scores zero and exposes a known risk; missing state or element remains unavailable |
| Legendary-book cost assignments | Evidence-backed assignments produce their defined effective costs | Unknown slots, invalid categories, missing assignments, duplicates, and unevidenced rules are rejected |
| GameData version and rule signatures | Supported versions and exact relevant signatures are accepted | Unsupported versions and changed relevant signatures block stale rules |
| Deterministic output | Generator input-order, threat ordering, scorer tie-break, explanation, and manual-plan tests assert stable output | Reordered inputs cannot alter candidate order or stable references |

`TaiWu.Architecture.Tests.Domain_rule_tests_do_not_require_the_installed_game`
guards the isolation boundary: the Domain test project cannot reference
Infrastructure or GameData, and its source cannot depend on an installed Steam
path or GameData namespace.
