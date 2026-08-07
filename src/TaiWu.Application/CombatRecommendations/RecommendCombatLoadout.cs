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
        if (!Enum.IsDefined(requestedPolicy))
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedPolicy),
                requestedPolicy,
                "Unknown recommendation policy.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var threatAnalysis = TargetThreatAnalyzer.Analyze(
            snapshot,
            VerifiedTargetThreatRuleSets.GoldenMagicSound);
        var threats = threatAnalysis.Threats
            .Select(analysis => analysis.Threat)
            .ToArray();
        cancellationToken.ThrowIfCancellationRequested();

        var generation = CombatLoadoutGenerator.Generate(
            new CombatLoadoutGenerationRequest(
                snapshot.Player,
                BuildOptions(snapshot.Player, threats),
                CreateBaseRequirementContext(snapshot.Player),
                snapshot.Player.GenericSlotAllocation));
        cancellationToken.ThrowIfCancellationRequested();

        List<CombatRecommendationStyleResult> styles = [];
        foreach (var policy in Enum.GetValues<RecommendationPolicy>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            styles.Add(
                BuildStyle(
                snapshot.Player,
                threats,
                generation,
                policy));
        }

        return new CombatLoadoutRecommendation(
            snapshot,
            threatAnalysis,
            generation,
            requestedPolicy,
            styles,
            targetObservation);
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
        IEnumerable<TargetThreat> threats)
    {
        var threatCodes = threats
            .Select(threat => threat.Code)
            .ToHashSet(StringComparer.Ordinal);
        var currentSkillIds = Enum
            .GetValues<SkillCategory>()
            .SelectMany(category => player.EquippedSkills.Get(category))
            .ToHashSet();
        var counterRules = VerifiedCombatCounterRuleSets
            .GoldenMagicSound
            .Rules
            .Where(rule => rule.ThreatCodes.Any(threatCodes.Contains))
            .ToArray();
        var counterSkillIds = counterRules
            .Select(rule => rule.Effect.SkillId)
            .ToHashSet();

        return
        [
            .. counterRules.Select(rule =>
                CombatLoadoutOption.FromCounterRule(
                    rule,
                    currentSkillIds.Contains(rule.Effect.SkillId),
                    allowBreakthrough: true)),
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
