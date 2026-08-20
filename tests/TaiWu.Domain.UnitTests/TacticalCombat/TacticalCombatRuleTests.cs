using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;
using Xunit;

namespace TaiWu.Domain.UnitTests.TacticalCombat;

public sealed class TacticalCombatRuleTests
{
    [Fact]
    public void Historical_rule_set_pins_version_goals_and_every_definition()
    {
        var rules = Rules;

        Assert.Equal(
            VerifiedCombatEffectCatalogs.GoldenGameDataVersion,
            Assert.Single(rules.SupportedGameDataVersions));
        Assert.Equal(new TacticalSemanticVersion(1, 0, 0), rules.SemanticVersion);
        Assert.Equal(
            [
                "DEFEAT_MARK_RESET_LOOP",
                "DISTRACTION_MARK_ACCUMULATION",
                "MIND_RESONANCE_CASCADE",
                "POSITIVE_MAGIC_SOUND_MIND_DAMAGE"
            ],
            rules.SupportedTargetGoalCodes);
        Assert.Equal(14, rules.Transitions.Length);
        Assert.Equal(7, rules.Roles.Length);
        Assert.Equal(64, rules.Fingerprint.Length);
        Assert.Equal(
            [
                "DEFEAT_THRESHOLD_CAN_TRIGGER_RESET",
                "DIRECT_267_SHORTENS_DISTRACTION_DURATION",
                "DIRECT_MAGIC_CAST_CREATES_MIND_PRESSURE",
                "FEASIBLE_REVERSE_CAST_REDUCES_LOCK_LAYER",
                "FIRST_MARK_STARTS_RESONANCE_COUNTDOWN",
                "MIND_PRESSURE_CREATES_DISTRACTION_MARKS",
                "RESONANCE_ZERO_STARTS_CASCADE",
                "REVERSE_134_SHORTENS_RESONANCE_DURATION",
                "REVERSE_291_PRESSURES_RANDOM_TRUE_QI",
                "REVERSE_604_APPLIES_DIRECT_PRACTICE_LOCK",
                "REVERSE_604_SUPPRESSES_DIRECT_CAST",
                "REVERSE_611_TRANSFERS_HINDRANCE_MARKS",
                "REVERSE_624_REDUCES_ATTACK_POWER",
                "REVERSE_686_REMOVES_HINDRANCE_MARK"
            ],
            rules.Transitions.Select(item => item.Identity.Code));
    }

    [Fact]
    public void Every_rule_has_semantics_versions_evidence_timing_and_limit()
    {
        Assert.All(
            Rules.Transitions,
            rule =>
            {
                Assert.Equal(Rules.SemanticVersion, rule.SemanticVersion);
                Assert.Equal(
                    Rules.SupportedGameDataVersions,
                    rule.SupportedGameDataVersions);
                Assert.True(Enum.IsDefined(rule.Purpose));
                Assert.True(Enum.IsDefined(rule.Timing));
                Assert.NotEmpty(rule.TriggerFacts);
                Assert.NotEmpty(rule.ResultingFacts);
                Assert.NotEmpty(rule.EvidenceRequirements);
                Assert.NotEmpty(rule.Evidence);
                Assert.False(string.IsNullOrWhiteSpace(rule.LimitationIdentity));
            });
        Assert.All(
            Rules.Roles,
            rule =>
            {
                Assert.Equal(Rules.SemanticVersion, rule.SemanticVersion);
                Assert.Equal(
                    Rules.SupportedGameDataVersions,
                    rule.SupportedGameDataVersions);
                Assert.True(Enum.IsDefined(rule.Purpose));
                Assert.True(Enum.IsDefined(rule.Timing));
                Assert.NotEmpty(rule.RequiredMechanics);
                Assert.Equal(rule.Effect.Mechanics, rule.RequiredMechanics);
                Assert.NotEmpty(rule.Transitions);
                Assert.NotEmpty(rule.EvidenceRequirements);
                Assert.NotEmpty(rule.Evidence);
                Assert.False(string.IsNullOrWhiteSpace(rule.LimitationIdentity));
            });
    }

