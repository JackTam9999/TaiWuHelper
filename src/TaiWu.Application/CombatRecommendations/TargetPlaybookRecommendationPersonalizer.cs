using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetPlaybookComposition;
using TaiWu.Domain.TargetPlaybooks;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Application.CombatRecommendations;

internal static class TargetPlaybookRecommendationPersonalizer
{
    private const string UnavailableGameDataVersion =
        "UNAVAILABLE_GAME_DATA_VERSION";

    internal static TargetPlaybookRecommendationPlan Prepare(
        CombatSnapshot snapshot,
        CombatRequirementContext requirementContext)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(requirementContext);
        var catalog = VerifiedTargetCounterPlaybooks.Initial;
        var analysis = TargetCombatProfileAnalyzer.Analyze(
            snapshot,
            VerifiedTargetThreatRuleSets.GoldenMagicSound,
            VerifiedTargetProfileExtractionRuleSets.Initial,
            catalog.Archetypes);
        var observedVersion = snapshot.Metadata.GameDataVersion.IsAvailable
            ? snapshot.Metadata.GameDataVersion.Value
            : UnavailableGameDataVersion;
        var composition = TargetPlaybookComposer.Compose(
            analysis.ArchetypeMatches,
            catalog,
            observedVersion);
        var adjustments = TargetSpecificPlaybookAdjuster.Apply(
            composition,
            analysis);
        var eligibleGoalCodes = EligibleGoalCodes(
            composition,
            adjustments);
        var eligibleGoals = composition.Goals
            .Where(goal => eligibleGoalCodes.Contains(goal.Code))
            .ToArray();
        var eligibleOptions = EligibleOptions(
            composition,
            adjustments,
            eligibleGoalCodes);
        var access = CombatCounterAccessEvaluator.Evaluate(
            snapshot.Player,
            requirementContext,
            new CombatCounterRuleSet(
                catalog.GameDataVersion.Value,
                eligibleOptions.Select(option => option.CounterRule)),
            allowBreakthrough: true,
            evaluateProposedSelection: true);
        return new TargetPlaybookRecommendationPlan(
            analysis,
            composition,
            adjustments,
            eligibleGoals,
            eligibleOptions,
            access);
    }

    internal static TargetPlaybookPersonalization Complete(
        TargetPlaybookRecommendationPlan plan,
        CombatLoadoutGenerationResult generation)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(generation);
        var truncated = generation.Diagnostics.Any(diagnostic =>
            diagnostic.Code is
                CombatLoadoutGenerationDiagnosticCode
                    .ExplorationLimitReached
                or CombatLoadoutGenerationDiagnosticCode
                    .ResultLimitReached);
        var counters = plan.Options.Select(option =>
        {
            var access = plan.Access.Evaluations.Single(value =>
                ReferenceEquals(value.Rule, option.CounterRule));
            var diagnostics = generation.Diagnostics
                .Where(value => value.SkillId == option.Effect.SkillId)
                .ToArray();
            var selected = generation.Candidates.Any(candidate =>
                candidate.SelectedOptions.Any(value =>
                    Matches(value, option.CounterRule)));
            var state = selected
                ? TargetPlaybookCounterAvailabilityState.Feasible
                : !access.IsAccessible
                    ? TargetPlaybookCounterAvailabilityState.Inaccessible
                    : truncated
                        ? TargetPlaybookCounterAvailabilityState.Unresolved
                        : TargetPlaybookCounterAvailabilityState.Infeasible;
            var gap = state == TargetPlaybookCounterAvailabilityState.Feasible
                ? null
                : new TargetCounterPlaybookGap(
                    $"PLAYER_UNAVAILABLE_{option.StableKey}",
                    TargetCounterPlaybookGapKind
                        .InaccessibleVerifiedOption,
                    "TargetPlaybook.Gap.PlayerCannotAccessVerifiedCounter",
                    ["E5-006", option.StableKey],
                    option.StableKey);
            return new TargetPlaybookCounterAvailability(
                option,
                access,
                state,
                gap,
                diagnostics);
        });
        return new TargetPlaybookPersonalization(
            plan.Analysis,
            plan.Composition,
            plan.Adjustments,
            plan.EligibleGoals,
            counters);
    }

    private static HashSet<string> EligibleGoalCodes(
        TargetPlaybookComposition composition,
        TargetPlaybookAdjustmentSet adjustments)
    {
        var compositionGoalCodes = composition.Goals
            .Select(value => value.Code)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> eligible = new(StringComparer.Ordinal);
        foreach (var adjustment in adjustments.Adjustments)
        {
            if (!adjustment.Evidence.Any(value =>
                    value.State
                        == TargetPlaybookAdjustmentEvidenceState.Confirmed))
            {
                continue;
            }

            switch (adjustment.Action)
            {
                case TargetPlaybookAdjustmentAction.Retained:
                case TargetPlaybookAdjustmentAction.Elevated:
                    AddGoal(adjustment.OriginalResponse);
                    break;
                case TargetPlaybookAdjustmentAction.Added:
                    AddGoal(adjustment.ResultResponse);
                    break;
                case TargetPlaybookAdjustmentAction.Replaced:
                    AddGoal(adjustment.ResultResponse);
                    break;
            }
        }

        return eligible;

        void AddGoal(TargetPlaybookResponseReference? response)
        {
            if (response?.Kind == TargetPlaybookResponseReferenceKind.Goal
                && compositionGoalCodes.Contains(response.StableCode))
            {
                eligible.Add(response.StableCode);
            }
        }
    }

    private static ComposedTargetCounterOption[] EligibleOptions(
        TargetPlaybookComposition composition,
        TargetPlaybookAdjustmentSet adjustments,
        HashSet<string> eligibleGoalCodes)
    {
        var optionCodes = composition.Options
            .Where(value => value.SourceGoalCodes.Any(
                eligibleGoalCodes.Contains))
            .Select(value => value.StableKey)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var adjustment in adjustments.Adjustments.Where(value =>
                     value.OriginalResponse?.Kind
                         == TargetPlaybookResponseReferenceKind.Option))
        {
            if (adjustment.Action is TargetPlaybookAdjustmentAction.Reduced
                or TargetPlaybookAdjustmentAction.Unresolved
                or TargetPlaybookAdjustmentAction.Replaced)
            {
                optionCodes.Remove(
                    adjustment.OriginalResponse!.StableCode);
            }
        }

        foreach (var adjustment in adjustments.Adjustments.Where(value =>
                     value.Action is TargetPlaybookAdjustmentAction.Added
                         or TargetPlaybookAdjustmentAction.Replaced
                     && value.ResultResponse?.Kind
                         == TargetPlaybookResponseReferenceKind.Option
                     && value.Evidence.Any(evidence => evidence.State
                         == TargetPlaybookAdjustmentEvidenceState.Confirmed)))
        {
            optionCodes.Add(adjustment.ResultResponse!.StableCode);
        }

        return
        [
            .. composition.Options
                .Where(option => optionCodes.Contains(option.StableKey)
                    && option.SourceGoalCodes.Any(
                        eligibleGoalCodes.Contains))
                .OrderByDescending(option => option.Strength)
                .ThenBy(option => option.StableKey, StringComparer.Ordinal)
        ];
    }

    private static bool Matches(
        CombatLoadoutOption option,
        CombatCounterRule rule) =>
        option.Candidate.SkillId == rule.Effect.SkillId
        && option.Candidate.RequiredDirection == rule.RequiredDirection
        && option.ExpectedEffectId == rule.Effect.RawEffectId;
}

internal sealed record TargetPlaybookRecommendationPlan(
    TargetCombatProfileAnalysis Analysis,
    TargetPlaybookComposition Composition,
    TargetPlaybookAdjustmentSet Adjustments,
    ComposedTargetResponseGoal[] EligibleGoals,
    ComposedTargetCounterOption[] Options,
    CombatCounterAccessReport Access);
