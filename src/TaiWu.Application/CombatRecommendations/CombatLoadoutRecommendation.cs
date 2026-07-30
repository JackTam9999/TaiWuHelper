using System.Collections.Immutable;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;

namespace TaiWu.Application.CombatRecommendations;

public sealed record CombatLoadoutRecommendation
{
    internal CombatLoadoutRecommendation(
        CombatSnapshot snapshot,
        TargetThreatAnalysis threatAnalysis,
        CombatLoadoutGenerationResult generation,
        CombatRecommendationScoringResult scoring,
        ManualCombatPlanResult manualPlan,
        CombatRecommendationExplanation? explanation)
    {
        Snapshot = snapshot;
        ThreatAnalysis = threatAnalysis;
        Generation = generation;
        Scoring = scoring;
        ManualPlan = manualPlan;
        Explanation = explanation;
    }

    public CombatSnapshot Snapshot { get; }

    public ImmutableArray<SnapshotWarning> SnapshotWarnings =>
        Snapshot.Warnings;

    public TargetThreatAnalysis ThreatAnalysis { get; }

    public CombatLoadoutGenerationResult Generation { get; }

    public CombatRecommendationScoringResult Scoring { get; }

    public ManualCombatPlanResult ManualPlan { get; }

    public CombatRecommendationExplanation? Explanation { get; }
}