    [Fact]
    public void Delivered_roles_pin_exact_skill_direction_effect_and_mechanics()
    {
        Assert.Equal(
            [
                (134, PracticeDirection.Reverse, 973),
                (267, PracticeDirection.Direct, 165),
                (291, PracticeDirection.Reverse, 915),
                (604, PracticeDirection.Reverse, 1064),
                (611, PracticeDirection.Reverse, 1165),
                (624, PracticeDirection.Reverse, 1234),
                (686, PracticeDirection.Reverse, 1422)
            ],
            Rules.Roles
                .Select(item => (item.SkillId, item.Direction, item.RawEffectId))
                .OrderBy(item => item.SkillId));
        Assert.All(
            Rules.Roles,
            role =>
            {
                var resolved = VerifiedCombatEffectCatalogs.GoldenAntiMagic
                    .Resolve(
                        VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion,
                        role.SkillId,
                        role.Direction,
                        role.RawEffectId);
                Assert.True(resolved.IsRecognized);
                Assert.Same(resolved.CatalogEntry, role.Effect);
                Assert.True(role.Effect.HasTypedMechanics);
            });
    }

    [Fact]
    public void Shared_counters_are_narrowed_to_selected_exact_target_goals()
    {
        var shared = Rules.Roles.Where(item => item.SharedCounter is not null)
            .ToArray();

        Assert.Equal(6, shared.Length);
        Assert.All(
            shared,
            role =>
            {
                Assert.Equal(role.SkillId, role.SharedCounter!.Effect.SkillId);
                Assert.Equal(
                    role.Direction,
                    role.SharedCounter.RequiredDirection);
                Assert.Equal(
                    role.RawEffectId,
                    role.SharedCounter.Effect.RawEffectId);
                Assert.All(
                    role.TargetGoalCodes,
                    goal => Assert.Contains(
                        goal,
                        role.SharedCounter.ThreatCodes));
                Assert.All(
                    role.TargetGoalCodes,
                    goal => Assert.Contains(
                        goal,
                        Rules.SupportedTargetGoalCodes));
            });
        Assert.Null(Role("REVERSE_611_CONDITIONAL_MARK_TRANSFER").SharedCounter);
    }

    [Fact]
    public void Suppression_self_lock_and_recovery_are_separate_interactions()
    {
        var suppression = Role("REVERSE_604_DIRECT_CAST_SUPPRESSION");

        Assert.Equal(TacticalRoleKind.Suppression, suppression.Identity.Kind);
        Assert.Equal(
            [
                "REVERSE_604_APPLIES_DIRECT_PRACTICE_LOCK",
                "REVERSE_604_SUPPRESSES_DIRECT_CAST"
            ],
            suppression.Transitions.Select(item => item.Code));
        Assert.Equal(
            TacticalRulePurpose.DirectPracticeSelfLock,
            Transition("REVERSE_604_APPLIES_DIRECT_PRACTICE_LOCK").Purpose);
        Assert.Equal(
            TacticalRulePurpose.DirectPracticeLockRecovery,
            Transition("FEASIBLE_REVERSE_CAST_REDUCES_LOCK_LAYER").Purpose);
        Assert.Equal(
            "THREE_EXACT_EXECUTABLE_CASTS_NOT_PRESELECTED",
            Transition("FEASIBLE_REVERSE_CAST_REDUCES_LOCK_LAYER")
                .LimitationIdentity);
    }

