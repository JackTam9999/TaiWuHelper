using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetPlaybookComposition;
using TaiWu.Domain.TargetPlaybooks;
using Xunit;
using CompositionResult = TaiWu.Domain.TargetPlaybookComposition.TargetPlaybookComposition;

namespace TaiWu.Domain.UnitTests.TargetPlaybookCompositions;

public sealed class TargetSpecificPlaybookAdjusterTests
{
    [Fact]
    public void Exact_profile_sources_create_all_typed_evidence_kinds()
    {
        var (analysis, composition) = FullInputs();

        var result = TargetSpecificPlaybookAdjuster.Apply(
            composition,
            analysis);

        Assert.Equal(
            Enum.GetValues<TargetPlaybookAdjustmentEvidenceKind>(),
            result.ExactEvidence
                .Select(value => value.Kind)
                .Distinct()
                .Order());
        Assert.Equal(analysis.Profile.Fingerprint, result.ProfileFingerprint);
        Assert.Equal(composition.StableKey, result.CompositionKey);
        Assert.Equal(64, result.StableKey.Length);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Current_observation_elevates_only_supported_noncritical_goals()
    {
        var (analysis, composition) = FullInputs();

        var result = TargetSpecificPlaybookAdjuster.Apply(
            composition,
            analysis);

        Assert.Equal(
            TargetPlaybookAdjustmentAction.Elevated,
            GoalAdjustment(result, "PREPARE_FOR_OUTER_DAMAGE").Action);
        Assert.Equal(
            TargetPlaybookAdjustmentAction.Elevated,
            GoalAdjustment(
                result,
                "MITIGATE_CONFIGURED_POISON_APPLICATION").Action);
        Assert.Equal(
            TargetPlaybookAdjustmentAction.Retained,
            GoalAdjustment(result, "EXPLOIT_LESS_RESISTED_CHANNEL").Action);
        Assert.Equal(
            TargetPlaybookAdjustmentAction.Retained,
            GoalAdjustment(result, "SURVIVE_MIND_DAMAGE_PRESSURE").Action);
        Assert.All(
            composition.KnownGaps,
            gap => Assert.Equal(
                TargetPlaybookAdjustmentAction.Unresolved,
                GapAdjustment(result, gap.Code).Action));
    }

    [Fact]
    public void Exact_threat_outside_composed_playbooks_is_added()
    {
        var analysis = TargetPlaybookFixture.OuterOnlyAnalysis(
            includeExtraThreat: true);
        var composition = Compose(analysis);

        var result = TargetSpecificPlaybookAdjuster.Apply(
            composition,
            analysis);

        var added = Assert.Single(
            result.Adjustments,
            adjustment => adjustment.Action
                == TargetPlaybookAdjustmentAction.Added);
        Assert.Null(added.OriginalResponse);
        Assert.Equal(
            TargetPlaybookResponseReferenceKind.Threat,
            added.ResultResponse!.Kind);
        Assert.Equal("EXACT_REPEATED_ATTACK", added.ResultResponse.StableCode);
        Assert.Contains(
            added.Evidence,
            evidence => evidence.Kind
                == TargetPlaybookAdjustmentEvidenceKind.Threat);
        Assert.Contains(
            added.Evidence,
            evidence => evidence.Kind
                == TargetPlaybookAdjustmentEvidenceKind.Skill);
        Assert.Contains(
            added.Evidence,
            evidence => evidence.Kind
                == TargetPlaybookAdjustmentEvidenceKind.Effect);
    }

    [Theory]
    [InlineData(TargetPlaybookAdjustmentAction.Retained)]
    [InlineData(TargetPlaybookAdjustmentAction.Elevated)]
    [InlineData(TargetPlaybookAdjustmentAction.Reduced)]
    [InlineData(TargetPlaybookAdjustmentAction.Added)]
    [InlineData(TargetPlaybookAdjustmentAction.Replaced)]
    [InlineData(TargetPlaybookAdjustmentAction.Unresolved)]
    public void Reviewed_rules_support_every_typed_adjustment_action(
        TargetPlaybookAdjustmentAction action)
    {
        var (analysis, composition) = FullInputs();
        var probe = TargetSpecificPlaybookAdjuster.Apply(
            composition,
            analysis);
        var rule = Rule(action, probe);

        var result = TargetSpecificPlaybookAdjuster.Apply(
            composition,
            analysis,
            [rule]);

        var adjustment = Assert.Single(
            result.Adjustments,
            value => value.RuleCode == rule.Code);
        Assert.Equal(action, adjustment.Action);
        Assert.Equal(rule.OriginalResponse, adjustment.OriginalResponse);
        Assert.Equal(rule.ResultResponse, adjustment.ResultResponse);
        Assert.Equal(rule.ReasonCode, adjustment.ReasonCode);
    }

    [Fact]
    public void Reviewed_rules_can_use_each_confirmed_exact_target_fact_kind()
    {
        var (analysis, composition) = FullInputs();
        var probe = TargetSpecificPlaybookAdjuster.Apply(
            composition,
            analysis);
        var expectedKinds = new[]
        {
            TargetPlaybookAdjustmentEvidenceKind.ProfileFacet,
            TargetPlaybookAdjustmentEvidenceKind.Threat,
            TargetPlaybookAdjustmentEvidenceKind.Skill,
            TargetPlaybookAdjustmentEvidenceKind.Effect,
            TargetPlaybookAdjustmentEvidenceKind.Equipment,
            TargetPlaybookAdjustmentEvidenceKind.Observation
        };

        foreach (var kind in expectedKinds)
        {
            var evidence = probe.ExactEvidence.First(value =>
                value.Kind == kind
                && value.State
                    == TargetPlaybookAdjustmentEvidenceState.Confirmed);
            var rule = new TargetPlaybookAdjustmentRule(
                $"USE_{kind.ToString().ToUpperInvariant()}",
                TargetPlaybookAdjustmentAction.Elevated,
                GoalReference("PREPARE_FOR_OUTER_DAMAGE"),
                resultResponse: null,
                "EXACT_TARGET_FACT_ELEVATES_RESPONSE",
                [evidence.Identity]);

            var result = TargetSpecificPlaybookAdjuster.Apply(
                composition,
                analysis,
                [rule]);

            var applied = GoalAdjustment(
                result,
                "PREPARE_FOR_OUTER_DAMAGE");
            Assert.Equal(rule.Code, applied.RuleCode);
            Assert.Contains(applied.Evidence, value => value.Kind == kind);
        }
    }

    [Fact]
    public void Contrary_exact_evidence_reduces_a_broad_goal()
    {
        var (analysis, composition) = FullInputs();
        var probe = TargetSpecificPlaybookAdjuster.Apply(
            composition,
            analysis);
        var reduction = Rule(
            TargetPlaybookAdjustmentAction.Reduced,
            probe);

        var result = TargetSpecificPlaybookAdjuster.Apply(
            composition,
            analysis,
            [reduction]);

        var outer = GoalAdjustment(result, "PREPARE_FOR_OUTER_DAMAGE");
        Assert.Equal(TargetPlaybookAdjustmentAction.Reduced, outer.Action);
        Assert.Equal(reduction.Code, outer.RuleCode);
        Assert.Contains(
            outer.Evidence,
            evidence => evidence.State
                == TargetPlaybookAdjustmentEvidenceState.Contrary);
    }

    [Fact]
    public void Missing_or_wrong_state_rule_evidence_stays_diagnostic()
    {
        var (analysis, composition) = FullInputs();
        var outerGoal = GoalReference("PREPARE_FOR_OUTER_DAMAGE");
        var missing = new TargetPlaybookAdjustmentRule(
            "MISSING_EVIDENCE_RULE",
            TargetPlaybookAdjustmentAction.Retained,
            outerGoal,
            resultResponse: null,
            "MISSING_EVIDENCE",
            ["SKILL:999999"]);
        var wrongState = new TargetPlaybookAdjustmentRule(
            "WRONG_STATE_RULE",
            TargetPlaybookAdjustmentAction.Reduced,
            outerGoal,
            resultResponse: null,
            "WRONG_STATE",
            [OuterFacetEvidenceIdentity(
                TargetSpecificPlaybookAdjuster.Apply(
                    composition,
                    analysis))]);

        var result = TargetSpecificPlaybookAdjuster.Apply(
            composition,
            analysis,
            [wrongState, missing]);

        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code
                    == TargetSpecificPlaybookAdjuster.EvidenceMissingCode
                && diagnostic.RuleCode == missing.Code);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code
                    == TargetSpecificPlaybookAdjuster
                        .EvidenceStateMismatchCode
                && diagnostic.RuleCode == wrongState.Code);
        Assert.Equal(
            TargetPlaybookAdjustmentAction.Elevated,
            GoalAdjustment(result, "PREPARE_FOR_OUTER_DAMAGE").Action);
    }

    [Fact]
    public void Missing_original_response_cannot_apply_a_reviewed_rule()
    {
        var (analysis, composition) = FullInputs();
        var probe = TargetSpecificPlaybookAdjuster.Apply(
            composition,
            analysis);
        var rule = new TargetPlaybookAdjustmentRule(
            "MISSING_RESPONSE_RULE",
            TargetPlaybookAdjustmentAction.Retained,
            GoalReference("UNKNOWN_GOAL"),
            resultResponse: null,
            "UNKNOWN_RESPONSE",
            [OuterFacetEvidenceIdentity(probe)]);

        var result = TargetSpecificPlaybookAdjuster.Apply(
            composition,
            analysis,
            [rule]);

        var diagnostic = Assert.Single(
            result.Diagnostics,
            value => value.RuleCode == rule.Code);
        Assert.Equal(
            TargetSpecificPlaybookAdjuster.ResponseMissingCode,
            diagnostic.Code);
    }

    [Fact]
    public void Reviewed_rule_precedence_and_reordering_are_deterministic()
    {
        var (analysis, composition) = FullInputs();
        var probe = TargetSpecificPlaybookAdjuster.Apply(
            composition,
            analysis);
        var retained = Rule(
            TargetPlaybookAdjustmentAction.Retained,
            probe,
            code: "ZZ_RETAIN_RULE");
        var elevated = Rule(
            TargetPlaybookAdjustmentAction.Elevated,
            probe,
            code: "AA_ELEVATE_RULE");

        var first = TargetSpecificPlaybookAdjuster.Apply(
            composition,
            analysis,
            [retained, elevated]);
        var second = TargetSpecificPlaybookAdjuster.Apply(
            composition,
            analysis,
            [elevated, retained]);

        Assert.Equal(first.StableKey, second.StableKey);
        Assert.Equal(
            elevated.Code,
            GoalAdjustment(first, "PREPARE_FOR_OUTER_DAMAGE").RuleCode);
        Assert.Contains(
            first.Diagnostics,
            diagnostic => diagnostic.Code
                    == TargetSpecificPlaybookAdjuster.RuleShadowedCode
                && diagnostic.RuleCode == retained.Code);
    }

    [Fact]
    public void Stale_profile_or_match_set_cannot_adjust_a_composition()
    {
        var (analysis, composition) = FullInputs();
        var other = TargetPlaybookFixture.OuterOnlyAnalysis(
            includeExtraThreat: false);

        Assert.Throws<ArgumentException>(() =>
            TargetSpecificPlaybookAdjuster.Apply(composition, other));
        Assert.NotEqual(analysis.Profile.Fingerprint, other.Profile.Fingerprint);
    }

    [Fact]
    public void Adjustment_rules_enforce_action_reference_shapes()
    {
        var goal = GoalReference("GOAL");
        var option = new TargetPlaybookResponseReference(
            TargetPlaybookResponseReferenceKind.Option,
            "OPTION");

        Assert.Throws<ArgumentException>(() =>
            new TargetPlaybookAdjustmentRule(
                "INVALID_ADDED",
                TargetPlaybookAdjustmentAction.Added,
                goal,
                option,
                "INVALID",
                ["FACET:1:OUTER_DAMAGE_CONFIGURED"]));
        Assert.Throws<ArgumentException>(() =>
            new TargetPlaybookAdjustmentRule(
                "INVALID_REPLACEMENT",
                TargetPlaybookAdjustmentAction.Replaced,
                goal,
                goal,
                "INVALID",
                ["FACET:1:OUTER_DAMAGE_CONFIGURED"]));
    }

    private static (TargetCombatProfileAnalysis Analysis,
        CompositionResult Composition) FullInputs()
    {
        var definitions = VerifiedTargetCounterPlaybooks.Initial.Archetypes
            .Append(TargetPlaybookFixture.ContraryResistanceDefinition())
            .ToArray();
        var analysis = TargetPlaybookFixture.FullAnalysis(definitions);
        return (analysis, Compose(analysis));
    }

    private static CompositionResult Compose(
        TargetCombatProfileAnalysis analysis) =>
        TargetPlaybookComposer.Compose(
            analysis.ArchetypeMatches,
            VerifiedTargetCounterPlaybooks.Initial,
            TargetPlaybookFixture.GameVersion);

    private static TargetPlaybookAdjustment GoalAdjustment(
        TargetPlaybookAdjustmentSet set,
        string goalCode) => Assert.Single(
            set.Adjustments,
            adjustment => adjustment.OriginalResponse is
            {
                Kind: TargetPlaybookResponseReferenceKind.Goal
            }
                && adjustment.OriginalResponse.StableCode == goalCode);

    private static TargetPlaybookAdjustment GapAdjustment(
        TargetPlaybookAdjustmentSet set,
        string gapCode) => Assert.Single(
            set.Adjustments,
            adjustment => adjustment.OriginalResponse is
            {
                Kind: TargetPlaybookResponseReferenceKind.Gap
            }
                && adjustment.OriginalResponse.StableCode == gapCode);

    private static TargetPlaybookAdjustmentRule Rule(
        TargetPlaybookAdjustmentAction action,
        TargetPlaybookAdjustmentSet evidence,
        string? code = null)
    {
        var outer = GoalReference("PREPARE_FOR_OUTER_DAMAGE");
        var confirmed = OuterFacetEvidenceIdentity(evidence);
        var contrary = evidence.ExactEvidence.Single(value =>
            value.Kind == TargetPlaybookAdjustmentEvidenceKind.ArchetypeMatch
            && value.State
                == TargetPlaybookAdjustmentEvidenceState.Contrary).Identity;
        var gap = evidence.ExactEvidence.Single(value =>
            value.Kind == TargetPlaybookAdjustmentEvidenceKind.Gap
            && value.Identity
                == "GAP:NO_VERIFIED_OUTER_DAMAGE_COUNTER").Identity;
        var (original, result, required) = action switch
        {
            TargetPlaybookAdjustmentAction.Added =>
                (null,
                    new TargetPlaybookResponseReference(
                        TargetPlaybookResponseReferenceKind.Threat,
                        "CUSTOM_ADDED_RESPONSE"),
                    confirmed),
            TargetPlaybookAdjustmentAction.Replaced =>
                (outer,
                    new TargetPlaybookResponseReference(
                        TargetPlaybookResponseReferenceKind.Option,
                        "CUSTOM_REPLACEMENT_OPTION"),
                    confirmed),
            TargetPlaybookAdjustmentAction.Reduced =>
                (outer, null, contrary),
            TargetPlaybookAdjustmentAction.Unresolved =>
                (new TargetPlaybookResponseReference(
                        TargetPlaybookResponseReferenceKind.Gap,
                        "NO_VERIFIED_OUTER_DAMAGE_COUNTER"),
                    null,
                    gap),
            _ => (outer, null, confirmed)
        };
        return new TargetPlaybookAdjustmentRule(
            code ?? $"TEST_{action.ToString().ToUpperInvariant()}",
            action,
            original,
            result,
            $"EXACT_TARGET_{action.ToString().ToUpperInvariant()}",
            [required]);
    }

    private static string OuterFacetEvidenceIdentity(
        TargetPlaybookAdjustmentSet set) => set.ExactEvidence.Single(value =>
            value.Kind == TargetPlaybookAdjustmentEvidenceKind.ProfileFacet
            && value.Identity.EndsWith(
                ":OUTER_DAMAGE_CONFIGURED",
                StringComparison.Ordinal)).Identity;

    private static TargetPlaybookResponseReference GoalReference(string code) =>
        new(TargetPlaybookResponseReferenceKind.Goal, code);
}
