using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;

namespace TaiWu.Domain.CombatRecommendations;

public static class CombatRecommendationScorer
{
    public static CombatRecommendationScoringResult Score(
        CombatRecommendationScoringRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var weights = RecommendationPolicyWeights.For(request.Policy);
        var damageByCandidate = request.DamageEvidence.ToDictionary(
            evidence => evidence.CandidateStableKey,
            StringComparer.Ordinal);
        var scored = request.Candidates
            .Select(candidate => ScoreCandidate(
                request,
                candidate,
                weights,
                damageByCandidate))
            .OrderByDescending(candidate => candidate.TotalScore)
            .ThenByDescending(candidate => candidate.Get(
                RecommendationScoreComponentKind.ThreatCoverage).Score)
            .ThenByDescending(
                candidate => candidate.Candidate.RetainedCurrentSkillCount)
            .ThenBy(
                candidate => candidate.Candidate.StableKey,
                StringComparer.Ordinal)
            .ToArray();

        return new CombatRecommendationScoringResult(weights, scored);
    }

    private static ScoredCombatLoadout ScoreCandidate(
        CombatRecommendationScoringRequest request,
        GeneratedCombatLoadout candidate,
        RecommendationPolicyWeights weights,
        IReadOnlyDictionary<string, CandidateDamageEvidence>
            damageByCandidate)
    {
        RecommendationScoreComponent[] components =
        [
            Available(
                RecommendationScoreComponentKind.ThreatCoverage,
                CoverageScore(request.TargetThreats, candidate),
                "Severity-weighted target threats covered by selected "
                + "counter options.",
                "domain:target-threats"),
            Available(
                RecommendationScoreComponentKind.Survival,
                SurvivalScore(request.TargetThreats, candidate),
                "Severity-weighted hard-counter and mitigation protection.",
                "domain:counter-strength"),
            Available(
                RecommendationScoreComponentKind.ExecutionReliability,
                ReliabilityScore(request.Player, candidate),
                "Penalizes manual direction changes and active-attack "
                + "execution steps.",
                "domain:candidate-validation"),
            Available(
                RecommendationScoreComponentKind
                    .CurrentLoadoutCompatibility,
                CompatibilityScore(request.Player, candidate),
                "Share of current equipped skills retained in the "
                + "candidate.",
                "snapshot:player:equipped-skills"),
            DamageComponent(candidate, damageByCandidate),
            Available(
                RecommendationScoreComponentKind.OpportunityCost,
                OpportunityCostScore(candidate),
                "Share of total feasible slot capacity left unused.",
                "domain:slot-budgets"),
            Available(
                RecommendationScoreComponentKind.ConditionalRisk,
                ConditionalRiskScore(candidate),
                "Penalizes unsatisfied or unknown conditional "
                + "requirements.",
                "domain:combat-requirements")
        ];

        var weightedScore = components
            .Where(component => component.IsAvailable)
            .Sum(component =>
                component.Score!.Value * weights.Get(component.Kind));
        var availableWeight = components
            .Where(component => component.IsAvailable)
            .Sum(component => weights.Get(component.Kind));
        var total = availableWeight == 0
            ? 0
            : decimal.Round(
                weightedScore / availableWeight,
                decimals: 4,
                MidpointRounding.AwayFromZero);
        var weightedComponents = components.Select(component =>
            new RecommendationScoreComponent(
                component.Kind,
                weights.Get(component.Kind),
                component.Score,
                component.Explanation,
                component.EvidenceReference));

        return new ScoredCombatLoadout(
            candidate,
            request.Policy,
            weightedComponents,
            total);
    }

    private static decimal CoverageScore(
        IEnumerable<TargetThreat> threats,
        GeneratedCombatLoadout candidate)
    {
        var values = threats.ToArray();
        var totalWeight = values.Sum(SeverityWeight);
        if (totalWeight == 0)
        {
            return 100;
        }

        var covered = candidate.ThreatCodes.ToHashSet(
            StringComparer.Ordinal);
        var coveredWeight = values
            .Where(threat => covered.Contains(threat.Code))
            .Sum(SeverityWeight);
        return Percent(coveredWeight, totalWeight);
    }