    [Fact]
    public void Reset_pressure_is_random_and_no_finish_role_is_delivered()
    {
        var reset = Role("REVERSE_291_RESET_RESOURCE_PRESSURE");

        Assert.Equal(
            [
                CombatEffectMechanic.AmplifyEnemyDamageStates,
                CombatEffectMechanic.DrainEnemyRandomTrueQi
            ],
            reset.RequiredMechanics);
        Assert.Equal("RANDOM_DRAIN_IS_NOT_RESET_LOCKOUT", reset.LimitationIdentity);
        Assert.DoesNotContain(
            Rules.Roles,
            item => item.Identity.Kind is TacticalRoleKind.Finish
                or TacticalRoleKind.DamageChannel);
        Assert.DoesNotContain(
            Rules.Roles,
            item => item.Purpose is TacticalRulePurpose.FinishWindowSupport
                or TacticalRulePurpose.DamageChannelChoice);
    }

    [Fact]
    public void Complete_evidence_applies_every_relevant_rule()
    {
        var resolution = Resolve(AllConfirmedObservations());

        Assert.True(resolution.IsResolved);
        Assert.All(
            resolution.Transitions,
            item => Assert.Equal(
                TacticalRuleApplicability.Applicable,
                item.Applicability));
        Assert.All(
            resolution.Roles,
            item => Assert.Equal(
                TacticalRuleApplicability.Applicable,
                item.Applicability));
    }

    [Fact]
    public void Every_prerequisite_identity_must_be_confirmed()
    {
        var observations = AllConfirmedObservations()
            .Where(item => item.Identity.Code
                != "MIND_LOSS_TO_DISTRACTION_VERIFIED")
            .ToArray();

        var match = Assert.Single(
            Resolve(observations).Transitions,
            item => item.Rule.Identity.Code
                == "MIND_PRESSURE_CREATES_DISTRACTION_MARKS");

        Assert.Equal(TacticalRuleApplicability.Incomplete, match.Applicability);
        Assert.Equal(
            ["MIND_LOSS_TO_DISTRACTION_VERIFIED"],
            match.UnmetEvidence.Select(item => item.Code));
    }

    [Fact]
    public void Exact_target_contrary_evidence_overrides_broad_confirmation()
    {
        var broadIdentity = new TacticalRuleEvidenceIdentity(
            "REVERSE_604_EFFECT_VERIFIED");
        var observations = AllConfirmedObservations().Append(
            Observation(
                broadIdentity,
                TacticalRuleEvidenceScope.ExactTarget,
                TacticalEvidenceSourceKind.ConfirmedObservation,
                TacticalRuleEvidenceDisposition.Contrary,
                "EXACT_CONTRARY_REVERSE_604"));

        var resolution = Resolve(observations);
        var transition = Assert.Single(
            resolution.Transitions,
            item => item.Rule.Identity.Code
                == "REVERSE_604_SUPPRESSES_DIRECT_CAST");
        var role = RoleMatch(
            resolution,
            "REVERSE_604_DIRECT_CAST_SUPPRESSION");

        Assert.Equal(
            TacticalRuleApplicability.Contrary,
            transition.Applicability);
        Assert.Equal(TacticalRuleApplicability.Contrary, role.Applicability);
    }

    [Fact]
    public void Exact_target_absence_does_not_negate_a_broad_verified_rule()
    {
        var identity = new TacticalRuleEvidenceIdentity(
            "MAGIC_SOUND_DIRECT_EFFECT_VERIFIED");
        var observations = AllConfirmedObservations().Append(
            Observation(
                identity,
                TacticalRuleEvidenceScope.ExactTarget,
                TacticalEvidenceSourceKind.ConfirmedObservation,
                TacticalRuleEvidenceDisposition.Absent,
                "EXACT_ABSENCE_MAGIC_SOUND"));

        var match = Assert.Single(
            Resolve(observations).Transitions,
            item => item.Rule.Identity.Code
                == "DIRECT_MAGIC_CAST_CREATES_MIND_PRESSURE");

        Assert.Equal(TacticalRuleApplicability.Applicable, match.Applicability);
    }

