using System.Collections.Immutable;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record RecommendationCaveat
{
    internal RecommendationCaveat(
        RecommendationCaveatKind kind,
        string code,
        string explanation,
        int? skillId,
        IEnumerable<string> evidenceReferences)
    {
        Kind = kind;
        Code = code;
        Explanation = explanation;
        SkillId = skillId;
        EvidenceReferences = [.. evidenceReferences];
    }

    public RecommendationCaveatKind Kind { get; }

    public string Code { get; }

    public string Explanation { get; }

    public int? SkillId { get; }

    public ImmutableArray<string> EvidenceReferences { get; }
}
