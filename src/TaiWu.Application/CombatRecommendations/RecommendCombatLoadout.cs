using TaiWu.Application.CombatSnapshots;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;

namespace TaiWu.Application.CombatRecommendations;

public sealed class RecommendCombatLoadout(ICombatSnapshotReader reader)
{
    public async Task<CombatLoadoutRecommendation> ExecuteAsync(
        RecommendCombatLoadoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var snapshotRequest = new CombatSnapshotReadRequest(
            request.SaveFilePath,
            request.TargetCharacterId,
            request.CurrentLoadoutObservation);
        var snapshot = await reader.ReadAsync(
            snapshotRequest,
            cancellationToken).ConfigureAwait(false);
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

        var scoring = CombatRecommendationScorer.Score(
            new CombatRecommendationScoringRequest(
                snapshot.Player,
                threats,
                generation.Candidates,
                request.Policy));
        var manualPlan = ManualCombatPlanBuilder.Build(
            snapshot.Player,
            scoring);
        var explanation = manualPlan.Plan is null
            ? null
            : CombatRecommendationExplanationBuilder.Build(
                snapshot.Player,
                threats,
                manualPlan.Plan);

        return new CombatLoadoutRecommendation(
            snapshot,
            threatAnalysis,
            generation,
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
                    currentSkillIds.Contains(rule.Effect.SkillId))),
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
