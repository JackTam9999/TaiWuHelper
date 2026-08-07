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
        RecommendationPolicy requestedPolicy,
        IEnumerable<CombatRecommendationStyleResult> styles,
        TargetObservationProcessingResult? targetObservation = null)
    {
        Snapshot = snapshot;
        ThreatAnalysis = threatAnalysis;
        Generation = generation;
        RequestedPolicy = requestedPolicy;
        Styles = [.. styles];
        SelectedStyle = Styles.Single(style =>
            style.Policy == requestedPolicy);
        TargetObservation = targetObservation;
    }

    public CombatSnapshot Snapshot { get; }

    public ImmutableArray<SnapshotWarning> SnapshotWarnings =>
        Snapshot.Warnings;

    public TargetThreatAnalysis ThreatAnalysis { get; }

    public CombatLoadoutGenerationResult Generation { get; }

    public RecommendationPolicy RequestedPolicy { get; }

    public ImmutableArray<CombatRecommendationStyleResult> Styles { get; }

    public CombatRecommendationStyleResult SelectedStyle { get; }

    public TargetObservationProcessingResult? TargetObservation { get; }

    public CombatRecommendationScoringResult Scoring =>
        SelectedStyle.Scoring;

    public ManualCombatPlanResult ManualPlan =>
        SelectedStyle.ManualPlan;

    public CombatRecommendationExplanation? Explanation =>
        SelectedStyle.Explanation;
}
