namespace TaiWu.Domain.CombatRecommendations;

public sealed record RecommendationPolicyWeights
{
    private RecommendationPolicyWeights(
        RecommendationPolicy policy,
        int threatCoverage,
        int survival,
        int executionReliability,
        int currentLoadoutCompatibility,
        int damagePotential,
        int opportunityCost,
        int conditionalRisk)
    {
        Policy = policy;
        ThreatCoverage = threatCoverage;
        Survival = survival;
        ExecutionReliability = executionReliability;
        CurrentLoadoutCompatibility = currentLoadoutCompatibility;
        DamagePotential = damagePotential;
        OpportunityCost = opportunityCost;
        ConditionalRisk = conditionalRisk;
    }

    public RecommendationPolicy Policy { get; }

    public int ThreatCoverage { get; }

    public int Survival { get; }

    public int ExecutionReliability { get; }

    public int CurrentLoadoutCompatibility { get; }

    public int DamagePotential { get; }

    public int OpportunityCost { get; }

    public int ConditionalRisk { get; }

    public int Get(RecommendationScoreComponentKind component) =>
        component switch
        {
            RecommendationScoreComponentKind.ThreatCoverage =>
                ThreatCoverage,
            RecommendationScoreComponentKind.Survival => Survival,
            RecommendationScoreComponentKind.ExecutionReliability =>
                ExecutionReliability,
            RecommendationScoreComponentKind.CurrentLoadoutCompatibility =>
                CurrentLoadoutCompatibility,
            RecommendationScoreComponentKind.DamagePotential =>
                DamagePotential,
            RecommendationScoreComponentKind.OpportunityCost =>
                OpportunityCost,
            RecommendationScoreComponentKind.ConditionalRisk =>
                ConditionalRisk,
            _ => throw new ArgumentOutOfRangeException(
                nameof(component),
                component,
                "Unknown score component.")
        };

    public static RecommendationPolicyWeights For(
        RecommendationPolicy policy) => policy switch
        {
            RecommendationPolicy.Safe => new(
                policy,
                threatCoverage: 30,
                survival: 30,
                executionReliability: 15,
                currentLoadoutCompatibility: 5,
                damagePotential: 5,
                opportunityCost: 5,
                conditionalRisk: 10),
            RecommendationPolicy.Balanced => new(
                policy,
                threatCoverage: 25,
                survival: 20,
                executionReliability: 15,
                currentLoadoutCompatibility: 10,
                damagePotential: 15,
                opportunityCost: 10,
                conditionalRisk: 5),
            RecommendationPolicy.Aggressive => new(
                policy,
                threatCoverage: 15,
                survival: 10,
                executionReliability: 10,
                currentLoadoutCompatibility: 10,
                damagePotential: 40,
                opportunityCost: 10,
                conditionalRisk: 5),
            _ => throw new ArgumentOutOfRangeException(
                nameof(policy),
                policy,
                "Unknown recommendation policy.")
        };
}