    private static decimal SurvivalScore(
        IEnumerable<TargetThreat> threats,
        GeneratedCombatLoadout candidate)
    {
        var values = threats.ToArray();
        var totalWeight = values.Sum(SeverityWeight);
        if (totalWeight == 0)
        {
            return 100;
        }

        var protectedWeight = values.Sum(threat =>
        {
            var bestProtection = candidate.SelectedOptions
                .Where(option => option.ThreatCodes.Contains(threat.Code))
                .Select(option => option.CounterStrength switch
                {
                    CombatCounterStrength.HardCounter => 100,
                    CombatCounterStrength.Mitigation => 60,
                    _ => 0
                })
                .DefaultIfEmpty(0)
                .Max();
            return SeverityWeight(threat) * bestProtection;
        });
        return Percent(protectedWeight, totalWeight * 100);
    }

    private static decimal ReliabilityScore(
        PlayerCombatSnapshot player,
        GeneratedCombatLoadout candidate)
    {
        var directionChanges = candidate.SelectedOptions.Count(option =>
            CombatSkillCandidateValidator.Validate(
                player,
                option.Candidate).RequiredDirectionChange.HasValue);
        var activeAttacks = candidate.SelectedOptions.Count(option =>
            option.ActivationTiming
            == CombatCounterActivationTiming.ActiveAttack);
        return Clamp(100 - (directionChanges * 15) - (activeAttacks * 5));
    }

    private static decimal CompatibilityScore(
        PlayerCombatSnapshot player,
        GeneratedCombatLoadout candidate)
    {
        var currentSkillCount = Enum
            .GetValues<SkillCategory>()
            .Sum(category => player.EquippedSkills.Get(category).Length);
        return currentSkillCount == 0
            ? 100
            : Percent(candidate.RetainedCurrentSkillCount, currentSkillCount);
    }

    private static RecommendationScoreComponent DamageComponent(
        GeneratedCombatLoadout candidate,
        IReadOnlyDictionary<string, CandidateDamageEvidence>
            damageByCandidate)
    {
        return damageByCandidate.TryGetValue(
            candidate.StableKey,
            out var evidence)
            ? Available(
                RecommendationScoreComponentKind.DamagePotential,
                evidence.Score,
                "Caller-supplied, evidence-backed damage potential.",
                evidence.EvidenceReference)
            : Unavailable(
                RecommendationScoreComponentKind.DamagePotential,
                "No verified damage evidence is available; this component "
                + "is excluded from the normalized total.",
                "unavailable:damage-evidence");
    }

    private static decimal OpportunityCostScore(
        GeneratedCombatLoadout candidate)
    {
        var budgets = candidate.FeasibleLoadout.SlotBudgets.Values
            .Where(budget => budget.Remaining.IsAvailable)
            .ToArray();
        var totalCapacity = budgets.Sum(budget => budget.Capacity);
        return totalCapacity == 0
            ? 100
            : Percent(
                budgets.Sum(budget => budget.Remaining.Value),
                totalCapacity);
    }

    private static decimal ConditionalRiskScore(
        GeneratedCombatLoadout candidate)
    {
        var proposal = candidate.FeasibleLoadout.Proposal;
        var evaluation = CombatRequirementEvaluator.Evaluate(
            proposal.Requirements,
            proposal.RequirementContext);
        return Clamp(100 - (evaluation.Warnings.Length * 25));
    }

    private static int SeverityWeight(TargetThreat threat) =>
        threat.Severity switch
        {
            TargetThreatSeverity.Informational => 1,
            TargetThreatSeverity.Moderate => 2,
            TargetThreatSeverity.High => 4,
            TargetThreatSeverity.Critical => 8,
            _ => throw new ArgumentOutOfRangeException(
                nameof(threat),
                threat.Severity,
                "Unknown threat severity.")
        };

    private static RecommendationScoreComponent Available(
        RecommendationScoreComponentKind kind,
        decimal score,
        string explanation,
        string evidenceReference) =>
        new(kind, weight: 0, Clamp(score), explanation, evidenceReference);

    private static RecommendationScoreComponent Unavailable(
        RecommendationScoreComponentKind kind,
        string explanation,
        string evidenceReference) =>
        new(kind, weight: 0, score: null, explanation, evidenceReference);

    private static decimal Percent(decimal value, decimal total) =>
        total == 0 ? 100 : Clamp(value * 100 / total);

    private static decimal Clamp(decimal score) =>
        Math.Clamp(score, 0, 100);
}