    [Fact]
    public void Missing_exact_target_evidence_is_incomplete_not_contrary()
    {
        var observations = AllConfirmedObservations()
            .Where(item => item.Identity.Code
                != "TARGET_DIRECT_MAGIC_SIGNATURE_ACTIVE")
            .ToArray();

        var match = Assert.Single(
            Resolve(observations).Transitions,
            item => item.Rule.Identity.Code
                == "DIRECT_MAGIC_CAST_CREATES_MIND_PRESSURE");

        Assert.Equal(TacticalRuleApplicability.Incomplete, match.Applicability);
        Assert.NotEqual(TacticalRuleApplicability.Contrary, match.Applicability);
    }

    [Fact]
    public void Unsupported_version_returns_no_stale_or_nearest_rules()
    {
        var resolution = Rules.Resolve(
            "1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20",
            Rules.SupportedTargetGoalCodes,
            AllConfirmedObservations());

        Assert.Equal(
            TacticalRuleSetResolutionStatus.UnsupportedGameDataVersion,
            resolution.Status);
        Assert.False(resolution.IsResolved);
        Assert.Empty(resolution.Transitions);
        Assert.Empty(resolution.Roles);
    }

    [Fact]
    public void Resolution_exposes_only_rules_relevant_to_requested_goals()
    {
        var resolution = Rules.Resolve(
            VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion,
            ["DEFEAT_MARK_RESET_LOOP"],
            AllConfirmedObservations());

        Assert.Equal(
            [
                "DEFEAT_THRESHOLD_CAN_TRIGGER_RESET",
                "REVERSE_291_PRESSURES_RANDOM_TRUE_QI"
            ],
            resolution.Transitions.Select(item => item.Rule.Identity.Code));
        Assert.Equal(
            ["REVERSE_291_RESET_RESOURCE_PRESSURE"],
            resolution.Roles.Select(item => item.Rule.Identity.Code));
    }

    [Fact]
    public void Raw_names_descriptions_and_source_text_do_not_define_rules()
    {
        var original = Rules.Roles[0];
        var displayVariant = new CombatEffectCatalogEntry(
            original.SkillId,
            "Changed display name",
            original.Direction,
            original.RawEffectId,
            "Changed raw description that must never be parsed.",
            "display-only:changed",
            original.Effect.Mechanics);
        var replacement = CloneRole(original, effect: displayVariant);
        var variant = new TacticalCombatRuleSet(
            Rules.SemanticVersion,
            Rules.SupportedGameDataVersions,
            Rules.SupportedTargetGoalCodes,
            Rules.Transitions,
            Rules.Roles.Select(item => item == original ? replacement : item));

        Assert.Equal(Rules.Fingerprint, variant.Fingerprint);
        Assert.Equal(original.RequiredMechanics, replacement.RequiredMechanics);
    }

