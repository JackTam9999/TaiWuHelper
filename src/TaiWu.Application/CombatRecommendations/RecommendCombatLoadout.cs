using TaiWu.Application.CombatSnapshots;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;

namespace TaiWu.Application.CombatRecommendations;

public sealed class RecommendCombatLoadout(ICombatSnapshotReader reader)
    : IRecommendCombatLoadout
{
    public async Task<CombatLoadoutRecommendation> ExecuteAsync(
        RecommendCombatLoadoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (request.TargetObservation is not null)
        {
            throw new ArgumentException(
                "A target observation requires the target-observation "
                + "recommendation workflow.",
                nameof(request));
        }

        var snapshotRequest = new CombatSnapshotReadRequest(
            request.SaveFilePath,
            request.TargetCharacterId,
            request.CurrentLoadoutObservation,
            request.Language);
        var snapshot = await reader.ReadAsync(
            snapshotRequest,
            cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return Build(
            snapshot,
            request.Policy,
            targetObservation: null,
            cancellationToken);
    }

    internal static CombatLoadoutRecommendation Build(
        CombatSnapshot snapshot,
        RecommendationPolicy requestedPolicy,
        TargetObservationProcessingResult? targetObservation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (targetObservation is not null
            && !ReferenceEquals(
                snapshot,
                targetObservation.Merge.Snapshot))
        {
            throw new ArgumentException(
                "Target-observation recommendations require their merged "
                + "snapshot.",
                nameof(targetObservation));
        }

        if (!Enum.IsDefined(requestedPolicy))
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedPolicy),
                requestedPolicy,
                "Unknown recommendation policy.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var requirementContext = CreateBaseRequirementContext(
            snapshot.Player);
        var playbookPlan = TargetPlaybookRecommendationPersonalizer.Prepare(
            snapshot,
            requirementContext);
        var recommendationThreats = playbookPlan.EligibleGoals
            .SelectMany(goal => goal.Threats)
            .DistinctBy(threat => threat.Code, StringComparer.Ordinal)
            .OrderBy(threat => threat.Code, StringComparer.Ordinal)
            .ToArray();
        cancellationToken.ThrowIfCancellationRequested();

        var generation = CombatLoadoutGenerator.Generate(
            new CombatLoadoutGenerationRequest(
                snapshot.Player,
                BuildOptions(snapshot.Player, playbookPlan),
                requirementContext,
                snapshot.Player.GenericSlotAllocation));
        cancellationToken.ThrowIfCancellationRequested();
        var targetPlaybook =
            TargetPlaybookRecommendationPersonalizer.Complete(
                playbookPlan,
                generation);

        List<CombatRecommendationStyleResult> styles = [];
        foreach (var policy in Enum.GetValues<RecommendationPolicy>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            styles.Add(
                BuildStyle(
                    snapshot.Player,
                    recommendationThreats,
                    generation,
                    policy));
        }

        return new CombatLoadoutRecommendation(
            snapshot,
            targetPlaybook.Analysis.ThreatAnalysis,
            generation,
            requestedPolicy,
            styles,
            targetObservation,
            targetObservationImpact: null,
            targetPlaybook);
    }

    private static CombatRecommendationStyleResult BuildStyle(
        PlayerCombatSnapshot player,
        TargetThreat[] threats,
        CombatLoadoutGenerationResult generation,
        RecommendationPolicy policy)
    {
        var scoring = CombatRecommendationScorer.Score(
            new CombatRecommendationScoringRequest(
                player,
                threats,
                generation.Candidates,
                policy));
        var manualPlan = ManualCombatPlanBuilder.Build(player, scoring);
        var explanation = manualPlan.Plan is null
            ? null
            : CombatRecommendationExplanationBuilder.Build(
                player,
                threats,
                manualPlan.Plan);
        return new CombatRecommendationStyleResult(
            policy,
            scoring,
            manualPlan,
            explanation);
    }

    private static CombatLoadoutOption[] BuildOptions(
        PlayerCombatSnapshot player,
        TargetPlaybookRecommendationPlan playbookPlan)
    {
        var currentSkillIds = Enum
            .GetValues<SkillCategory>()
            .SelectMany(category => player.EquippedSkills.Get(category))
            .ToHashSet();
        var counterOptions = playbookPlan.Options
            .GroupBy(option => option.CounterRule.Effect.SkillId)
            .Select(group => group
                .OrderByDescending(option => playbookPlan.Access.Evaluations
                    .Single(evaluation => ReferenceEquals(
                        evaluation.Rule,
                        option.CounterRule)).IsAccessible)
                .ThenByDescending(option => option.Strength)
                .ThenBy(option => option.StableKey, StringComparer.Ordinal)
                .First())
            .OrderBy(option => option.CounterRule.Effect.SkillId)
            .ToArray();
        var counterSkillIds = counterOptions
            .Select(option => option.CounterRule.Effect.SkillId)
            .ToHashSet();

        return
        [
            .. counterOptions.Select(option =>
                CombatLoadoutOption.FromCounterRule(
                    option.CounterRule,
                    currentSkillIds.Contains(
                        option.CounterRule.Effect.SkillId),
                    allowBreakthrough: true,
                    applicableThreatCodes: option.ApplicableThreatCodes(
                        playbookPlan.EligibleGoals))),
            .. currentSkillIds
                .Except(counterSkillIds)
                .Order()
                .Select(skillId =>
                    CombatLoadoutOption.RetainCurrentSkill(
                        skillId,
                        $"snapshot:player:equipped-skill:{skillId}"))
        ];
    }

    private static CombatRequirementContext CreateBaseRequirementContext(
        PlayerCombatSnapshot player)
    {
        var currentSkillIds = Enum
            .GetValues<SkillCategory>()
            .SelectMany(category => player.EquippedSkills.Get(category));
        return new CombatRequirementContext(
            equippedWeaponTypeIds: [],
            trickCounts: [],
            SnapshotValue<int>.Unavailable(
                "Current combat distance was not supplied."),
            resources: [],
            unlockedWeaponTypeIds: [],
            currentSkillIds);
    }
}
