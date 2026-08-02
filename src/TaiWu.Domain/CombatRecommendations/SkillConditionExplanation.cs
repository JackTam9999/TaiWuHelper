using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record SkillConditionExplanation
{
    internal SkillConditionExplanation(
        RecommendationConditionKind kind,
        CombatRequirementCriticality criticality,
        CombatRequirementStatus status,
        string evaluation,
        string evidenceReference)
    {
        Kind = kind;
        Criticality = criticality;
        Status = status;
        Evaluation = evaluation;
        EvidenceReference = evidenceReference;
    }

    public RecommendationConditionKind Kind { get; }

    public CombatRequirementCriticality Criticality { get; }

    public CombatRequirementStatus Status { get; }

    public string Evaluation { get; }

    public string EvidenceReference { get; }
}