    [Fact]
    public void Duplicate_and_unknown_rule_references_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => new TacticalCombatRuleSet(
            Rules.SemanticVersion,
            Rules.SupportedGameDataVersions,
            Rules.SupportedTargetGoalCodes,
            [Rules.Transitions[0], Rules.Transitions[0]],
            Rules.Roles));

        var role = Rules.Roles[0];
        var unknown = CloneRole(
            role,
            transitions: [new TacticalTransitionIdentity("UNKNOWN_TRANSITION")]);
        Assert.Throws<ArgumentException>(() => new TacticalCombatRuleSet(
            Rules.SemanticVersion,
            Rules.SupportedGameDataVersions,
            Rules.SupportedTargetGoalCodes,
            Rules.Transitions,
            Rules.Roles.Select(item => item == role ? unknown : item)));
    }

    [Fact]
    public void Invalid_timing_mechanics_and_source_versions_are_rejected()
    {
        var transition = Rules.Transitions[0];
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TacticalTransitionRule(
                transition.Identity,
                transition.SemanticVersion,
                transition.SupportedGameDataVersions,
                transition.Purpose,
                (TacticalTransitionTiming)999,
                transition.TriggerFacts,
                transition.ResultingFacts,
                transition.TargetGoalCodes,
                transition.EvidenceRequirements,
                transition.LimitationIdentity,
                transition.Evidence));

        var role = Rules.Roles[0];
        Assert.Throws<ArgumentException>(() => new TacticalSkillRoleRule(
            role.Identity,
            role.SemanticVersion,
            role.SupportedGameDataVersions,
            role.Purpose,
            role.Timing,
            role.Effect,
            [CombatEffectMechanic.DrainEnemyRandomTrueQi],
            role.TargetGoalCodes,
            role.Transitions,
            role.EvidenceRequirements,
            role.LimitationIdentity,
            role.Evidence,
            role.SharedCounter));

        Assert.Throws<ArgumentException>(() => new TacticalTransitionRule(
            transition.Identity,
            transition.SemanticVersion,
            ["CHANGED_VERSION"],
            transition.Purpose,
            transition.Timing,
            transition.TriggerFacts,
            transition.ResultingFacts,
            transition.TargetGoalCodes,
            transition.EvidenceRequirements,
            transition.LimitationIdentity,
            transition.Evidence));
    }

    private static TacticalCombatRuleSet Rules =>
        VerifiedTacticalCombatRuleSets.HistoricalMagicSound;

    private static TacticalTransitionRule Transition(string code) =>
        Rules.Transitions.Single(item => item.Identity.Code == code);

    private static TacticalSkillRoleRule Role(string code) =>
        Rules.Roles.Single(item => item.Identity.Code == code);

    private static TacticalSkillRoleRuleMatch RoleMatch(
        TacticalCombatRuleResolution resolution,
        string code) => resolution.Roles.Single(
            item => item.Rule.Identity.Code == code);

    private static TacticalCombatRuleResolution Resolve(
        IEnumerable<TacticalRuleEvidenceObservation> observations) =>
        Rules.Resolve(
            VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion,
            Rules.SupportedTargetGoalCodes,
            observations);

    private static TacticalRuleEvidenceObservation[]
        AllConfirmedObservations()
    {
        return Rules.Transitions
            .SelectMany(item => item.EvidenceRequirements)
            .Concat(Rules.Roles.SelectMany(item => item.EvidenceRequirements))
            .DistinctBy(item => (
                item.Identity.Code,
                item.Scope,
                item.Source))
            .OrderBy(item => item.Identity.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Scope)
            .ThenBy(item => item.Source)
            .Select((item, index) => Observation(
                item.Identity,
                item.Scope,
                item.Source,
                TacticalRuleEvidenceDisposition.Confirmed,
                $"CONFIRMED_{index:000}"))
            .ToArray();
    }

    private static TacticalRuleEvidenceObservation Observation(
        TacticalRuleEvidenceIdentity identity,
        TacticalRuleEvidenceScope scope,
        TacticalEvidenceSourceKind source,
        TacticalRuleEvidenceDisposition disposition,
        string evidenceIdentity) => new(
            identity,
            scope,
            source,
            disposition,
            new TacticalEvidenceReference(
                source,
                evidenceIdentity,
                VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion,
                VerifiedTacticalCombatRuleSets.RuleVersion,
                scope == TacticalRuleEvidenceScope.ExactTarget
                    ? "EXACT_TARGET"
                    : "BROAD_RULE"));

    private static TacticalSkillRoleRule CloneRole(
        TacticalSkillRoleRule role,
        CombatEffectCatalogEntry? effect = null,
        IEnumerable<TacticalTransitionIdentity>? transitions = null) => new(
            role.Identity,
            role.SemanticVersion,
            role.SupportedGameDataVersions,
            role.Purpose,
            role.Timing,
            effect ?? role.Effect,
            role.RequiredMechanics,
            role.TargetGoalCodes,
            transitions ?? role.Transitions,
            role.EvidenceRequirements,
            role.LimitationIdentity,
            role.Evidence,
            role.SharedCounter);
}
