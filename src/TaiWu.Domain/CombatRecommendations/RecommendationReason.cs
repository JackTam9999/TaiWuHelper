using System.Collections.Immutable;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record RecommendationReason
{
    internal RecommendationReason(
        string code,
        string summary,
        IEnumerable<string> evidenceReferences,
        IEnumerable<string> threatCodes)
    {
        Code = code;
        Summary = summary;
        EvidenceReferences = [.. evidenceReferences];
        ThreatCodes = [.. threatCodes];
    }

    public string Code { get; }

    public string Summary { get; }

    public ImmutableArray<string> EvidenceReferences { get; }

    public ImmutableArray<string> ThreatCodes { get; }
}
