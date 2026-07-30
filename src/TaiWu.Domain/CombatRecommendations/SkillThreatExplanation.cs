using System.Collections.Immutable;
using TaiWu.Domain.CombatThreats;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record SkillThreatExplanation
{
    internal SkillThreatExplanation(TargetThreat threat)
    {
        Code = threat.Code;
        Kind = threat.Kind;
        Severity = threat.Severity;
        Title = threat.Title;
        ActivationTiming = threat.ActivationTiming;
        EvidenceReferences =
        [
            .. threat.Evidence.Select(evidence => evidence.Reference)
        ];
    }

    public string Code { get; }

    public TargetThreatKind Kind { get; }

    public TargetThreatSeverity Severity { get; }

    public string Title { get; }

    public TargetThreatActivationTiming ActivationTiming { get; }

    public ImmutableArray<string> EvidenceReferences { get; }
}
